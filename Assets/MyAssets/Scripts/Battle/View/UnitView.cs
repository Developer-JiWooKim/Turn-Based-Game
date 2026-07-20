using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Core의 Unit 하나에 대응하는 연출 담당 컴포넌트. 애니메이터 트리거 재생과 체력바 갱신만 하며,
    /// 전투 규칙은 전혀 갖지 않는다. 각 재생 메소드는 애니메이션이 끝날 때까지의 Task를 반환하여
    /// 시뮬레이션이 연출 완료를 기다릴 수 있게 한다.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private UnitHealthBar _unitHealthBar;

        /// <summary>대응하는 Core Unit의 Id (타겟팅 시 역참조에 사용).</summary>
        public int UnitId { get; private set; }

        public void Initialize(int unitId, int currentHp, int maxHp)
        {
            UnitId = unitId;
            if (_unitAnimator == null)
            {
                Debug.LogError("_unitAnimator is null");
                _unitAnimator = GetComponentInChildren<UnitAnimator>();
            }

            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
            else
            {
                Debug.LogError("_unitHealthBar is null");
                _unitHealthBar = GetComponentInChildren<UnitHealthBar>();
                _unitHealthBar.Set(currentHp, maxHp);
            }
        }

        /// <summary>피격 연출 없이 체력바만 갱신(회복·최대체력 증가 등 스탯 변화 반영용).</summary>
        public void RefreshHealth(int currentHp, int maxHp)
        {
            if (_unitHealthBar != null)
                _unitHealthBar.Set(currentHp, maxHp);
        }

        /// <summary>
        /// 등장 연출 대기. Spawned는 Animator의 기본 진입 상태라 트리거 없이 자동 재생되므로,
        /// 여기서는 그 클립 길이(_spawnDuration)만큼 기다려 전투 시작을 늦춘다.
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
            var hitTask = Awaitable.WaitForSecondsAsync(_unitAnimator.PlayHit(), ct);
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
    // #TODO: UnitView는 연출 담당 컴포넌트라고 했는데, 애니메이터와 체력바 갱신 두가지 작업을 하고 있음 -> 기능 모듈화, UnitAnimator.cs / UnitHealthBar.cs 로 나눠서 해당 스크립트들은 각 기능만 가지고 있고
    // UnitView.cs(이름 변경할 가능성 높음) 은 이 스크립트들을 가지고 컨트롤 하는 역할 담당할듯(UnitViewController? 로 바꿀듯)
}
