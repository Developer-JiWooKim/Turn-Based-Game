using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 화면 모드 1종(해상도 + 전체화면 여부). 
    /// </summary>
    public readonly struct DisplayPreset
    {
        public readonly int Width;
        public readonly int Height;
        public readonly FullScreenMode Mode;

        /// <summary>옵션 UI에 표시할 문구의 키(<see cref="Loc"/>).</summary>
        public readonly string LabelKey;

        public DisplayPreset(int width, int height, FullScreenMode mode, string labelKey)
        {
            Width = width;
            Height = height;
            Mode = mode;
            LabelKey = labelKey;
        }
    }

    /// <summary>
    /// 옵션 값의 단일 진입점. 
    /// 
    /// "저장값 갱신 + 실제 적용"을 한곳에 모아, 옵션 UI는 값만 대입하고 적용 규칙을 알지 않아도 되게 한다.
    ///
    /// <see cref="SaveService"/>와 같은 static으로 둔 이유: 관리자(<see cref="AudioManager"/>)가
    /// 아직/전혀 없는 상황(부팅 직후, BattleScene 단독 실행)에서도 호출이 안전해야 하기 때문.
    /// 적용 대상이 없으면 저장값만 갱신하고 조용히 넘어간다.
    ///
    /// 파일 저장은 값을 만질 때마다 하지 않고 <see cref="Flush"/>로 한 번에 한다
    /// </summary>
    public static class GameSettings
    {
        private static OptionsData Options => SaveService.Current.Options;

        // ── 볼륨 ──
        // 값 자체는 세이브가 소유하고, 여기서는 "바뀌면 누구에게 반영되는지"만 정한다.

        public static float MasterVolume
        {
            get => Options.MasterVolume;
            set
            {
                Options.MasterVolume = value;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMasterVolume(value);
                }
            }
        }

        public static float BgmVolume
        {
            get => Options.BgmVolume;
            set
            {
                Options.BgmVolume = value;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetBgmVolume(value);
                }
            }
        }

        public static float SfxVolume
        {
            get => Options.SfxVolume;
            set
            {
                Options.SfxVolume = value;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSfxVolume(value);
                }
            }
        }

        /// <summary>
        /// 저장된 볼륨을 <see cref="AudioManager"/>에 일괄 적용한다.
        /// </summary>
        public static void ApplyAudio()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            audio.SetMasterVolume(Options.MasterVolume);
            audio.SetBgmVolume(Options.BgmVolume);
            audio.SetSfxVolume(Options.SfxVolume);
        }

        // ── 언어 ──
        // 저장은 문자열("ko"/"en"), 화면은 enum을 쓴다. 둘을 잇는 곳은 여기 하나뿐이다.

        public static LanguageCode Language
        {
            get => Loc.Parse(Options.Language);
            set
            {
                Options.Language = Loc.ToSaveValue(value);
                Loc.Language = value; // 값이 실제로 바뀌었을 때만 LanguageChanged가 나간다.
            }
        }

        /// <summary>저장된 언어를 <see cref="Loc"/>에 적용한다.</summary>
        public static void ApplyLocalization() => Loc.Language = Loc.Parse(Options.Language);

        // ── 화면 모드 ──

        /// <summary>
        /// 선택 가능한 화면 모드. 밸런싱 값이 아니라 플랫폼 표시 설정이라 SO가 아니라 코드에 둔다.
        /// 늘릴 때는 <b>뒤에 추가</b>할 것 — 세이브에 인덱스가 저장되므로 순서를 바꾸면 기존 설정이 다른 모드를 가리킨다.
        /// </summary>
        public static readonly DisplayPreset[] DisplayPresets =
        {
            new(1280, 720, FullScreenMode.Windowed, "ui.option.display.windowed720"),
            new(1920, 1080, FullScreenMode.FullScreenWindow, "ui.option.display.fullscreen1080"),
        };

        /// <summary>현재 화면 모드 인덱스. 고른 적이 없으면 -1(현재 해상도 유지).</summary>
        public static int DisplayPresetIndex
        {
            get => Options.ResolutionIndex;
            set
            {
                if (value < 0 || value >= DisplayPresets.Length)
                {
                    return;
                }

                Options.ResolutionIndex = value;
                ApplyDisplay();
            }
        }

        /// <summary>
        /// 저장된 화면 모드를 실제 화면에 적용한다(<c>GameManager.Awake</c>에서 1회).
        /// 고른 적이 없으면(-1) 아무것도 하지 않아 플레이어가 OS/런처로 정한 해상도를 빼앗지 않는다.
        ///
        /// ⚠️ 에디터의 Game 뷰는 <see cref="Screen.SetResolution"/>을 따르지 않는다 — 확인은 빌드에서 해야 한다.
        /// </summary>
        public static void ApplyDisplay()
        {
            int index = Options.ResolutionIndex;
            if (index < 0 || index >= DisplayPresets.Length)
            {
                return;
            }

            DisplayPreset preset = DisplayPresets[index];
            Screen.SetResolution(preset.Width, preset.Height, preset.Mode);
        }

        /// <summary>변경된 옵션을 파일에 기록한다(옵션 팝업을 닫을 때 1회).</summary>
        public static void Flush() => SaveService.Save();
    }
}