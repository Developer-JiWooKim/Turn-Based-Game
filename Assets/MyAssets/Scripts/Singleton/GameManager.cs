using Assets.MyAssets.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.MyAssets.Scripts.Singleton
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private FadeScreenEffect _fadeScreenEffect; // 씬 전환 시 Fade 효과를 연출할 컴포넌트

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

