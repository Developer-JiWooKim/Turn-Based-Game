using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
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
    ///
    /// <para>
    /// 파티 강화 수치는 전부 <b>비율</b>이라(2026-08-12 전환) 스탯 리스케일의 영향을 받지 않는다 —
    /// 과거엔 고정 증분이라 스탯을 10배 할 때 이 에셋들도 함께 고쳐야 했다.
    /// 비율의 기준과 혼합 모델(정수 스탯=비율 / 치명타·저항=%p)의 근거는 <see cref="RoguelikeEffect"/> 참고.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Roguelike Choice", fileName = "RoguelikeChoice")]
    public sealed class RoguelikeChoiceSO : ScriptableObject
    {
        [SerializeField] private string _title;
        [TextArea][SerializeField] private string _description;
        [SerializeField] private RoguelikeCategory _category;
        [SerializeField] private Sprite _icon;

        [Header("파티 강화 — 정수 스탯은 비율 (0.45 = +45%)")]
        [Tooltip("증가 비율. 기준은 '기준 스탯 + 스테이지 자동 성장'이라\n" +
                 "스테이지가 오를수록 실제 증가량도 함께 커진다(선택지로 얻은 몫은 기준에서 빠져 복리로 부풀지 않는다)")]
        [SerializeField] private float _hpRate;
        [SerializeField] private float _atkRate;
        [SerializeField] private float _spdRate;
        [SerializeField] private float _defRate;
        [Tooltip("즉시 회복량(회복 선택지). 이 항목만 실제 최대 HP 대비 비율이다 — 0.5 = 최대 체력의 50% 회복")]
        [Range(0f, 1f)][SerializeField] private float _healRate;

        [Header("파티 강화 — 비율 스탯은 %p 가산 (0.1 = +10%p)")]
        [Tooltip("치명타·저항은 이미 0~1 비율값이라 비율을 곱하면 사실상 변화가 없어 더하는 값으로 둔다")]
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

        /// <summary>
        /// 카드 제목·본문. 키를 원문에서 뽑지 않고 <see cref="_category"/>에서 만든다 —
        /// 본문이 여러 줄이라 원문을 키로 쓰면 공백·줄바꿈이 한 글자만 달라도 조용히 번역이 빠진다.
        /// 카테고리는 선택지 1종을 가리키는 안정적인 식별자라 이런 위험이 없다.
        /// (표에 행이 없으면 에셋에 적힌 원문이 그대로 나온다.)
        /// </summary>
        public string Title => Loc.Get($"choice.{_category}.title", _title);

        public string Description => Loc.Get($"choice.{_category}.desc", _description);

        public RoguelikeCategory Category => _category;
        public Sprite Icon => _icon;

        /// <summary>영입 로스터가 없으면 제시되면 안 되는 선택지인지(영입 계열)</summary>
        public bool RequiresPartySlot => _category == RoguelikeCategory.Recruit;

        /// <summary>현재 파티 상황을 반영한 등장 가중치</summary>
        public float GetWeight(int emptyPartySlots) =>
            Mathf.Max(0f, _weight + _weightPerEmptySlot * Mathf.Max(0, emptyPartySlots));

        public RoguelikeEffect CreateEffect() =>
            new RoguelikeEffect(_hpRate, _atkRate, _spdRate, _defRate, _healRate, _resFlat, _critRateFlat, _critDmgFlat,
                                _enemyHpMul, _enemyAtkMul, _enemySkipFirstTurn,
                                _category == RoguelikeCategory.Recruit);
    }
}
