using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 한 전투(스테이지)의 진행을 담당하는 시뮬레이터
    ///
    /// 흐름: BattleStarted → [턴: SPD 재정렬 → 각 유닛 행동(입력/AI 대기 → 데미지 해소 → 연출 대기 → 사망 처리)]
    ///       → 전투 종료 판정 → BattleEnded
    ///
    /// View는 이벤트를 구독하여 연출하고, 애니메이션이 끝날 때까지 <see cref="PlaybackEventArgs.RegisterPlayback"/>로
    /// 시뮬레이션을 대기시킨다. 플레이어 입력은 셀렉터의 await로 자연스럽게 멈춘다.
    ///
    /// 이 클래스의 await는 "시간이 흐르기"를 기다리는 게 아니라 "바깥이 끝났다고 알려주기"를 기다린다.
    /// 멈추는 지점은 셋뿐이다 — 플레이어 입력(셀렉터), 연출 완료(WhenPlaybackComplete), 퍼즈 해제(<see cref="IPauseGate"/>).
    /// Core는 UnityEngine에 의존하지 않아 프레임도 시간도 모르므로, 실제로 얼마나 멈출지는 전부 View가 정한다
    /// (구독자가 아무것도 등록하지 않으면 모든 대기가 즉시 통과해 전투가 연출 없이 끝까지 돌아간다).
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
        public BattleSimulation(BattleState state,
                                IActionSelector playerSelector,
                                IActionSelector enemySelector,
                                IRandom rng,
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

                // order는 "턴 시작 시점의 생존자"를 굳힌 스냅샷이다. 도중에 죽어도 목록에는 남아 있으므로
                // 행동 직전에 IsAlive를 다시 보고(아래), 도중에 합류한 유닛은 다음 턴부터 순서에 들어온다.
                // TurnOrder/BattleState는 재사용 버퍼를 돌려주므로 이 턴 안에서만 순회한다(보관 금지).
                IReadOnlyList<Unit> order = TurnOrder.Build(_state.AliveUnits);

                for (int i = 0; i < order.Count; i++)
                {
                    order[i].TickCooldown(); // 스킬이 없는 유닛은 내부에서 그냥 통과
                }

                TurnStarted?.Invoke(this, new TurnStartedEventArgs(turnNumber, order)); // 턴 순서 HUD 갱신

                for (int i = 0; i < order.Count; i++)
                {
                    Unit actor = order[i];

                    // 배틀 중단(취소)을 매 유닛 행동 직전에 확인     
                    // 버그 사례 — 턴 시작 지점에서만 보면 플레이어 차례 중 중단해도 이번 턴의 남은 몬스터 행동이 모두 끝난 뒤에야 반영된다.                
                    cancellationToken.ThrowIfCancellationRequested();

                    // 퍼즈는 행동 "직전"에만 걸린다 — 진행 중인 연출을 자르지 않기 위함.
                    if (_pauseGate != null)
                    {
                        await _pauseGate.WaitWhilePausedAsync(cancellationToken);
                    }

                    // 두 검사 모두 await(퍼즈 대기·직전 유닛의 연출) "뒤"에 있어야 한다 — 기다리는 사이에 상태가 바뀐다.
                    if (!actor.IsAlive)
                    {
                        continue;   // 이번 턴에 먼저 행동한 유닛에게 맞아 죽었다
                    }

                    if (_state.IsBattleOver)
                    {
                        break; // while 조건은 턴 사이에만 보므로, 턴 도중 승부가 나면 여기서 끊는다
                    }

                    // 상태이상 처리(도트 → 지속시간 감소 → 기절 판정). 행동할 수 없으면 이번 차례는 넘어간다.
                    // 로그라이크 '몬스터 행동불가' 선택지도 스폰 시 부여된 Stun이라 여기서 함께 처리된다.
                    // 반드시 ActorTurnStarted "전"에 온다 
                    // 버그 사례 — 기절한 플레이어 유닛에게도 차례를 알리면 HUD에 "당신의 차례" 프롬프트가 뜬 채 아무 입력도 받지 않는 상태가 된다.
                    if (!await ResolveStatusesAsync(actor))
                    {
                        continue;
                    }
                    ActorTurnStarted?.Invoke(this, new ActorTurnEventArgs(actor));

                    // 플레이어 셀렉터는 여기서 타겟 입력이 들어올 때까지 멈춘다(몬스터 AI는 즉시 반환).
                    IActionSelector selector = actor.Team == TeamSide.Player ? _playerSelector : _enemySelector;
                    ActionPlan plan = await selector.SelectAsync(actor, _state, cancellationToken);
                    if (plan == null || plan.Targets.Count == 0)
                    {
                        continue; // 때릴 대상이 없다 = 이번 차례는 아무것도 하지 않음
                    }

                    // 데미지는 여기서 "전부" 확정된다. 연출은 그 뒤에 이미 정해진 결과를 재생할 뿐이라,
                    // 애니메이션 길이나 재생 실패가 전투 결과를 바꾸지 못한다.
                    ActionResult result = ResolveAction(plan);
                    var actionArgs = new ActionResolvedEventArgs(result);
                    ActionResolved?.Invoke(this, actionArgs);
                    await actionArgs.WhenPlaybackComplete();

                    // 사망 연출은 공격 연출이 끝난 "뒤" 하나씩 순차로 기다린다(라인 스킬로 여럿이 같이 죽어도 마찬가지).
                    // IsAlive가 아니라 WasLethal을 보는 이유 — 죽음에 이르게 한 그 히트에만 표시가 붙으므로,
                    // 한 유닛이 한 행동에 여러 번 맞아도 사망 이벤트가 두 번 나가지 않는다.
                    foreach (HitResult hit in result.Hits)
                    {
                        if (!hit.WasLethal)
                        {
                            continue;
                        }
                        var deathArgs = new UnitDiedEventArgs(hit.Target);
                        UnitDied?.Invoke(this, deathArgs);
                        await deathArgs.WhenPlaybackComplete();
                    }
                }

                TurnEnded?.Invoke(this, new TurnEndedEventArgs(turnNumber));
                turnNumber++;
            }

            // 양쪽이 동시에 전멸하면 패배로 친다(파티 전멸을 먼저 보므로).
            BattleOutcome outcome = _state.IsPartyWiped ? BattleOutcome.Defeat : BattleOutcome.Victory;

            // 여기만 재사용 버퍼가 아니라 새 리스트다 — 이벤트 밖으로 나가 BattleDirector가 런 데이터에
            // 반영할 때까지 들고 있으므로, 버퍼로 넘기면 다음 조회에 덮어써진다.
            List<Unit> survivors = _state.Players.Where(u => u.IsAlive).ToList();
            BattleEnded?.Invoke(this, new BattleEndedEventArgs(outcome, survivors));
            return outcome;
        }

        /// <summary>
        /// 자기 차례 시작 시 상태이상을 처리한다. 
        /// 도트 피해 → 지속 턴 감소 → 기절 판정 순.
        /// 행동할 수 있으면 true, 기절했거나 도트로 쓰러졌으면 false(호출자는 차례를 통째로 건너뛴다).
        ///
        /// 이 세 단계의 순서가 곧 규칙.
        /// 도트를 먼저 넣어야 도트로 죽은 유닛이 그 차례에 행동하지 못하고,
        /// 기절 판정을 턴 감소보다 뒤로 미루면 1턴짜리 기절이 아무 행동도 막지 못한 채 사라진다.
        /// "지속 턴 = 자기 행동 기회 수"이므로 <see cref="Unit.TickStatuses"/>는 자기 차례에 딱 1회만 불린다.
        /// </summary>
        private async Task<bool> ResolveStatusesAsync(Unit actor)
        {
            if (!actor.HasAnyStatus)
            {
                return true;
            }

            int dot = actor.GetDotDamage();
            if (dot > 0)
            {
                actor.ApplyDamage(dot);

                // 타격과 같은 기준 — 적용량이 아니라 계산된 피해량을 넘긴다(HitResult.Damage 참고).
                var tickArgs = new StatusTickedEventArgs(actor, dot);
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
                {
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(actor, kind, StatusChangeReason.Expired));
                }
            }

            // 남은 상태들도 턴 수가 줄었으므로 알린다 — 이게 없으면 표시가 부여 시점 값에 멈춘다.
            // View가 목록 전체를 다시 읽으므로 상태 개수만큼이 아니라 유닛당 1회만 보낸다.
            if (actor.HasAnyStatus)
            {
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(actor, null, StatusChangeReason.Ticked));
            }

            return !stunned;
        }

        /// <summary>
        /// 계획된 행동의 데미지를 실제로 계산·적용한다.
        /// 라인 스킬은 대상별로 순차 처리.
        /// </summary>
        private ActionResult ResolveAction(ActionPlan plan)
        {
            var hits = new List<HitResult>(plan.Targets.Count);
            foreach (Unit target in plan.Targets)
            {
                // 라인 스킬은 앞 대상이 이 공격으로 쓰러져도 나머지를 계속 때린다. 
                // 계획을 세운 시점에 이미 죽어 있던 대상만 건너뛴다(대상 목록은 셀렉터가 생존자 중에서 고른 것).
                if (!target.IsAlive)
                {
                    continue;
                }

                DamageResult dmg = DamageCalculator.Calculate(plan.Actor, target, plan.PowerMultiplier, _rng);
                target.ApplyDamage(dmg.Amount);

                // 적용량(클램프된 값)이 아니라 계산된 피해량을 넘긴다 — 오버킬을 그대로 보여주기 위함(HitResult.Damage 참고).
                hits.Add(new HitResult(target, dmg.Amount, dmg.IsCritical, !target.IsAlive));

                // 스킬에 딸린 상태이상은 살아남은 대상에게만 부여를 시도한다(저항 판정은 Unit이 담당).
                if (plan.Kind == ActionKind.Skill && target.IsAlive)
                {
                    TryApplySkillStatus(plan.Actor.Skill, target);
                }
            }

            return new ActionResult(plan.Actor, plan.Kind, hits);
        }

        private void TryApplySkillStatus(SkillProfile skill, Unit target)
        {
            if (skill?.Status == null)
            {
                return;
            }

            StatusEffect effect = skill.Status.Value;
            bool applied = target.TryApplyStatus(effect, _rng);

            StatusChanged?.Invoke(this, new StatusChangedEventArgs(target, effect.Kind,
                                                    applied ? StatusChangeReason.Applied : StatusChangeReason.Resisted));
        }
    }
}