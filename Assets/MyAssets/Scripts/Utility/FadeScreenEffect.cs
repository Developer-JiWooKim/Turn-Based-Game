using UnityEngine;

namespace Assets.MyAssets.Scripts.Utility
{
    public class FadeScreenEffect : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _fadeInDuration = 1f;

        private void Awake()
        {
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

