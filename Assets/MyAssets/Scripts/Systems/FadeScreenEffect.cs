using UnityEngine;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 화면 Fade 연출 컴포넌트
    /// CanvasGroup의 Alpha 값 0 ~ 1로 페이드 연출
    ///
    /// UI 패널이 아니라 씬 전환 인프라라서 Systems에 있다(GameManager가 소유하며, 그 자식으로 두어야
    /// 씬 전환에도 파괴되지 않는다). UI 어셈블리에 두면 Systems→UI→Systems 순환 참조가 된다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeScreenEffect : MonoBehaviour
    {
        [Header("Fade Time Setting")]
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _fadeInDuration = 1f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        public async Awaitable FadeOutAsync()
        {
            await FadeAsync(0f, 1f, _fadeOutDuration);
        }
        public async Awaitable FadeInAsync()
        {
            await FadeAsync(1f, 0f, _fadeInDuration);
        }

        private async Awaitable FadeAsync(float from, float to, float duration)
        {
            float elapsed = 0f;

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                await Awaitable.NextFrameAsync();
            }

            _canvasGroup.alpha = to;
            if (to == 0f)
            {
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
}