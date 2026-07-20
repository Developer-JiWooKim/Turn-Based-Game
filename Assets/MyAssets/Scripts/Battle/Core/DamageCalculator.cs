using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>한 번의 타격 결과(순수 값).</summary>
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
    /// 비율 감소 방식 데미지 계산(순수 함수). 밸런싱 상수는 상단에 모아두고,
    /// 확정되면 값만 조정한다(추후 SO/설정으로 이전 가능).
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>DEF 감쇠 상수. 최종 데미지 = ATK × (K / (K + DEF)).</summary>
        private const float DefenseConstant = 100f;

        /// <summary>모든 유효 타격의 최소 데미지(0 방지).</summary>
        private const int MinimumDamage = 1;

        /// <summary>
        /// 공격자 → 대상 데미지 1건 계산. powerMultiplier는 스킬 배율(일반 공격은 1.0).
        /// 치명타 판정에 rng를 사용한다.
        /// </summary>
        public static DamageResult Calculate(Unit attacker, Unit target, float powerMultiplier, IRandom rng)
        {
            float mitigation = DefenseConstant / (DefenseConstant + target.Stats.Def);
            float raw = attacker.Stats.Atk * mitigation * powerMultiplier;

            bool isCrit = rng.Value01() < attacker.Stats.CritRate;
            if (isCrit)
                raw *= attacker.Stats.CritDmg;

            int amount = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            if (amount < MinimumDamage) amount = MinimumDamage;

            return new DamageResult(amount, isCrit);
        }
    }
}
