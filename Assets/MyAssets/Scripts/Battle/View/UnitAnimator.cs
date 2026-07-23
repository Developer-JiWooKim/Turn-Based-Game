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

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SkillHash = Animator.StringToHash("Skill");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private Animator _animator;

        public float SpawnDuration => _spawnDuration;

        void Awake()
        {
            _animator = GetComponent<Animator>();
        }

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
        /// 풀에서 재사용될 때 애니메이터를 초기 상태(기본 진입 상태 = Spawned)로 되돌린다.
        /// 이걸 빼먹으면 사망 포즈 그대로 다음 웨이브에 등장한다.
        ///
        /// Rebind()가 상태와 파라미터를 초기화하지만, 아직 소비되지 않은 트리거가 남아 있으면
        /// 복귀 직후 그 연출이 한 번 재생될 수 있어 명시적으로 먼저 지운다.
        /// Update(0f)는 초기화 결과를 이번 프레임에 바로 반영해 한 프레임짜리 잔상 포즈를 막는다.
        /// </summary>
        public void ResetToSpawn()
        {
            _animator.ResetTrigger(AttackHash);
            _animator.ResetTrigger(SkillHash);
            _animator.ResetTrigger(HitHash);
            _animator.ResetTrigger(DieHash);
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}