using UnityEngine;

namespace Assets.MyAssets.Scripts.Systems
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }
        protected bool IsValidInstance => Instance == this;

        protected virtual void Awake()
        {

            // 이미 다른 인스턴스가 있으면 이 중복 오브젝트를 파괴한다.
            // (기존 조건은 Instance == this라 중복이 파괴되지 않고 Instance만 덮어써져,
            //  씬을 왕복할 때마다 DontDestroyOnLoad 오브젝트가 하나씩 쌓이는 버그가 있었다.)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
