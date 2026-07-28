using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 성향 포인트 배분 팝업
    /// 최고 스테이지 돌파로 얻은 영구 포인트를 로그라이크 카테고리별 선택지의 등장 가중치에 투자한다.
    ///
    /// 화면 담당 — 카테고리 목록은 <see cref="AllocationRowsView"/>가 그리고,
    /// 얼마를 투자할 수 있는지는 <see cref="SaveData.TryAdjustPoints"/>가 판정한다.
    /// 파일 저장은 팝업을 닫을 때(Hide) 1회만 수행한다.
    /// </summary>
    public sealed class PointAllocationPopupUI : BasePanelUI
    {
        [Header("로그라이크 선택지 SO")]
        [SerializeField] private RoguelikeChoiceSO[] _categories;

        [Tooltip("몇 스테이지마다 영구 포인트 1점을 획득하는지(밸런싱 값이라 인스펙터에서 조정)")]
        [SerializeField] private int _stagesPerPoint = 10; // Default = 10( n(StagesPerPoint)스테이지 도달 시 1 포인트 획득 )

        private readonly AllocationRowsView _rows = new();

        private Label _headerLabel;

        protected override void Start()
        {
            _headerLabel = Root.Q<Label>("allocation-points");

            Root.Q<Button>("allocation-reset-button").clicked += OnReset;
            Root.Q<Button>("allocation-close-button").clicked += Hide;

            _rows.Build(container: Root.Q<VisualElement>("allocation-rows"),
                        categories: _categories,
                        onAdjust: Adjust);

            Refresh();
        }

        /// <summary>팝업을 열 때마다 최신 획득 포인트를 반영</summary>
        public override void Show()
        {
            base.Show();
            Refresh();
        }

        public override void Hide()
        {
            base.Hide();
            SaveService.Save(); // 팝업을 숨기고 나서 값을 영구 저장
        }

        /// <summary>값이 바뀌었으면 UI에 반영</summary>
        private void Adjust(RoguelikeCategory category, int delta)
        {
            if (SaveService.Current.TryAdjustPoints(category, delta, _stagesPerPoint))
            {
                Refresh();
            }
        }

        private void OnReset()
        {
            SaveService.Current.ResetPoints();
            Refresh();
        }

        /// <summary>헤더와 모든 행을 세이브 값으로 다시 그린다.</summary>
        private void Refresh()
        {
            SaveData save = SaveService.Current;
            int earned = save.GetEarnedPoints(_stagesPerPoint);
            int available = save.GetAvailablePoints(_stagesPerPoint);

            if (_headerLabel is not null)
                _headerLabel.text = $"보유 {available} / 총 {earned}";

            _rows.Refresh(save, available);
        }
    }
}
