using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 크리티컬 등 주요 순간에 카메라를 짧게 흔드는 연출. 카메라(또는 카메라 부모)에 붙인다.
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private float _magnitude = 0.15f;

        private Vector3 _origin;
        private bool _shaking;

        private void Awake() => _origin = transform.localPosition;

        public void Shake()
        {
            if (!_shaking)
                _ = ShakeRoutine();
        }

        private async Awaitable ShakeRoutine()
        {
            _shaking = true;
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                Vector3 offset = Random.insideUnitCircle * _magnitude;
                transform.localPosition = _origin + offset;
                elapsed += Time.deltaTime;
                await Awaitable.NextFrameAsync();
            }
            transform.localPosition = _origin;
            _shaking = false;
        }
    }
}
