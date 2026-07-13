using NUnit.Framework;

namespace TurnBasedGame.Core.Tests
{
    public class BattleUnitTests
    {
        private static BattleUnit MakeUnit(int maxHp)
        {
            var stats = new StatBlock(maxHp: maxHp, atk: 1, spd: 1, def: 0, critRate: 0, critDmg: 1f, res: 0);
            return new BattleUnit(1, "Unit", Team.Player, stats);
        }

        [Test]
        public void ApplyDamage_ReducesCurrentHp()
        {
            BattleUnit unit = MakeUnit(50);

            unit.ApplyDamage(20);

            Assert.AreEqual(30, unit.CurrentHp);
        }

        [Test]
        public void ApplyDamage_ClampsAtZero_AndNeverGoesNegative()
        {
            BattleUnit unit = MakeUnit(10);

            unit.ApplyDamage(999);

            Assert.AreEqual(0, unit.CurrentHp);
        }

        [Test]
        public void IsAlive_BecomesFalse_WhenHpReachesZero()
        {
            BattleUnit unit = MakeUnit(10);

            Assert.IsTrue(unit.IsAlive);

            unit.ApplyDamage(10);

            Assert.IsFalse(unit.IsAlive);
        }
    }
}
