using System.Collections.Generic;
using System.Linq;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 전투 화면 HUD(UI Toolkit). 스테이지·턴 순서·현재 행동 유닛·플레이어 차례 프롬프트를 표시하는
    /// 수동 View다. Core를 직접 구독하지 않고 BattleDirector가 밀어넣어 갱신한다
    /// (시뮬레이션이 웨이브마다 재생성되므로 구동 주체를 BattleDirector로 통일).
    /// </summary>
    public sealed class BattleHUD : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private Label _stageLabel;
        private Label _synergyLabel;
        private VisualElement _turnOrder;
        private Label _playerPrompt;

        private readonly Dictionary<int, VisualElement> _chips = new();

        private void Awake()
        {
            VisualElement root = _document.rootVisualElement;
            _stageLabel = root.Q<Label>("stage-label");
            _synergyLabel = root.Q<Label>("synergy-label");
            _turnOrder = root.Q<VisualElement>("turn-order");
            _playerPrompt = root.Q<Label>("player-prompt");
            HidePrompt();
        }

        public void SetStage(int stage)
        {
            if (_stageLabel != null)
                _stageLabel.text = $"STAGE {stage}";
        }

        /// <summary>발동 중인 파티 시너지를 표시한다(없으면 숨김).</summary>
        public void ShowSynergies(IReadOnlyList<ActiveSynergy> synergies)
        {
            if (_synergyLabel == null) return;

            if (synergies == null || synergies.Count == 0)
            {
                _synergyLabel.style.display = DisplayStyle.None;
                return;
            }

            _synergyLabel.text = string.Join("\n", synergies.Select(DescribeSynergy));
            _synergyLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 시너지 한 줄을 만든다. 문구를 SO에 따로 적어두면 수치를 조정할 때 설명이 어긋나므로
        /// 실제 효과 값에서 생성한다.
        /// </summary>
        private static string DescribeSynergy(ActiveSynergy synergy)
        {
            RoguelikeEffect e = synergy.Effect;
            var parts = new List<string>();

            if (e.AtkFlat != 0) parts.Add($"ATK +{e.AtkFlat}");
            if (e.SpdFlat != 0) parts.Add($"SPD +{e.SpdFlat}");
            if (e.DefFlat != 0) parts.Add($"DEF +{e.DefFlat}");
            if (e.CritRateFlat != 0f) parts.Add($"치명타 +{e.CritRateFlat * 100f:0}%");
            if (e.CritDmgFlat != 0f) parts.Add($"치명피해 +{e.CritDmgFlat * 100f:0}%");
            if (e.ResFlat != 0f) parts.Add($"저항 +{e.ResFlat * 100f:0}%");

            string name = synergy.Source != null ? synergy.Source.DisplayName : "?";
            return $"{name} ×{synergy.Count}  {string.Join(" · ", parts)}";
        }

        /// <summary>이번 턴의 SPD 순서로 칩을 다시 그린다.</summary>
        public void ShowTurnOrder(IReadOnlyList<Unit> order)
        {
            if (_turnOrder == null) return;

            _turnOrder.Clear();
            _chips.Clear();

            foreach (Unit unit in order)
            {
                var chip = new Label(unit.DisplayName);
                chip.AddToClassList("turn-chip");
                chip.AddToClassList(unit.Team == TeamSide.Player ? "turn-chip-player" : "turn-chip-enemy");
                chip.pickingMode = PickingMode.Ignore;
                _turnOrder.Add(chip);
                _chips[unit.Id] = chip;
            }
        }

        /// <summary>현재 행동 유닛 칩을 강조하고, 플레이어 차례면 프롬프트를 띄운다.</summary>
        public void SetActiveUnit(Unit actor)
        {
            foreach (KeyValuePair<int, VisualElement> kv in _chips)
                kv.Value.EnableInClassList("turn-chip-active", kv.Key == actor.Id);

            if (actor.Team == TeamSide.Player)
                ShowPrompt("당신의 차례 — 공격할 몬스터를 클릭하세요");
            else
                HidePrompt();
        }

        public void MarkDead(int unitId)
        {
            if (_chips.TryGetValue(unitId, out VisualElement chip))
                chip.AddToClassList("turn-chip-dead");
        }

        public void HidePrompt()
        {
            if (_playerPrompt != null)
                _playerPrompt.style.display = DisplayStyle.None;
        }

        private void ShowPrompt(string text)
        {
            if (_playerPrompt == null) return;
            _playerPrompt.text = text;
            _playerPrompt.style.display = DisplayStyle.Flex;
        }
    }
}