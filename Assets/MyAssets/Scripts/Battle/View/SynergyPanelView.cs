using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 파티 시너지 패널. 시너지 보유 캐릭터마다 <c>아이콘 + 이름 + ×요구인원 + 효과</c> 한 행을 그린다.
    ///
    /// <b>아직 인원이 모자란 시너지도 흐리게 함께 보여준다</b> — "한 명만 더 모으면 발동한다"가 보여야
    /// 영입 선택지에서 누구를 고를지 판단할 수 있기 때문. 발동 여부는 숫자가 아니라 농담(濃淡)으로만 구분한다.
    ///
    /// <see cref="PartyStatusBarView"/>와 같은 순수 C# View — 컨테이너를 받아 그 안에서만 동작하므로
    /// <see cref="BattleHUD"/>가 필드로 들고 쓴다.
    /// </summary>
    public sealed class SynergyPanelView
    {
        private sealed class SynergyRow
        {
            public VisualElement Root;
            public VisualElement Icon;
            public Label Name;
            public Label Count;
            public Label Effects;
        }

        private VisualElement _container;
        private readonly List<SynergyRow> _rows = new();

        /// <summary>행을 미리 만들어 둔다 — 시너지 종류는 파티 인원을 넘을 수 없다.</summary>
        public void Build(VisualElement container)
        {
            _container = container;
            if (_container == null)
            {
                return;
            }

            _container.Clear();
            _rows.Clear();

            var title = new Label(Loc.Get("ui.synergy.title"));
            title.AddToClassList("synergy-title");
            _container.Add(title);

            for (int i = 0; i < RunData.MaxPartySize; i++)
            {
                _rows.Add(CreateRow());
            }

            _container.style.display = DisplayStyle.None; // 보여줄 게 생길 때까지 숨김
        }

        private SynergyRow CreateRow()
        {
            var root = new VisualElement();
            root.AddToClassList("synergy-row");
            root.style.display = DisplayStyle.None;

            var head = new VisualElement();
            head.AddToClassList("synergy-head");

            var icon = new VisualElement();
            icon.AddToClassList("synergy-icon");

            var name = new Label();
            name.AddToClassList("synergy-name");

            var count = new Label();
            count.AddToClassList("synergy-count");

            head.Add(icon);
            head.Add(name);
            head.Add(count);

            var effects = new Label();
            effects.AddToClassList("synergy-effects");

            root.Add(head);
            root.Add(effects);
            _container.Add(root);

            return new SynergyRow { Root = root, Icon = icon, Name = name, Count = count, Effects = effects };
        }

        /// <summary>파티가 보유한 시너지를 그린다(발동 중이 아닌 것 포함). 하나도 없으면 패널 전체를 숨긴다.</summary>
        public void Show(IReadOnlyList<PartySynergy> synergies)
        {
            if (_container == null)
            {
                return;
            }

            bool any = synergies != null && synergies.Count > 0;
            _container.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;

            for (int i = 0; i < _rows.Count; i++)
            {
                SynergyRow row = _rows[i];
                bool used = any && i < synergies.Count;

                row.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (!used)
                {
                    continue;
                }

                PartySynergy synergy = synergies[i];
                CharacterStatsSO source = synergy.Source;

                row.Name.text = source != null ? source.DisplayName : "?";

                // ×N은 이 시너지가 요구하는 인원이라 파티 구성이 바뀌어도 변하지 않는다
                // (현재 모인 인원이 아니다 — 그건 아래 농담으로만 나타낸다).
                row.Count.text = source != null ? $"×{source.SynergyThreshold}" : string.Empty;

                row.Effects.text = DescribeEffect(synergy.Effect);
                row.Root.EnableInClassList("synergy-row--inactive", !synergy.IsActive);

                Sprite icon = source != null ? source.Icon : null;
                row.Icon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
                row.Icon.style.backgroundImage = icon != null ? Background.FromSprite(icon) : default;
            }
        }

        /// <summary>
        /// 시너지 효과 한 줄을 만든다. 문구를 SO에 따로 적어두면 수치를 조정할 때 설명이 어긋나므로
        /// 실제 효과 값에서 생성한다.
        ///
        /// 아래 6개 분기는 <c>CharacterStatsSO.CreateSynergy</c>가 채우는 6개 필드와 1:1이다.
        /// 최대 HP·회복에 분기가 없는 건 빠뜨린 게 아니라 시너지 대상이 아니기 때문 —
        /// 조건이 깨지면 되돌려야 하는 효과라 제외돼 있다(<see cref="SynergyBonus"/> 참고).
        /// ⚠️ 시너지 필드가 늘면 여기도 같이 늘려야 한다(다른 어셈블리라 컴파일러가 잡아주지 않는다).
        ///
        /// ATK/SPD/DEF는 비율이라 그대로 %로 찍고, 치명타·치명피해·저항은 가산치라 <c>%p</c>로 구분해 적는다 —
        /// 같은 "+10%"로 보이면 15%가 16.5%가 되는 건지 25%가 되는 건지 플레이어가 알 수 없다.
        /// </summary>
        private static string DescribeEffect(in SynergyBonus e)
        {
            var parts = new List<string>();

            if (e.AtkRate != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.atk")} +{e.AtkRate * 100f:0}%");
            }

            if (e.SpdRate != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.spd")} +{e.SpdRate * 100f:0}%");
            }

            if (e.DefRate != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.def")} +{e.DefRate * 100f:0}%");
            }

            if (e.CritRate != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.critRate")} +{e.CritRate * 100f:0}%p");
            }

            if (e.CritDmg != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.critDmg")} +{e.CritDmg * 100f:0}%p");
            }

            if (e.Res != 0f)
            {
                parts.Add($"{Loc.Get("ui.stat.res")} +{e.Res * 100f:0}%p");
            }

            return string.Join(" · ", parts);
        }
    }
}
