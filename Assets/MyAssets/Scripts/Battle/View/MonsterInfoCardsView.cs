using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Battle.View.Panels;
using Assets.MyAssets.Scripts.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Tab 몬스터 정보 창의 카드 목록. 이번 웨이브의 몬스터마다 카드 하나를 그린다 —
    /// <c>아이콘 · 배치 라벨 + 이름 · 등급 · 스탯 7종 · (있으면) 스킬</c>.
    ///
    /// <see cref="PartyStatusBarView"/>·<see cref="SynergyPanelView"/>와 같은 순수 C# View다 —
    /// 컨테이너를 넘겨받아 그 안에서만 동작하므로 <see cref="MonsterInfoPanel"/>이 필드로 들고 쓴다.
    /// 조회 실패를 보고하지 않는 것도 같은 이유다(UXML의 주인인 패널이 이미 더 정확하게 보고한다).
    /// </summary>
    public sealed class MonsterInfoCardsView
    {
        /// <summary>
        /// 표시할 스탯 행. 항목·순서·% 표기를 하단 파티 표기(<see cref="PartyStatusBarView"/>)와 맞춰 뒀다 —
        /// 두 화면이 다른 항목 수로 보여주면 "내 파티 vs 이 몬스터" 비교가 되지 않기 때문이다.
        /// 어그로 행이 없는 건 빠뜨린 게 아니라, 몬스터를 고르는 건 플레이어의 수동 타겟팅이라
        /// 어그로 추첨을 타지 않기 때문이다(<see cref="Unit.AggroWeight"/> 참고).
        /// </summary>
        private enum StatRow { Hp, Atk, Spd, Def, CritRate, CritDmg, Res }

        private static readonly StatRow[] Rows =
        {
            StatRow.Hp, StatRow.Atk, StatRow.Spd, StatRow.Def,
            StatRow.CritRate, StatRow.CritDmg, StatRow.Res
        };

        private static readonly string[] RowLabelKeys =
        {
            "ui.stat.hp", "ui.stat.atk", "ui.stat.spd", "ui.stat.def",
            "ui.stat.critRate", "ui.stat.critDmg", "ui.stat.res"
        };

        /// <summary>카드 1장의 요소들. 텍스트는 전부 <see cref="Show"/>가 채운다(언어 변경 시 다시 그리기 위함).</summary>
        private sealed class Card
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Title;
            public Label Tier;
            public Label[] StatCaptions;
            public Label[] StatValues;

            /// <summary>스킬 블록 전체. 스킬이 없는 몬스터(Normal)에서는 통째로 숨긴다.</summary>
            public VisualElement Skill;
            public Label SkillName;
            public Label SkillMeta;

            /// <summary>스킬이 거는 상태이상 줄. 데미지만 주는 스킬에서는 숨긴다.</summary>
            public VisualElement StatusRow;
            public VisualElement StatusIcon;
            public Label StatusText;
        }

        private VisualElement _container;
        private readonly List<Card> _cards = new();

        /// <summary>카드를 담을 컨테이너를 받아둔다. 카드는 웨이브 구성에 달려 있어 <see cref="Show"/>에서 필요한 만큼 만든다.</summary>
        public void Build(VisualElement container)
        {
            _container = container;
            _container?.Clear();
            _cards.Clear();
        }

        /// <summary>
        /// 이번 웨이브의 몬스터를 카드로 그린다.
        /// 카드는 파괴하지 않고 재사용하며(웨이브마다 몬스터 수가 달라진다) 남는 카드는 숨긴다.
        /// </summary>
        public void Show(IReadOnlyList<SpawnedMonster> monsters)
        {
            if (_container == null)
            {
                return;
            }

            int count = monsters?.Count ?? 0;
            while (_cards.Count < count)
            {
                _cards.Add(CreateCard());
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                Card card = _cards[i];
                bool used = i < count;

                card.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                {
                    Fill(card, monsters[i]);
                }
            }
        }

        private Card CreateCard()
        {
            var root = new VisualElement();
            root.AddToClassList("monster-card");
            root.AddToClassList("panel-frame");

            var icon = new VisualElement();
            icon.AddToClassList("monster-card-icon");

            var title = new Label();
            title.AddToClassList("monster-card-title");

            var tier = new Label();
            tier.AddToClassList("monster-card-tier");

            root.Add(icon);
            root.Add(title);
            root.Add(tier);

            var stats = new VisualElement();
            stats.AddToClassList("monster-card-stats");
            root.Add(stats);

            var captions = new Label[Rows.Length];
            var values = new Label[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("monster-stat-row");

                captions[i] = new Label();
                captions[i].AddToClassList("monster-stat-label");

                values[i] = new Label();
                values[i].AddToClassList("monster-stat-value");

                row.Add(captions[i]);
                row.Add(values[i]);
                stats.Add(row);
            }

            var skill = new VisualElement();
            skill.AddToClassList("monster-card-skill");

            var skillName = new Label();
            skillName.AddToClassList("monster-skill-name");

            var skillMeta = new Label();
            skillMeta.AddToClassList("monster-skill-meta");

            var statusRow = new VisualElement();
            statusRow.AddToClassList("monster-skill-status");

            var statusIcon = new VisualElement();
            statusIcon.AddToClassList("monster-status-icon");

            var statusText = new Label();
            statusText.AddToClassList("monster-status-text");

            statusRow.Add(statusIcon);
            statusRow.Add(statusText);

            skill.Add(skillName);
            skill.Add(skillMeta);
            skill.Add(statusRow);
            root.Add(skill);

            _container.Add(root);

            return new Card
            {
                Root = root,
                Icon = icon,
                Title = title,
                Tier = tier,
                StatCaptions = captions,
                StatValues = values,
                Skill = skill,
                SkillName = skillName,
                SkillMeta = skillMeta,
                StatusRow = statusRow,
                StatusIcon = statusIcon,
                StatusText = statusText
            };
        }

        private static void Fill(Card card, in SpawnedMonster monster)
        {
            Unit unit = monster.Unit;
            MonsterStatsSO source = monster.Source;

            // 배치 라벨을 이름 앞에 붙여 화면 위 몬스터와 카드를 눈으로 맞출 수 있게 한다
            // (턴 순서 칩과 같은 이유 — 같은 몬스터가 2마리 나와도 구분된다).
            card.Title.text = string.IsNullOrEmpty(monster.SlotLabel)
                ? unit.DisplayName
                : $"{monster.SlotLabel} {unit.DisplayName}";

            card.Tier.text = source != null ? TierLabel(source.Tier) : string.Empty;

            Sprite icon = source != null ? source.Icon : null;
            card.Icon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            card.Icon.style.backgroundImage = icon != null ? Background.FromSprite(icon) : default;

            for (int i = 0; i < Rows.Length; i++)
            {
                card.StatCaptions[i].text = Loc.Get(RowLabelKeys[i]);
                card.StatValues[i].text = Describe(Rows[i], unit);
            }

            FillSkill(card, unit.Skill, source);

            // 이미 쓰러진 몬스터는 흐리게 — 절반쯤 정리된 웨이브에서 남은 상대가 한눈에 보여야 한다.
            card.Root.EnableInClassList("monster-card--dead", !unit.IsAlive);
        }

        private static void FillSkill(Card card, SkillProfile skill, MonsterStatsSO source)
        {
            if (skill == null)
            {
                card.Skill.style.display = DisplayStyle.None;
                return;
            }

            card.Skill.style.display = DisplayStyle.Flex;

            SkillSO asset = source != null ? source.Skill : null;
            card.SkillName.text = asset != null ? asset.DisplayName : Loc.Get("ui.monsterInfo.skill");
            card.SkillMeta.text = DescribeSkill(skill);

            StatusEffect? status = skill.Status;
            bool hasStatus = status.HasValue && status.Value.IsValid;
            card.StatusRow.style.display = hasStatus ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasStatus)
            {
                return;
            }

            // 아이콘 그림은 USS 클래스가 들고 코드는 클래스만 토글한다(.party-row-icon과 같은 방식).
            // 체력바(TMP 스프라이트)와 달리 여기는 UI Toolkit이라 PNG를 background-image로 바로 쓴다 —
            // Sprite Asset·Fallback 체인과는 무관하다.
            foreach (KeyValuePair<StatusKind, string> pair in StatusIconClasses)
            {
                card.StatusIcon.EnableInClassList(pair.Value, pair.Key == status.Value.Kind);
            }

            card.StatusText.text = DescribeStatus(status.Value);
        }

        /// <summary>상태이상 종류별 아이콘 USS 클래스(그림 경로는 MonsterInfo.uss가 들고 있다).</summary>
        private static readonly Dictionary<StatusKind, string> StatusIconClasses = new()
        {
            { StatusKind.Stun, "monster-status-icon--stun" },
            { StatusKind.Poison, "monster-status-icon--poison" },
            { StatusKind.AtkDown, "monster-status-icon--atk-down" },
            { StatusKind.DefDown, "monster-status-icon--def-down" },
            { StatusKind.SpdDown, "monster-status-icon--spd-down" }
        };

        /// <summary>"단일 대상 · 위력 180% · 쿨타임 2턴" 한 줄.</summary>
        private static string DescribeSkill(SkillProfile skill)
        {
            string scope = Loc.Get(skill.Scope == TargetScope.Line
                ? "ui.monsterInfo.scopeLine"
                : "ui.monsterInfo.scopeSingle");

            string power = Loc.Format("ui.monsterInfo.power", Mathf.RoundToInt(skill.PowerMultiplier * 100f));
            string cooldown = skill.Cooldown > 0
                ? Loc.Format("ui.monsterInfo.cooldown", skill.Cooldown)
                : Loc.Get("ui.monsterInfo.cooldownNone");

            return $"{scope} · {power} · {cooldown}";
        }

        /// <summary>
        /// "중독 5% · 2턴 · 기본 확률 40%".
        ///
        /// ⚠️ 확률은 <see cref="StatusEffect.ApplyChance"/> = <b>저항 적용 전 기본값</b>이다.
        /// 실제 부여 확률은 <c>ApplyChance × (1 − 대상 RES)</c>라 맞는 파티원마다 다르므로,
        /// 여기서 "이 확률로 걸린다"고 단정하면 거짓말이 된다 — 창 아래 안내 문구가 그 점을 밝힌다.
        /// </summary>
        private static string DescribeStatus(in StatusEffect status)
        {
            string name = Loc.Get(StatusNameKey(status.Kind));

            // 기절은 크기 개념이 없고(지속 턴이 전부), 도트는 최대 HP 대비 비율,
            // 감소형은 해당 스탯의 감소 비율이라 부호를 붙여 방향까지 드러낸다.
            int magnitude = Mathf.RoundToInt(status.Magnitude * 100f);
            if (status.Kind == StatusKind.Poison)
            {
                name = $"{name} {magnitude}%";
            }
            else if (status.Kind != StatusKind.Stun)
            {
                name = $"{name} -{magnitude}%";
            }

            return Loc.Format("ui.monsterInfo.statusLine",
                              name, status.Duration, Mathf.RoundToInt(status.ApplyChance * 100f));
        }

        private static string StatusNameKey(StatusKind kind) => kind switch
        {
            StatusKind.Stun => "ui.status.stun",
            StatusKind.Poison => "ui.status.poison",
            StatusKind.AtkDown => "ui.status.atkDown",
            StatusKind.DefDown => "ui.status.defDown",
            _ => "ui.status.spdDown"
        };

        private static string TierLabel(MonsterTier tier) => Loc.Get(tier switch
        {
            MonsterTier.Boss => "ui.tier.boss",
            MonsterTier.Elite => "ui.tier.elite",
            _ => "ui.tier.normal"
        });

        /// <summary>
        /// 한 행의 표기. ATK/SPD/DEF는 <b>유효 스탯</b>이라 플레이어가 건 감소형 상태이상이 즉시 반영된다
        /// (하단 파티 표기의 앞 숫자와 같은 기준).
        /// HP는 <b>최대 HP</b>다 — 현재 HP는 몬스터 머리 위 체력바가 이미 보여주고,
        /// 이 창은 열린 순간의 스냅샷이라 남은 체력을 여기 적으면 금세 낡은 숫자가 된다.
        /// </summary>
        private static string Describe(StatRow row, Unit unit) => row switch
        {
            StatRow.Hp => unit.Stats.MaxHp.ToString(),
            StatRow.Atk => unit.EffectiveAtk.ToString(),
            StatRow.Spd => unit.EffectiveSpd.ToString(),
            StatRow.Def => unit.EffectiveDef.ToString(),
            StatRow.CritRate => Percent(unit.Stats.CritRate),
            StatRow.CritDmg => Percent(unit.Stats.CritDmg),
            _ => Percent(unit.Stats.Res)
        };

        /// <summary>배율(0~1 또는 1.5 같은 배수)을 정수 %로. 치명피해 1.5 → "150%".</summary>
        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
