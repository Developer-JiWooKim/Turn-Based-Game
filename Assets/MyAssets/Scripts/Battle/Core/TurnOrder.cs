using System;
using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 매 턴 시작 시 살아있는 전체 유닛을 SPD 내림차순으로 정렬
    /// (SpdDown 상태이상이 반영된 유효 SPD 기준, 동점은 Id 오름차순으로 안정화)
    ///
    /// 매 턴 호출되므로 LINQ 체인 대신 재사용 버퍼에 담아 in-place 정렬한다.
    /// </summary>
    public static class TurnOrder
    {
        private static readonly List<Unit> Buffer = new();

        private static readonly Comparison<Unit> BySpeedThenId = (a, b) =>
        {
            int bySpeed = b.EffectiveSpd.CompareTo(a.EffectiveSpd); // 내림차순
            return bySpeed != 0 ? bySpeed : a.Id.CompareTo(b.Id);
        };

        /// <summary>
        /// 이번 턴의 행동 순서를 만든다.
        /// 반환된 리스트는 다음 호출 때 재사용되므로 턴을 넘겨 보관하지 말 것
        /// (<see cref="BattleSimulation"/>은 한 턴 안에서만 순회)
        /// </summary>
        public static IReadOnlyList<Unit> Build(IReadOnlyList<Unit> aliveUnits)
        {
            Buffer.Clear();
            for (int i = 0; i < aliveUnits.Count; i++)
            {
                if (aliveUnits[i].IsAlive)
                    Buffer.Add(aliveUnits[i]);
            }

            Buffer.Sort(BySpeedThenId);
            return Buffer;
        }
    }
}
