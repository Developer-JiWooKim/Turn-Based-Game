using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 로그라이크 선택지 1종의 순수 효과. SO(View 데이터)에서 이 값으로 변환된다.
    /// 적용 대상이 셋으로 나뉘므로 카테고리별 구현체로 쪼개지 않고, 어느 쪽에 값이 채워졌는지로 구분한다.
    ///
    ///  1. 파티 강화/회복 → <see cref="ApplyTo"/>로 유닛 Stats에 직접 누적(런 지속)
    ///  2. 다음 스테이지 몬스터 디버프 → <see cref="RunModifiers"/>에 예약되어 스폰 시 1회 소모
    ///  3. 파티원 영입 → <see cref="Recruit"/> 플래그를 보고 런 데이터가 파티에 추가
    /// </summary>
    public readonly struct RoguelikeEffect
    {
        // 파티 강화(런 종료까지 유지)
        public readonly int HpFlat;      // MaxHp 증가(증가분만큼 현재 HP도 회복)
        public readonly int AtkFlat;
        public readonly int SpdFlat;
        public readonly int DefFlat;
        public readonly int HealFlat;    // 즉시 회복량(회복 선택지)
        public readonly float ResFlat;
        public readonly float CritRateFlat;
        public readonly float CritDmgFlat;

        // 다음 스테이지 몬스터 디버프(일회성) 
        /// <summary>몬스터 MaxHp 배율. 1 = 변화 없음(0.7 = 30% 감소).</summary>
        public readonly float EnemyHpMul;
        /// <summary>몬스터 ATK 배율. 1 = 변화 없음.</summary>
        public readonly float EnemyAtkMul;
        /// <summary>다음 스테이지 첫 턴에 몬스터 전체 행동 불가.</summary>
        public readonly bool EnemySkipFirstTurn;

        // 파티원 영입 
        public readonly bool Recruit;

        public RoguelikeEffect(int hpFlat, int atkFlat, int spdFlat, int defFlat, int healFlat,
                               float resFlat, float critRateFlat, float critDmgFlat,
                               float enemyHpMul, float enemyAtkMul, bool enemySkipFirstTurn,
                               bool recruit)
        {
            HpFlat = hpFlat;
            AtkFlat = atkFlat;
            SpdFlat = spdFlat;
            DefFlat = defFlat;
            HealFlat = healFlat;
            ResFlat = resFlat;
            CritRateFlat = critRateFlat;
            CritDmgFlat = critDmgFlat;
            EnemyHpMul = enemyHpMul <= 0f ? 1f : enemyHpMul;
            EnemyAtkMul = enemyAtkMul <= 0f ? 1f : enemyAtkMul;
            EnemySkipFirstTurn = enemySkipFirstTurn;
            Recruit = recruit;
        }

        /// <summary>다음 스테이지에 예약해야 할 몬스터 디버프가 들어 있는지.</summary>
        public bool HasEnemyDebuff =>
            EnemyHpMul != 1f || EnemyAtkMul != 1f || EnemySkipFirstTurn;

        /// <summary>
        /// 파티 강화치를 스탯에 누적하고, 그로 인해 즉시 회복해야 할 HP량(최대체력 증가분 + 회복량)을 반환한다.
        /// 현재 HP는 호출자(Unit/RunMember)가 자신의 규칙대로 채운다.
        /// </summary>
        public int ApplyTo(Stats stats)
        {
            stats.MaxHp += HpFlat;
            stats.Atk += AtkFlat;
            stats.Spd += SpdFlat;
            stats.Def += DefFlat;
            stats.Res = Math.Min(1f, stats.Res + ResFlat);           // 저항은 100%로 클램프
            stats.CritRate = Math.Min(1f, stats.CritRate + CritRateFlat); // 확률도 100%로 클램프
            stats.CritDmg += CritDmgFlat;

            return HpFlat + HealFlat;
        }
    }
}
