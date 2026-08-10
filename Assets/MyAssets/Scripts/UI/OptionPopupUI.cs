using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 옵션 팝업의 화면 담당.
    /// 값의 적용·저장은 <see cref="GameSettings"/>에,
    /// 키 설정 목록은 <see cref="KeybindListView"/>에 위임한다.
    /// </summary>
    public class OptionPopupUI : BasePanelUI
    {
        private readonly KeybindListView _keybinds = new();

        /// <summary>화면 모드 버튼(<see cref="GameSettings.DisplayPresets"/>와 같은 순서).</summary>
        private readonly List<Button> _displayButtons = new();

        /// <summary>언어 버튼(<see cref="LanguageOrder"/>와 같은 순서).</summary>
        private readonly List<Button> _languageButtons = new();

        /// <summary>
        /// 언어 버튼의 순서와 표기. 각 언어 이름은 그 언어로 적는 것이 관례라
        /// 번역 대상이 아니고, 그래서 문자열 표가 아니라 여기에 둔다.
        /// </summary>
        private static readonly (LanguageCode Code, string Label)[] LanguageOrder =
        {
            (LanguageCode.Ko, "한국어"),
            (LanguageCode.En, "English"),
        };

        protected override void Start()
        {
            base.Start(); // UXML 문구 번역

            Root.Q<Button>("option-close-button").clicked += Hide;

            _keybinds.Build(container: Root.Q<VisualElement>("option-keybinds"),
                            resetButton: Root.Q<Button>("keybind-reset-button"));

            BuildDisplayToggles();
            BuildLanguageToggles();

            // 드래그하는 내내 GameSettings에 대입하면 저장값 갱신과 실제 적용이 함께 일어난다.
            // 파일 저장은 팝업을 닫을 때 1회만(Hide).
            BindSlider(elementName: "master-volume-slider",
                       initial: GameSettings.MasterVolume,
                       onChange: value => GameSettings.MasterVolume = value);

            BindSlider(elementName: "bgm-volume-slider",
                       initial: GameSettings.BgmVolume,
                       onChange: value => GameSettings.BgmVolume = value);

            // 효과음은 평소 재생 중인 소리가 없어 드래그해도 아무 변화가 들리지 않는다.
            // 손을 뗄 때 한 번 들려줘야 어느 크기로 맞췄는지 귀로 확인할 수 있다.
            BindSlider(elementName: "sfx-volume-slider",
                       initial: GameSettings.SfxVolume,
                       onChange: value => GameSettings.SfxVolume = value,
                       onRelease: AudioManager.UiClick);
        }

        public override void Hide()
        {
            base.Hide();
            GameSettings.Flush();
        }

        /// <summary>화면 모드 이름은 코드가 채우는 문구라 트리 갱신만으로는 바뀌지 않는다.</summary>
        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            RefreshDisplayLabels();
            RefreshToggleStates();
            _keybinds.RefreshNames();
        }

        // ── 화면 모드 ──

        /// <summary>
        /// 버튼 목록을 <see cref="GameSettings.DisplayPresets"/>에서 만든다 —
        /// 프리셋을 늘려도 UXML과 이 코드를 고치지 않게 하기 위함이다.
        /// </summary>
        private void BuildDisplayToggles()
        {
            VisualElement container = Root.Q<VisualElement>("option-display");
            if (NullCheck.LogIfNull(container, nameof(container), this, "UXML에 'option-display'가 없습니다"))
            {
                return;
            }

            for (int i = 0; i < GameSettings.DisplayPresets.Length; i++)
            {
                int index = i; // 클로저 캡처 고정
                Button button = CreateToggle(Loc.Get(GameSettings.DisplayPresets[i].LabelKey),
                                             () => GameSettings.DisplayPresetIndex = index);

                container.Add(button);
                _displayButtons.Add(button);
            }

            RefreshToggleStates();
        }

        private void RefreshDisplayLabels()
        {
            for (int i = 0; i < _displayButtons.Count && i < GameSettings.DisplayPresets.Length; i++)
            {
                _displayButtons[i].text = Loc.Get(GameSettings.DisplayPresets[i].LabelKey);
            }
        }

        // ── 언어 ──

        private void BuildLanguageToggles()
        {
            VisualElement container = Root.Q<VisualElement>("option-language");
            if (NullCheck.LogIfNull(container, nameof(container), this, "UXML에 'option-language'가 없습니다"))
            {
                return;
            }

            foreach ((LanguageCode code, string label) in LanguageOrder)
            {
                LanguageCode language = code; // 클로저 캡처 고정
                Button button = CreateToggle(label, () => GameSettings.Language = language);

                container.Add(button);
                _languageButtons.Add(button);
            }

            RefreshToggleStates();
        }

        // ── 공통 ──

        private Button CreateToggle(string text, Action onClick)
        {
            var button = new Button { text = text };
            button.AddToClassList("btn");
            button.AddToClassList("btn-secondary");
            button.AddToClassList("option-toggle");

            // 선택 표시는 클릭 직후가 아니라 GameSettings에 반영된 뒤의 값으로 다시 그린다
            // (프리셋 범위를 벗어나는 등으로 대입이 무시될 수 있으므로).
            button.clicked += () =>
            {
                onClick();
                RefreshToggleStates();
            };

            return button;
        }

        /// <summary>어느 버튼이 선택 상태인지를 저장값 기준으로 다시 칠한다.</summary>
        private void RefreshToggleStates()
        {
            for (int i = 0; i < _displayButtons.Count; i++)
            {
                _displayButtons[i].EnableInClassList("option-toggle--active", i == GameSettings.DisplayPresetIndex);
            }

            for (int i = 0; i < _languageButtons.Count; i++)
            {
                _languageButtons[i].EnableInClassList("option-toggle--active", LanguageOrder[i].Code == GameSettings.Language);
            }
        }

        /// <summary>슬라이더 초기값을 채운 뒤 변경 콜백을 건다.</summary>
        /// <param name="onRelease">드래그를 끝냈을 때 1회 호출(선택). 값이 바뀌는 내내가 아니라 손을 뗄 때만 필요한 피드백용.</param>
        private void BindSlider(string elementName, float initial, Action<float> onChange, Action onRelease = null)
        {
            Slider slider = Root.Q<Slider>(elementName);
            if (NullCheck.LogIfNull(slider, nameof(slider), this, $"UXML에 '{elementName}' 슬라이더가 없습니다"))
            {
                return;
            }

            slider.value = initial;
            slider.RegisterValueChangedCallback(evt => onChange(evt.newValue));

            if (onRelease != null)
            {
                slider.RegisterCallback<PointerUpEvent>(_ => onRelease());
            }
        }
    }
}
