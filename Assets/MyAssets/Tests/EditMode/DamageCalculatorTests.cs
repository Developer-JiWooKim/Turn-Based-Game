using NUnit.Framework;

namespace TurnBasedGame.Core.Tests
{
    public class DamageCalculatorTests
    {
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly double _value;

            public FixedRandomSource(double value)
            {
                _value = value;
            }

            public double NextDouble() => _value;
        }

        [Test]
        public void Calculate_AppliesRatioFormula_WhenNotCritical()
        {
            var attacker = new StatBlock(maxHp: 100, atk: 100, spd: 10, def: 0, critRate: 0, critDmg: 1.5f, res: 0);
            var defender = new StatBlock(maxHp: 100, atk: 0, spd: 10, def: 100, critRate: 0, critDmg: 1f, res: 0);
            var randomSource = new FixedRandomSource(0.99);

            DamageCalculator.DamageResult result = DamageCalculator.Calculate(attacker, defender, randomSource);

            // 100 * (100 / (100 + 100)) = 50
            Assert.AreEqual(50, result.Amount);
            Assert.IsFalse(result.IsCritical);
        }

        [Test]
        public void Calculate_ZeroDefense_DealsFullAttackAsDamage()
        {
            var attacker = new StatBlock(maxHp: 100, atk: 42, spd: 10, def: 0, critRate: 0, critDmg: 1f, res: 0);
            var defender = new StatBlock(maxHp: 100, atk: 0, spd: 10, def: 0, critRate: 0, critDmg: 1f, res: 0);
            var randomSource = new FixedRandomSource(0.99);

            DamageCalculator.DamageResult result = DamageCalculator.Calculate(attacker, defender, randomSource);

            Assert.AreEqual(42, result.Amount);
        }

        [Test]
        public void Calculate_RollBelowCritRate_IsCriticalAndMultiplied()
        {
            var attacker = new StatBlock(maxHp: 100, atk: 100, spd: 10, def: 0, critRate: 50, critDmg: 2f, res: 0);
            var defender = new StatBlock(maxHp: 100, atk: 0, spd: 10, def: 0, critRate: 0, critDmg: 1f, res: 0);
            var randomSource = new FixedRandomSource(0.0);

            DamageCalculator.DamageResult result = DamageCalculator.Calculate(attacker, defender, randomSource);

            Assert.IsTrue(result.IsCritical);
            Assert.AreEqual(200, result.Amount);
        }

        [Test]
        public void Calculate_RollAtOrAboveCritRate_IsNotCritical()
        {
            var attacker = new StatBlock(maxHp: 100, atk: 100, spd: 10, def: 0, critRate: 50, critDmg: 2f, res: 0);
            var defender = new StatBlock(maxHp: 100, atk: 0, spd: 10, def: 0, critRate: 0, critDmg: 1f, res: 0);
            var randomSource = new FixedRandomSource(0.5);

            DamageCalculator.DamageResult result = DamageCalculator.Calculate(attacker, defender, randomSource);

            Assert.IsFalse(result.IsCritical);
            Assert.AreEqual(100, result.Amount);
        }

        [Test]
        public void StatBlock_ClampsCritRate_ToZeroAndHundred()
        {
            var belowRange = new StatBlock(maxHp: 1, atk: 1, spd: 1, def: 1, critRate: -10, critDmg: 1f, res: 0);
            var aboveRange = new StatBlock(maxHp: 1, atk: 1, spd: 1, def: 1, critRate: 150, critDmg: 1f, res: 0);

            Assert.AreEqual(0, belowRange.CritRate);
            Assert.AreEqual(100, aboveRange.CritRate);
        }
    }
}
