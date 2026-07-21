using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Unit 하나에 대응하는 연출 담당 컴포넌트
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private UnitHealthBar _unitHealthBar;

        public int UnitId { get; private set; }

        public void Initialize(int unitId, int currentHp, int maxHp)
        {
            UnitId = unitId;

            if (_unitAnimator == null)
            {
                Debug.LogWarning("_unitAnimator is null");
                _unitAnimator = GetComponentInChildren<UnitAnimator>();
            }

            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
            else
            {
                Debug.LogWarning("_unitHealthBar is null");
                _unitHealthBar = GetComponentInChildren<UnitHealthBar>();
                _unitHealthBar.Set(currentHp, maxHp);
            }
        }

        /// <summary>
        /// 체력바 갱신        
        /// </summary>
        public void RefreshHealth(int currentHp, int maxHp)
        {
            if (_unitHealthBar != null)
                _unitHealthBar.Set(currentHp, maxHp);
        }

        /// <summary>
        /// Spawned는 Animator의 기본 진입 상태라 트리거 없이 자동 재생되므로,
        /// 그 클립 길이(_spawnDuration)만큼 기다림
        /// </summary>
        public async Task PlaySpawnAsync(CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(_unitAnimator.SpawnDuration, ct);
        }

        public async Task PlayAttackAsync(CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayAttack(), ct);
        }

        public async Task PlaySkillAsync(CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlaySkill(), ct);
        }

        public async Task PlayHitAsync(int currentHp, int maxHp, CancellationToken ct = default)
        {
            Awaitable hitTask = Awaitable.WaitForSecondsAsync(_unitAnimator.PlayHit(), ct);
            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
            await hitTask;
        }

        public async Task PlayDieAsync(CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayDie(), ct);
        }
    }
}
