using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TurnBasedGame.Core.Tests
{
    public class BattleEngineTests
    {
        private sealed class NeverCritRandomSource : IRandomSource
        {
            public double NextDouble() => 0.99;
        }

        private static BattleUnit MakeUnit(int id, Team team, int spd = 10, int hp = 100, int atk = 100, int def = 0)
        {
            var stats = new StatBlock(maxHp: hp, atk: atk, spd: spd, def: def, critRate: 0, critDmg: 1f, res: 0);
            return new BattleUnit(id, $"{team}{id}", team, stats);
        }

        [Test]
        public void Start_RaisesTurnStartedThenTargetRequested_ForFastestUnit()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 5);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 20);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            var events = new List<string>();
            engine.TurnStarted += _ => events.Add("TurnStarted");
            engine.TargetRequested += args => events.Add($"TargetRequested:{args.Actor.Id}");

            engine.Start();

            Assert.AreEqual(new[] { "TurnStarted", "TargetRequested:2" }, events);
            Assert.AreEqual(BattlePhase.AwaitingTarget, engine.Phase);
            Assert.AreEqual(enemy, engine.CurrentActor);
        }

        [Test]
        public void SubmitTarget_WhenNotAwaitingTarget_Throws()
        {
            BattleUnit player = MakeUnit(1, Team.Player);
            BattleUnit enemy = MakeUnit(2, Team.Enemy);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            Assert.Throws<InvalidOperationException>(() => engine.SubmitTarget(2));
        }

        [Test]
        public void SubmitTarget_WithSameTeamId_Throws()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20);
            BattleUnit otherPlayer = MakeUnit(3, Team.Player, spd: 1);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1);
            var engine = new BattleEngine(new[] { player, otherPlayer }, new[] { enemy }, new NeverCritRandomSource());
            engine.Start();

            Assert.Throws<ArgumentException>(() => engine.SubmitTarget(3));
        }

        [Test]
        public void SubmitTarget_WithUnknownId_Throws()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());
            engine.Start();

            Assert.Throws<ArgumentException>(() => engine.SubmitTarget(999));
        }

        [Test]
        public void SubmitTarget_AppliesDamage_AndRaisesDamageDealt()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20, atk: 100, def: 0);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1, hp: 100, def: 100);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            DamageDealtEventArgs? dealt = null;
            engine.DamageDealt += args => dealt = args;

            engine.Start();
            engine.SubmitTarget(2);

            // 100 * 100/(100+100) = 50
            Assert.IsTrue(dealt.HasValue);
            Assert.AreEqual(50, dealt.Value.Amount);
            Assert.AreEqual(50, enemy.CurrentHp);
        }

        [Test]
        public void SubmitTarget_LethalDamage_RaisesUnitDefeated()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20, atk: 1000, def: 0);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1, hp: 10, def: 0);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            BattleUnit defeated = null;
            engine.UnitDefeated += args => defeated = args.Unit;

            engine.Start();
            engine.SubmitTarget(2);

            Assert.AreEqual(enemy, defeated);
            Assert.IsFalse(enemy.IsAlive);
        }

        [Test]
        public void SubmitTarget_SkipsUnitThatDiedEarlierThisTurn()
        {
            // Player (fastest) kills Enemy A (queued between Player and Enemy B this turn).
            // Enemy A must be skipped, never asked to act.
            BattleUnit player = MakeUnit(1, Team.Player, spd: 30, atk: 1000, def: 0);
            BattleUnit enemyA = MakeUnit(2, Team.Enemy, spd: 20, hp: 1, def: 0);
            BattleUnit enemyB = MakeUnit(3, Team.Enemy, spd: 10, hp: 100, def: 0);
            var engine = new BattleEngine(new[] { player }, new[] { enemyA, enemyB }, new NeverCritRandomSource());

            var requestedActors = new List<int>();
            engine.TargetRequested += args => requestedActors.Add(args.Actor.Id);

            engine.Start();          // requests for Player (id 1)
            engine.SubmitTarget(2);  // Player kills Enemy A, must skip straight to Enemy B (id 3)

            Assert.AreEqual(new[] { 1, 3 }, requestedActors);
        }

        [Test]
        public void SubmitTarget_WhenTurnOrderExhausted_StartsNextTurnAndResorts()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20, atk: 1, def: 1000);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 10, hp: 1000, atk: 1, def: 1000);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            var turnNumbers = new List<int>();
            engine.TurnStarted += args => turnNumbers.Add(args.TurnNumber);

            engine.Start();          // Turn 1 starts, Player acts first
            engine.SubmitTarget(2);  // Player's action resolves, Enemy acts next
            engine.SubmitTarget(1);  // Enemy's action resolves, turn order exhausted -> Turn 2 starts

            Assert.AreEqual(new[] { 1, 2 }, turnNumbers);
            Assert.AreEqual(2, engine.TurnNumber);
        }

        [Test]
        public void Battle_EnemyWipedOut_EndsWithPlayerVictory()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20, atk: 1000, def: 0);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1, hp: 1, def: 0);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            BattleOutcome? outcome = null;
            engine.BattleEnded += args => outcome = args.Outcome;

            engine.Start();
            engine.SubmitTarget(2);

            Assert.AreEqual(BattleOutcome.PlayerVictory, outcome);
            Assert.AreEqual(BattlePhase.BattleEnded, engine.Phase);
        }

        [Test]
        public void Battle_PlayersWipedOut_EndsWithPlayerDefeat()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 1, hp: 1, def: 0);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 20, atk: 1000, def: 0);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            BattleOutcome? outcome = null;
            engine.BattleEnded += args => outcome = args.Outcome;

            engine.Start();          // Enemy acts first (higher SPD)
            engine.SubmitTarget(1);  // Enemy kills the only player -> defeat

            Assert.AreEqual(BattleOutcome.PlayerDefeat, outcome);
        }

        [Test]
        public void SubmitTarget_AfterBattleEnded_Throws()
        {
            BattleUnit player = MakeUnit(1, Team.Player, spd: 20, atk: 1000, def: 0);
            BattleUnit enemy = MakeUnit(2, Team.Enemy, spd: 1, hp: 1, def: 0);
            var engine = new BattleEngine(new[] { player }, new[] { enemy }, new NeverCritRandomSource());

            engine.Start();
            engine.SubmitTarget(2);

            Assert.Throws<InvalidOperationException>(() => engine.SubmitTarget(2));
        }

        [Test]
        public void ManyVsMany_TurnOrderIsUnifiedAcrossTeams_IgnoringTeamBoundaries()
        {
            BattleUnit p1 = MakeUnit(1, Team.Player, spd: 30, hp: 1000, atk: 1, def: 1000);
            BattleUnit p2 = MakeUnit(2, Team.Player, spd: 10, hp: 1000, atk: 1, def: 1000);
            BattleUnit e1 = MakeUnit(3, Team.Enemy, spd: 25, hp: 1000, atk: 1, def: 1000);
            BattleUnit e2 = MakeUnit(4, Team.Enemy, spd: 5, hp: 1000, atk: 1, def: 1000);
            var engine = new BattleEngine(new[] { p1, p2 }, new[] { e1, e2 }, new NeverCritRandomSource());

            var order = new List<int>();
            engine.TargetRequested += args => order.Add(args.Actor.Id);

            engine.Start();          // order: [1]      (p1 requested)
            engine.SubmitTarget(3);  // p1 -> e1, order: [1, 3]      (e1 requested)
            engine.SubmitTarget(1);  // e1 -> p1, order: [1, 3, 2]   (p2 requested)
            engine.SubmitTarget(3);  // p2 -> e1, order: [1, 3, 2, 4] (e2 requested)
            // Stop here: submitting e2's action would exhaust turn 1 and start turn 2,
            // which fires another TargetRequested (p1 again) and would pollute this assertion.

            Assert.AreEqual(new[] { 1, 3, 2, 4 }, order);
        }
    }
}
