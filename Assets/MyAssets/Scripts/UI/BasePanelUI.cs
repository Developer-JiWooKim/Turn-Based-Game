using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public abstract class BasePanelUI : MonoBehaviour
    {
        [Header("UI Document Root")]
        [SerializeField] protected UIDocument _document;
        [Tooltip("UXML에서 이 패널의 루트가 되는 엘리먼트 이름.\n" +
                 "RootElementName을 코드에서 오버라이드한 패널(배틀 선택지/결과/퍼즈)은 이 값을 쓰지 않으므로 비워둔다.")]
        [SerializeField] protected string _rootElementName;

        private VisualElement _root;
        protected VisualElement Root => _root ??= _document.rootVisualElement.Q<VisualElement>(RootElementName);

        protected virtual string RootElementName => _rootElementName;

        protected virtual void Start() { }

        private void SetVisible(bool visible)
        {
            // UXML에 RootElementName과 같은 이름의 엘리먼트가 없을 때 걸린다(_document 누락 포함).
            if (NullCheck.LogIfNull(Root, nameof(Root), this, $"'{RootElementName}' 엘리먼트를 찾지 못했습니다"))
            {
                return;
            }

            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public virtual void Show() => SetVisible(true);
        public virtual void Hide() => SetVisible(false);
    }
}