using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 성향 포인트 배분 목록 UI. 카테고리마다 "이름 + [-] + 현재 포인트 + [+]" 행을 만들고,
    /// 세이브 값으로 숫자와 버튼 활성 상태를 다시 그린다.
    ///
    /// 얼마를 투자할 수 있는지(잔여 포인트 규칙)는 <see cref="SaveData"/>가 판정하고
    /// 여기서는 결과를 화면에 옮기기만 한다. <see cref="KeybindListView"/>와 같은 방식.
    /// </summary>
    public sealed class AllocationRowsView
    {
        /// <summary>카테고리 1종의 행 UI</summary>
        private sealed class CategoryRow
        {
            public RoguelikeCategory Category;

            /// <summary>이름을 다시 뽑을 원본(언어가 바뀌면 SO의 Title이 달라진다).</summary>
            public RoguelikeChoiceSO Source;

            public Label NameLabel;
            public Label PointsLabel;
            public Button MinusButton;
            public Button PlusButton;
        }

        private readonly List<CategoryRow> _rows = new();

        /// <summary>
        /// 카테고리 행을 컨테이너에 채운다. 행 순서·이름은 넘겨받은 선택지 SO에서 오므로
        /// 카테고리를 늘려도 이 코드는 그대로다.
        /// </summary>
        /// <param name="onAdjust">[-]/[+]를 눌렀을 때 호출(카테고리, ±1).</param>
        public void Build(VisualElement container, RoguelikeChoiceSO[] categories, Action<RoguelikeCategory, int> onAdjust)
        {
            _rows.Clear();

            // 누락은 화면이 비는 것으로만 드러나 원인 파악이 늦어지므로 로그를 남긴다.
            if (NullCheck.LogIfNull(container, nameof(container), this, "UXML에서 행 컨테이너를 찾지 못했습니다"))
            {
                return;
            }

            // 이 배열의 출처는 PointAllocationPopupUI의 인스펙터라 그쪽을 확인해야 한다.
            if (NullCheck.LogIfEmpty(categories, nameof(categories), this,
                                    $"{nameof(PointAllocationPopupUI)}의 카테고리 목록이 비어 있습니다"))
            {
                return;
            }

            foreach (RoguelikeChoiceSO category in categories)
            {
                if (category == null)
                {
                    continue;
                }

                var row = new VisualElement();
                row.AddToClassList("allocation-row");

                var nameLabel = new Label(category.Title);
                nameLabel.AddToClassList("allocation-name");

                var minusButton = new Button { text = "-" };
                minusButton.AddToClassList("btn");
                minusButton.AddToClassList("allocation-step");

                var pointsLabel = new Label("0");
                pointsLabel.AddToClassList("allocation-value");

                var plusButton = new Button { text = "+" };
                plusButton.AddToClassList("btn");
                plusButton.AddToClassList("allocation-step");

                RoguelikeCategory cat = category.Category; // 클로저 캡처 고정
                minusButton.clicked += () => onAdjust?.Invoke(cat, -1);
                plusButton.clicked += () => onAdjust?.Invoke(cat, +1);

                row.Add(nameLabel);
                row.Add(minusButton);
                row.Add(pointsLabel);
                row.Add(plusButton);
                container.Add(row);

                _rows.Add(new CategoryRow
                {
                    Category = cat,
                    Source = category,
                    NameLabel = nameLabel,
                    PointsLabel = pointsLabel,
                    MinusButton = minusButton,
                    PlusButton = plusButton,
                });
            }
        }

        /// <summary>카테고리 이름을 현재 언어로 다시 그린다(행을 새로 만들지 않는다).</summary>
        public void RefreshLabels()
        {
            foreach (CategoryRow row in _rows)
            {
                if (row.Source != null)
                {
                    row.NameLabel.text = row.Source.Title;
                }
            }
        }

        /// <summary>각 행의 현재 포인트와 버튼 활성 상태를 세이브 값으로 다시 그린다.</summary>
        public void Refresh(SaveData save, int available)
        {
            if (save == null)
            {
                return;
            }

            foreach (CategoryRow row in _rows)
            {
                int points = save.GetPoints(row.Category);
                row.PointsLabel.text = points.ToString();
                row.MinusButton.SetEnabled(points > 0);
                row.PlusButton.SetEnabled(available > 0);
            }
        }
    }
}