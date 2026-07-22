using Assets.MyAssets.Scripts.Audio.Data;
using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// BGM/SFX 재생과 볼륨을 총괄하는 전역 오디오 관리자
    /// </summary>
    public sealed class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioLibrarySO _library;

        [Tooltip("BGM 전환 크로스페이드 시간(초)")]
        [SerializeField] private float _bgmCrossfadeDuration = 1f;

        // 크로스페이드용 BGM 소스 A/B
        private AudioSource _bgmA;
        private AudioSource _bgmB;

        private AudioSource _sfx;
        private AudioSource _activeBgm;
        private AudioClip _currentBgmClip;

        // 크로스페이드가 겹칠 때(빠른 씬 전환 등) 이전 코루틴이 계속 볼륨을 만지지 않도록 세대 번호로 무효화
        private int _bgmGeneration;

        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        // ── 정적 헬퍼: 호출부에서 매번 null 체크하지 않도록 관리자/클립이 없으면 조용히 무시 ──
        public static AudioLibrarySO Library => Instance != null ? Instance._library : null;
        public static void Sfx(AudioClip clip) { if (Instance != null) Instance.PlaySfx(clip); }
        public static void Bgm(AudioClip clip) { if (Instance != null) Instance.PlayBgm(clip); }
        /// <summary>UI 클릭음. 마우스 클릭(UiClickSfx)과 키보드 선택 양쪽에서 같은 소리를 낸다.</summary>
        public static void UiClick() => Sfx(Library?.UiClick);
        /// <summary>방향키로 선택지/카드를 옮길 때의 이동음.</summary>
        public static void UiNavigate() => Sfx(Library?.UiNavigate);
        /// <summary>배틀 타겟 확정 등 확정 순간의 소리.</summary>
        public static void Confirm() => Sfx(Library?.Confirm);

        protected override void Awake()
        {
            base.Awake();
            if (!IsValidInstance) return; // 중복 인스턴스는 base.Awake에서 파괴 예약됨 — 소스 생성/구독을 하지 않는다

            DontDestroyOnLoad(gameObject);

            _bgmA = CreateSource("BGM_A", loop: true);
            _bgmB = CreateSource("BGM_B", loop: true);
            _sfx = CreateSource("SFX", loop: false);
            _activeBgm = _bgmA;

            OptionsData options = SaveService.Current.Options;
            _masterVolume = options.MasterVolume;
            _bgmVolume = options.BgmVolume;
            _sfxVolume = options.SfxVolume;

            ApplyBgmVolume();
            ApplySfxVolume();

            SceneManager.sceneLoaded += OnSceneLoaded;
            // 관리자가 얹혀 있는 부팅 씬은 sceneLoaded가 이미 지나갔을 수 있으므로 직접 한 번 재생한다.
            PlaySceneBgm(SceneManager.GetActiveScene().name);
        }

        protected override void OnDestroy()
        {
            // 유효 인스턴스만 구독했지만, 구독하지 않은(중복) 인스턴스의 -=는 무해한 no-op이라 무조건 해제한다.
            // (base.OnDestroy가 Instance=null로 만들면 IsValidInstance가 false가 되어 조건 해제는 누락되므로.)
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D
            return source;
        }

        // ── BGM ──

        /// <summary>
        /// BGM을 재생한다. 이미 같은 클립이 재생 중이면 무시해 스테이지마다 처음부터 다시 시작되는 것을 막는다.
        /// 다른 클립이면 크로스페이드로 부드럽게 전환한다.
        /// </summary>
        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || _currentBgmClip == clip)
                return;

            _currentBgmClip = clip;
            _ = CrossfadeAsync(clip);
        }

        private async Awaitable CrossfadeAsync(AudioClip next)
        {
            int generation = ++_bgmGeneration;

            AudioSource from = _activeBgm;
            AudioSource to = _activeBgm == _bgmA ? _bgmB : _bgmA;
            _activeBgm = to;

            float target = _masterVolume * _bgmVolume;
            to.clip = next;
            to.volume = 0f;
            to.Play();

            float duration = Mathf.Max(0.01f, _bgmCrossfadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (generation != _bgmGeneration)
                    return; // 더 최신 크로스페이드가 시작됨 — 이 코루틴은 손을 뗀다

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                to.volume = Mathf.Lerp(0f, target, t);
                if (from != null)
                    from.volume = Mathf.Lerp(target, 0f, t);

                await Awaitable.NextFrameAsync();
            }

            if (generation != _bgmGeneration)
                return;

            to.volume = target;
            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }
        }

        private void PlaySceneBgm(string sceneName)
        {
            if (_library != null)
                PlayBgm(_library.GetSceneBgm(sceneName));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlaySceneBgm(scene.name);

        // ── SFX ──

        /// <summary>단일 소스에 PlayOneShot으로 재생 — 동시 재생이 기본 지원된다.</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || _sfx == null)
                return;

            _sfx.PlayOneShot(clip);
        }

        // ── 볼륨(옵션 UI에서 즉시 반영) ──

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyBgmVolume();
            ApplySfxVolume();
        }

        public void SetBgmVolume(float value)
        {
            _bgmVolume = Mathf.Clamp01(value);
            ApplyBgmVolume();
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            ApplySfxVolume();
        }

        private void ApplyBgmVolume()
        {
            if (_activeBgm != null)
                _activeBgm.volume = _masterVolume * _bgmVolume;
        }

        private void ApplySfxVolume()
        {
            if (_sfx != null)
                _sfx.volume = _masterVolume * _sfxVolume;
        }
    }
}
