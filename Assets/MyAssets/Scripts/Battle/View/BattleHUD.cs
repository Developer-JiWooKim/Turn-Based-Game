using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 턴 순서 칩 하나에 필요한 값. 라벨은 유닛 이름이 아니라 화면 배치 인덱스("A1")라
    /// 칩과 화면 위 유닛을 눈으로 맞출 수 있다(같은 캐릭터를 2명 영입해도 구분된다).
    /// 라벨을 아는 건 View이고 HUD에는 레지스트리가 없어 <see cref="BattlePresenter"/>가 채워 넘긴다.
    /// </summary>
    public readonly struct TurnChipInfo
    {
        public readonly int UnitId;
        public readonly string Label;
        public readonly TeamSide Team;

        public TurnChipInfo(int unitId, string label, TeamSide team)
        {
            UnitId = unitId;
            Label = label;
            Team = team;
        }
    }

    /// <summary>
    /// 전투 화면 HUD(UI Toolkit). 스테이지·턴 순서·현재 행동 유닛·플레이어 차례 프롬프트를 표시하는
    /// 수동 View다. Core를 직접 구독하지 않고 BattleDirector가 밀어넣어 갱신한다
    /// (시뮬레이션이 웨이브마다 재생성되므로 구동 주체를 BattleDirector로 통일).
    /// </summary>
    public sealed class BattleHUD : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Label _stageLabel;
        private VisualElement _turnOrder;
        private Label _playerPrompt;

        /// <summary>우상단 버튼 줄 전체. 전투 구간에서만 보인다(개별 버튼을 따로 켜고 끄지 않는다).</summary>
        private VisualElement _buttonBar;

        /// <summary>시너지·선택 기록·스탯 토글의 눈 아이콘. 켜짐/꺼짐 그림은 USS가 들고 여기서는 클래스만 바꾼다.</summary>
        private VisualElement _synergyToggleIcon;
        private VisualElement _pickToggleIcon;
        private VisualElement _partyToggleIcon;

        /// <summary>플레이어가 버튼으로 끄지 않았는지. 표는 이 값과 "보여줄 내용이 있는지"를 함께 보고 그려진다.</summary>
        private bool _synergyVisible = true;
        private bool _choicePicksVisible = true;
        private bool _partyStatsVisible = true;

        private readonly Dictionary<int, VisualElement> _chips = new();

        /// <summary>하단 파티 스탯 바. 화면 일부의 표현 전담이라 순수 C# View로 두고 여기서 들고 쓴다.</summary>
        private readonly PartyStatusBarView _partyStatus = new();

        /// <summary>좌상단 시너지 패널(위와 같은 이유로 별도 View).</summary>
        private readonly SynergyPanelView _synergyPanel = new();

        /// <summary>시너지 표 바로 아래의 선택지 횟수 패널(위와 같은 이유로 별도 View).</summary>
        private readonly ChoicePickPanelView _choicePicks = new();

        /// <summary>우상단 퍼즈 버튼이 눌렸다(구독자는 <see cref="BattlePausePanel"/>).</summary>
        public event Action PauseClicked;

        /// <summary>우상단 몬스터 정보 버튼이 눌렸다 — Tab과 같은 토글이다(구독자는 <see cref="MonsterInfoPanel"/>).</summary>
        public event Action MonsterInfoClicked;

        private void Awake()
        {
            // BasePanelUI가 아니라 자체 UIDocument를 쓰는 수동 View라 Require 헬퍼를 못 쓴다.
            // 대신 같은 규칙을 따른다 — UXML 조회는 1회 대입 지점인 여기서 전부 보고한다
            // (이름 오타는 인스펙터 검증으로도 OnValidate로도 잡히지 않는다).
            if (NullCheck.LogIfMissing(_document, nameof(_document), this, "HUD를 표시할 수 없습니다"))
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            _stageLabel = Find<Label>(root, "stage-label", "스테이지 번호가 표시되지 않습니다");
            _turnOrder = Find<VisualElement>(root, "turn-order", "턴 순서 칩이 표시되지 않습니다");
            _playerPrompt = Find<Label>(root, "player-prompt", "플레이어 차례 안내가 표시되지 않습니다");

            _partyStatus.Build(Find<VisualElement>(root, "party-status", "하단 파티 스탯이 표시되지 않습니다"));
            _synergyPanel.Build(Find<VisualElement>(root, "synergy-panel", "시너지 패널이 표시되지 않습니다"));
            _choicePicks.Build(Find<VisualElement>(root, "choice-pick-panel", "선택지 선택 횟수가 표시되지 않습니다"));

            BindButtons(root);
            HidePrompt();
        }

        /// <summary>
        /// 우상단 버튼 줄을 배선한다. 퍼즈·몬스터 정보는 밖으로 알리기만 하고(패널이 각자 처리),
        /// 시너지·스탯 토글은 대상 View를 <see cref="BattleHUD"/>가 이미 들고 있으므로 여기서 직접 처리한다.
        /// </summary>
        private void BindButtons(VisualElement root)
        {
            _buttonBar = Find<VisualElement>(root, "hud-buttons", "HUD 버튼이 표시되지 않습니다");

            Bind(root, "pause-button", "퍼즈 버튼을 누를 수 없습니다", () => PauseClicked?.Invoke());
            Bind(root, "monster-info-button", "몬스터 정보 버튼을 누를 수 없습니다", () => MonsterInfoClicked?.Invoke());
            Bind(root, "synergy-toggle", "시너지 표를 끄고 켤 수 없습니다", ToggleSynergy);
            Bind(root, "pick-toggle", "선택 기록 표를 끄고 켤 수 없습니다", ToggleChoicePicks);
            Bind(root, "party-toggle", "파티 스탯 표를 끄고 켤 수 없습니다", TogglePartyStats);

            _synergyToggleIcon = Find<VisualElement>(root, "synergy-toggle-icon", "시너지 토글 아이콘이 표시되지 않습니다");
            _pickToggleIcon = Find<VisualElement>(root, "pick-toggle-icon", "선택 기록 토글 아이콘이 표시되지 않습니다");
            _partyToggleIcon = Find<VisualElement>(root, "party-toggle-icon", "스탯 토글 아이콘이 표시되지 않습니다");

            // 토글 이름은 UXML이 아니라 여기서 채운다 — BattleHUD는 BasePanelUI가 아니라
            // UXML의 "ui.…" 키를 자동으로 번역해주는 경로가 없다(하단 스탯 행 라벨과 같은 처리).
            SetText(root, "synergy-toggle-label", Loc.Get("ui.hud.synergyToggle"));
            SetText(root, "pick-toggle-label", Loc.Get("ui.hud.picksToggle"));
            SetText(root, "party-toggle-label", Loc.Get("ui.hud.statsToggle"));

            RefreshToggleIcons();
        }

        /// <summary>버튼을 찾아 클릭을 연결한다(<c>BasePanelUI.BindButton</c>과 같은 역할).</summary>
        private void Bind(VisualElement root, string elementName, string consequence, Action onClick)
        {
            Button button = Find<Button>(root, elementName, consequence);
            if (button != null)
            {
                button.clicked += onClick;
            }
        }

        private void SetText(VisualElement root, string elementName, string text)
        {
            Label label = Find<Label>(root, elementName, "버튼 이름이 표시되지 않습니다");
            if (label != null)
            {
                label.text = text;
            }
        }

        /// <summary>BasePanelUI의 Require와 같은 역할. 없으면 그 자리에서 알린다.</summary>
        private T Find<T>(VisualElement root, string elementName, string consequence) where T : VisualElement
        {
            T element = root?.Q<T>(elementName);
            NullCheck.LogIfNull(element, $"'{elementName}' 엘리먼트", this, consequence);

            return element;
        }

        /// <summary>이번 스테이지의 파티를 하단 스탯 바에 배정한다(각 캐릭터 위치 아래로 정렬).</summary>
        public void SetParty(IReadOnlyList<PartyMemberSlot> slots) => _partyStatus.SetParty(slots);

        /// <summary>하단 스탯 바를 현재 유효 스탯으로 다시 그린다(상태이상·시너지 변동 시).</summary>
        public void RefreshPartyStats() => _partyStatus.Refresh();

        /// <summary>우상단 버튼 줄 노출 여부. 전투 중이 아닐 때(선택지·결과 화면) 숨긴다.</summary>
        public void SetButtonBarVisible(bool visible)
        {
            if (_buttonBar != null)
            {
                _buttonBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// 시너지 표를 끄고 켠다. 전투 화면이 가려서 답답할 때 잠깐 치우기 위한 것이라
        /// 상태는 런 내내(HUD 수명 동안) 유지된다 — 스테이지가 바뀌어도 껐던 표가 다시 켜지지 않는다.
        /// </summary>
        private void ToggleSynergy()
        {
            _synergyVisible = !_synergyVisible;
            _synergyPanel.SetVisible(_synergyVisible);
            RefreshToggleIcons();
        }

        /// <summary>
        /// 선택 기록 표를 끄고 켠다. 시너지 토글과 <b>별도</b>다 — 두 표는 같은 세로 열에 있지만
        /// 하나를 껐다고 다른 하나까지 사라지면 "무엇을 껐는지" 알 수 없다.
        /// </summary>
        private void ToggleChoicePicks()
        {
            _choicePicksVisible = !_choicePicksVisible;
            _choicePicks.SetVisible(_choicePicksVisible);
            RefreshToggleIcons();
        }

        private void TogglePartyStats()
        {
            _partyStatsVisible = !_partyStatsVisible;
            _partyStatus.SetVisible(_partyStatsVisible);
            RefreshToggleIcons();
        }

        /// <summary>눈 아이콘은 "지금 보이는 상태"를 나타낸다(켜져 있으면 Show, 꺼져 있으면 Off).</summary>
        private void RefreshToggleIcons()
        {
            ApplyToggleIcon(_synergyToggleIcon, _synergyVisible);
            ApplyToggleIcon(_pickToggleIcon, _choicePicksVisible);
            ApplyToggleIcon(_partyToggleIcon, _partyStatsVisible);
        }

        private static void ApplyToggleIcon(VisualElement icon, bool visible)
        {
            if (icon == null)
            {
                return;
            }

            icon.EnableInClassList("hud-icon--show", visible);
            icon.EnableInClassList("hud-icon--off", !visible);
        }

        public void SetStage(int stage)
        {
            if (_stageLabel != null)
            {
                _stageLabel.text = Loc.Format("ui.hud.stage", stage);
            }
        }

        /// <summary>파티 시너지를 표시한다(아직 인원이 모자란 것은 흐리게, 하나도 없으면 숨김).</summary>
        public void ShowSynergies(IReadOnlyList<PartySynergy> synergies) => _synergyPanel.Show(synergies);

        /// <summary>지금까지 고른 성장 선택지 횟수를 시너지 표 아래에 그린다.</summary>
        public void ShowChoicePicks(IReadOnlyList<ChoicePick> picks) => _choicePicks.Show(picks);

        /// <summary>이번 턴의 SPD 순서로 칩을 다시 그린다(라벨 = 화면 배치 인덱스, 색 = 진영).</summary>
        public void ShowTurnOrder(IReadOnlyList<TurnChipInfo> order)
        {
            if (_turnOrder == null)
            {
                return;
            }

            _turnOrder.Clear();
            _chips.Clear();

            foreach (TurnChipInfo info in order)
            {
                var chip = new Label(info.Label);
                chip.AddToClassList("turn-chip");
                chip.AddToClassList(info.Team == TeamSide.Player ? "turn-chip-player" : "turn-chip-enemy");
                chip.pickingMode = PickingMode.Ignore;
                _turnOrder.Add(chip);
                _chips[info.UnitId] = chip;
            }
        }

        /// <summary>현재 행동 유닛 칩을 강조하고, 플레이어 차례면 프롬프트를 띄운다.</summary>
        public void SetActiveUnit(Unit actor)
        {
            foreach (KeyValuePair<int, VisualElement> kv in _chips)
            {
                kv.Value.EnableInClassList("turn-chip-active", kv.Key == actor.Id);
            }

            if (actor.Team == TeamSide.Player)
            {
                ShowPrompt(Loc.Get("ui.hud.yourTurn"));
            }
            else
            {
                HidePrompt();
            }
        }

        public void MarkDead(int unitId)
        {
            if (_chips.TryGetValue(unitId, out VisualElement chip))
            {
                chip.AddToClassList("turn-chip-dead");
            }

            _partyStatus.MarkDead(unitId);
        }

        public void HidePrompt()
        {
            if (_playerPrompt != null)
            {
                _playerPrompt.style.display = DisplayStyle.None;
            }
        }

        private void ShowPrompt(string text)
        {
            if (_playerPrompt == null)
            {
                return;
            }
            _playerPrompt.text = text;
            _playerPrompt.style.display = DisplayStyle.Flex;
        }
    }
}