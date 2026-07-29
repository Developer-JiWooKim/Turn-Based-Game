using System;
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

        protected override void Start()
        {
            Root.Q<Button>("option-close-button").clicked += Hide;

            _keybinds.Build(container: Root.Q<VisualElement>("option-keybinds"),
                            resetButton: Root.Q<Button>("keybind-reset-button"));

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

        /// <summary>슬라이더 초기값을 채운 뒤 변경 콜백을 건다.</summary>
        /// <param name="onRelease">드래그를 끝냈을 때 1회 호출(선택). 값이 바뀌는 내내가 아니라 손을 뗄 때만 필요한 피드백용.</param>
        private void BindSlider(string elementName, float initial, Action<float> onChange, Action onRelease = null)
        {
            Slider slider = Root.Q<Slider>(elementName);
            if (NullCheck.LogIfNull(slider, nameof(slider), this, $"UXML에 '{elementName}' 슬라이더가 없습니다"))
                return;

            slider.value = initial;
            slider.RegisterValueChangedCallback(evt => onChange(evt.newValue));

            if (onRelease != null)
                slider.RegisterCallback<PointerUpEvent>(_ => onRelease());
        }
    }
}
