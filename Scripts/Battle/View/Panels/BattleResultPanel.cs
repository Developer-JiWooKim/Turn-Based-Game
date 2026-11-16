using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.UI;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View.Panels
{
    /// <summary>
    /// 런이 끝났을 때 성과(도달 스테이지)를 보여주는 결과 팝업(UI Toolkit).
    /// 확인 버튼 클릭을 await로 받아, 플레이어가 결과를 본 뒤에 씬을 전환할 수 있게 한다
    ///
    /// 무한 진행형이라 승리 조건이 없으므로 현재는 전멸(리타이어)에서만 뜬다.
    /// </summary>
    public sealed class BattleResultPanel : BasePanelUI
    {
        protected override string RootElementName => "result-panel";

        private readonly PendingSignal<bool> _pending = new();

        private Label _title;
        private Label _stage;
        private Label _best;

        private void Awake()
        {
            _title = Require<Label>("result-title", "결과 제목이 표시되지 않습니다");
            _stage = Require<Label>("result-stage", "도달 스테이지가 표시되지 않습니다");
            _best = Require<Label>("result-best", "최고 기록이 표시되지 않습니다");

            BindButton("result-confirm", () => _pending.Complete(true));

            Hide();
        }

        /// <summary>결과를 띄우고 플레이어가 확인을 누를 때까지 기다린다.</summary>
        /// <param name="reachedStage">이번 런에서 도달한 스테이지</param>
        /// <param name="bestStage">이번 런 이전까지의 최고 기록. 0 이하면 표시하지 않는다(기록이 없는 첫 런).</param>
        /// <param name="isNewRecord">
        /// 기록을 새로 세웠는지. 판정은 저장 계층이 하고 패널은 결과만 표시한다
        /// — 여기서 <paramref name="reachedStage"/>와 비교해 직접 판정하면 동점도 신기록으로 처리된다.
        /// </param>
        public async Task PresentAsync(int reachedStage, int bestStage, bool isNewRecord, CancellationToken ct)
        {
            if (_stage != null)
            {
                _stage.text = reachedStage.ToString();
            }

            if (_best != null)
            {
                bool hasRecord = bestStage > 0;
                _best.style.display = hasRecord ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasRecord)
                {
                    _best.text = isNewRecord ? Loc.Get("ui.result.newRecord") : Loc.Format("ui.result.bestRecord", bestStage);
                }
            }

            Show();

            try
            {
                await _pending.WaitAsync(ct);
            }
            finally
            {
                Hide(); // 취소(씬 종료 등)로 빠져나가도 패널이 열린 채 남지 않도록
            }
        }

        /// <summary>결과 문구를 바꾼다(승리 조건이 생기면 사용).</summary>
        public void SetTitle(string text)
        {
            if (_title != null)
            {
                _title.text = text;
            }
        }
    }
}
