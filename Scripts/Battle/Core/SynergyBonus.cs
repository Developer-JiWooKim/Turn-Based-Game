using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 파티 시너지 1종의 순수 효과.
    ///
    /// <b>정수 스탯(ATK/SPD/DEF)은 비율, 비율 스탯(치명타/치명피해/저항)은 %p 가산</b>이라는 혼합 모델이다.
    /// 무한 타워라 고정 증분은 스테이지가 오를수록 무의미해지므로 비율이 맞지만,
    /// 치명타·저항은 이미 0~1 비율값이라 거기에 다시 비율을 곱하면
    /// (치명타 15%에 +10% → 16.5%) 사실상 아무 일도 일어나지 않는다 — 그래서 둘을 나눴다.
    ///
    /// 최대 HP는 시너지 대상이 아니다 — 전투 중 조건이 깨지면 되돌려야 하는데 현재 HP가 따라오지 못한다.
    ///
    /// <see cref="RoguelikeEffect"/>를 재사용하지 않는 이유 — 그쪽도 2026-08-12부터 비율이지만
    /// <b>비율의 기준과 되돌리는 방식이 다르다</b>. 선택지는 "기준값 + 스테이지 자동 성장"을 기준으로
    /// 런 데이터에 영구 누적하는 반면, 시너지는 얹기 직전의 <see cref="Stats"/>를 기준으로
    /// 전투 중에만 붙였다 떼며 제거가 뺄셈이 아니라 스냅샷 복원이다.
    /// 몬스터 디버프·영입 플래그까지 함께 들고 있는 것도 그대로다.
    /// </summary>
    public readonly struct SynergyBonus
    {
        /// <summary>ATK 증가 비율(0.15 = +15%).</summary>
        public readonly float AtkRate;

        /// <summary>SPD 증가 비율.</summary>
        public readonly float SpdRate;

        /// <summary>DEF 증가 비율.</summary>
        public readonly float DefRate;

        /// <summary>치명타 확률 가산치(0.1 = +10%p). 비율이 아니라 더하는 값이다.</summary>
        public readonly float CritRate;

        /// <summary>치명타 피해 배율 가산치(0.2 = +20%p).</summary>
        public readonly float CritDmg;

        /// <summary>디버프 저항 가산치(0.1 = +10%p).</summary>
        public readonly float Res;

        public SynergyBonus(float atkRate, float spdRate, float defRate,
                            float critRate, float critDmg, float res)
        {
            AtkRate = atkRate;
            SpdRate = spdRate;
            DefRate = defRate;
            CritRate = critRate;
            CritDmg = critDmg;
            Res = res;
        }

        /// <summary>수치가 하나도 채워지지 않았는지(전부 0이면 이 캐릭터는 시너지가 없다).</summary>
        public bool IsEmpty => AtkRate == 0f && SpdRate == 0f && DefRate == 0f
                            && CritRate == 0f && CritDmg == 0f && Res == 0f;

        /// <summary>
        /// 전투용 <see cref="Stats"/>에 시너지를 얹는다.
        /// 되돌리기는 뺄셈이 아니라 적용 전 스냅샷 복원이므로
        /// (<c>PartySynergyTracker</c> 참고) 여기서는 적용만 신경 쓰면 된다.
        /// </summary>
        public void ApplyTo(Stats stats)
        {
            // 비율은 시너지를 얹기 직전 값(= 성장이 모두 반영된 값) 기준이다.
            stats.Atk += Scale(stats.Atk, AtkRate);
            stats.Spd += Scale(stats.Spd, SpdRate);
            stats.Def += Scale(stats.Def, DefRate);

            stats.CritRate = Math.Min(1f, stats.CritRate + CritRate); // 확률은 100%로 클램프
            stats.CritDmg += CritDmg;
            stats.Res = Math.Min(1f, stats.Res + Res);                // 저항도 100%로 클램프
        }

        /// <summary>
        /// 비율 증가분(정수). <see cref="DamageCalculator"/>와 같은 반올림 규칙을 쓴다.
        ///
        /// "0이 되면 최소 1" 같은 바닥값은 두지 않는다 — 과거 <see cref="StageScaling"/>에 그런 바닥이 있어
        /// 저스탯 캐릭터가 비정상적으로 급성장한 전례가 있다. 스탯 규모상 비율이 0이 아니면
        /// 증가분도 0이 되지 않으므로 바닥값이 필요하지도 않다.
        /// </summary>
        private static int Scale(int value, float rate) =>
            rate == 0f ? 0 : (int)Math.Round(value * rate, MidpointRounding.AwayFromZero);
    }
}
