using System.Collections.Generic;
using System.Linq;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 매 턴 시작 시 살아있는 전체 유닛을 SPD 내림차순으로 정렬해 행동 순서를 만든다(순수 로직).
    /// SPD가 같으면 Id 오름차순으로 안정적으로 결정한다.
    /// </summary>
    public static class TurnOrder
    {
        public static List<Unit> Build(IEnumerable<Unit> aliveUnits)
        {
            return aliveUnits
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.Stats.Spd)
                .ThenBy(u => u.Id)
                .ToList();
        }
    }
}
