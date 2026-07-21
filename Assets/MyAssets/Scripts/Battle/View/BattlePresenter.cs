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
        [SerializeField] private UnitViewRegistry _registry;
        [SerializeField] private BattleHUD _hud;
        [SerializeField] private CameraShake _cameraShake;

        [Header("연출")]
        [Tooltip("공격 시작 후 타격이 적중하는 시점까지의 지연(초).")]
        [SerializeField] private float _impactDelay = 0.35f;

        private BattleSimulation _simulation;
        private CancellationToken _ct;

        /// <summary>씬 종료 시 연출을 취소할 토큰을 받아둔다(전투 시작 전 1회)</summary>
        public void Initialize(CancellationToken ct) => _ct = ct;

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
            _simulation.BattleEnded -= OnBattleEnded;
            _simulation = null;
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
            Debug.Log($"[BattlePresenter] 전투 종료: {e.Outcome} (생존 아군 {e.Survivors.Count}명)");
        }
    }
}
