using System;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public class OptionPopupUI : BasePanelUI
    {
        protected override void Start()
        {
            Root.Q<Button>("option-close-button").clicked += Hide;

            OptionsData options = SaveService.Current.Options;

            // 슬라이더를 드래그하는 내내 AudioManager에 즉시 반영하고 세이브 메모리 값을 갱신한다.
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

        private void BindSlider1(string elementName, float initial, Action<float> onChange)
        {
            Slider slider = Root.Q<Slider>(elementName);

            slider.value = initial;
            slider.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        }
    }
}
