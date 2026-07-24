using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using Assets.MyAssets.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 배틀 퍼즈 오버레이(UI Toolkit) 겸 <see cref="IPauseGate"/> 구현.
    /// ESC 키와 HUD 우상단 퍼즈 버튼 두 경로로 열리고, 같은 두 경로로 닫힌다.
    ///
    /// 멈추는 방식은 Time.timeScale이 아니라 게이트 대기다(이유는 <see cref="IPauseGate"/> 주석 참고).
    ///  - 몬스터 차례: 시뮬레이션이 행동 직전에 <see cref="WaitWhilePausedAsync"/>에서 멈춘다
    ///  - 플레이어 차례: 셀렉터가 입력을 기다리는 중이므로, 입력 게이트만 닫으면 자연히 멈춘다
    /// </summary>
    public sealed class BattlePausePanel : BasePanelUI, IPauseGate
    {
        protected override string RootElementName => "pause-panel";

        [Tooltip("우상단 퍼즈 버튼을 제공하는 HUD. 비워두면 ESC로만 퍼즈할 수 있다.")]
        [SerializeField] private BattleHUD _hud;

        private Label _stageLabel;
        private Label _bestLabel;

        /// <summary>
        /// 퍼즈 해제를 기다리는 신호. null이면 퍼즈 중이 아니다.
        ///
        /// 여기는 <see cref="PendingSignal{T}"/>를 쓰지 않는다 — 그쪽은 "기다리는 쪽이 신호를 연다"는 모델인데,
        /// 이 게이트는 <b>퍼즈가 신호를 열고 시뮬레이션이 기다린다</b>(아예 기다리지 않을 수도 있다).
        /// 소유 관계가 반대라 억지로 맞추면 오히려 흐려진다.
        /// </summary>
        private TaskCompletionSource<bool> _resumeSignal;

        /// <summary>전투가 진행 중일 때만 퍼즈를 허용한다(선택지·결과 화면에서는 입력이 겹친다).</summary>
        private bool _battleActive;

        private int _stage;

        /// <summary>
        /// 배틀 입력을 되살릴 프레임(-1이면 예약 없음).
        /// '계속하기'를 <b>클릭</b>해서 풀면 같은 프레임에 <see cref="TargetingController"/>가 그 클릭을
        /// 타겟팅 클릭으로 다시 읽어 몬스터를 겨냥해버린다(퍼즈 오버레이는 UI Toolkit이라 3D 레이캐스트를 막지 않음).
        /// 그래서 입력 복구를 한 프레임 미룬다.
        /// </summary>
        private int _enableInputAtFrame = -1;

        /// <summary>플레이어가 '배틀 중단'을 눌렀다. 구독자(<see cref="BattleDirector"/>)가 전투를 취소한다.</summary>
        public event Action AbortRequested;

        public bool IsPaused => _resumeSignal != null;

        private void Awake()
        {
            VisualElement root = _document.rootVisualElement;
            _stageLabel = root.Q<Label>("pause-stage");
            _bestLabel = root.Q<Label>("pause-best");

            Button resume = root.Q<Button>("pause-resume");
            if (resume != null)
                resume.clicked += Resume;

            Button abort = root.Q<Button>("pause-abort");
            if (abort != null)
                abort.clicked += Abort;

            HideOverlay();
        }

        private void OnEnable()
        {
            if (_hud != null)
                _hud.PauseClicked += Pause;
        }

        private void OnDisable()
        {
            if (_hud != null)
                _hud.PauseClicked -= Pause;

            // InputManager는 DontDestroyOnLoad라, 퍼즈 중에 씬이 바뀌면 배틀 입력이 잠긴 채로 다음 런까지
            // 이어진다. 복구 예약(_enableInputAtFrame)도 Update가 더 돌지 않으면 소멸하므로 여기서 확정한다.
            bool needsRestore = _enableInputAtFrame >= 0 || IsPaused;

            TaskCompletionSource<bool> pending = _resumeSignal;
            _resumeSignal = null;
            _enableInputAtFrame = -1;
            pending?.TrySetCanceled();

            if (needsRestore && InputManager.Instance != null)
                InputManager.Instance.IsGameplayInputEnabled = true;
        }

        private void Update()
        {
            InputManager input = InputManager.Instance;
            if (input == null)
                return;

            if (_enableInputAtFrame >= 0 && Time.frameCount >= _enableInputAtFrame)
            {
                _enableInputAtFrame = -1;
                input.IsGameplayInputEnabled = true;
            }

            if (!input.PauseTogglePressed)
                return;

            if (IsPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>퍼즈 가능 구간을 켜고 끈다. 끌 때 퍼즈 중이었다면 안전하게 해제한다.</summary>
        public void SetBattleActive(bool active)
        {
            _battleActive = active;

            if (!active && IsPaused)
                Resume();

            if (_hud != null)
                _hud.SetPauseButtonVisible(active);
        }

        /// <summary>퍼즈 화면에 표시할 현재 스테이지(스테이지 시작 시 <see cref="BattleDirector"/>가 밀어넣는다).</summary>
        public void SetStage(int stage) => _stage = stage;

        public void Pause()
        {
            if (!_battleActive || IsPaused)
                return;

            _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 배틀 입력만 닫는다 — UI 입력은 열려 있어야 퍼즈 메뉴를 조작할 수 있다.
            _enableInputAtFrame = -1; // 직전 해제의 복구 예약이 남아 있으면 취소
            if (InputManager.Instance != null)
                InputManager.Instance.IsGameplayInputEnabled = false;

            RefreshTexts();
            ShowOverlay();
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            ReleaseGate()?.TrySetResult(true);
            HideOverlay();
        }

        /// <summary>
        /// 전투를 즉시 끝내고 결과 화면으로 넘어간다.
        /// 구독자가 먼저 취소를 걸도록 이벤트를 발생시킨 뒤 대기를 푼다 — 순서가 뒤집히면
        /// 시뮬레이션이 취소 플래그를 보기 전에 한 행동을 더 진행할 수 있다.
        /// </summary>
        private void Abort()
        {
            if (!IsPaused)
                return;

            HideOverlay();
            AbortRequested?.Invoke();

            // 취소 토큰이 이미 대기를 끊었더라도 TrySetCanceled는 안전하게 무시된다.
            ReleaseGate()?.TrySetCanceled();
        }

        public Task WaitWhilePausedAsync(CancellationToken ct)
        {
            TaskCompletionSource<bool> pending = _resumeSignal;
            return pending == null ? Task.CompletedTask : AwaitResumeAsync(pending, ct);
        }

        private static async Task AwaitResumeAsync(TaskCompletionSource<bool> pending, CancellationToken ct)
        {
            using (ct.Register(() => pending.TrySetCanceled(ct)))
                await pending.Task;
        }

        /// <summary>퍼즈 상태를 끝내고 대기 신호를 돌려준다(호출자가 완료/취소 중 하나로 매듭짓는다).</summary>
        private TaskCompletionSource<bool> ReleaseGate()
        {
            TaskCompletionSource<bool> pending = _resumeSignal;
            _resumeSignal = null;
            _enableInputAtFrame = Time.frameCount + 1; // 이유는 필드 주석 참고
            return pending;
        }

        private void RefreshTexts()
        {
            if (_stageLabel != null)
                _stageLabel.text = _stage.ToString();

            if (_bestLabel == null)
                return;

            // 이번 런은 아직 기록되지 않았으므로 여기 값이 곧 "이전 최고 기록"이다.
            int best = SaveService.Current.BestStage;
            bool hasRecord = best > 0;
            _bestLabel.style.display = hasRecord ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasRecord)
                _bestLabel.text = $"이전 최고 기록 {best}";
        }

        // BasePanelUI의 Show/Hide를 그대로 쓰되, 이 패널에서는 "퍼즈 상태 전환"(Pause/Resume)과
        // "오버레이 표시"를 구분해야 해서 표시 전용 별칭을 둔다.
        private void ShowOverlay() => Show();
        private void HideOverlay() => Hide();
    }
}
