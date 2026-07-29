using Assets.MyAssets.Scripts.Progression.Save;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 옵션 값의 단일 진입점. "저장값 갱신 + 실제 적용"을 한곳에 모아,
    /// 옵션 UI는 값만 대입하고 적용 규칙을 알지 않아도 되게 한다.
    ///
    /// <see cref="SaveService"/>와 같은 static으로 둔 이유: 관리자(<see cref="AudioManager"/>)가
    /// 아직/전혀 없는 상황(부팅 직후, BattleScene 단독 실행)에서도 호출이 안전해야 하기 때문.
    /// 적용 대상이 없으면 저장값만 갱신하고 조용히 넘어간다.
    ///
    /// 파일 저장은 값을 만질 때마다 하지 않고 <see cref="Flush"/>로 한 번에 한다
    /// (슬라이더 드래그 중 매 프레임 디스크에 쓰지 않기 위해).
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
            if (audio == null) return;

            audio.SetMasterVolume(Options.MasterVolume);
            audio.SetBgmVolume(Options.BgmVolume);
            audio.SetSfxVolume(Options.SfxVolume);
        }

        /// <summary>변경된 옵션을 파일에 기록한다(옵션 팝업을 닫을 때 1회).</summary>
        public static void Flush() => SaveService.Save();
    }
}