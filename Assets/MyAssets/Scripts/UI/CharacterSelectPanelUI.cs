using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
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
    /// 3D 프리뷰(리그·렌더 텍스처·모델)는 <see cref="CharacterPreview"/>가 전부 소유하고,
    /// 이 패널은 "무엇을 보여줄지"만 지시한다 — UXML 배선만 여기서 한다(UXML의 주인이 패널이므로).
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

        private int _selectedIndex;
        private bool _visible; // 방향키 입력을 이 패널이 보일 때만 받기 위한 플래그
        private Label _nameLabel;
        private List<VisualElement> _dots;
        private List<VisualElement> _statFills;
        private List<Label> _statValues;
        /// <summary>바 길이의 기준이 되는 로스터 내 최댓값(HP/ATK/SPD/DEF 순). 비율 스탯은 0~1 고정이라 제외.</summary>
        private Stats _statCeiling;

        /// <summary>현재 선택된 캐릭터. 로스터가 비어 있으면 null.</summary>
        public CharacterStatsSO SelectedCharacter =>
            (_roster != null && _roster.Count > 0) ? _roster[_selectedIndex] : null;

        protected override void Start()
        {
            // 프리뷰 카메라가 그리는 텍스처를 프리뷰 영역 배경으로 배선한다(UXML의 주인이 패널이라 여기서 처리).
            if (_preview != null && _preview.Texture != null)
            {
                Root.Q<VisualElement>("character-preview").style.backgroundImage =
                    Background.FromRenderTexture(_preview.Texture);
            }

            _nameLabel = Root.Q<Label>("character-name");
            _dots = Root.Q<VisualElement>("indicator").Query<VisualElement>(className: "dot").ToList();

            VisualElement statPanel = Root.Q<VisualElement>("stat-panel");
            _statFills = statPanel.Query<VisualElement>("stat-fill").ToList();
            _statValues = statPanel.Query<Label>("stat-value").ToList();
            _statCeiling = CreateStatCeiling();

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
            if (!_visible) return;

            InputManager input = InputManager.Instance;
            if (input == null) return;

            // 방향키 순환은 prev/next 버튼 클릭과 동등하므로 버튼과 같은 클릭음을 낸다
            // (마우스 클릭은 UiClickSfx가 ClickEvent로 재생하고, 여기선 키보드 경로만 재생).
            if (input.UiNavigateNextPressed) { Cycle(1); AudioManager.UiClick(); }
            else if (input.UiNavigatePrevPressed) { Cycle(-1); AudioManager.UiClick(); }
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

            RefreshStats(character.CreateStats());

            if (_preview != null)
                _preview.Show(character);
        }

        /// <summary>
        /// 스탯 바를 현재 캐릭터 수치로 갱신한다. HP/ATK/SPD/DEF는 로스터 최댓값 대비 비율,
        /// 치명타·저항은 그 자체가 0~1 비율이라 % 로 표시한다(UXML의 행 순서와 같은 순서).
        /// </summary>
        private void RefreshStats(Stats s)
        {
            SetStatRow(0, s.MaxHp, _statCeiling.MaxHp, s.MaxHp.ToString());
            SetStatRow(1, s.Atk, _statCeiling.Atk, s.Atk.ToString());
            SetStatRow(2, s.Spd, _statCeiling.Spd, s.Spd.ToString());
            SetStatRow(3, s.Def, _statCeiling.Def, s.Def.ToString());
            SetStatRow(4, s.CritRate, 1f, $"{s.CritRate * 100f:0}%");
            SetStatRow(5, s.Res, 1f, $"{s.Res * 100f:0}%");
        }

        private void SetStatRow(int index, float value, float ceiling, string text)
        {
            if (index < _statFills.Count)
                _statFills[index].style.width = Length.Percent(ceiling > 0f ? Mathf.Clamp01(value / ceiling) * 100f : 0f);

            if (index < _statValues.Count)
                _statValues[index].text = text;
        }

        /// <summary>바 길이의 기준을 로스터 전체의 최댓값에서 구한다(밸런싱 값을 코드에 박지 않기 위함).</summary>
        private Stats CreateStatCeiling()
        {
            var ceiling = new Stats(1, 1, 1, 1, 1f, 1f, 1f);
            for (int i = 0; i < (_roster != null ? _roster.Count : 0); i++)
            {
                if (_roster[i] == null) continue;

                Stats s = _roster[i].CreateStats();
                ceiling.MaxHp = Mathf.Max(ceiling.MaxHp, s.MaxHp);
                ceiling.Atk = Mathf.Max(ceiling.Atk, s.Atk);
                ceiling.Spd = Mathf.Max(ceiling.Spd, s.Spd);
                ceiling.Def = Mathf.Max(ceiling.Def, s.Def);
            }
            return ceiling;
        }

        public override void Show()
        {
            base.Show();
            _visible = true;
            SetPreviewActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            _visible = false;
            SetPreviewActive(false);
        }

        private void SetPreviewActive(bool active)
        {
            if (_preview != null)
                _preview.SetVisible(active);
        }
    }
}
