using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// <see cref="Camera.main"/> 조회 결과를 캐시한다.
    ///
    /// Camera.main은 "MainCamera" 태그를 검색하는 호출이라 매 프레임 쓰면 비용이 쌓인다 —
    /// 체력바 빌보드는 유닛마다 LateUpdate에서 호출하므로 전장에 8유닛이면 초당 수백 회가 된다.
    ///
    /// 캐시된 카메라가 파괴되거나(씬 전환) 비활성화되면 자동으로 다시 조회하므로 수동 무효화가 필요 없다.
    /// </summary>
    public static class MainCameraCache
    {
        private static Camera _camera;
        private static Transform _transform;

        /// <summary>현재 메인 카메라(없으면 null)</summary>
        public static Camera Current
        {
            get
            {
                if (_camera == null || !_camera.isActiveAndEnabled)
                {
                    _camera = Camera.main;
                    _transform = _camera != null ? _camera.transform : null;
                }

                return _camera;
            }
        }

        /// <summary>메인 카메라의 Transform(없으면 null). 캐시된 참조라 매번 .transform을 타지 않는다.</summary>
        public static Transform CurrentTransform
        {
            get
            {
                _ = Current; // 캐시 유효성 검사 및 갱신
                return _transform;
            }
        }

        /// <summary>
        /// 도메인 리로드를 끄고 플레이하면 static 필드가 이전 세션 값을 그대로 들고 있으므로 명시적으로 비운다.
        /// (Enter Play Mode Options를 쓰지 않더라도 무해하다.)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _camera = null;
            _transform = null;
        }
    }
}
