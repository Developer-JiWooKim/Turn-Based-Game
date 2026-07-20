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

        public BattleState State => _state;

        public event EventHandler BattleStarted;
        public event EventHandler<TurnStartedEventArgs> TurnStarted;
        public event EventHandler<TurnEndedEventArgs> TurnEnded;
        public event EventHandler<ActionResolvedEventArgs> ActionResolved;
        public event EventHandler<UnitDiedEventArgs> UnitDied;
        public event EventHandler<BattleEndedEventArgs> BattleEnded;

        public BattleSimulation(BattleState state, IActionSelector playerSelector, IActionSelector enemySelector, IRandom rng)
        {
            _state = state;
            _playerSelector = playerSelector;
            _enemySelector = enemySelector;
            _rng = rng;
        }

        /// <summary>전투를 끝까지 진행하고 결과를 반환한다. 씬 종료 등으로 취소되면 OperationCanceledException.</summary>
        public async Task<BattleOutcome> RunAsync(CancellationToken cancellationToken = default)
        {
            BattleStarted?.Invoke(this, EventArgs.Empty);

            int turnNumber = 1;
            while (!_state.IsBattleOver)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<Unit> order = TurnOrder.Build(_state.AliveUnits);
                foreach (Unit u in order)
                    u.TickCooldown();

                TurnStarted?.Invoke(this, new TurnStartedEventArgs(turnNumber, order));

                foreach (Unit actor in order)
                {
                    if (!actor.IsAlive) continue;      // 이번 턴에 먼저 사망한 유닛은 건너뜀
                    if (_state.IsBattleOver) break;

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
            }
            return new ActionResult(plan.Actor, plan.Kind, hits);
        }
    }
}
