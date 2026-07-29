using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 크리티컬 등 주요 순간에 카메라를 짧게 흔드는 연출. 카메라(또는 카메라 부모)에 붙인다.
    /// 
    /// TODO#: 현재는 CineMachine 카메라를 사용하지 않아 직접 컴포넌트를 생성 후 카메라에 붙여서 사용하고 있으나,
    /// 시네머신 카메라를 쓰면 내장되어 있는 기능을 사용하면 됨
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private float _magnitude = 0.15f;

        private Vector3 _origin;
        private bool _shaking;

        private void Awake() => _origin = transform.localPosition;

        public void Shake()
        {
            if (!_shaking)
            {
                _ = ShakeRoutine();
            }
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
