using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
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
        private List<VisualElement> _statFills;
        private List<Label> _statValues;
        /// <summary>바 길이의 기준이 되는 로스터 내 최댓값(HP/ATK/SPD/DEF 순). 비율 스탯은 0~1 고정이라 제외.</summary>
        private Stats _statCeiling;

        /// <summary>현재 선택된 캐릭터. 로스터가 비어 있으면 null.</summary>
        public CharacterStatsSO SelectedCharacter =>
            (_roster != null && _roster.Count > 0) ? _roster[_selectedIndex] : null;

        protected override void Start()
        {
            if (_previewTexture != null)
            {
                Root.Q<VisualElement>("character-preview").style.backgroundImage =
                    Background.FromRenderTexture(_previewTexture);
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

            RefreshStats(character.CreateStats());
            OnSelectionChanged?.Invoke(_selectedIndex, character);
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
