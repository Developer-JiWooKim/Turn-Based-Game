using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 스테이지별 양측 스탯 성장률. 몬스터 성장률을 플레이어보다 높게 잡는 것이 전제이며
    /// (플레이어는 로그라이크 선택지로 원하는 스탯을 몰아 올릴 수 있으므로),
    /// 정확한 수치는 밸런싱 단계에서 조정한다.
    ///
    /// 조정 근거와 스테이지별 계산 결과는 `BalanceCurve.md` 참고.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Stage Scaling", fileName = "StageScaling")]
    public sealed class StageScalingSO : ScriptableObject
    {
        [Header("플레이어 — 스테이지당 기준 스탯 대비 증가율 (선형 누적)")]
        [Range(0f, 0.5f)][SerializeField] private float _playerHpRate = 0.05f;
        [Range(0f, 0.5f)][SerializeField] private float _playerAtkRate = 0.04f;
        [Range(0f, 0.5f)][SerializeField] private float _playerDefRate = 0.03f;

        [Header("몬스터 — 스테이지당 증가율 (Compound 활성화 시 복리)")]
        [Range(0f, 0.5f)][SerializeField] private float _monsterHpRate = 0.12f;
        [Range(0f, 0.5f)][SerializeField] private float _monsterAtkRate = 0.09f;
        [Range(0f, 0.5f)][SerializeField] private float _monsterDefRate = 0.07f;

        [Tooltip("켜면 몬스터 배율이 복리로 붙어 후반에 급격히 강해진다(무한 타워의 한계선). 끄면 선형.")]
        [SerializeField] private bool _monsterCompound = true;

        [Header("난이도 가속 — 이 지점까지는 편하게, 이후 빠르게 어려워진다")]
        [Tooltip("이 스테이지까지는 위 성장률을 그대로 쓴다. 0이면 가속 없음.")]
        [SerializeField] private int _accelStartStage = 30;
        [Tooltip("가속 구간에서 몬스터 성장률에 곱하는 배수. 1이면 가속 없음.\n" +
                 "올릴수록 벽이 앞당겨진다(1.5 = 50스테이지 전후, 2.0 = 45스테이지 전후).")]
        [Range(1f, 4f)][SerializeField] private float _accelMultiplier = 1.5f;

        [Header("몬스터 속도 — 선공을 지키려면 속도에 투자하게 만드는 압박")]
        [Tooltip("보스 1회 처치당 몬스터 SPD 증가율. 0이면 SPD를 올리지 않는다(속도 강화 선택지가 무의미해진다).\n" +
                 "스테이지당이 아니라 보스 단위인 이유: 아래 플레이어 보상과 갱신 주기를 맞춰야\n" +
                 "'이번 보스로 따라잡았는지'가 읽히고, 두 비율을 같은 축에서 비교할 수 있다.")]
        [Range(0f, 0.3f)][SerializeField] private float _monsterSpdRate = 0.12f;
        [Tooltip("몬스터 SPD 증가를 세기 시작하는 기준 스테이지(그 전까지는 기준값 유지).\n" +
                 "보스 간격의 위상도 함께 정한다 — 1이면 '보스를 잡은 바로 다음 스테이지부터' 빨라진다.\n" +
                 "보스 스테이지 번호(5)를 넣으면 증가가 보스 스테이지 자체에 걸려 한 구간 늦어진다.")]
        [SerializeField] private int _spdStartStage = 1;

        [Header("플레이어 — 보스 처치 보상")]
        [Tooltip("보스 1회 처치당 얻는 SPD(기준 SPD 대비 비율).\n" +
                 "⚠️ 위 몬스터 SPD 증가율보다 작아야 한다 — 같은 값을 주면 선공 압박이 사라져 속도 강화가 다시 무의미해진다.\n" +
                 "게다가 몬스터는 복리, 이쪽은 선형이라 같은 값이어도 시간이 지날수록 몬스터가 앞선다.")]
        [Range(0f, 0.5f)][SerializeField] private float _bossSpdRate = 0.08f;

        [Header("영입자 소급 — 파티가 이미 쌓아둔 선택지 성장을 얼마나 물려받는가")]
        [Tooltip("0.6 = 현재 파티원들의 선택지 누계 평균의 60%를 갖고 합류한다. 0이면 소급하지 않는다.\n" +
                 "스테이지 자동 성장은 이 값과 무관하게 항상 전부 소급된다.\n" +
                 "⚠️ 1에 가까울수록 '파티를 살려두는 게 이득'이라는 규칙이 사라진다 —\n" +
                 "0이면 후반 영입자가 기존 파티원 공격력의 25% 수준이라 연쇄 사망을 부른다(BalanceCurve.md G절).")]
        [Range(0f, 1f)][SerializeField] private float _recruitChoiceCatchUpRate = 0.6f;

        /// <summary>
        /// Core가 쓰는 순수 성장 규칙을 만든다.
        /// </summary>
        /// <param name="bossStageInterval">
        /// 보스가 등장하는 스테이지 간격. 보스 처치 보상 판정에 쓴다.
        /// 값의 출처는 <c>MonsterSpawner._bossStageInterval</c> 하나뿐이며(웨이브를 고르는 쪽이 주인),
        /// 여기에 같은 값을 또 두면 두 곳이 어긋났을 때 알아챌 방법이 없어 인자로 받는다.
        /// </param>
        public StageScaling Create(int bossStageInterval) =>
            new StageScaling(_playerHpRate, _playerAtkRate, _playerDefRate,
                             _monsterHpRate, _monsterAtkRate, _monsterDefRate,
                             _monsterCompound,
                             _accelStartStage, _accelMultiplier,
                             _monsterSpdRate, _spdStartStage,
                             _bossSpdRate, bossStageInterval,
                             _recruitChoiceCatchUpRate);
    }
}
