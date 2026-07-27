using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 스탯 바 6종(HP/ATK/SPD/DEF/치명타/저항)의 표시 담당.
    /// 어떤 캐릭터를 보여줄지는 <see cref="CharacterSelectPanelUI"/>가 정하고,
    /// 여기서는 넘겨받은 수치를 막대 길이와 텍스트로 옮기기만 한다.
    ///
    /// <see cref="KeybindListView"/>와 같은 방식 — MonoBehaviour가 아니라 컨테이너를 받아
    /// 그 안에서만 동작하므로 패널이 필드로 들고 쓰면 된다.
    /// </summary>
    public sealed class CharacterStatBarsView
    {
        private List<VisualElement> _fills;
        private List<Label> _values;

        /// <summary>바 길이의 기준이 되는 최댓값(HP/ATK/SPD/DEF). 비율 스탯은 0~1 고정이라 쓰지 않는다.</summary>
        private Stats _ceiling;

        /// <summary>스탯 패널 안의 막대/숫자 요소를 찾아두고, 바 길이의 기준값을 받는다.</summary>
        public void Build(VisualElement statPanel, Stats ceiling)
        {
            _ceiling = ceiling;
            if (statPanel == null) return;

            _fills = statPanel.Query<VisualElement>("stat-fill").ToList();
            _values = statPanel.Query<Label>("stat-value").ToList();
        }

        /// <summary>
        /// 스탯 바를 넘겨받은 수치로 갱신한다. HP/ATK/SPD/DEF는 기준 최댓값 대비 비율,
        /// 치명타·저항은 그 자체가 0~1 비율이라 % 로 표시한다(UXML의 행 순서와 같은 순서).
        /// </summary>
        public void Refresh(Stats s)
        {
            if (_fills == null || _ceiling == null) return;

            SetRow(0, s.MaxHp, _ceiling.MaxHp, s.MaxHp.ToString());
            SetRow(1, s.Atk, _ceiling.Atk, s.Atk.ToString());
            SetRow(2, s.Spd, _ceiling.Spd, s.Spd.ToString());
            SetRow(3, s.Def, _ceiling.Def, s.Def.ToString());
            SetRow(4, s.CritRate, 1f, $"{s.CritRate * 100f:0}%");
            SetRow(5, s.Res, 1f, $"{s.Res * 100f:0}%");
        }

        private void SetRow(int index, float value, float ceiling, string text)
        {
            if (index < _fills.Count)
                _fills[index].style.width = Length.Percent(ceiling > 0f ? Mathf.Clamp01(value / ceiling) * 100f : 0f);

            if (_values != null && index < _values.Count)
                _values[index].text = text;
        }
    }
}
