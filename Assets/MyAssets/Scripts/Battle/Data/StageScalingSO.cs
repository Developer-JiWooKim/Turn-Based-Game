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

        [Header("몬스터 — 스테이지당 증가율 (플레이어보다 높게)")]
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
        [Tooltip("스테이지당 몬스터 SPD 증가율. 0이면 SPD를 올리지 않는다(속도 강화 선택지가 무의미해진다).")]
        [Range(0f, 0.2f)][SerializeField] private float _monsterSpdRate = 0.03f;
        [Tooltip("이 스테이지 이후부터 몬스터 SPD가 오른다(첫 보스 구간까지는 기준값 유지).")]
        [SerializeField] private int _spdStartStage = 5;

        [Header("플레이어 — 보스 처치 보상")]
        [Tooltip("보스 1회 처치당 얻는 SPD(기준 SPD 대비 비율).\n" +
                 "⚠️ 몬스터 SPD 증가분보다 작아야 한다 — 같은 속도로 주면 선공 압박이 사라져 속도 강화가 다시 무의미해진다.")]
        [Range(0f, 0.5f)][SerializeField] private float _bossSpdRate = 0.08f;

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
                             _bossSpdRate, bossStageInterval);
    }
}
