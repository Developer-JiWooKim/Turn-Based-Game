using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Audio.View
{
    /// <summary>
    /// UIDocument 루트에 <see cref="ClickEvent"/> 콜백을 걸어 버튼 클릭음을 재생한다.
    /// ClickEvent는 루트까지 버블링되므로 개별 버튼 핸들러를 수정하지 않고 전 버튼을 커버한다.
    /// UIDocument 오브젝트에 부착만 하면 되며, 코드 수정이 필요 없다.
    /// </summary>
    // UIDocument가 OnEnable에서 rootVisualElement를 만든 뒤 콜백을 걸도록 실행 순서를 늦춘다.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiClickSfx : MonoBehaviour
    {
        private UIDocument _document;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root != null)
                root.RegisterCallback<ClickEvent>(OnClick);
        }

        private void OnDisable()
        {
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.UnregisterCallback<ClickEvent>(OnClick);
        }

        // 빈 공간 클릭까지 소리 내지 않도록 실제 버튼을 눌렀을 때만 재생한다.
        private void OnClick(ClickEvent evt)
        {
            if (evt.target is Button)
                AudioManager.Sfx(AudioManager.Library?.UiClick);
        }
    }
}
