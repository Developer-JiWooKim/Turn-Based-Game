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
        private Button _pauseButton;

        private readonly Dictionary<int, VisualElement> _chips = new();

        /// <summary>하단 파티 스탯 바. 화면 일부의 표현 전담이라 순수 C# View로 두고 여기서 들고 쓴다.</summary>
        private readonly PartyStatusBarView _partyStatus = new();

        /// <summary>좌상단 시너지 패널(위와 같은 이유로 별도 View).</summary>
        private readonly SynergyPanelView _synergyPanel = new();

        /// <summary>우상단 퍼즈 버튼이 눌렸다(구독자는 <see cref="BattlePausePanel"/>).</summary>
        public event Action PauseClicked;

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

            _pauseButton = Find<Button>(root, "pause-button", "퍼즈 버튼을 누를 수 없습니다");
            if (_pauseButton != null)
            {
                _pauseButton.clicked += () => PauseClicked?.Invoke();
            }

            _partyStatus.Build(Find<VisualElement>(root, "party-status", "하단 파티 스탯이 표시되지 않습니다"));
            _synergyPanel.Build(Find<VisualElement>(root, "synergy-panel", "시너지 패널이 표시되지 않습니다"));

            HidePrompt();
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

        /// <summary>퍼즈 버튼 노출 여부. 전투 중이 아닐 때(선택지·결과 화면) 숨긴다.</summary>
        public void SetPauseButtonVisible(bool visible)
        {
            if (_pauseButton != null)
            {
                _pauseButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
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