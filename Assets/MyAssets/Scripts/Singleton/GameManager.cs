using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Run;
using Assets.MyAssets.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.MyAssets.Scripts.Singleton
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private FadeScreenEffect _fadeScreenEffect; // 씬 전환 시 Fade 효과를 연출할 컴포넌트

        // 현재 진행 중인 런 데이터(파티/스테이지) 
        public RunData CurrentRun { get; private set; } // 캐릭터 선택 후 생성되어 씬을 넘어 유지

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 선택한 시작 캐릭터로 새 런을 시작 - BattleScene에서 선택한 파티로 게임 시작
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
            await _fadeScreenEffect.FadeOutAsync();
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 씬이 로드 되면 다시 페이드 인 효과 적용
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _ = _fadeScreenEffect.FadeInAsync();
        }

        public void GameExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}