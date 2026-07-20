using System;
using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 로그라이크 선택지의 순수 효과(스탯 증가/회복 수치). SO(View 데이터)에서 이 값으로 변환되어
    /// 살아있는 파티 유닛의 Stats에 직접 누적 적용된다. 런 지속 강화가 대부분이라
    /// 별도 modifier 레이어 없이 Stats를 직접 더하는 방식을 쓴다.
    /// </summary>
    public readonly struct RoguelikeReward
    {
        public readonly int HpFlat;      // MaxHp 증가(증가분만큼 현재 HP도 회복)
        public readonly int AtkFlat;
        public readonly int SpdFlat;
        public readonly int DefFlat;
        public readonly int HealFlat;    // 즉시 회복량(회복 선택지)
        public readonly float ResFlat;
        public readonly float CritRateFlat;
        public readonly float CritDmgFlat;

        public RoguelikeReward(int hpFlat, int atkFlat, int spdFlat, int defFlat, int healFlat,
                               float resFlat, float critRateFlat, float critDmgFlat)
        {
            HpFlat = hpFlat;
            AtkFlat = atkFlat;
            SpdFlat = spdFlat;
            DefFlat = defFlat;
            HealFlat = healFlat;
            ResFlat = resFlat;
            CritRateFlat = critRateFlat;
            CritDmgFlat = critDmgFlat;
        }

        /// <summary>살아있는 파티 유닛 전체에 효과를 적용한다.</summary>
        public void Apply(IEnumerable<Unit> party)
        {
            foreach (Unit unit in party)
            {
                if (!unit.IsAlive) continue;

                Stats s = unit.Stats;
                s.MaxHp += HpFlat;
                s.Atk += AtkFlat;
                s.Spd += SpdFlat;
                s.Def += DefFlat;
                s.Res += ResFlat;
                s.CritRate = Math.Min(1f, s.CritRate + CritRateFlat); // 확률은 100%로 클램프
                s.CritDmg += CritDmgFlat;

                if (HpFlat > 0) unit.ApplyHeal(HpFlat);   // 늘어난 최대치만큼 현재 HP도 채움
                if (HealFlat > 0) unit.ApplyHeal(HealFlat);
            }
        }
    }
}
