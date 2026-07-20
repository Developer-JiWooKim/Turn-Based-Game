using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 시 프리뷰 앵커 아래에 해당 캐릭터의 3D 모델을 생성/교체한다.
    /// 프리뷰 카메라가 이 앵커를 비추고, 카메라의 RenderTexture가 UI로 배선된다.
    /// (선택 로직은 CharacterSelectPanelUI가 담당하고, 여기서는 모델 연출만.)
    /// </summary>
    public sealed class CharacterPreview : MonoBehaviour
    {
        [SerializeField] private CharacterSelectPanelUI _panel;
        [Tooltip("프리뷰 모델이 생성될 위치(프리뷰 카메라 앞).")]
        [SerializeField] private Transform _modelAnchor;

        private GameObject _current;

        private void OnEnable()
        {
            if (_panel == null) return;
            _panel.OnSelectionChanged += OnSelectionChanged;
            Refresh(_panel.SelectedCharacter); // 활성화 시점의 현재 선택으로 즉시 동기화
        }

        private void OnDisable()
        {
            if (_panel != null)
                _panel.OnSelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(int index, CharacterStatsSO character) => Refresh(character);

        private void Refresh(CharacterStatsSO character)
        {
            if (_current != null)
                Destroy(_current);

            if (character == null || character.Prefab == null || _modelAnchor == null)
                return;

            _current = Instantiate(character.Prefab, _modelAnchor.position, _modelAnchor.rotation, _modelAnchor);
        }
    }
}
