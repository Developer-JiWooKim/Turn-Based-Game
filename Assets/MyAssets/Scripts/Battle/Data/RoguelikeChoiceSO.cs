using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>로그라이크 선택지 카테고리(아이콘/가중치 구분용, 가중치는 추후).</summary>
    public enum RoguelikeCategory
    {
        AttackUp,
        SpeedUp,
        DefensiveUp,
        CritUp,
        Heal
    }

    /// <summary>
    /// 스테이지 클리어 후 제시되는 성장 선택지 1종. 카테고리에 해당하는 효과 값만 채우면 된다.
    /// (예: 방어형 강화 = Hp/Def/Res 동시 입력, 크리 강화 = CritRate/CritDmg 입력.)
    /// 수치는 임시값이며 밸런싱 단계에서 조정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Roguelike Choice", fileName = "RoguelikeChoice")]
    public sealed class RoguelikeChoiceSO : ScriptableObject
    {
        [SerializeField] private string _title;
        [TextArea][SerializeField] private string _description;
        [SerializeField] private RoguelikeCategory _category;

        [Header("효과 값 (해당 카테고리에 맞는 것만 채움)")]
        [SerializeField] private int _hpFlat;
        [SerializeField] private int _atkFlat;
        [SerializeField] private int _spdFlat;
        [SerializeField] private int _defFlat;
        [Tooltip("즉시 회복량(회복 선택지).")]
        [SerializeField] private int _healFlat;
        [Range(0f, 1f)][SerializeField] private float _resFlat;
        [Range(0f, 1f)][SerializeField] private float _critRateFlat;
        [SerializeField] private float _critDmgFlat;

        public string Title => _title;
        public string Description => _description;
        public RoguelikeCategory Category => _category;

        public RoguelikeReward CreateReward() =>
            new RoguelikeReward(_hpFlat, _atkFlat, _spdFlat, _defFlat, _healFlat, _resFlat, _critRateFlat, _critDmgFlat);
    }
}
