using System;
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
                 "RootElementName을 코드에서 오버라이드한 패널(배틀 선택지/결과/퍼즈)은 이 값을 쓰지 않으므로 비워둘 것.")]
        [SerializeField] protected string _rootElementName;

        private VisualElement _root;

        /// <summary>
        /// 이 패널의 루트 엘리먼트. 배선이 빠졌으면 예외 대신 null이고,
        /// 보고는 <see cref="ValidateRoot"/>가 한 번만 한다(게터가 매번 로그를 찍지 않도록).
        /// </summary>
        protected VisualElement Root
        {
            get
            {
                if (_root == null && _document != null)
                {
                    _root = _document.rootVisualElement?.Q<VisualElement>(RootElementName);
                }

                return _root;
            }
        }

        protected virtual string RootElementName => _rootElementName;

        /// <summary>
        /// UXML에 키가 적혀 있던 엘리먼트와 그 키. 번역을 덮어쓰고 나면 키가 사라지므로
        /// 언어를 다시 바꿀 때 조회할 수 있도록 처음 한 번 걷어둔다.
        /// </summary>
        private readonly List<(TextElement Element, string Key)> _localizedTexts = new();

        private bool _textsCollected;

        // 파생 클래스는 Start를 선언하지 말고 InitPanel을 오버라이드할 것 — Unity는 같은 이름의 메시지가
        // 파생에도 있으면 파생 쪽만 호출하는데, 여기가 private라 컴파일러가 hides 경고조차 내지 않는다
        // (그러면 번역과 InitPanel이 통째로 조용히 실행되지 않는다).
        private void Start()
        {
            ValidateRoot();

            // 번역이 InitPanel보다 먼저다. 순서를 바꾸면 InitPanel이 채운 런타임 값(캐릭터 이름·숫자) 때문에
            // 그 엘리먼트가 "ui.…" 키를 잃어 수집 대상에서 빠지고, 언어를 바꿔도 영영 갱신되지 않는다.
            LocalizeTree();
            InitPanel();
        }

        /// <summary>배선이 빠졌으면 원인부터 한 번만 보고한다(엘리먼트 조회 실패는 Require가 따로 알린다).</summary>
        private void ValidateRoot()
        {
            if (NullCheck.LogIfMissing(_document, nameof(_document), this, "패널을 표시할 수 없습니다"))
            {
                return;
            }

            NullCheck.LogIfNull(Root, nameof(Root), this, $"UXML에서 '{RootElementName}' 엘리먼트를 찾지 못했습니다");
        }

        /// <summary>
        /// UXML에서 요소를 찾고, 없으면 그 자리에서 로그를 남긴다.
        ///
        /// 요소 이름 오타는 인스펙터 검증으로도 <c>OnValidate</c>로도 잡을 수 없고(트리가 런타임에만 존재한다)
        /// 화면이 조용히 비는 것으로만 드러나므로, 1회 대입 지점인 여기서 즉시 알린다.
        /// 반환값을 쓰는 쪽은 평소처럼 null을 건너뛰면 된다(런타임 가드는 로그를 남기지 않는다).
        /// </summary>
        /// <param name="consequence">"보유 포인트 표기가 갱신되지 않습니다"처럼 누락 시 무슨 일이 벌어지는지(선택).</param>
        protected T Require<T>(string elementName, string consequence = null) where T : VisualElement
        {
            // 루트 자체가 없는 경우는 ValidateRoot가 이미 보고했다 — 같은 원인으로 N번 더 찍지 않는다.
            if (Root == null)
            {
                return null;
            }

            T element = Root.Q<T>(elementName);
            NullCheck.LogIfNull(element, $"'{elementName}' 엘리먼트", this, consequence);

            return element;
        }

        /// <summary>
        /// 클래스 이름으로 요소 여럿을 찾고, 하나도 없으면 로그를 남긴다
        /// (선택지 카드처럼 개수가 UXML에 달려 있어 이름으로 하나씩 찾을 수 없는 경우).
        /// </summary>
        protected List<T> RequireAll<T>(string className, string consequence = null) where T : VisualElement
        {
            if (Root == null)
            {
                return new List<T>();
            }

            List<T> elements = Root.Query<T>(className: className).ToList();
            NullCheck.LogIfEmpty(elements, $"'.{className}' 요소", this, consequence, inspector: false);

            return elements;
        }

        /// <summary>
        /// 버튼을 찾아 클릭을 연결한다. 없으면 로그만 남기고 넘어가므로
        /// <see cref="InitPanel"/>의 나머지 배선이 그 자리에서 끊기지 않는다.
        /// </summary>
        protected void BindButton(string elementName, Action onClick)
        {
            Button button = Require<Button>(elementName, "이 버튼이 동작하지 않습니다");
            if (button != null)
            {
                button.clicked += onClick;
            }
        }

        /// <summary>Root(BasePanel의 UIDocument)와 번역이 준비된 뒤 이 메소드로 Panel 셋팅.</summary>
        protected virtual void InitPanel() { }

        // private가 아니라 protected virtual인 이유: Unity는 같은 이름의 메시지가 파생 클래스에도 있으면
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
            // 배선 누락은 ValidateRoot가 보고하므로 여기서는 조용히 넘어간다.
            if (Root == null)
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
            if (Root == null)
            {
                return;
            }

            Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public virtual void Show() => SetVisible(true);
        public virtual void Hide() => SetVisible(false);
    }
}
