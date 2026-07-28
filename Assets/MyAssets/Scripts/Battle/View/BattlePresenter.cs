using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Core(BattleSimulation)의 이벤트를 구독해 연출과 HUD를 갱신하는 연출 담당.
    /// 전투 규칙은 전혀 모르고 "언제 무엇을 보여줄지"만 담당하며,
    /// 애니메이션이 끝날 때까지의 Task를 이벤트에 등록해 시뮬레이션을 대기시킨다.
    ///
    /// 시뮬레이션은 웨이브마다 새로 만들어지므로 <see cref="Bind"/>/<see cref="Unbind"/>로 구독을 갈아끼운다.
    /// </summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private BattleHUD _hud;
        [SerializeField] private CameraShake _cameraShake;

        // 레지스트리는 인스펙터가 아니라 BattleDirector가 Initialize로 주입한다
        // (MonsterSpawner·TargetingController와 같은 방식 — 슬롯을 중복으로 두면 둘이 다른 오브젝트를 가리켜도 알 수 없다).
        private UnitViewRegistry _registry;

        [Header("연출")]
        [Tooltip("공격 시작 후 타격이 적중하는 시점까지의 지연(초).")]
        [SerializeField] private float _impactDelay = 0.35f;
        [Tooltip("현재 행동 중인 유닛에 씌울 레이어 이름(파랑 아웃라인). 렌더러만 이 레이어로 옮긴다.")]
        [SerializeField] private string _activeLayerName = "ActiveUnit";

        private BattleSimulation _simulation;
        private CancellationToken _ct;

        private int _activeLayer;
        // 현재 파랑 강조 중인 행동 유닛 View(다음 행동 유닛으로 넘어갈 때 원복).
        private UnitView _activeView;

        private void Awake() => _activeLayer = LayerMask.NameToLayer(_activeLayerName);

        /// <summary>씬 종료 시 연출을 취소할 토큰과 View 레지스트리를 받아둔다(전투 시작 전 1회).</summary>
        public void Initialize(CancellationToken ct, UnitViewRegistry registry)
        {
            _ct = ct;
            _registry = registry;
        }

        public void SetStage(int stage)
        {
            if (_hud != null)
                _hud.SetStage(stage);
        }

        /// <summary>이번 스테이지에 발동 중인 파티 시너지를 HUD에 표시한다.</summary>
        public void SetSynergies(IReadOnlyList<ActiveSynergy> synergies)
        {
            if (_hud != null)
                _hud.ShowSynergies(synergies);
        }

        /// <summary>이번 웨이브의 시뮬레이션에 연출을 연결한다.</summary>
        public void Bind(BattleSimulation simulation)
        {
            Unbind();

            _simulation = simulation;
            _simulation.TurnStarted += OnTurnStarted;
            _simulation.ActorTurnStarted += OnActorTurnStarted;
            _simulation.ActionResolved += OnActionResolved;
            _simulation.UnitDied += OnUnitDied;
            _simulation.StatusChanged += OnStatusChanged;
            _simulation.StatusTicked += OnStatusTicked;
            _simulation.BattleEnded += OnBattleEnded;
        }

        public void Unbind()
        {
            if (_simulation == null)
                return;

            _simulation.TurnStarted -= OnTurnStarted;
            _simulation.ActorTurnStarted -= OnActorTurnStarted;
            _simulation.ActionResolved -= OnActionResolved;
            _simulation.UnitDied -= OnUnitDied;
            _simulation.StatusChanged -= OnStatusChanged;
            _simulation.StatusTicked -= OnStatusTicked;
            _simulation.BattleEnded -= OnBattleEnded;
            _simulation = null;

            ClearActive();
        }

        private void OnDestroy() => Unbind();

        // ── HUD 갱신 ──

        private void OnTurnStarted(object sender, TurnStartedEventArgs e)
        {
            if (_hud != null)
                _hud.ShowTurnOrder(e.Order);
        }

        private void OnActorTurnStarted(object sender, ActorTurnEventArgs e)
        {
            if (_hud != null)
                _hud.SetActiveUnit(e.Actor);

            HighlightActive(e.Actor.Id);
        }

        /// <summary>이전 행동 유닛의 파랑 강조를 원복하고, 새 행동 유닛을 ActiveUnit 레이어로 옮긴다.</summary>
        private void HighlightActive(int unitId)
        {
            ClearActive();

            if (_activeLayer >= 0 && _registry.TryGet(unitId, out UnitView view))
            {
                view.SetOutlineLayer(_activeLayer);
                _activeView = view;
            }
        }

        private void ClearActive()
        {
            if (_activeView != null)
                _activeView.ResetOutlineLayer();
            _activeView = null;
        }

        // ── 연출 (연출 Task를 등록하면 시뮬레이션이 완료를 대기) ──

        private void OnActionResolved(object sender, ActionResolvedEventArgs e)
        {
            if (_hud != null)
                _hud.HidePrompt(); // 입력 완료 → 연출 단계이므로 "당신의 차례" 프롬프트 숨김

            e.RegisterPlayback(PlayActionAsync(e.Result));
        }

        private void OnUnitDied(object sender, UnitDiedEventArgs e)
        {
            if (_hud != null)
                _hud.MarkDead(e.Unit.Id);

            if (_registry.TryGet(e.Unit.Id, out UnitView view))
                e.RegisterPlayback(view.PlayDieAsync(_ct));
        }

        /// <summary>상태이상이 붙거나 풀렸을 때 체력바 옆 표기를 다시 그린다(저항은 표기 변화가 없다).</summary>
        private void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Reason == StatusChangeReason.Resisted)
                return;

            if (_registry.TryGet(e.Unit.Id, out UnitView view))
                view.RefreshStatuses(e.Unit.Statuses);
        }

        /// <summary>도트 피해는 일반 피격과 같은 연출을 재사용한다(체력바 갱신 포함).</summary>
        private void OnStatusTicked(object sender, StatusTickedEventArgs e)
        {
            if (_registry.TryGet(e.Unit.Id, out UnitView view))
                e.RegisterPlayback(view.PlayHitAsync(e.Unit.CurrentHp, e.Unit.Stats.MaxHp, _ct));
        }

        private async Task PlayActionAsync(ActionResult result)
        {
            if (!_registry.TryGet(result.Actor.Id, out UnitView actorView))
                return;

            // 1) 공격/스킬 애니메이션 시작
            Task actorAnim = result.Kind == ActionKind.Skill
                ? actorView.PlaySkillAsync(_ct)
                : actorView.PlayAttackAsync(_ct);

            // 2) 타격 시점까지 대기 후 피격 연출 + 체력바 갱신
            await Awaitable.WaitForSecondsAsync(_impactDelay, _ct);

            var playback = new List<Task> { actorAnim };
            bool anyCritical = false;
            foreach (HitResult hit in result.Hits)
            {
                if (hit.IsCritical) anyCritical = true;
                if (_registry.TryGet(hit.Target.Id, out UnitView targetView))
                    playback.Add(targetView.PlayHitAsync(hit.Target.CurrentHp, hit.Target.Stats.MaxHp, _ct));
            }

            if (anyCritical)
            {
                if (_cameraShake != null)
                    _cameraShake.Shake();
                AudioManager.Sfx(AudioManager.Library?.Critical);
            }

            await Task.WhenAll(playback);
        }

        private void OnBattleEnded(object sender, BattleEndedEventArgs e)
        {
            ClearActive();
            Debug.Log($"[BattlePresenter] 전투 종료: {e.Outcome} (생존 아군 {e.Survivors.Count}명)");
        }
    }
}
