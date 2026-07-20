using UnityEngine;
using UnityEngine.UI;

namespace Assets.MyAssets.Scripts.Battle.View
{
    public sealed class UnitHealthBar : MonoBehaviour
    {
        [Tooltip("Image Type = Filled 로 설정된 채력 게이지")]
        [SerializeField] private Image _fill;
        [Tooltip("카메라를 향하게 회전시킬 루트(보통 이 오브젝트 자신)")]
        [SerializeField] private Transform _billboardRoot;

        public void Set(int current, int max)
        {
            if (_fill == null) return;
            _fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
        }

        private void LateUpdate()
        {
            if (_billboardRoot == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            _billboardRoot.forward = camera.transform.forward;
        }
    }
}
