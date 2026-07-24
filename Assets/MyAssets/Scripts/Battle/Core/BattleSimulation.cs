using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 한 전투(스테이지)의 진행을 담당하는 비동기 상태 머신(순수 로직, UnityEngine 비의존).
    ///
    /// 흐름: BattleStarted → [턴: SPD 재정렬 → 각 유닛 행동(입력/AI 대기 → 데미지 해소 → 연출 대기 → 사망 처리)]
    ///       → 전투 종료 판정 → BattleEnded
    ///
    /// View는 이벤트를 구독하여 연출하고, 애니메이션이 끝날 때까지 <see cref="PlaybackEventArgs.RegisterPlayback"/>로
    /// 시뮬레이션을 대기시킨다. 플레이어 입력은 셀렉터의 await로 자연스럽게 멈춘다.
    /// </summary>
    public sealed class BattleSimulation
    {
        private readonly BattleState _state;
        private readonly IActionSelector _playerSelector;
        private readonly IActionSelector _enemySelector;
        private readonly IRandom _rng;
        private readonly IPauseGate _pauseGate;

        public BattleState State => _state;

        public event EventHandler BattleStarted;
        public event EventHandler<TurnStartedEventArgs> TurnStarted;
        public event EventHandler<TurnEndedEventArgs> TurnEnded;
        public event EventHandler<ActorTurnEventArgs> ActorTurnStarted;
        public event EventHandler<ActionResolvedEventArgs> ActionResolved;
        public event EventHandler<UnitDiedEventArgs> UnitDied;
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler<StatusTickedEventArgs> StatusTicked;
        public event EventHandler<BattleEndedEventArgs> BattleEnded;

        /// <param name="pauseGate">퍼즈 게이트. null이면 퍼즈 없이 그대로 진행한다.</param>
        public BattleSimulation(BattleState state, IActionSelector playerSelector, IActionSelector enemySelector, IRandom rng,
                                IPauseGate pauseGate = null)
        {
            _state = state;
            _playerSelector = playerSelector;
            _enemySelector = enemySelector;
            _rng = rng;
            _pauseGate = pauseGate;
        }

        /// <summary>전투를 끝까지 진행하고 결과를 반환한다. 씬 종료 등으로 취소되면 OperationCanceledException.</summary>
        public async Task<BattleOutcome> RunAsync(CancellationToken cancellationToken = default)
        {
            BattleStarted?.Invoke(this, EventArgs.Empty);

            int turnNumber = 1;
            while (!_state.IsBattleOver)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // TurnOrder/BattleState는 재사용 버퍼를 돌려주므로 이 턴 안에서만 순회한다(보관 금지).
                IReadOnlyList<Unit> order = TurnOrder.Build(_state.AliveUnits);
                for (int i = 0; i < order.Count; i++)
                    order[i].TickCooldown();

                TurnStarted?.Invoke(this, new TurnStartedEventArgs(turnNumber, order));

                for (int i = 0; i < order.Count; i++)
                {
                    Unit actor = order[i];

                    // 배틀 중단(취소)을 매 유닛 행동 직전에 확인한다 — 턴 시작 지점에서만 보면
                    // 플레이어 차례 중 중단해도 이번 턴의 남은 몬스터 행동이 모두 끝난 뒤에야 반영된다.
                    cancellationToken.ThrowIfCancellationRequested();

                    // 퍼즈는 행동 "직전"에만 걸린다 — 진행 중인 연출을 자르지 않기 위함.
                    if (_pauseGate != null)
                        await _pauseGate.WaitWhilePausedAsync(cancellationToken);

                    if (!actor.IsAlive) continue;      // 이번 턴에 먼저 사망한 유닛은 건너뜀
                    if (_state.IsBattleOver) break;

                    // 상태이상 처리(도트 → 지속시간 감소 → 기절 판정). 행동할 수 없으면 이번 차례는 넘어간다.
                    // 로그라이크 '몬스터 행동불가' 선택지도 스폰 시 부여된 Stun이라 여기서 함께 처리된다.
                    if (!await ResolveStatusesAsync(actor)) continue;

                    ActorTurnStarted?.Invoke(this, new ActorTurnEventArgs(actor));

                    IActionSelector selector = actor.Team == TeamSide.Player ? _playerSelector : _enemySelector;
                    ActionPlan plan = await selector.SelectAsync(actor, _state, cancellationToken);
                    if (plan == null || plan.Targets.Count == 0) continue;

                    ActionResult result = ResolveAction(plan);

                    var actionArgs = new ActionResolvedEventArgs(result);
                    ActionResolved?.Invoke(this, actionArgs);
                    await actionArgs.WhenPlaybackComplete();

                    foreach (HitResult hit in result.Hits)
                    {
                        if (!hit.WasLethal) continue;
                        var deathArgs = new UnitDiedEventArgs(hit.Target);
                        UnitDied?.Invoke(this, deathArgs);
                        await deathArgs.WhenPlaybackComplete();
                    }
                }

                TurnEnded?.Invoke(this, new TurnEndedEventArgs(turnNumber));
                turnNumber++;
            }

            BattleOutcome outcome = _state.IsPartyWiped ? BattleOutcome.Defeat : BattleOutcome.Victory;
            List<Unit> survivors = _state.Players.Where(u => u.IsAlive).ToList();
            BattleEnded?.Invoke(this, new BattleEndedEventArgs(outcome, survivors));
            return outcome;
        }

        /// <summary>
        /// 자기 차례 시작 시 상태이상을 처리한다. 도트 피해 → 지속 턴 감소 → 기절 판정 순.
        /// 행동할 수 있으면 true, 기절했거나 도트로 쓰러졌으면 false.
        /// </summary>
        private async Task<bool> ResolveStatusesAsync(Unit actor)
        {
            if (!actor.HasAnyStatus) return true;

            int dot = actor.GetDotDamage();
            if (dot > 0)
            {
                int applied = actor.ApplyDamage(dot);

                var tickArgs = new StatusTickedEventArgs(actor, applied);
                StatusTicked?.Invoke(this, tickArgs);
                await tickArgs.WhenPlaybackComplete();

                if (!actor.IsAlive)
                {
                    var deathArgs = new UnitDiedEventArgs(actor);
                    UnitDied?.Invoke(this, deathArgs);
                    await deathArgs.WhenPlaybackComplete();
                    return false;
                }
            }

            // 기절 여부는 감소 "전"에 읽는다 — 그래야 1턴짜리 기절이 한 번의 행동을 실제로 막는다.
            bool stunned = actor.IsStunned;

            List<StatusKind> expired = actor.TickStatuses();
            if (expired != null)
            {
                foreach (StatusKind kind in expired)
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(actor, kind, StatusChangeReason.Expired));
            }

            // 남은 상태들도 턴 수가 줄었으므로 알린다 — 이게 없으면 표시가 부여 시점 값에 멈춘다.
            // View가 목록 전체를 다시 읽으므로 상태 개수만큼이 아니라 유닛당 1회만 보낸다.
            if (actor.HasAnyStatus)
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(actor, null, StatusChangeReason.Ticked));

            return !stunned;
        }

        /// <summary>계획된 행동의 데미지를 실제로 계산·적용한다(순수). 라인 스킬은 대상별로 순차 처리.</summary>
        private ActionResult ResolveAction(ActionPlan plan)
        {
            var hits = new List<HitResult>(plan.Targets.Count);
            foreach (Unit target in plan.Targets)
            {
                if (!target.IsAlive) continue;

                DamageResult dmg = DamageCalculator.Calculate(plan.Actor, target, plan.PowerMultiplier, _rng);
                int applied = target.ApplyDamage(dmg.Amount);
                hits.Add(new HitResult(target, applied, dmg.IsCritical, !target.IsAlive));

                // 스킬에 딸린 상태이상은 살아남은 대상에게만 부여를 시도한다(저항 판정은 Unit이 담당).
                if (plan.Kind == ActionKind.Skill && target.IsAlive)
                    TryApplySkillStatus(plan.Actor.Skill, target);
            }
            return new ActionResult(plan.Actor, plan.Kind, hits);
        }

        private void TryApplySkillStatus(SkillProfile skill, Unit target)
        {
            if (skill?.Status == null) return;

            StatusEffect effect = skill.Status.Value;
            bool applied = target.TryApplyStatus(effect, _rng);

            StatusChanged?.Invoke(this, new StatusChangedEventArgs(
                target, effect.Kind, applied ? StatusChangeReason.Applied : StatusChangeReason.Resisted));
        }
    }
}