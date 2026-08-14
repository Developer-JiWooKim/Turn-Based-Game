using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 이번 런의 <b>선택 기록</b> — 지금까지 고른 선택지의 카테고리별 누적 횟수 패널(시너지 표 바로 아래).
    /// "무엇에 투자해왔는가"를 한눈에 보여줘 다음 선택지를 고를 때 판단 근거가 되게 한다.
    ///
    /// <b>9종을 가리지 않고 전부 센다</b>(성장·회복·몬스터 디버프·영입). 집계는 <c>RunData</c>가 하고
    /// 이 View는 받은 것을 그리기만 한다 — 한 번도 고르지 않은 항목은 애초에 넘어오지 않는다.
    ///
    /// <see cref="SynergyPanelView"/>와 같은 순수 C# View — 컨테이너를 받아 그 안에서만 동작하므로
    /// <see cref="BattleHUD"/>가 필드로 들고 쓴다. 누락 보고도 하지 않는다(컨테이너의 주인이 보고한다).
    /// </summary>
    public sealed class ChoicePickPanelView
    {
        private sealed class PickRow
        {
            public VisualElement Root;
            public Label Name;
            public Label Count;
        }

        private VisualElement _container;
        private readonly List<PickRow> _rows = new();

        /// <summary>지금 보여줄 항목이 있는지. 아래 <see cref="SetVisible"/>와 함께 판정해야 한다.</summary>
        private bool _hasAny;

        /// <summary>플레이어가 HUD 토글로 꺼두지 않았는지.</summary>
        private bool _userVisible = true;

        /// <summary>
        /// 행을 카테고리 수만큼 미리 만들어 둔다 — 9종 전부가 동시에 뜰 수 있고,
        /// 선택지를 추가해도 행이 모자라지 않는다.
        /// </summary>
        public void Build(VisualElement container)
        {
            _container = container;
            if (_container == null)
            {
                return;
            }

            _container.Clear();
            _rows.Clear();

            var title = new Label(Loc.Get("ui.choicePicks.title"));
            title.AddToClassList("pick-title");
            _container.Add(title);

            int slots = System.Enum.GetValues(typeof(RoguelikeCategory)).Length;
            for (int i = 0; i < slots; i++)
            {
                _rows.Add(CreateRow());
            }

            _container.style.display = DisplayStyle.None; // 첫 선택지를 고를 때까지 숨김
        }

        private PickRow CreateRow()
        {
            var root = new VisualElement();
            root.AddToClassList("pick-row");
            root.style.display = DisplayStyle.None;

            var name = new Label();
            name.AddToClassList("pick-name");

            var count = new Label();
            count.AddToClassList("pick-count");

            root.Add(name);
            root.Add(count);
            _container.Add(root);

            return new PickRow { Root = root, Name = name, Count = count };
        }

        /// <summary>
        /// 선택 횟수를 그린다. 한 번도 고르지 않았으면 패널 전체를 숨긴다(시너지 패널과 같은 규칙).
        /// </summary>
        public void Show(IReadOnlyList<ChoicePick> picks)
        {
            if (_container == null)
            {
                return;
            }

            _hasAny = picks != null && picks.Count > 0;
            ApplyVisibility();

            for (int i = 0; i < _rows.Count; i++)
            {
                PickRow row = _rows[i];
                bool used = _hasAny && i < picks.Count;

                row.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (!used)
                {
                    continue;
                }

                // 이름은 선택지 SO가 아니라 카테고리에서 만든다 — 표시 이름의 번역 키가
                // RoguelikeChoiceSO.Title과 같은 규칙(choice.<카테고리>.title)이라 SO를 들고 있을 필요가 없다.
                row.Name.text = Loc.Get($"choice.{picks[i].Category}.title");
                row.Count.text = $"×{picks[i].Count}";
            }
        }

        /// <summary>
        /// 플레이어가 HUD 토글로 이 표를 껐다 켠다. "고른 게 없으면 숨긴다"는 규칙은 그대로라
        /// 켜도 선택 이력이 없으면 표는 나타나지 않는다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _userVisible = visible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_container != null)
            {
                _container.style.display = _userVisible && _hasAny ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
