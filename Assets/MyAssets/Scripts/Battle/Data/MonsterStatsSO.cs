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
    /// 몬스터 스탯 데이터. Normal은 스킬이 없고, Elite/Boss는 스킬을 가지며 준비되면 무조건 우선 사용된다.
    /// 스킬 내용은 <see cref="SkillSO"/> 에셋에 있고 여기서는 참조만 한다 —
    /// 등급 차이는 그 에셋의 범위로 낸다(Elite=단일 대상, Boss=라인 = 적 진영 전체).
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Monster Stats", fileName = "MonsterStats")]
    public sealed class MonsterStatsSO : UnitStatsSO
    {
        [Header("Monster")]
        [SerializeField] private MonsterTier _tier = MonsterTier.Normal;

        [Header("Skill (Elite/Boss)")]
        [Tooltip("사용할 스킬 에셋. 비워두면 일반 공격만 한다(Normal은 비워둘 것).\n" +
                 "관례상 Elite는 Scope=Single, Boss는 Scope=Line인 스킬을 연결한다.")]
        [SerializeField] private SkillSO _skill;

        public MonsterTier Tier => _tier;

        /// <summary>스킬 정의를 만든다. 스킬이 없으면 null.</summary>
        public SkillProfile CreateSkill() => _skill != null ? _skill.Create() : null;
    }
}
