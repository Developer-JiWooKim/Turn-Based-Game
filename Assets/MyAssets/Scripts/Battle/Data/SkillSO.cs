using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 스킬 1종의 데이터. 유닛 종류에 묶이지 않은 별도 에셋이라 여러 유닛이 같은 스킬을 공유할 수 있다.
    /// <b>캐릭터 스킬은 2026-08-19에 범위에서 제외됐으므로</b>(`TODO.md`의 '구현하지 않기로 한 것')
    /// 이 타입을 참조하는 것은 <see cref="MonsterStatsSO"/>뿐이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Skill", fileName = "Skill")]
    public sealed class SkillSO : ScriptableObject
    {
        [Tooltip("UI/로그 표기용 이름. 전투 규칙에는 영향 없음.")]
        [SerializeField] private string _displayName = "스킬";

        [Header("기본")]
        [Tooltip("스킬 사용 후 다시 쓰기까지 대기할 턴 수. 0이면 매 턴 사용 가능.")]
        [SerializeField] private int _cooldown = 2;
        [Tooltip("Single = 단일 대상(엘리트 관례), Line = 적 진영 전체(보스 관례).")]
        [SerializeField] private TargetScope _scope = TargetScope.Single;
        [Tooltip("일반 공격 대비 데미지 배율.")]
        [SerializeField] private float _powerMultiplier = 1.4f;

        [Header("상태이상 (지속 턴 또는 부여 확률이 0이면 없음)")]
        [SerializeField] private StatusKind _statusKind = StatusKind.Stun;
        [Tooltip("지속 턴 수(대상의 행동 기회 기준).")]
        [SerializeField] private int _statusDuration;
        [Tooltip("Poison=최대 HP 대비 비율(0.05 = 5%), *Down=스탯 감소 비율(0.3 = 30% 감소). Stun은 사용 안 함.")]
        [SerializeField] private float _statusMagnitude;
        [Tooltip("저항(RES) 적용 전 기본 부여 확률. 최종 확률 = 이 값 × (1 − 대상 RES).")]
        [Range(0f, 1f)][SerializeField] private float _statusApplyChance;

        /// <summary>
        /// 화면 표시 이름. <see cref="UnitStatsSO.DisplayName"/>과 같은 규약으로,
        /// 에셋에 적힌 원문이 곧 문자열 표의 키다 — 표에 행이 없으면 원문이 그대로 나온다.
        /// </summary>
        public string DisplayName => Loc.Get(_displayName);

        /// <summary>Core가 쓰는 순수 스킬 정의를 만든다.</summary>
        public SkillProfile Create() =>
            new SkillProfile(_cooldown, _scope, _powerMultiplier, CreateStatus());

        private StatusEffect? CreateStatus()
        {
            var effect = new StatusEffect(_statusKind, _statusDuration, _statusMagnitude, _statusApplyChance);
            return effect.IsValid ? effect : (StatusEffect?)null;
        }
    }
}
