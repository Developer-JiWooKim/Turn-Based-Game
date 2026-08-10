using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.MyAssets.Scripts.Systems
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("씬 전환 시 Fade 효과를 연출할 컴포넌트")]
        [Tooltip("Fade Canvas는 GameManager의 자식으로 두어야 씬 전환에도 파괴되지 않는다.")]
        [SerializeField] private FadeScreenEffect _fadeScreenEffect;

        [Header("문자열 표")]
        [Tooltip("화면 문구의 언어별 값. 비워두면 UI에 키(ui.…)가 그대로 보이고 SO 표시 이름은 원문으로 나온다.")]
        [SerializeField] private LocalizationTableSO _stringTable;

        // 현재 진행 중인 런 데이터(파티/스테이지)
        public RunData CurrentRun { get; private set; } // 캐릭터 선택 후 생성되어 씬을 넘어 유지

        protected override void Awake()
        {
            base.Awake();
            if (!IsValidInstance)
            {
                return;
            }

            ValidateReferences(); // 인스펙터 연결 누락을 시작 시 1회 보고한다.

            DontDestroyOnLoad(this.gameObject);

            // 저장된 설정을 실제로 적용한다. 볼륨은 AudioManager가 자기 Awake에서 스스로 가져가지만
            // (적용 대상이 자기 자신이라 초기화 순서에 기대지 않으려고), 화면과 언어는 주인이 없어 여기서 민다.
            Loc.SetTable(_stringTable);
            GameSettings.ApplyLocalization();
            GameSettings.ApplyDisplay();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// 인스펙터 연결 누락을 보고한다.
        /// </summary>
        private void ValidateReferences()
        {
            NullCheck.LogIfMissing(_fadeScreenEffect, nameof(_fadeScreenEffect), this, "페이드 없이 씬을 전환합니다");
            NullCheck.LogIfMissing(_stringTable, nameof(_stringTable), this, "UI에 번역 키가 그대로 표시됩니다");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 선택한 시작 캐릭터로 새 런을 시작 - 선택한 캐릭터로 BattleScene에서 게임 시작
        /// </summary>
        public void BeginRun(CharacterStatsSO starter)
        {
            CurrentRun = new RunData(starter);
        }

        /// <summary>
        /// 페이드 아웃 효과 후 씬 전환
        /// </summary>
        public async void LoadScene(string sceneName)
        {
            // 페이드는 연출일 뿐이므로, 없더라도 씬 전환 자체는 반드시 진행한다.
            if (_fadeScreenEffect != null)
            {
                await _fadeScreenEffect.FadeOutAsync();
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 씬이 로드 되면 다시 페이드 인 효과 적용
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_fadeScreenEffect != null)
            {
                _ = _fadeScreenEffect.FadeInAsync();
            }
        }

        public void GameExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}