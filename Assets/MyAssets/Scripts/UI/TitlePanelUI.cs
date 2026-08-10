using System;
using Assets.MyAssets.Scripts.Localization;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public sealed class TitlePanelUI : BasePanelUI
    {
        // Button Click Events
        public event Action OnPlayClicked;
        public event Action OnOptionClicked;
        public event Action OnQuitClicked;

        /// <summary>로고와 그 그림자. 같은 이미지를 쓰므로 언어에 따라 <b>둘 다</b> 바꾼다.</summary>
        private VisualElement _logo;
        private VisualElement _logoShadow;

        protected override void Start()
        {
            base.Start(); // UXML 문구 번역

            _logo = Root.Q<VisualElement>("game-title");
            _logoShadow = Root.Q<VisualElement>("game-title-shadow");
            ApplyLogoLanguage();

            Root.Q<Button>("start-button").clicked += () => OnPlayClicked?.Invoke();
            Root.Q<Button>("option-button").clicked += () => OnOptionClicked?.Invoke();
            Root.Q<Button>("quit-button").clicked += () => OnQuitClicked?.Invoke();
        }

        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            ApplyLogoLanguage();
        }

        /// <summary>
        /// 한국어면 한글 로고로 바꾼다. 이미지 경로는 USS가 들고 있고 여기서는 클래스만 토글한다 —
        /// 코드가 텍스처를 직접 참조하면 인스펙터 슬롯이 늘고 USS의 로고 규칙이 둘로 갈라진다.
        /// </summary>
        private void ApplyLogoLanguage()
        {
            bool korean = Loc.Language == LanguageCode.Ko;

            _logo?.EnableInClassList("title-logo--ko", korean);
            _logoShadow?.EnableInClassList("title-logo-shadow--ko", korean);
        }
    }
}
