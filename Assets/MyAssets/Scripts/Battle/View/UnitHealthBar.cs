using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.MyAssets.Scripts.Battle.View
{
    public sealed class UnitHealthBar : MonoBehaviour
    {
        [Tooltip("설정된 채력 게이지(Image Type = Filled)")]
        [SerializeField] private Image _fill;

        [Tooltip("체력을 숫자로 표기할 텍스트")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("카메라를 향하게 회전시킬 루트")]
        [SerializeField] private Transform _billboardRoot;

        public void Set(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

            if (_hpText != null)
                _hpText.SetText("{0} / {1}", current, max);
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
