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
    ///
    /// <para>
    /// <b>선택지 성장은 비율이다</b>(2026-08-12에 고정 증분에서 전환). 무한 타워라 "ATK +120" 같은
    /// 고정값은 스테이지가 오를수록 무의미해진다 — 자동 성장만으로도 후반 ATK가 기준값의 몇 배가 되므로,
    /// 같은 선택지가 초반엔 판을 뒤집고 후반엔 아무것도 바꾸지 못하는 상태가 된다.
    /// </para>
    ///
    /// <para>
    /// <b>정수 스탯(HP/ATK/SPD/DEF)은 비율, 비율 스탯(치명타/치명피해/저항)은 %p 가산</b>이라는
    /// 혼합 모델이며, 이유는 <see cref="SynergyBonus"/>와 같다 — 치명타·저항은 이미 0~1 비율값이라
    /// 거기에 다시 비율을 곱하면(15%에 +10% → 16.5%) 사실상 아무 일도 일어나지 않는다.
    /// 덤으로 이 셋은 스테이지 자동 성장 대상이 아니라 애초에 후반 희석 문제가 없다.
    /// </para>
    ///
    /// <para>
    /// ⚠️ <b>비율의 기준은 <see cref="ApplyTo"/>에 넘기는 <c>scaleBase</c>이지 현재 스탯이 아니다.</b>
    /// 호출자(<c>RunMember</c>)가 "기준 스탯 + 스테이지 자동 성장"(= 현재 스탯 − 선택지 몫)을 넘기므로,
    /// 선택지로 스탯을 몰아줘도 그 몫이 다시 비율의 기준이 되어 복리로 부풀지 않는다.
    /// 스테이지 자동 성장은 <see cref="StageScaling"/>이 <c>BaseStats</c>를 기준으로 삼는 것과 같은 원칙이다.
    /// </para>
    ///
    /// <para>
    /// 스테이지 자동 성장(<see cref="StageScaling.CreatePlayerGrowth"/>)은 <b>여전히 flat</b>이라 별도 필드를 쓴다.
    /// 그쪽은 "누적 총량의 차분"을 돌려줘 반올림 오차가 쌓이지 않게 하는 것이 목적이라 비율로 바꿀 수 없다.
    /// 두 성장 경로가 같은 구조체를 공유하되 채우는 필드가 갈리므로, 생성자도 목적별로 둘로 나눠 뒀다.
    /// </para>
    /// </summary>
    public readonly struct RoguelikeEffect
    {
        // 파티 강화 — 정수 스탯(런 종료까지 유지). 0.45 = +45%
        /// <summary>MaxHp 증가 비율(증가분만큼 현재 HP도 회복).</summary>
        public readonly float HpRate;
        public readonly float AtkRate;
        public readonly float SpdRate;
        public readonly float DefRate;

        /// <summary>즉시 회복량(회복 선택지). <b>실제 MaxHp</b> 대비 비율이다 — 아래 <see cref="ApplyTo"/> 참고.</summary>
        public readonly float HealRate;

        // 파티 강화 — 비율 스탯은 %p 가산(0.1 = +10%p)
        public readonly float ResFlat;
        public readonly float CritRateFlat;
        public readonly float CritDmgFlat;

        // 스테이지 자동 성장 전용 고정 증분(선택지는 채우지 않는다)
        public readonly int HpFlat;
        public readonly int AtkFlat;
        public readonly int SpdFlat;
        public readonly int DefFlat;

        // 다음 스테이지 몬스터 디버프(일회성)
        /// <summary>몬스터 MaxHp 배율. 1 = 변화 없음(0.7 = 30% 감소).</summary>
        public readonly float EnemyHpMul;
        /// <summary>몬스터 ATK 배율. 1 = 변화 없음.</summary>
        public readonly float EnemyAtkMul;
        /// <summary>다음 스테이지 첫 턴에 몬스터 전체 행동 불가.</summary>
        public readonly bool EnemySkipFirstTurn;

        // 파티원 영입
        public readonly bool Recruit;

        /// <summary>
        /// 로그라이크 선택지 효과(비율 성장 + 몬스터 디버프 + 영입).
        /// 스테이지 자동 성장용 flat 필드는 채우지 않는다.
        /// </summary>
        public RoguelikeEffect(float hpRate, float atkRate, float spdRate, float defRate, float healRate,
                               float resFlat, float critRateFlat, float critDmgFlat,
                               float enemyHpMul, float enemyAtkMul, bool enemySkipFirstTurn,
                               bool recruit) : this()
        {
            HpRate = hpRate;
            AtkRate = atkRate;
            SpdRate = spdRate;
            DefRate = defRate;
            HealRate = healRate;
            ResFlat = resFlat;
            CritRateFlat = critRateFlat;
            CritDmgFlat = critDmgFlat;
            EnemyHpMul = enemyHpMul <= 0f ? 1f : enemyHpMul;
            EnemyAtkMul = enemyAtkMul <= 0f ? 1f : enemyAtkMul;
            EnemySkipFirstTurn = enemySkipFirstTurn;
            Recruit = recruit;
        }

        /// <summary>
        /// 스테이지 진급 1칸에 해당하는 자동 성장(<see cref="StageScaling.CreatePlayerGrowth"/> 전용).
        /// 비율이 아니라 이미 계산된 고정 증분을 받는다 — 누적 차분 방식이라야 반올림 오차가 쌓이지 않고,
        /// 영입자 소급(<c>RunData.ApplyCatchUp</c>)이 같은 step을 되짚기만 해도 값이 정확히 일치한다.
        /// </summary>
        public RoguelikeEffect(int hpFlat, int atkFlat, int spdFlat, int defFlat) : this()
        {
            HpFlat = hpFlat;
            AtkFlat = atkFlat;
            SpdFlat = spdFlat;
            DefFlat = defFlat;
            EnemyHpMul = 1f;
            EnemyAtkMul = 1f;
        }

        /// <summary>다음 스테이지에 예약해야 할 몬스터 디버프가 들어 있는지.</summary>
        public bool HasEnemyDebuff =>
            EnemyHpMul != 1f || EnemyAtkMul != 1f || EnemySkipFirstTurn;

        /// <summary>
        /// 파티 강화치를 스탯에 누적하고, 그로 인해 즉시 회복해야 할 HP량(최대체력 증가분 + 회복량)을 반환한다.
        /// 현재 HP는 호출자(Unit/RunMember)가 자신의 규칙대로 채운다.
        /// </summary>
        /// <param name="stats">효과를 누적할 대상 스탯.</param>
        /// <param name="scaleBase">
        /// 비율 성장의 기준이 되는 스탯(= 기준값 + 스테이지 자동 성장). 선택지로 얻은 몫을 뺀 값이라
        /// 같은 선택지를 반복해 골라도 증가폭이 복리로 부풀지 않는다.
        /// </param>
        public int ApplyTo(Stats stats, Stats scaleBase)
        {
            int hpGain = Scale(scaleBase.MaxHp, HpRate) + HpFlat;

            stats.MaxHp += hpGain;
            stats.Atk += Scale(scaleBase.Atk, AtkRate) + AtkFlat;
            stats.Spd += Scale(scaleBase.Spd, SpdRate) + SpdFlat;
            stats.Def += Scale(scaleBase.Def, DefRate) + DefFlat;
            stats.Res = Math.Min(1f, stats.Res + ResFlat);               // 저항은 100%로 클램프
            stats.CritRate = Math.Min(1f, stats.CritRate + CritRateFlat); // 확률도 100%로 클램프
            stats.CritDmg += CritDmgFlat;

            // 회복만 scaleBase가 아니라 실제 MaxHp(방금 늘어난 값 포함) 기준이다 —
            // "빈 체력을 채우는" 효과라 플레이어가 체력바에서 보는 최대치와 어긋나면 안 된다.
            return hpGain + Scale(stats.MaxHp, HealRate);
        }

        /// <summary>
        /// 비율 증가분(정수). 반올림 규칙과 "최소 1 바닥값을 두지 않는" 이유는 <see cref="SynergyBonus"/>와 같다.
        /// </summary>
        private static int Scale(int value, float rate) =>
            rate == 0f ? 0 : (int)Math.Round(value * rate, MidpointRounding.AwayFromZero);
    }
}
