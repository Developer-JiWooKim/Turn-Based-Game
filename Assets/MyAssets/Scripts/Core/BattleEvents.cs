using System.Collections.Generic;

namespace TurnBasedGame.Core
{
    public enum BattleOutcome
    {
        None,
        PlayerVictory,
        PlayerDefeat
    }

    public readonly struct TurnStartedEventArgs
    {
        public int TurnNumber { get; }
        public IReadOnlyList<BattleUnit> ActingOrder { get; }

        public TurnStartedEventArgs(int turnNumber, IReadOnlyList<BattleUnit> actingOrder)
        {
            TurnNumber = turnNumber;
            ActingOrder = actingOrder;
        }
    }

    public readonly struct TargetRequestedEventArgs
    {
        public BattleUnit Actor { get; }
        public IReadOnlyList<BattleUnit> ValidTargets { get; }

        public TargetRequestedEventArgs(BattleUnit actor, IReadOnlyList<BattleUnit> validTargets)
        {
            Actor = actor;
            ValidTargets = validTargets;
        }
    }

    public readonly struct DamageDealtEventArgs
    {
        public BattleUnit Attacker { get; }
        public BattleUnit Target { get; }
        public int Amount { get; }
        public bool IsCritical { get; }
        public int TargetRemainingHp { get; }

        public DamageDealtEventArgs(BattleUnit attacker, BattleUnit target, int amount, bool isCritical, int targetRemainingHp)
        {
            Attacker = attacker;
            Target = target;
            Amount = amount;
            IsCritical = isCritical;
            TargetRemainingHp = targetRemainingHp;
        }
    }

    public readonly struct UnitDefeatedEventArgs
    {
        public BattleUnit Unit { get; }

        public UnitDefeatedEventArgs(BattleUnit unit)
        {
            Unit = unit;
        }
    }

    public readonly struct BattleEndedEventArgs
    {
        public BattleOutcome Outcome { get; }

        public BattleEndedEventArgs(BattleOutcome outcome)
        {
            Outcome = outcome;
        }
    }
}
