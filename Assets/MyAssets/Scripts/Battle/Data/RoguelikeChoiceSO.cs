using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>로그라이크 선택지 카테고리 9종(아이콘/가중치 구분용, 가중치는 추후)</summary>
    public enum RoguelikeCategory
    {
        AttackUp,
        SpeedUp,
        DefensiveUp,
        CritUp,
        Heal,
        EnemyStun,      // 다음 스테이지 몬스터 1턴 행동불가
        EnemyHpDown,    // 다음 스테이지 몬스터 HP 감소
        EnemyAtkDown,   // 다음 스테이지 몬스터 ATK 감소
        Recruit         // 파티원 영입
    }

    /// <summary>
    /// 스테이지 클리어 후 제시되는 성장 선택지 1종. 카테고리에 해당하는 효과 값만 채우면 된다.
    /// (예: 방어형 강화 = Hp/Def/Res 동시 입력, 크리 강화 = CritRate/CritDmg 입력,
    ///  몬스터 HP 감소 = EnemyHpMul에 0.7 입력. 영입은 값 입력 없이 카테고리만 지정.)
    /// 수치는 임시값이며 밸런싱 단계에서 조정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Roguelike Choice", fileName = "RoguelikeChoice")]
    public sealed class RoguelikeChoiceSO : ScriptableObject
    {
        [SerializeField] private string _title;
        [TextArea][SerializeField] private string _description;
        [SerializeField] private RoguelikeCategory _category;

        [Header("파티 강화 (런 종료까지 유지)")]
        [SerializeField] private int _hpFlat;
        [SerializeField] private int _atkFlat;
        [SerializeField] private int _spdFlat;
        [SerializeField] private int _defFlat;
        [Tooltip("즉시 회복량(회복 선택지)")]
        [SerializeField] private int _healFlat;
        [Range(0f, 1f)][SerializeField] private float _resFlat;
        [Range(0f, 1f)][SerializeField] private float _critRateFlat;
        [SerializeField] private float _critDmgFlat;

        [Header("다음 스테이지 몬스터 디버프 (일회성)")]
        [Tooltip("몬스터 MaxHp 배율. 1 = 변화 없음(0.7이면 30% 감소)")]
        [Range(0.1f, 1f)][SerializeField] private float _enemyHpMul = 1f;
        [Tooltip("몬스터 ATK 배율. 1 = 변화 없음")]
        [Range(0.1f, 1f)][SerializeField] private float _enemyAtkMul = 1f;
        [Tooltip("다음 스테이지 첫 턴에 몬스터 전체 행동 불가")]
        [SerializeField] private bool _enemySkipFirstTurn;

        [Header("등장 가중치")]
        [Tooltip("기본 등장 가중치. 클수록 자주 제시(전부 같은 값이면 균등 추첨)")]
        [SerializeField] private float _weight = 1f;
        [Tooltip("파티 빈자리 1개당 더해지는 가중치. 영입 선택지에만 값을 넣는다 —\n" +
                 "파티가 비어 있을수록 자주 등장하고, 4명이 차면 기본 가중치로 돌아간다\n" +
                 "(꽉 차도 선택지 자체는 계속 등장하며, 고르면 교체 대상을 플레이어가 선택)")]
        [SerializeField] private float _weightPerEmptySlot = 0f;

        public string Title => _title;
        public string Description => _description;
        public RoguelikeCategory Category => _category;

        /// <summary>영입 로스터가 없으면 제시되면 안 되는 선택지인지(영입 계열)</summary>
        public bool RequiresPartySlot => _category == RoguelikeCategory.Recruit;

        /// <summary>현재 파티 상황을 반영한 등장 가중치</summary>
        public float GetWeight(int emptyPartySlots) =>
            Mathf.Max(0f, _weight + _weightPerEmptySlot * Mathf.Max(0, emptyPartySlots));

        public RoguelikeEffect CreateEffect() =>
            new RoguelikeEffect(_hpFlat, _atkFlat, _spdFlat, _defFlat, _healFlat, _resFlat, _critRateFlat, _critDmgFlat,
                                _enemyHpMul, _enemyAtkMul, _enemySkipFirstTurn,
                                _category == RoguelikeCategory.Recruit);
    }
}
