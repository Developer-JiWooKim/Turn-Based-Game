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
        [SerializeField] private Animator _animator;
        [SerializeField] private UnitHealthBar _healthBar;

        [Header("연출 시간(초) — 애니메이션 길이에 맞게 조정")]
        [Tooltip("등장 연출 시간. 몬스터는 Animator 기본 상태가 Spawned(자동 재생 후 Idle 전환)이므로 그 클립 길이를 넣는다. 등장 연출이 없는 프리팹은 0.")]
        [SerializeField] private float _spawnDuration = 0f;
        [SerializeField] private float _attackDuration = 0f;
        [SerializeField] private float _skillDuration = 0f;
        [SerializeField] private float _hitDuration = 0f;
        [SerializeField] private float _dieDuration = 0f;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SkillHash = Animator.StringToHash("Skill");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        /// <summary>대응하는 Core Unit의 Id (타겟팅 시 역참조에 사용).</summary>
        public int UnitId { get; private set; }

        public void Initialize(int unitId, int currentHp, int maxHp)
        {
            UnitId = unitId;
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_healthBar != null)
                _healthBar.Set(currentHp, maxHp);
        }

        /// <summary>피격 연출 없이 체력바만 갱신(회복·최대체력 증가 등 스탯 변화 반영용).</summary>
        public void RefreshHealth(int currentHp, int maxHp)
        {
            if (_healthBar != null)
                _healthBar.Set(currentHp, maxHp);
        }

        /// <summary>
        /// 등장 연출 대기. Spawned는 Animator의 기본 진입 상태라 트리거 없이 자동 재생되므로,
        /// 여기서는 그 클립 길이(_spawnDuration)만큼 기다려 전투 시작을 늦춘다.
        /// </summary>
        public async Task PlaySpawnAsync(CancellationToken ct = default)
        {
            await Awaitable.WaitForSecondsAsync(_spawnDuration, ct);
        }

        public async Task PlayAttackAsync(CancellationToken ct = default)
        {
            _animator.SetTrigger(AttackHash);
            await Awaitable.WaitForSecondsAsync(_attackDuration, ct);
        }

        public async Task PlaySkillAsync(CancellationToken ct = default)
        {
            _animator.SetTrigger(SkillHash);
            await Awaitable.WaitForSecondsAsync(_skillDuration, ct);
        }

        public async Task PlayHitAsync(int currentHp, int maxHp, CancellationToken ct = default)
        {
            _animator.SetTrigger(HitHash);
            if (_healthBar != null)
                _healthBar.Set(currentHp, maxHp);
            await Awaitable.WaitForSecondsAsync(_hitDuration, ct);
        }

        public async Task PlayDeathAsync(CancellationToken ct = default)
        {
            _animator.SetTrigger(DieHash);
            await Awaitable.WaitForSecondsAsync(_dieDuration, ct);
        }
    }
}
