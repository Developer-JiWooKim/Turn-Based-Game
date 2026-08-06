using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 패널 뷰
    /// prev/next로 로스터 6종을 순환하며, 현재 선택 캐릭터를 외부(전투 시작)에서 참조한다.
    ///
    /// 화면의 각 부분은 전담 컴포넌트가 그린다 — 3D 프리뷰는 <see cref="CharacterPreview"/>,
    /// 스탯 바는 <see cref="CharacterStatBarsView"/>. 이 패널은 "무엇을 보여줄지"만 지시하고
    /// UXML 배선만 여기서 한다(UXML의 주인이 패널이므로).
    /// </summary>
    public class CharacterSelectPanelUI : BasePanelUI
    {
        [Header("3D 캐릭터 프리뷰")]
        [Tooltip("프리뷰 리그·모델을 소유하는 컴포넌트. 비워두면 프리뷰 없이 동작한다.")]
        [SerializeField] private CharacterPreview _preview;

        [Header("로스터")]
        [SerializeField] private CharacterRosterSO _roster;

        public event Action OnBackClicked;
        public event Action OnBattleClicked;
        /// <summary>'성향' 버튼 클릭. GameUIController가 성향 포인트 배분 팝업을 연다.</summary>
        public event Action OnAllocationClicked;

        private readonly CharacterStatBarsView _statBars = new();

        private int _selectedIndex;
        private bool _visible; // 방향키 입력을 이 패널이 보일 때만 받기 위한 플래그
        private Label _nameLabel;
        private VisualElement _icon;
        private List<VisualElement> _dots;

        /// <summary>현재 선택된 캐릭터. 로스터가 비어 있으면 null.</summary>
        public CharacterStatsSO SelectedCharacter =>
            (_roster != null && _roster.Count > 0) ? _roster[_selectedIndex] : null;

        private void Awake() => ValidateReferences();

        /// <summary>
        /// 로스터 누락을 보고한다.
        /// (<see cref="_preview"/>는 비워두면 프리뷰 없이 동작하는 선택 항목이라 대상이 아니다.)
        /// </summary>
        private void ValidateReferences()
        {
            NullCheck.LogIfMissing(_roster, nameof(_roster), this, "선택할 캐릭터가 없습니다");
        }

        protected override void Start()
        {
            // 프리뷰 카메라가 그리는 텍스처를 프리뷰 영역 배경으로 배선한다(UXML의 주인이 패널이라 여기서 처리).
            if (_preview != null && _preview.Texture != null)
            {
                Root.Q<VisualElement>("character-preview").style.backgroundImage =
                    Background.FromRenderTexture(_preview.Texture);
            }

            _nameLabel = Root.Q<Label>("character-name");
            _icon = Root.Q<VisualElement>("character-icon");
            _dots = Root.Q<VisualElement>("indicator").Query<VisualElement>(className: "dot").ToList();

            // 바 길이의 기준은 로스터가 계산한다(밸런싱 값을 UI에 박지 않기 위함).
            _statBars.Build(statPanel: Root.Q<VisualElement>("stat-panel"),
                            ceiling: _roster != null ? _roster.CreateStatCeiling() : null);

            Root.Q<Button>("prev-button").clicked += () => Cycle(-1);
            Root.Q<Button>("next-button").clicked += () => Cycle(1);
            Root.Q<Button>("select-back-button").clicked += () => OnBackClicked?.Invoke();
            Root.Q<Button>("allocation-button").clicked += () => OnAllocationClicked?.Invoke();
            Root.Q<Button>("battle-button").clicked += () => OnBattleClicked?.Invoke();

            ApplySelection(); // 초기 선택(0) 반영 + 프리뷰 통지
        }

        private void Update()
        {
            // 패널이 보일 때만 방향키로 좌/우 순환(prev/next 버튼과 동일 동작)
            if (!_visible)
            {
                return;
            }

            InputManager input = InputManager.Instance;
            if (input == null)
            {
                return;
            }

            // 방향키 순환은 prev/next 버튼 클릭과 동등하므로 버튼과 같은 클릭음을 낸다
            // (마우스 클릭은 UiClickSfx가 ClickEvent로 재생하고, 여기선 키보드 경로만 재생).
            if (input.UiNavigateNextPressed)
            {
                Cycle(1); AudioManager.UiClick();
            }
            else if (input.UiNavigatePrevPressed)
            {
                Cycle(-1); AudioManager.UiClick();
            }
        }

        private void Cycle(int direction)
        {
            if (_roster == null || _roster.Count == 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + direction + _roster.Count) % _roster.Count;
            ApplySelection();
        }

        /// <summary>선택이 바뀌었을 때 화면 각 부분에 새 캐릭터를 통지한다.</summary>
        private void ApplySelection()
        {
            CharacterStatsSO character = SelectedCharacter;
            if (character == null)
            {
                return;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = character.DisplayName;
            }

            if (_icon != null)
            {
                _icon.style.backgroundImage = character.Icon != null ? Background.FromSprite(character.Icon) : default;
            }

            if (_dots != null)
            {
                for (int i = 0; i < _dots.Count; i++)
                {
                    _dots[i].EnableInClassList(className: "dot-active", enable: i == _selectedIndex);
                }
            }

            _statBars.Refresh(character.CreateStats());

            if (_preview != null)
            {
                _preview.Show(character);
            }
        }

        public override void Show()
        {
            base.Show();
            _visible = true;
            SetPreviewActive(active: true);
        }

        public override void Hide()
        {
            base.Hide();
            _visible = false;
            SetPreviewActive(active: false);
        }

        private void SetPreviewActive(bool active)
        {
            if (_preview != null)
            {
                _preview.SetVisible(active);
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}
