using System.Collections.Generic;
using System.Text;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>파티원 1명분의 표시 대상 — 전투 유닛 + 런 데이터 + 어느 월드 좌표 아래에 그릴지.</summary>
    public readonly struct PartyMemberSlot
    {
        /// <summary>이번 전투의 유닛. <see cref="Unit.Stats"/>에는 시너지가 이미 얹혀 있다.</summary>
        public readonly Unit Unit;

        /// <summary>증가량 분해에 필요한 런 데이터(기준 스탯 + 시너지 제외 성장 스탯).</summary>
        public readonly RunMember Member;

        public readonly Vector3 WorldPosition;

        public PartyMemberSlot(Unit unit, RunMember member, Vector3 worldPosition)
        {
            Unit = unit;
            Member = member;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>
    /// 화면 하단의 파티 스탯 표기. 파티원마다 패널 하나를 그 캐릭터의 스폰 위치 바로 아래에 정렬한다.
    ///
    /// 각 행은 <c>현재 적용 중인 값 + 그 값이 어디서 왔는지</c>를 색으로 나눠 보여준다:
    /// 앞 숫자는 실제 전투에 쓰이는 유효 스탯이고, 뒤 괄호는 기여분 내역이다(0이면 생략).
    /// <code>ATK  52 (+12) (+4) (+8)       ← 선택지 +12, 스테이지 자동 성장 +4, 시너지 +8
    /// DEF  10 (+5) (+2) (+8) (-16)  ← 마지막은 디버프로 깎인 몫</code>
    ///
    /// <see cref="CharacterStatBarsView"/>와 같은 순수 C# View — 컨테이너를 받아 그 안에서만 동작하므로
    /// <see cref="BattleHUD"/>가 필드로 들고 쓴다.
    /// </summary>
    public sealed class PartyStatusBarView
    {
        /// <summary>
        /// 표시할 스탯 행. <see cref="RowLabelKeys"/>와 순서·개수가 같아야 한다.
        ///
        /// <see cref="Aggro"/>는 <see cref="Stats"/>에서 오지 않는 유일한 행이고 성장하지도 않으므로
        /// 맨 뒤에 둔다 — 앞 7종의 항목·순서가 영입 카드(<c>RoguelikeRewardService.DescribeStats</c>)와
        /// 그대로 맞아야 같은 캐릭터를 두 화면에서 비교할 수 있기 때문이다.
        /// </summary>
        private enum StatRow { Hp, Atk, Spd, Def, CritRate, CritDmg, Res, Aggro }

        private static readonly StatRow[] Rows =
        {
            StatRow.Hp, StatRow.Atk, StatRow.Spd, StatRow.Def,
            StatRow.CritRate, StatRow.CritDmg, StatRow.Res, StatRow.Aggro
        };

        private static readonly string[] RowLabelKeys =
        {
            "ui.stat.hp", "ui.stat.atk", "ui.stat.spd", "ui.stat.def",
            "ui.stat.critRate", "ui.stat.critDmg", "ui.stat.res", "ui.stat.aggro"
        };

        // 리치 텍스트 색은 태그 문자열이라 USS 변수를 참조할 수 없어 여기에 둔다.
        private const string ChoiceColor = "#7FB8FF";  // 로그라이크 선택지로 얻은 성장
        private const string StageColor = "#9FB3C8";   // 스테이지 진급 자동 성장(고른 게 아니라 흐릿한 톤)
        private const string SynergyColor = "#FF8A8A"; // 파티 시너지
        private const string DebuffColor = "#FFB367";  // 상태이상 감소

        /// <summary>패널 1개 = 요소 + 행 라벨 + 현재 배정된 파티원.</summary>
        private sealed class MemberPanel
        {
            public VisualElement Root;

            /// <summary>전열/후열 아이콘. 그림은 USS 클래스가 정하고 여기서는 요소만 들고 있는다.</summary>
            public VisualElement RowIcon;

            public Label Name;
            public Label[] Values;
            public Unit Unit;
            public RunMember Member;
            public Vector3 WorldPosition;
        }

        private VisualElement _container;
        private readonly List<MemberPanel> _panels = new();
        private readonly StringBuilder _text = new();

        /// <summary>
        /// 살아 있는 파티원의 어그로 합 — 지분(%) 계산의 분모다.
        /// 실제 추첨도 생존자끼리만 정규화하므로(<c>MonsterAiSelector.PickTarget</c>) 화면 숫자가 실제 확률과 같다.
        /// </summary>
        private float _aliveAggroTotal;

        /// <summary>패널을 미리 만들어 둔다(파티 최대 인원 고정). 이후에는 보이기/숨기기만 한다.</summary>
        public void Build(VisualElement container)
        {
            _container = container;
            if (_container == null)
            {
                return;
            }

            _container.Clear();
            _panels.Clear();

            for (int i = 0; i < RunData.MaxPartySize; i++)
            {
                _panels.Add(CreatePanel());
            }

            // 첫 레이아웃 전에는 panel이 없어 화면 좌표 변환이 불가능하다.
            // 해상도 변경으로 매핑이 달라질 때도 같은 경로로 다시 맞춘다.
            _container.RegisterCallback<GeometryChangedEvent>(_ => AlignAll());
        }

        private MemberPanel CreatePanel()
        {
            var root = new VisualElement();
            root.AddToClassList("party-member");
            root.AddToClassList("panel-frame");
            root.pickingMode = PickingMode.Ignore;
            root.style.display = DisplayStyle.None;

            // 이름 줄 = 전열/후열 아이콘 + 이름
            var nameRow = new VisualElement();
            nameRow.AddToClassList("party-name-row");
            nameRow.pickingMode = PickingMode.Ignore;

            var rowIcon = new VisualElement();
            rowIcon.AddToClassList("party-row-icon");

            var name = new Label();
            name.AddToClassList("party-member-name");

            nameRow.Add(rowIcon);
            nameRow.Add(name);
            root.Add(nameRow);

            var values = new Label[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("party-stat-row");

                var caption = new Label(Loc.Get(RowLabelKeys[i]));
                caption.AddToClassList("party-stat-label");

                values[i] = new Label();
                values[i].AddToClassList("party-stat-value");

                row.Add(caption);
                row.Add(values[i]);
                root.Add(row);
            }

            _container.Add(root);
            return new MemberPanel { Root = root, RowIcon = rowIcon, Name = name, Values = values };
        }

        /// <summary>이번 스테이지의 파티를 패널에 배정하고 캐릭터 위치에 맞춰 정렬한다.</summary>
        public void SetParty(IReadOnlyList<PartyMemberSlot> slots)
        {
            if (_container == null)
            {
                return;
            }

            for (int i = 0; i < _panels.Count; i++)
            {
                MemberPanel panel = _panels[i];
                bool used = slots != null && i < slots.Count;

                panel.Unit = used ? slots[i].Unit : null;
                panel.Member = used ? slots[i].Member : null;
                panel.WorldPosition = used ? slots[i].WorldPosition : Vector3.zero;
                panel.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;

                if (used)
                {
                    panel.Name.text = panel.Unit.DisplayName;
                    ApplyRowIcon(panel);
                }
            }

            Refresh();
            AlignAll();
        }

        /// <summary>
        /// 이름 앞에 맡은 자리를 아이콘으로 표기한다.
        /// 전열/후열은 화면 배치를 바꾸지 않고 표적 확률만 다르게 하는 규칙이라,
        /// 이 표기가 없으면 플레이어가 시스템의 존재를 알 방법이 없다.
        /// 소속을 모르는 경우(테스트 파티 등)에는 아이콘을 숨겨 빈 칸이 뜨지 않게 한다.
        /// </summary>
        private static void ApplyRowIcon(MemberPanel panel)
        {
            bool known = panel.Member?.Source != null;
            panel.RowIcon.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
            if (!known)
            {
                return;
            }

            bool front = panel.Member.Source.Row == BattleRow.Front;
            panel.RowIcon.EnableInClassList("party-row-icon--front", front);
            panel.RowIcon.EnableInClassList("party-row-icon--back", !front);
        }

        /// <summary>
        /// 플레이어가 HUD 토글로 이 표를 껐다 켠다.
        /// 컨테이너째 숨기므로 개별 패널의 표시 여부(<see cref="SetParty"/>)와 서로를 덮지 않는다 —
        /// 다시 켜면 레이아웃이 잡히며 <c>GeometryChangedEvent</c>가 정렬을 알아서 다시 맞춘다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_container != null)
            {
                _container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>현재 수치와 증감 내역을 다시 쓴다(상태이상 부여/해제, 시너지 변동, 사망 시).</summary>
        public void Refresh()
        {
            // 어그로 지분의 분모를 먼저 구한다 — 행을 그리는 도중에는 값이 변하면 안 된다.
            _aliveAggroTotal = 0f;
            foreach (MemberPanel panel in _panels)
            {
                if (panel.Unit != null && panel.Unit.IsAlive)
                {
                    _aliveAggroTotal += Mathf.Max(0f, panel.Unit.AggroWeight);
                }
            }

            foreach (MemberPanel panel in _panels)
            {
                if (panel.Unit == null)
                {
                    continue;
                }

                for (int i = 0; i < Rows.Length; i++)
                {
                    panel.Values[i].text = Describe(Rows[i], panel.Unit, panel.Member);
                }

                panel.Root.EnableInClassList("party-member--dead", !panel.Unit.IsAlive);
            }
        }

        /// <summary>사망 표시(턴 순서 칩의 turn-chip-dead와 같은 처리).</summary>
        public void MarkDead(int unitId)
        {
            foreach (MemberPanel panel in _panels)
            {
                if (panel.Unit != null && panel.Unit.Id == unitId)
                {
                    panel.Root.AddToClassList("party-member--dead");
                }
            }

            // 어그로 지분은 생존자 기준이라 누가 쓰러지면 남은 인원의 몫이 함께 올라간다 — 그래서 전부 다시 그린다.
            Refresh();
        }

        /// <summary>
        /// 한 행의 표기를 만든다. 값의 출처는 세 갈래로 나뉜다:
        /// <list type="bullet">
        /// <item>성장 = <see cref="RunMember.Stats"/> − <see cref="RunMember.BaseStats"/> (선택지 + 스테이지 자동 성장)</item>
        /// <item>시너지 = <see cref="Unit.Stats"/> − <see cref="RunMember.Stats"/> (트래커가 전투 중에만 얹는다)</item>
        /// <item>디버프 = <see cref="Unit.Stats"/> − 유효 스탯 (상태이상, 비율 스탯에는 없다)</item>
        /// </list>
        /// 앞 숫자는 실제 적용 중인 값이고 괄호는 그 내역이라, 디버프가 걸리면 앞 숫자가 이미 깎여 있다.
        /// HP 행은 <b>최대 HP</b>다 — 현재 HP는 캐릭터 위 체력바가 보여주므로 여기서는 성장 대상인 최대치만 다룬다.
        /// </summary>
        private string Describe(StatRow row, Unit unit, RunMember member)
        {
            // 어그로만 Stats에서 오지 않고 파티 전체와의 비교가 필요하다 — 증감 내역도 없어 여기서 갈라진다.
            if (row == StatRow.Aggro)
            {
                return DescribeAggro(unit);
            }

            // 치명타·치명피해·저항은 배율(0~1, 또는 1.5 같은 배수)이라 100을 곱해 정수 %로 다룬다.
            bool percent = row is StatRow.CritRate or StatRow.CritDmg or StatRow.Res;
            float scale = percent ? 100f : 1f;

            int current = Mathf.RoundToInt(Effective(row, unit) * scale);
            int battle = Mathf.RoundToInt(Raw(row, unit.Stats) * scale);
            int grown = member != null ? Mathf.RoundToInt(Raw(row, member.Stats) * scale) : battle;
            int origin = member != null ? Mathf.RoundToInt(Raw(row, member.BaseStats) * scale) : grown;
            int choice = member != null ? Mathf.RoundToInt(Raw(row, member.ChoiceGrowth) * scale) : 0;

            _text.Clear();
            _text.Append(current);
            if (percent)
            {
                _text.Append('%');
            }

            Append(choice, '+', ChoiceColor, percent);
            Append(grown - origin - choice, '+', StageColor, percent); // 성장 총량에서 선택지 몫을 뺀 나머지
            Append(battle - grown, '+', SynergyColor, percent);
            Append(battle - current, '-', DebuffColor, percent);       // 유효값이 낮아진 만큼이 감소량

            return _text.ToString();
        }

        /// <summary>
        /// 단일 대상 공격이 이 파티원을 노릴 확률(생존자 어그로 합 대비 지분).
        ///
        /// 전열/후열 아이콘만으로는 "얼마나 더 맞는가"가 드러나지 않아 숫자로 함께 보여준다.
        /// 죽은 파티원은 표적이 될 수 없어 0%이고, 누가 쓰러지면 남은 인원의 지분이 함께 올라간다 —
        /// 실제 추첨(<c>MonsterAiSelector.PickTarget</c>)도 같은 기준으로 재정규화되므로 화면과 확률이 어긋나지 않는다.
        /// </summary>
        private string DescribeAggro(Unit unit)
        {
            if (!unit.IsAlive || _aliveAggroTotal <= 0f)
            {
                return "0%";
            }

            return $"{Mathf.RoundToInt(Mathf.Max(0f, unit.AggroWeight) / _aliveAggroTotal * 100f)}%";
        }

        /// <summary>0이 아닐 때만 색이 붙은 괄호를 덧붙인다(변화 없는 항목으로 줄이 길어지지 않도록).</summary>
        private void Append(int delta, char sign, string color, bool percent)
        {
            if (delta <= 0)
            {
                return;
            }

            _text.Append(" <color=").Append(color).Append(">(").Append(sign).Append(delta);
            if (percent)
            {
                _text.Append('%');
            }

            _text.Append(")</color>");
        }

        /// <summary>
        /// 상태이상 감소가 반영된 실제 적용 값. HP·비율 스탯은 감소 수단이 없어 그대로라
        /// 감소량 괄호가 자동으로 0이 되어 생략된다.
        /// </summary>
        private static float Effective(StatRow row, Unit unit) => row switch
        {
            StatRow.Atk => unit.EffectiveAtk,
            StatRow.Spd => unit.EffectiveSpd,
            StatRow.Def => unit.EffectiveDef,
            _ => Raw(row, unit.Stats)
        };

        private static float Raw(StatRow row, Stats stats) => row switch
        {
            StatRow.Hp => stats.MaxHp,
            StatRow.Atk => stats.Atk,
            StatRow.Spd => stats.Spd,
            StatRow.Def => stats.Def,
            StatRow.CritRate => stats.CritRate,
            StatRow.CritDmg => stats.CritDmg,
            _ => stats.Res
        };

        /// <summary>
        /// 각 패널을 담당 캐릭터의 화면 X에 맞춘다.
        /// 매 프레임이 아니라 갱신 시점에만 부른다 — <see cref="CameraShake"/>가 카메라를 흔들기 때문에
        /// 계속 추적하면 하단 바까지 함께 떨린다. 슬롯도 카메라도 고정이라 이걸로 충분하다.
        /// </summary>
        private void AlignAll()
        {
            Camera camera = MainCameraCache.Current;
            if (camera == null || _container?.panel == null)
            {
                return;
            }

            foreach (MemberPanel panel in _panels)
            {
                if (panel.Unit == null)
                {
                    continue;
                }

                Vector3 screen = camera.WorldToScreenPoint(panel.WorldPosition);

                // WorldToScreenPoint는 좌하단 원점, UI Toolkit은 좌상단 원점이라 Y를 뒤집는다.
                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(
                    _container.panel, new Vector2(screen.x, Screen.height - screen.y));

                // 가운데 맞춤은 USS의 translate(-50%)가 처리한다 — 레이아웃 전에는 폭을 알 수 없기 때문.
                panel.Root.style.left = panelPoint.x;
            }
        }
    }
}
