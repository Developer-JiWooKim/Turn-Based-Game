using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 플레이어 캐릭터 6종의 스탯 데이터. 캐릭터는 공격만 하므로 스킬 필드가 없다.
    ///
    /// 파티 시너지는 "같은 캐릭터가 임계 인원 이상 모이면 그 캐릭터들에게만" 적용되며,
    /// 전투 중 대상이 죽어 조건이 깨지면 즉시 사라져야 하므로 런 데이터에 누적하지 않고
    /// <see cref="Run.PartySynergyTracker"/>가 전투용 Unit에만 얹었다 뺐다 한다. 되돌릴 수 없는 최대 HP는 시너지 대상에서 제외한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Character Stats", fileName = "CharacterStats")]
    public sealed class CharacterStatsSO : UnitStatsSO
    {
        [Header("파티 시너지 (같은 캐릭터가 임계 인원 이상 모이면 그 캐릭터들에게만 적용)")]
        [Tooltip("시너지가 발동하는 최소 인원.")]
        [SerializeField] private int _synergyThreshold = 2;
        [SerializeField] private int _synergyAtk;
        [SerializeField] private int _synergySpd;
        [SerializeField] private int _synergyDef;
        [Range(0f, 1f)][SerializeField] private float _synergyCritRate;
        [SerializeField] private float _synergyCritDmg;
        [Range(0f, 1f)][SerializeField] private float _synergyRes;

        public int SynergyThreshold => _synergyThreshold;

        /// <summary>시너지 수치가 하나라도 설정돼 있는지(전부 0이면 이 캐릭터는 시너지가 없다).</summary>
        public bool HasSynergy => _synergyAtk != 0 || _synergySpd != 0 || _synergyDef != 0
                               || _synergyCritRate != 0f || _synergyCritDmg != 0f || _synergyRes != 0f;

        /// <summary>
        /// 시너지 효과. 스탯 증분 묶음이라는 점이 같아 <see cref="RoguelikeEffect"/>를 그대로 쓴다
        /// (<see cref="StageScaling.CreatePlayerGrowth"/>도 같은 방식). HP를 0으로 두므로 회복 부작용이 없다.
        /// </summary>
        public RoguelikeEffect CreateSynergy() =>
            new RoguelikeEffect(0, _synergyAtk, _synergySpd, _synergyDef, 0,
                                _synergyRes, _synergyCritRate, _synergyCritDmg,
                                1f, 1f, false, false);
    }
}
