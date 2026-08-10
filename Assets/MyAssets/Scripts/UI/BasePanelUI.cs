using System.Collections.Generic;
using Assets.MyAssets.Scripts.Localization;
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

        /// <summary>
        /// UXML에 키가 적혀 있던 엘리먼트와 그 키. 번역을 덮어쓰고 나면 키가 사라지므로
        /// 언어를 다시 바꿀 때 조회할 수 있도록 처음 한 번 걷어둔다.
        /// </summary>
        private readonly List<(TextElement Element, string Key)> _localizedTexts = new();

        private bool _textsCollected;

        /// <summary>
        /// ⚠️ 오버라이드할 때 <c>base.Start()</c>를 반드시 부를 것 —
        /// UXML에 박힌 문구를 번역하는 곳이라, 빠뜨리면 그 패널만 키(ui.…)가 그대로 보인다.
        /// </summary>
        protected virtual void Start()
        {
            LocalizeTree();
        }

        // ⚠️ private가 아니라 protected virtual인 이유: Unity는 같은 이름의 메시지가 파생 클래스에도 있으면
        // 파생 쪽만 호출한다(베이스의 private OnEnable은 조용히 실행되지 않는다). virtual로 두면
        // 파생이 같은 이름을 선언할 때 컴파일러가 hides 경고를 내주므로 실수가 드러난다.
        protected virtual void OnEnable() => Loc.LanguageChanged += OnLanguageChanged;

        protected virtual void OnDisable() => Loc.LanguageChanged -= OnLanguageChanged;

        /// <summary>
        /// 언어가 바뀌었을 때. 기본 동작은 UXML 문구를 다시 그리는 것이고,
        /// 코드가 채우는 문구(캐릭터 이름·스탯 표기 등)가 있는 패널은 여기서 함께 갱신한다.
        /// </summary>
        protected virtual void OnLanguageChanged() => LocalizeTree();

        /// <summary>
        /// 패널 트리에서 <see cref="Loc.UiKeyPrefix"/>로 시작하는 문구를 번역문으로 바꾼다.
        /// 접두어가 없는 문자열은 코드가 채우는 값(캐릭터 이름, 스탯 숫자)이므로 건드리지 않는다.
        /// </summary>
        protected void LocalizeTree()
        {
            // 배선 누락(_document 미할당 포함)은 SetVisible이 보고하므로 여기서는 조용히 넘어간다.
            // Root 게터가 _document를 그대로 참조하므로 순서상 _document를 먼저 본다.
            if (_document == null || Root == null)
            {
                return;
            }

            if (!_textsCollected)
            {
                CollectLocalizedTexts(Root);
                _textsCollected = true;
            }

            for (int i = 0; i < _localizedTexts.Count; i++)
            {
                (TextElement element, string key) = _localizedTexts[i];
                element.text = Loc.Get(key);
            }
        }

        private void CollectLocalizedTexts(VisualElement element)
        {
            // Label·Button 모두 TextElement라 한 번에 걸린다.
            if (element is TextElement text && Loc.IsUiKey(text.text))
            {
                _localizedTexts.Add((text, text.text));
            }

            foreach (VisualElement child in element.Children())
            {
                CollectLocalizedTexts(child);
            }
        }

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
