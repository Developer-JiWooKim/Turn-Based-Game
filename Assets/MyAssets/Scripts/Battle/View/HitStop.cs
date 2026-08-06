using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 타격 순간 잠깐 연출을 늦춰 타격감을 강조하는 히트 스톱.
    ///
    /// ⚠️ <see cref="Time.timeScale"/>을 쓰지 않는다 — 이 프로젝트의 연출 대기와 씬 전환 페이드가
    /// 모두 <see cref="Awaitable"/>(WaitForSecondsAsync) 기반이라 timeScale에 함께 묶일 수 있고,
    /// <see cref="BattlePausePanel"/>이 timeScale을 피한 것도 같은 이유다.
    /// 대신 <b>타격에 관여한 유닛의 애니메이터 속도만</b> 낮춘다.
    ///
    /// 늦춘 만큼 애니메이션이 뒤로 밀리므로, 지속시간을 길게 잡으면
    /// <see cref="UnitAnimator"/>의 연출 시간(_attackDuration 등)을 그만큼 늘려 잡아야 끝이 잘리지 않는다.
    /// </summary>
    public sealed class HitStop : MonoBehaviour
    {
        [Tooltip("타격 순간 느려지는 시간(초). 0이면 히트 스톱을 쓰지 않는다.")]
        [SerializeField] private float _duration = 0.08f;

        [Tooltip("느려지는 정도(1 = 보통 속도, 0에 가까울수록 정지에 가깝다).")]
        [Range(0f, 1f)]
        [SerializeField] private float _speedScale = 0.05f;

        [Tooltip("크리티컬일 때 지속시간에 곱하는 배율(1이면 일반 타격과 같다).")]
        [SerializeField] private float _criticalMultiplier = 2f;

        /// <summary>
        /// 지정한 유닛들의 연출을 잠깐 늦춘다. 완료를 기다려도 되고 아니어도 되지만,
        /// 호출자가 기다려야 이어지는 연출과 겹쳐 어색해지지 않는다.
        /// </summary>
        public async Task PlayAsync(IReadOnlyList<UnitView> views, bool critical, CancellationToken ct)
        {
            float duration = critical ? _duration * _criticalMultiplier : _duration;
            if (duration <= 0f || views == null || views.Count == 0)
            {
                return;
            }

            SetSpeedScale(views, _speedScale);
            try
            {
                await Awaitable.WaitForSecondsAsync(duration, ct);
            }
            finally
            {
                // 취소(씬 종료·배틀 중단)로 빠져나가도 느려진 채로 남지 않게 한다.
                SetSpeedScale(views, 1f);
            }
        }

        private static void SetSpeedScale(IReadOnlyList<UnitView> views, float scale)
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i] != null)
                {
                    views[i].SetAnimationSpeed(scale);
                }
            }
        }
    }
}
