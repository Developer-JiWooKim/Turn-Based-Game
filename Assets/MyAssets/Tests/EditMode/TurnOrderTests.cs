using System.Collections.Generic;
using NUnit.Framework;

namespace TurnBasedGame.Core.Tests
{
    public class TurnOrderTests
    {
        private static BattleUnit MakeUnit(int id, int spd, Team team = Team.Player)
        {
            var stats = new StatBlock(maxHp: 10, atk: 1, spd: spd, def: 0, critRate: 0, critDmg: 1f, res: 0);
            return new BattleUnit(id, $"Unit{id}", team, stats);
        }

        [Test]
        public void SortForTurn_OrdersBySpeedDescending()
        {
            BattleUnit slow = MakeUnit(1, spd: 5);
            BattleUnit fast = MakeUnit(2, spd: 20);
            BattleUnit medium = MakeUnit(3, spd: 10);

            IReadOnlyList<BattleUnit> sorted = TurnOrder.SortForTurn(new[] { slow, fast, medium });

            Assert.AreEqual(new[] { fast, medium, slow }, sorted);
        }

        [Test]
        public void SortForTurn_BreaksSpeedTiesByAscendingId()
        {
            BattleUnit higherId = MakeUnit(5, spd: 10);
            BattleUnit lowerId = MakeUnit(2, spd: 10);

            IReadOnlyList<BattleUnit> sorted = TurnOrder.SortForTurn(new[] { higherId, lowerId });

            Assert.AreEqual(new[] { lowerId, higherId }, sorted);
        }

        [Test]
        public void SortForTurn_DoesNotMutateInputCollection()
        {
            var units = new List<BattleUnit> { MakeUnit(1, spd: 5), MakeUnit(2, spd: 20) };
            var originalOrder = new List<BattleUnit>(units);

            TurnOrder.SortForTurn(units);

            Assert.AreEqual(originalOrder, units);
        }
    }
}
