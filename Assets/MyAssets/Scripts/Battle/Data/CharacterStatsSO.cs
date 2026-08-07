using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 플레이어 캐릭터 6종의 스탯 데이터
    ///
    /// 파티 시너지는 "같은 캐릭터가 임계 인원 이상 모이면 그 캐릭터들에게만" 적용되며,
    /// 전투 중 대상이 죽어 조건이 깨지면 즉시 사라져야 하므로 런 데이터에 누적하지 않고
    /// <see cref="Progression.Run.PartySynergyTracker"/>가 전투용 Unit에만 얹었다 뺐다 한다. 되돌릴 수 없는 최대 HP는 시너지 대상에서 제외한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Character Stats", fileName = "CharacterStats")]
    public sealed class CharacterStatsSO : UnitStatsSO
    {
        [Header("파티 시너지 (같은 캐릭터가 임계 인원 이상 모이면 그 캐릭터들에게만 적용)")]
        [Tooltip("시너지가 발동하는 최소 인원.")]
        [SerializeField] private int _synergyThreshold = 2;

        // ⚠️ 스탯마다 "같은 %가 같은 가치"가 아니다 — 조정 전에 이 차이를 먼저 볼 것.
        //  ATK  : 피해량에 선형으로 들어간다. +20%면 그대로 피해 +20%
        //  DEF  : 감쇠 공식 K/(K+DEF)이라 수익이 급격히 체감한다. DEF 150→300(+100%)이 받는 피해 −11.5%뿐
        //  SPD  : 모든 유닛이 턴당 1회 행동하므로 턴 "순서"만 바꾼다. 전투력 환산 가치가 가장 낮다
        [Tooltip("ATK 증가 비율(0.15 = +15%). 무한 타워라 고정값은 후반에 무의미해지므로 비율로 준다. 피해량에 그대로 반영된다.")]
        [Range(0f, 2f)][SerializeField] private float _synergyAtkRate;
        [Tooltip("SPD 증가 비율(0.15 = +15%). 턴 순서만 바꾸므로 같은 %라도 ATK보다 가치가 낮다.")]
        [Range(0f, 2f)][SerializeField] private float _synergySpdRate;
        [Tooltip("DEF 증가 비율(0.15 = +15%). 감쇠 공식 때문에 수익이 체감하므로 ATK와 같은 체감을 주려면 훨씬 큰 값이 필요하다.")]
        [Range(0f, 2f)][SerializeField] private float _synergyDefRate;

        [Tooltip("치명타 확률 가산치(0.1 = +10%p). 이미 0~1 비율이라 비율 증가가 아니라 더하는 값이다.")]
        [Range(0f, 1f)][SerializeField] private float _synergyCritRate;
        [Tooltip("치명타 피해 가산치(0.2 = +20%p).")]
        [SerializeField] private float _synergyCritDmg;
        [Tooltip("디버프 저항 가산치(0.1 = +10%p).")]
        [Range(0f, 1f)][SerializeField] private float _synergyRes;

        public int SynergyThreshold => _synergyThreshold;

        /// <summary>시너지 수치가 하나라도 설정돼 있는지(전부 0이면 이 캐릭터는 시너지가 없다)</summary>
        public bool HasSynergy => !CreateSynergy().IsEmpty;

        /// <summary>
        /// 시너지 효과. 정수 스탯은 비율, 비율 스탯은 %p 가산이며 이유는 <see cref="SynergyBonus"/> 참고.
        /// 최대 HP는 시너지 대상이 아니라 회복 부작용이 없다.
        ///
        /// ⚠️ 시너지 필드를 늘릴 때 함께 고쳐야 하는 곳은 <c>SynergyPanelView.DescribeEffect</c>(HUD 효과 표기)다 —
        /// 다른 어셈블리(Game.View)라 컴파일러가 잡아주지 않아, 빠뜨리면 효과는 적용되는데 화면에만 안 나온다.
        /// (발동 판정 <see cref="HasSynergy"/>는 <see cref="SynergyBonus.IsEmpty"/>에 위임하므로 자동으로 따라온다.)
        /// </summary>
        public SynergyBonus CreateSynergy() => new(
            atkRate: _synergyAtkRate,
            spdRate: _synergySpdRate,
            defRate: _synergyDefRate,
            critRate: _synergyCritRate,
            critDmg: _synergyCritDmg,
            res: _synergyRes
            );
    }
}
