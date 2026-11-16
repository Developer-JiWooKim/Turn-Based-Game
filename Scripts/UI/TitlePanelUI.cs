using System;
using Assets.MyAssets.Scripts.Localization;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 타이틀 화면 패널
    /// </summary>
    public sealed class TitlePanelUI : BasePanelUI
    {
        private VisualElement _logo;
        private VisualElement _logoShadow;

        private Button _continueButton;

        private bool _continueEnabled;

        // Button Click Events
        public event Action OnPlayClicked;
        public event Action OnContinueClicked;
        public event Action OnOptionClicked;
        public event Action OnQuitClicked;

        protected override void InitPanel()
        {
            _logo = Require<VisualElement>("game-title", "로고가 언어에 따라 바뀌지 않습니다");
            _logoShadow = Require<VisualElement>("game-title-shadow", "로고 그림자가 언어에 따라 바뀌지 않습니다");
            ApplyLogoLanguage();

            BindButton("start-button", () => OnPlayClicked?.Invoke());
            BindButton("option-button", () => OnOptionClicked?.Invoke());
            BindButton("quit-button", () => OnQuitClicked?.Invoke());

            _continueButton = Require<Button>("continue-button", "이어하기를 누를 수 없습니다");
            if (_continueButton != null)
            {
                _continueButton.clicked += () => OnContinueClicked?.Invoke();
                _continueButton.SetEnabled(_continueEnabled);
            }
        }

        public void SetContinueEnabled(bool enabled)
        {
            _continueEnabled = enabled;
            _continueButton?.SetEnabled(enabled);
        }

        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            ApplyLogoLanguage();
        }

        /// <summary>설정된 언어에 맞게 로고를 이미지를 변경(Ko ver, En ver).</summary>
        private void ApplyLogoLanguage()
        {
            bool korean = Loc.Language == LanguageCode.Ko;

            _logo?.EnableInClassList("title-logo--ko", korean);
            _logoShadow?.EnableInClassList("title-logo-shadow--ko", korean);
        }
    }
}
