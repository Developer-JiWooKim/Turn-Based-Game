using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace Assets.MyAssets.Scripts.Battle.View
{

    [RequireComponent(typeof(Animator))]
    public class UnitAnimator : MonoBehaviour
    {
        [Header("연출 시간(초) — 애니메이션 길이에 맞게 조정")]
        [Tooltip("등장 연출 시간 - 등장 연출이 없는 프리팹은 0")]
        [SerializeField] private float _spawnDuration = 0f;
        [SerializeField] private float _attackDuration = 0f;
        [SerializeField] private float _skillDuration = 0f;
        [SerializeField] private float _hitDuration = 0f;
        [SerializeField] private float _dieDuration = 0f;

        [Header("타격 시점")]
        [Tooltip("공격/스킬 클립에 타격 프레임 애니메이션 이벤트(OnImpactFrame)를 심었으면 켠다. " +
                 "끄면 호출자가 넘긴 고정 지연을 그대로 쓴다(기존 동작).")]
        [SerializeField] private bool _useImpactEvent;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SkillHash = Animator.StringToHash("Skill");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private Animator _animator;

        /// <summary>타격 프레임을 기다리는 신호(대기 중이 아니면 null).</summary>
        private TaskCompletionSource<bool> _impactSignal;

        public float SpawnDuration => _spawnDuration;

        void Awake() => _animator = GetComponent<Animator>();

        public float PlayAttack()
        {
            _animator.SetTrigger(AttackHash);
            return _attackDuration;
        }

        public float PlayHit()
        {
            _animator.SetTrigger(HitHash);
            return _hitDuration;
        }

        public float PlayDie()
        {
            _animator.SetTrigger(DieHash);
            return _dieDuration;
        }

        public float PlaySkill()
        {
            _animator.SetTrigger(SkillHash);
            return _skillDuration;
        }

        /// <summary>
        /// 공격/스킬 클립의 타격 프레임에 심은 <b>애니메이션 이벤트</b>가 호출한다.
        /// 클립 에셋이 이 메서드를 <b>이름 문자열로</b> 참조하므로 이름을 바꾸면 이벤트가 조용히 끊긴다.
        /// (애니메이션 이벤트는 Animator와 같은 GameObject의 컴포넌트만 호출할 수 있어
        ///  이 클래스가 <see cref="RequireComponentAttribute"/>로 Animator와 같은 자리에 묶여 있다.)
        /// </summary>
        public void OnImpactFrame() => _impactSignal?.TrySetResult(true);

        /// <summary>
        /// 타격 시점까지 기다린다.
        ///
        /// <see cref="_useImpactEvent"/>가 꺼져 있으면 <paramref name="fallbackSeconds"/>만큼 기다린다(기존 동작).
        /// 켜져 있으면 클립의 타격 이벤트를 기다리되, 이벤트가 없는 클립에서 전투가 멈추지 않도록
        /// 연출 길이만큼의 안전 타임아웃을 둔다 — 타임아웃으로 풀리면 경고를 남겨
        /// "이벤트를 심지 않은 클립"이 조용히 넘어가지 않게 한다.
        /// </summary>
        public async Task WaitForImpactAsync(float fallbackSeconds, CancellationToken ct)
        {
            if (!_useImpactEvent)
            {
                await Awaitable.WaitForSecondsAsync(Mathf.Max(0f, fallbackSeconds), ct);
                return;
            }

            var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _impactSignal = signal;

            // 이벤트(TrySetResult(true)) / 타임아웃(TrySetResult(false)) / 취소(TrySetCanceled) 중 먼저 오는 하나로 풀린다.
            float timeout = Mathf.Max(_attackDuration, _skillDuration);
            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            try
            {
                using (ct.Register(() => signal.TrySetCanceled(ct)))
                using (timeoutSource.Token.Register(() => signal.TrySetResult(false)))
                {
                    if (!await signal.Task)
                    {
                        Debug.LogWarning($"[UnitAnimator] '{name}'의 타격 이벤트가 오지 않아 {timeout}초 뒤 진행합니다 — " +
                                         $"클립에 {nameof(OnImpactFrame)} 애니메이션 이벤트가 있는지 확인하세요.", this);
                    }
                }
            }
            finally
            {
                _impactSignal = null;
            }
        }

        /// <summary>
        /// 히트 스톱용 재생 속도 배율(1 = 보통 속도).
        /// <see cref="Time.timeScale"/>을 쓰지 않는 이유는 <see cref="HitStop"/> 주석 참고.
        /// </summary>
        public void SetSpeedScale(float scale) => _animator.speed = scale;

        /// <summary>
        /// 풀에서 재사용될 때 애니메이터를 초기 상태(기본 진입 상태 = Spawned)로 되돌린다.
        /// 버그 사례 - 이걸 빼먹으면 사망 포즈 그대로 다음 웨이브에 등장한다.
        ///
        /// Rebind()가 상태와 파라미터를 초기화하지만, 아직 소비되지 않은 트리거가 남아 있으면
        /// 복귀 직후 그 연출이 한 번 재생될 수 있어 명시적으로 먼저 지운다.
        /// Update(0f)는 초기화 결과를 이번 프레임에 바로 반영해 한 프레임짜리 잔상 포즈를 막는다.
        /// </summary>
        public void ResetToSpawn()
        {
            // 히트 스톱 도중 전투가 끝나 반납됐다면 느려진 속도가 그대로 남는다 — 재사용 전에 되돌린다.
            _animator.speed = 1f;

            _animator.ResetTrigger(AttackHash);
            _animator.ResetTrigger(SkillHash);
            _animator.ResetTrigger(HitHash);
            _animator.ResetTrigger(DieHash);
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}
