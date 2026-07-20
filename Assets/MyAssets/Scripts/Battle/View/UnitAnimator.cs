using UnityEngine;
namespace Assets.MyAssets.Scripts.Battle.View
{

    [RequireComponent(typeof(Animator))]
    public class UnitAnimator : MonoBehaviour
    {
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

        private Animator _animator;

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
    }
}