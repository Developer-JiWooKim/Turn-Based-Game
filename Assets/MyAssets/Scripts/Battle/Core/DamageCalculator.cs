using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>한 번의 타격 결과(순수 값)</summary>
    public readonly struct DamageResult
    {
        public readonly int Amount;
        public readonly bool IsCritical;

        public DamageResult(int amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// 비율 감소 방식 데미지 계산기
    /// 밸런싱 상수는 상단에 모아두고, 확정되면 값만 조정한다(추후 SO/설정으로 이전 가능).
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// DEF 감쇠 상수. 최종 데미지 = ATK × (K / (K + DEF)).
        ///
        /// ⚠️ <b>스탯 스케일과 함께 움직여야 하는 값이다.</b> K는 "피해가 절반으로 줄어드는 DEF 값"이라
        /// 스탯만 10배 하고 이 값을 두면 방어력이 10배 세져 모두가 탱커가 된다
        /// (DEF 20에서 통과율 83% → DEF 200에서 33%). 2026-08 전체 스탯 10배 리스케일에 맞춰 100 → 1000.
        /// </summary>
        private const float DefenseConstant = 1000f;

        /// <summary>
        /// 모든 유효 타격의 최소 데미지(0 방지).
        /// 스탯 스케일이 커질수록 이 바닥이 실제 계산에 개입하는 빈도가 줄어드는 것이 정상이다 —
        /// 10배 리스케일 후에도 1로 두는 이유이며, 같이 올리면 오히려 초저데미지 구간을 다시 왜곡한다.
        /// </summary>
        private const int MinimumDamage = 1;

        /// <summary>
        /// 공격자 → 대상 데미지 1건 계산. powerMultiplier는 스킬 배율(일반 공격은 1.0).
        /// 치명타 판정에 rng를 사용한다.
        /// </summary>
        public static DamageResult Calculate(Unit attacker, Unit target, float powerMultiplier, IRandom rng)
        {
            // 감소형 상태이상(AtkDown/DefDown)이 반영된 유효 스탯을 쓴다.
            float mitigation = DefenseConstant / (DefenseConstant + target.EffectiveDef);
            float raw = attacker.EffectiveAtk * mitigation * powerMultiplier;

            bool isCrit = rng.Value01() < attacker.Stats.CritRate;
            if (isCrit)
            {
                raw *= attacker.Stats.CritDmg;
            }

            int amount = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            if (amount < MinimumDamage)
            {
                amount = MinimumDamage;
            }
            return new DamageResult(amount, isCrit);
        }
    }
}
