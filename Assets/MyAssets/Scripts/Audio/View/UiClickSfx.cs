using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Audio.View
{
    /// <summary>
    /// UIDocument 루트에 <see cref="ClickEvent"/> 콜백을 걸어 버튼 클릭음을 재생한다.
    /// ClickEvent는 루트까지 버블링되므로 개별 버튼 핸들러를 수정하지 않고 전 버튼을 커버한다.
    /// UIDocument 오브젝트에 부착만 하면 되며, 코드 수정이 필요 없다.
    /// UIDocument가 OnEnable에서 rootVisualElement를 만든 뒤 콜백을 걸도록 실행 순서를 늦춘다.
    /// </summary>    
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiClickSfx : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _registeredRoot;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            _registeredRoot = _document.rootVisualElement;
            if (_registeredRoot is not null)
            {
                _registeredRoot.RegisterCallback<ClickEvent>(OnClick);
            }
            else
            {
                NullCheck.LogIfNull(_registeredRoot, nameof(UIDocument.rootVisualElement), this, "버튼 클릭음이 재생되지 않습니다");
            }
        }

        private void OnDisable()
        {
            if (_registeredRoot is null)
            {
                return;
            }

            _registeredRoot.UnregisterCallback<ClickEvent>(OnClick);
            _registeredRoot = null;
        }

        /// <summary>빈 공간 클릭까지 소리 내지 않도록 실제 버튼을 눌렀을 때만 재생한다.</summary>
        private void OnClick(ClickEvent evt)
        {
            if (evt.target is not VisualElement element)
            {
                return;
            }

            if (element is Button || element.GetFirstAncestorOfType<Button>() != null)
            {
                AudioManager.UiClick();
            }
        }
    }
}
