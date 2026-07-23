using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 성향 포인트 배분 팝업.
    /// 최고 스테이지 돌파로 얻은 영구 포인트를 로그라이크 카테고리별 선택지의 등장 가중치에 투자한다.
    /// 카테고리 행은 데이터(할당된 선택지 SO) 기반으로 C#에서 동적 생성하고, 파일 저장은 팝업을 닫을 때(Hide) 1회만 수행한다.
    /// </summary>
    public sealed class PointAllocationPopupUI : BasePanelUI
    {
        [SerializeField] private RoguelikeChoiceSO[] _categories;

        [Tooltip("몇 스테이지마다 영구 포인트 1점을 획득하는지(밸런싱 값이라 인스펙터에서 조정)")]
        [SerializeField] private int _stagesPerPoint = 10; // Default = 10( n(StagesPerPoint)스테이지 도달 시 1 포인트 획득 )

        private Label _headerLabel;
        private VisualElement _rowsContainer;
        private readonly List<CategoryRow> _rows = new List<CategoryRow>();

        /// <summary>카테고리 1종의 행 UI</summary>
        private sealed class CategoryRow
        {
            public RoguelikeCategory Category;
            public Label PointsLabel;
            public Button MinusButton;
            public Button PlusButton;
        }

        protected override void Start()
        {
            _headerLabel = Root.Q<Label>("allocation-points");
            _rowsContainer = Root.Q<VisualElement>("allocation-rows");

            Root.Q<Button>("allocation-reset-button").clicked += OnReset;
            Root.Q<Button>("allocation-close-button").clicked += Hide;

            BuildRows();
            Refresh();
        }

        public override void Hide()
        {
            base.Hide();
            SaveService.Save();
        }

        /// <summary>팝업을 열 때마다 최신 획득 포인트를 반영</summary>
        public override void Show()
        {
            base.Show();
            Refresh();
        }

        /// <summary>카테고리별 행(이름 + 현재 포인트 + [-]/[+])을 데이터 기반으로 제작</summary>
        private void BuildRows()
        {
            if (_rowsContainer is null || _categories is null)
            {
                Debug.LogError("[PointAllocationPopupUI] BuildRows(): _rowsContainer or _categories is null");
                return;
            }

            foreach (RoguelikeChoiceSO category in _categories)
            {
                if (category == null)
                    continue;

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

                RoguelikeCategory cat = category.Category;
                minusButton.clicked += () => Adjust(cat, -1);
                plusButton.clicked += () => Adjust(cat, +1);

                row.Add(nameLabel);
                row.Add(minusButton);
                row.Add(pointsLabel);
                row.Add(plusButton);
                _rowsContainer.Add(row);

                _rows.Add(new CategoryRow
                {
                    Category = cat,
                    PointsLabel = pointsLabel,
                    MinusButton = minusButton,
                    PlusButton = plusButton,
                });
            }
        }

        private void Adjust(RoguelikeCategory category, int delta)
        {
            SaveData save = SaveService.Current;
            int current = save.GetPoints(category);

            if (delta > 0 && Available(save) <= 0)
                return;
            if (delta < 0 && current <= 0)
                return;

            save.SetPoints(category, current + delta);
            Refresh();
        }

        private void OnReset()
        {
            SaveService.Current.ResetPoints();
            Refresh();
        }

        /// <summary>남은 배분 가능 포인트 = 획득 총량 - 이미 투자한 합계</summary>
        private int Available(SaveData save) => save.GetEarnedPoints(_stagesPerPoint) - save.GetSpentPoints();

        /// <summary>헤더와 모든 행(현재 포인트 + 버튼 활성 상태)을 세이브 값으로 다시 Refresh</summary>
        private void Refresh()
        {
            SaveData save = SaveService.Current;
            int earned = save.GetEarnedPoints(_stagesPerPoint);
            int available = earned - save.GetSpentPoints();

            if (_headerLabel is not null)
                _headerLabel.text = $"보유 {available} / 총 {earned}";

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
