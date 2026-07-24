using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public class OptionPopupUI : BasePanelUI
    {
        // 현재 키를 표시하는 버튼들(재설정 후 갱신용). 인덱스 = InputManager의 논리 컨트롤 인덱스
        private readonly List<Button> _keyButtons = new();

        protected override void Start()
        {
            Root.Q<Button>("option-close-button").clicked += Hide;

            BuildKeybindRows();

            OptionsData options = SaveService.Current.Options;

            // 슬라이더를 드래그하는 내내 AudioManager에 즉시 반영하고 세이브 메모리 값을 갱신
            // 파일 저장은 팝업을 닫을 때 1회만(Hide)
            BindSlider("master-volume-slider", options.MasterVolume, value =>
            {
                SaveService.Current.Options.MasterVolume = value;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetMasterVolume(value);
            });

            BindSlider("bgm-volume-slider", options.BgmVolume, value =>
            {
                SaveService.Current.Options.BgmVolume = value;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetBgmVolume(value);
            });

            BindSlider("sfx-volume-slider", options.SfxVolume, value =>
            {
                SaveService.Current.Options.SfxVolume = value;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetSfxVolume(value);
            });
        }

        public override void Hide()
        {
            base.Hide();
            SaveService.Save();
        }

        /// <summary>InputManager의 논리 컨트롤(이전/다음/확정/퍼즈)마다 "이름 + 현재 키 버튼" 행을 만든다.</summary>
        private void BuildKeybindRows()
        {
            VisualElement container = Root.Q<VisualElement>("option-keybinds");
            Button resetButton = Root.Q<Button>("keybind-reset-button");

            InputManager input = InputManager.Instance;
            if (container == null || input == null)
            {
                // InputManager가 없는 씬(테스트 등)에서는 키 설정 UI를 숨겨 빈 섹션이 남지 않게 한다.
                if (container != null) container.style.display = DisplayStyle.None;
                if (resetButton != null) resetButton.style.display = DisplayStyle.None;
                return;
            }

            for (int i = 0; i < input.RebindControlCount; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("keybind-row");

                var nameLabel = new Label(input.GetRebindLabel(i));
                nameLabel.AddToClassList("keybind-name");

                var keyButton = new Button { text = input.GetRebindDisplay(i) };
                keyButton.AddToClassList("btn");
                keyButton.AddToClassList("keybind-key");

                int index = i; // 클로저 캡처 고정
                keyButton.clicked += () => BeginRebind(index);

                row.Add(nameLabel);
                row.Add(keyButton);
                container.Add(row);
                _keyButtons.Add(keyButton);
            }

            if (resetButton != null)
                resetButton.clicked += OnResetBindings;
        }

        /// <summary>버튼을 눌러 다음 키 입력을 대기한다(InputManager가 캡처·저장까지 처리).</summary>
        private void BeginRebind(int index)
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            Button keyButton = _keyButtons[index];
            keyButton.text = "...";
            keyButton.AddToClassList("keybind-key--waiting");

            input.StartRebind(index, () =>
            {
                keyButton.RemoveFromClassList("keybind-key--waiting");
                RefreshKeyLabels();
            });
        }

        private void OnResetBindings()
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            input.ResetBindings();
            RefreshKeyLabels();
        }

        private void RefreshKeyLabels()
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            for (int i = 0; i < _keyButtons.Count; i++)
                _keyButtons[i].text = input.GetRebindDisplay(i);
        }

        /// <summary>슬라이더 초기값을 세이브에서 채운 뒤 변경 콜백을 건다.</summary>
        private void BindSlider(string elementName, float initial, Action<float> onChange)
        {
            Slider slider = Root.Q<Slider>(elementName);
            if (slider is null)
            {
                UnityEngine.Debug.Log("[OptionPopupUI] BindSlider(): slider is null");
                return;
            }

            slider.value = initial;
            slider.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        }
    }
}
