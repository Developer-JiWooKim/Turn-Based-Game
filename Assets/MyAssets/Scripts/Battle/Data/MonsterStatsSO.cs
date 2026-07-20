using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    public enum MonsterTier
    {
        Normal,
        Elite,
        Boss
    }

    /// <summary>
    /// 몬스터 스탯 데이터. 보스(Tier=Boss)만 스킬을 가지며, 스킬은 준비되면 무조건 우선 사용된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Monster Stats", fileName = "MonsterStats")]
    public sealed class MonsterStatsSO : UnitStatsSO
    {
        [Header("Monster")]
        [SerializeField] private MonsterTier _tier = MonsterTier.Normal;

        [Header("Skill (보스 전용)")]
        [Tooltip("체크 시 보스 스킬을 사용한다. Normal/Elite는 꺼둔다.")]
        [SerializeField] private bool _hasSkill = false;
        [Tooltip("스킬 재사용까지 대기 턴 수.")]
        [SerializeField] private int _skillCooldown = 2;
        [SerializeField] private TargetScope _skillScope = TargetScope.Single;
        [Tooltip("일반 공격 대비 데미지 배율.")]
        [SerializeField] private float _skillPowerMultiplier = 1.4f;

        public MonsterTier Tier => _tier;

        /// <summary>스킬 정의를 만든다. 스킬이 없으면 null.</summary>
        public SkillProfile CreateSkill() =>
            _hasSkill ? new SkillProfile(_skillCooldown, _skillScope, _skillPowerMultiplier) : null;
    }
}
