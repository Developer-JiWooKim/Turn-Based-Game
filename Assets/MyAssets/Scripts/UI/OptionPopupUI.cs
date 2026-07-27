using System;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 옵션 팝업의 화면 담당. 어떤 요소가 어떤 설정에 대응하는지만 알고,
    /// 값의 적용·저장은 <see cref="GameSettings"/>에, 키 설정 목록은 <see cref="KeybindListView"/>에 위임한다.
    /// </summary>
    public class OptionPopupUI : BasePanelUI
    {
        private readonly KeybindListView _keybinds = new();

        protected override void Start()
        {
            Root.Q<Button>("option-close-button").clicked += Hide;

            _keybinds.Build(Root.Q<VisualElement>("option-keybinds"), Root.Q<Button>("keybind-reset-button"));

            // 드래그하는 내내 GameSettings에 대입하면 저장값 갱신과 실제 적용이 함께 일어난다.
            // 파일 저장은 팝업을 닫을 때 1회만(Hide).
            BindSlider("master-volume-slider", GameSettings.MasterVolume, value => GameSettings.MasterVolume = value);
            BindSlider("bgm-volume-slider", GameSettings.BgmVolume, value => GameSettings.BgmVolume = value);
            BindSlider("sfx-volume-slider", GameSettings.SfxVolume, value => GameSettings.SfxVolume = value);
        }

        public override void Hide()
        {
            base.Hide();
            GameSettings.Flush();
        }

        /// <summary>슬라이더 초기값을 채운 뒤 변경 콜백을 건다.</summary>
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
