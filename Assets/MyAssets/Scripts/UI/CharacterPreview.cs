using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 화면의 3D 프리뷰 전담 컴포넌트.
    /// 프리뷰 리그(카메라+조명) · 렌더 텍스처 · 모델 인스턴스를 전부 소유하며,
    /// <see cref="CharacterSelectPanelUI"/>는 이 컴포넌트에 "무엇을 보여줄지"만 지시한다.
    /// </summary>
    public sealed class CharacterPreview : MonoBehaviour
    {
        [Header("PreviewRig")]
        [Tooltip("프리뷰 카메라 + 조명을 묶은 루트. 선택 화면이 보일 때만 활성화된다.\n" +
                 "모델 앵커가 이 아래에 있으므로 리그를 끄면 캐시된 모델도 함께 멈춘다.")]
        [SerializeField] private GameObject _previewRig;

        [Tooltip("프리뷰 카메라의 Target Texture. 패널이 프리뷰 영역의 background-image로 배선한다.")]
        [SerializeField] private RenderTexture _previewTexture;

        [Tooltip("프리뷰 모델이 생성될 위치(프리뷰 카메라 앞).")]
        [SerializeField] private Transform _modelAnchor;

        /// <summary>캐릭터별 프리뷰 인스턴스 캐시. 처음 선택될 때 만들어 이후 재사용한다.</summary>
        private readonly Dictionary<CharacterStatsSO, GameObject> _instances = new();

        private GameObject _current;

        private bool _isValid;

        /// <summary>UI에 배선할 렌더 텍스처(비어 있으면 null). 배선 자체는 UXML을 소유한 패널이 한다.</summary>
        public RenderTexture Texture => _previewTexture;

        private void Awake()
        {
            _isValid = ValidateReferences();

            SetVisible(false); // 패널이 열릴 때 켜지도록 초기 상태는 꺼둠
        }

        /// <summary>
        /// 필수 인스펙터 연결을 검사하고 누락을 보고한다.
        /// </summary>
        /// <returns>전부 연결돼 있으면 true</returns>
        private bool ValidateReferences()
        {
            bool hasError = false;
            hasError |= NullCheck.LogIfMissing(_previewRig, nameof(_previewRig), this);
            hasError |= NullCheck.LogIfMissing(_modelAnchor, nameof(_modelAnchor), this);
            return !hasError;
        }

        /// <summary>프리뷰 리그를 켜고 끈다(선택 화면 진입/이탈). 꺼진 동안에는 모델도 함께 멈춘다.</summary>
        public void SetVisible(bool visible)
        {
            if (!_isValid) return;

            _previewRig.SetActive(visible);
        }

        /// <summary>지정한 캐릭터의 모델을 보여준다(이전 모델은 숨김). null이면 아무것도 표시하지 않는다.</summary>
        public void Show(CharacterStatsSO character)
        {
            if (!_isValid) return;

            if (_current != null)
            {
                _current.SetActive(false);
            }

            _current = null;

            if (character == null) return;
            if (character.Prefab == null)
            {
                Debug.LogError($"[CharacterPreview] {character.name}에 프리뷰용 Prefab이 없습니다.", character);
                return;
            }

            _current = GetOrCreate(character);
            _current.SetActive(true);
        }

        /// <summary>이 캐릭터의 프리뷰 인스턴스를 가져온다. 없으면(또는 파괴됐으면) 이때 한 번 만든다.</summary>
        private GameObject GetOrCreate(CharacterStatsSO character)
        {
            if (_instances.TryGetValue(character, out GameObject instance) && instance != null)
                return instance;

            instance = Instantiate(character.Prefab, _modelAnchor.position, _modelAnchor.rotation, _modelAnchor);
            _instances[character] = instance;
            return instance;
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}