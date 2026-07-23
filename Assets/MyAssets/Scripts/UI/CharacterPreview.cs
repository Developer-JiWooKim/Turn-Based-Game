using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 시 프리뷰 앵커 아래에 해당 캐릭터의 3D 모델을 보여주는 연출 컴포넌트
    /// 프리뷰 카메라가 앵커를 비추고, 카메라의 RenderTexture가 UI로 배선된다.
    ///
    /// 모델은 선택이 바뀔 때마다 만들었다 부수지 않고 캐릭터별로 <b>한 번만 생성해 캐시</b>하고 활성/비활성만 토글한다
    /// (후보가 로스터 6종으로 고정이라 상한이 명확하고, 리깅된 캐릭터 인스턴스화 비용이 방향키 연타에서 그대로 드러난다).
    /// </summary>
    public sealed class CharacterPreview : MonoBehaviour
    {
        [SerializeField] private CharacterSelectPanelUI _panel;

        [Tooltip("프리뷰 모델이 생성될 위치(프리뷰 카메라 앞).")]
        [SerializeField] private Transform _modelAnchor;

        /// <summary>캐릭터별 프리뷰 인스턴스 캐시. 처음 선택될 때 만들어 이후 재사용한다.</summary>
        private readonly Dictionary<CharacterStatsSO, GameObject> _instances = new();

        private GameObject _current;

        private void OnEnable()
        {
            if (_panel == null)
            {
                Debug.Log("[CharacterPreview] OnEnable(): _panel is null");
                return;
            }

            _panel.OnSelectionChanged += OnSelectionChanged;
            Refresh(_panel.SelectedCharacter); // 활성화 시점의 현재 선택으로 즉시 동기화
        }

        private void OnDisable()
        {
            if (_panel != null)
                _panel.OnSelectionChanged -= OnSelectionChanged;

            // 화면을 벗어난 동안 모델과 Animator가 계속 갱신되지 않도록 꺼둔다(다시 켤 때 OnEnable이 되살린다).
            if (_current != null)
                _current.SetActive(false);
        }

        private void OnSelectionChanged(int index, CharacterStatsSO character) => Refresh(character);

        private void Refresh(CharacterStatsSO character)
        {
            if (_current != null)
                _current.SetActive(false);

            _current = null;

            if (character == null || character.Prefab == null || _modelAnchor == null)
                return;

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
    }
}