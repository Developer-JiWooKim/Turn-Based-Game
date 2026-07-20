using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 패널 뷰. 이 패널이 보이는 동안에만 3D 프리뷰 리그를 켜고,
    /// 프리뷰 카메라의 RenderTexture를 프리뷰 영역의 background-image로 배선한다.
    /// prev/next로 로스터 6종을 순환하며, 현재 선택 캐릭터를 외부(전투 시작)에서 참조한다.
    /// </summary>
    public class CharacterSelectPanelUI : BasePanelUI
    {
        [Header("3D 캐릭터 프리뷰 → RenderTexture")]
        [Tooltip("프리뷰 카메라 + 조명을 묶은 루트. 이 패널이 보일 때만 활성화된다.")]
        [SerializeField] private GameObject _previewRig;
        [Tooltip("프리뷰 카메라의 Target Texture. 프리뷰 영역의 background-image로 배선된다.")]
        [SerializeField] private RenderTexture _previewTexture;

        [Header("로스터")]
        [SerializeField] private CharacterRosterSO _roster;

        public event Action OnBackClicked;
        public event Action OnBattleClicked;
        /// <summary>선택이 바뀔 때(초기화 포함) 발생. CharacterPreview 등 연출이 구독한다.</summary>
        public event Action<int, CharacterStatsSO> OnSelectionChanged;

        private int _selectedIndex;
        private Label _nameLabel;
        private List<VisualElement> _dots;

        /// <summary>현재 선택된 캐릭터. 로스터가 비어 있으면 null.</summary>
        public CharacterStatsSO SelectedCharacter =>
            (_roster != null && _roster.Count > 0) ? _roster[_selectedIndex] : null;

        protected override void Start()
        {
            base.Start();

            if (_previewTexture != null)
            {
                Root.Q<VisualElement>("character-preview").style.backgroundImage =
                    Background.FromRenderTexture(_previewTexture);
            }

            _nameLabel = Root.Q<Label>("character-name");
            _dots = Root.Q<VisualElement>("indicator").Query<VisualElement>(className: "dot").ToList();

            Root.Q<Button>("prev-button").clicked += () => Cycle(-1);
            Root.Q<Button>("next-button").clicked += () => Cycle(1);
            Root.Q<Button>("select-back-button").clicked += () => OnBackClicked?.Invoke();
            Root.Q<Button>("battle-button").clicked += () => OnBattleClicked?.Invoke();

            ApplySelection(); // 초기 선택(0) 반영 + 프리뷰 통지
        }

        private void Cycle(int direction)
        {
            if (_roster == null || _roster.Count == 0) return;
            _selectedIndex = (_selectedIndex + direction + _roster.Count) % _roster.Count;
            ApplySelection();
        }

        private void ApplySelection()
        {
            CharacterStatsSO character = SelectedCharacter;
            if (character == null) return;

            if (_nameLabel != null)
                _nameLabel.text = character.DisplayName;

            if (_dots != null)
            {
                for (int i = 0; i < _dots.Count; i++)
                    _dots[i].EnableInClassList("dot-active", i == _selectedIndex);
            }

            OnSelectionChanged?.Invoke(_selectedIndex, character);
        }

        public override void Show()
        {
            base.Show();
            SetPreviewRigActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            SetPreviewRigActive(false);
        }

        private void SetPreviewRigActive(bool active)
        {
            if (_previewRig != null)
                _previewRig.SetActive(active);
        }
    }
}
