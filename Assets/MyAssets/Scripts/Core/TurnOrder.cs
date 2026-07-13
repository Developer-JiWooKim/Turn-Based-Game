using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnBasedGame.Core
{
    public static class TurnOrder
    {
        public static IReadOnlyList<BattleUnit> SortForTurn(IEnumerable<BattleUnit> livingUnits)
        {
            if (livingUnits == null)
            {
                throw new ArgumentNullException(nameof(livingUnits));
            }

            return livingUnits
                .OrderByDescending(unit => unit.BaseStats.Spd)
                .ThenBy(unit => unit.Id)
                .ToList();
        }
    }
}
