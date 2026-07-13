using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnBasedGame.Core
{
    public enum BattlePhase
    {
        NotStarted,
        AwaitingTarget,
        BattleEnded
    }

    public sealed class BattleEngine
    {
        private readonly List<BattleUnit> _playerUnits;
        private readonly List<BattleUnit> _enemyUnits;
        private readonly IRandomSource _randomSource;

        private IReadOnlyList<BattleUnit> _turnOrder = Array.Empty<BattleUnit>();
        private int _turnIndex;

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
        public int TurnNumber { get; private set; }
        public BattleUnit CurrentActor { get; private set; }

        public event Action<TurnStartedEventArgs> TurnStarted;
        public event Action<TargetRequestedEventArgs> TargetRequested;
        public event Action<DamageDealtEventArgs> DamageDealt;
        public event Action<UnitDefeatedEventArgs> UnitDefeated;
        public event Action<BattleEndedEventArgs> BattleEnded;

        public BattleEngine(IReadOnlyList<BattleUnit> playerUnits, IReadOnlyList<BattleUnit> enemyUnits, IRandomSource randomSource)
        {
            if (playerUnits == null || playerUnits.Count == 0)
            {
                throw new ArgumentException("Player roster must contain at least one unit.", nameof(playerUnits));
            }

            if (enemyUnits == null || enemyUnits.Count == 0)
            {
                throw new ArgumentException("Enemy roster must contain at least one unit.", nameof(enemyUnits));
            }

            List<BattleUnit> combined = playerUnits.Concat(enemyUnits).ToList();
            if (combined.Select(unit => unit.Id).Distinct().Count() != combined.Count)
            {
                throw new ArgumentException("Unit ids must be unique across both rosters.");
            }

            _playerUnits = playerUnits.ToList();
            _enemyUnits = enemyUnits.ToList();
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public void Start()
        {
            if (Phase != BattlePhase.NotStarted)
            {
                throw new InvalidOperationException("Battle has already been started.");
            }

            TurnNumber = 0;
            StartNextTurn();
        }

        public void SubmitTarget(int targetUnitId)
        {
            if (Phase != BattlePhase.AwaitingTarget)
            {
                throw new InvalidOperationException($"Cannot submit a target while phase is {Phase}.");
            }

            BattleUnit target = FindLivingOpponent(CurrentActor, targetUnitId);
            if (target == null)
            {
                throw new ArgumentException("Target must be a living unit on the opposing team.", nameof(targetUnitId));
            }

            DamageCalculator.DamageResult result = DamageCalculator.Calculate(CurrentActor.BaseStats, target.BaseStats, _randomSource);
            target.ApplyDamage(result.Amount);

            DamageDealt?.Invoke(new DamageDealtEventArgs(CurrentActor, target, result.Amount, result.IsCritical, target.CurrentHp));

            if (!target.IsAlive)
            {
                UnitDefeated?.Invoke(new UnitDefeatedEventArgs(target));
            }

            if (TryEndBattle())
            {
                return;
            }

            AdvanceToNextActor();
        }

        private bool TryEndBattle()
        {
            bool playersAlive = _playerUnits.Any(unit => unit.IsAlive);
            bool enemiesAlive = _enemyUnits.Any(unit => unit.IsAlive);

            if (playersAlive && enemiesAlive)
            {
                return false;
            }

            Outcome = playersAlive ? BattleOutcome.PlayerVictory : BattleOutcome.PlayerDefeat;
            Phase = BattlePhase.BattleEnded;
            CurrentActor = null;
            BattleEnded?.Invoke(new BattleEndedEventArgs(Outcome));
            return true;
        }

        private void AdvanceToNextActor()
        {
            for (int i = _turnIndex + 1; i < _turnOrder.Count; i++)
            {
                if (_turnOrder[i].IsAlive)
                {
                    _turnIndex = i;
                    CurrentActor = _turnOrder[i];
                    RequestTarget();
                    return;
                }
            }

            StartNextTurn();
        }

        private void StartNextTurn()
        {
            IEnumerable<BattleUnit> livingUnits = _playerUnits.Concat(_enemyUnits).Where(unit => unit.IsAlive);
            _turnOrder = TurnOrder.SortForTurn(livingUnits);
            _turnIndex = 0;
            TurnNumber++;

            TurnStarted?.Invoke(new TurnStartedEventArgs(TurnNumber, _turnOrder));

            CurrentActor = _turnOrder[0];
            RequestTarget();
        }

        private void RequestTarget()
        {
            Phase = BattlePhase.AwaitingTarget;
            IReadOnlyList<BattleUnit> validTargets = GetOpposingRoster(CurrentActor.Team).Where(unit => unit.IsAlive).ToList();
            TargetRequested?.Invoke(new TargetRequestedEventArgs(CurrentActor, validTargets));
        }

        private BattleUnit FindLivingOpponent(BattleUnit actor, int targetId)
        {
            return GetOpposingRoster(actor.Team).FirstOrDefault(unit => unit.Id == targetId && unit.IsAlive);
        }

        private List<BattleUnit> GetOpposingRoster(Team team)
        {
            return team == Team.Player ? _enemyUnits : _playerUnits;
        }
    }
}
