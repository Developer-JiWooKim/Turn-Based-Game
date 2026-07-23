using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public abstract class BasePanelUI : MonoBehaviour
    {
        [Header("UI Document Root")]
        [SerializeField] protected UIDocument _document;
        [SerializeField] protected string _rootElementName;

        private VisualElement _root;
        protected VisualElement Root => _root ??= _document.rootVisualElement.Q<VisualElement>(_rootElementName);

        protected virtual void Start() { }

        public virtual void Show() => Root.style.display = DisplayStyle.Flex;
        public virtual void Hide() => Root.style.display = DisplayStyle.None;
    }
}