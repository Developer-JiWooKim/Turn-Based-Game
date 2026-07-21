using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 스테이지별 양측 스탯 성장률. 몬스터 성장률을 플레이어보다 높게 잡는 것이 전제이며
    /// (플레이어는 로그라이크 선택지로 원하는 스탯을 몰아 올릴 수 있으므로),
    /// 정확한 수치는 밸런싱 단계에서 조정한다.
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

        public StageScaling Create() =>
            new StageScaling(_playerHpRate, _playerAtkRate, _playerDefRate,
                             _monsterHpRate, _monsterAtkRate, _monsterDefRate,
                             _monsterCompound);
    }
}
