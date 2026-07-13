using System;

namespace TurnBasedGame.Core
{
    public static class DamageCalculator
    {
        private const float DamageRatioNumerator = 100f;
        private const float DamageRatioOffset = 100f;

        public readonly struct DamageResult
        {
            public int Amount { get; }
            public bool IsCritical { get; }

            public DamageResult(int amount, bool isCritical)
            {
                Amount = amount;
                IsCritical = isCritical;
            }
        }

        public static DamageResult Calculate(StatBlock attacker, StatBlock defender, IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            float baseDamage = attacker.Atk * (DamageRatioNumerator / (DamageRatioOffset + defender.Def));

            bool isCritical = randomSource.NextDouble() * 100.0 < attacker.CritRate;
            float finalDamage = isCritical ? baseDamage * attacker.CritDmg : baseDamage;

            int amount = Math.Max(0, (int)Math.Round((double)finalDamage, MidpointRounding.AwayFromZero));
            return new DamageResult(amount, isCritical);
        }
    }
}
