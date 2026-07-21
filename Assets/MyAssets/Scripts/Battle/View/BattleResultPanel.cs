using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 런이 끝났을 때 성과(도달 스테이지)를 보여주는 결과 팝업(UI Toolkit).
    /// 확인 버튼 클릭을 await로 받아, 플레이어가 결과를 본 뒤에 씬을 전환할 수 있게 한다
    /// (RoguelikeChoicePanel과 동일한 입력-대기 패턴).
    ///
    /// 무한 타워라 승리 조건이 없으므로 현재는 전멸(리타이어)에서만 뜬다.
    /// </summary>
    public sealed class BattleResultPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _root;
        private Label _title;
        private Label _stage;
        private Label _best;
        private TaskCompletionSource<bool> _pending;

        private void Awake()
        {
            VisualElement root = _document.rootVisualElement;
            _root = root.Q<VisualElement>("result-panel");
            _title = root.Q<Label>("result-title");
            _stage = root.Q<Label>("result-stage");
            _best = root.Q<Label>("result-best");

            Button confirm = root.Q<Button>("result-confirm");
            if (confirm != null)
                confirm.clicked += () => _pending?.TrySetResult(true);

            Hide();
        }

        /// <summary>결과를 띄우고 플레이어가 확인을 누를 때까지 기다린다.</summary>
        /// <param name="reachedStage">이번 런에서 도달한 스테이지.</param>
        /// <param name="bestStage">이번 런 이전까지의 최고 기록. 0 이하면 표시하지 않는다(기록이 없는 첫 런).</param>
        /// <param name="isNewRecord">
        /// 기록을 새로 세웠는지. 판정은 저장 계층이 하고 패널은 결과만 표시한다 —
        /// 여기서 <paramref name="reachedStage"/>와 비교해 직접 판정하면 동점도 신기록으로 처리된다.
        /// </param>
        public async Task PresentAsync(int reachedStage, int bestStage, bool isNewRecord, CancellationToken ct)
        {
            if (_stage != null)
                _stage.text = reachedStage.ToString();

            if (_best != null)
            {
                bool hasRecord = bestStage > 0;
                _best.style.display = hasRecord ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasRecord)
                    _best.text = isNewRecord ? "신기록!" : $"최고 기록 {bestStage}";
            }

            _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Show();
            using (ct.Register(() => _pending.TrySetCanceled(ct)))
            {
                await _pending.Task;
                _pending = null;
                Hide();
            }
        }

        /// <summary>결과 문구를 바꾼다(승리 조건이 생기면 사용).</summary>
        public void SetTitle(string text)
        {
            if (_title != null)
                _title.text = text;
        }

        private void Show() => _root.style.display = DisplayStyle.Flex;
        private void Hide() => _root.style.display = DisplayStyle.None;
    }
}
