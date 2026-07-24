using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 플레이어 유닛의 행동을 입력으로 받는 셀렉터. 플레이어는 공격만 하므로 "대상 선택"이 곧 행동 결정이다.
    /// View(타겟팅 컨트롤러/HUD)는 <see cref="ActionRequested"/>를 구독해 UI를 띄우고,
    /// 몬스터를 클릭하면 <see cref="SubmitTarget"/>를 호출한다(View→Core 콜백).
    /// </summary>
    public sealed class PlayerActionSelector : IActionSelector
    {
        /// <summary>지금 행동할 유닛과 선택 가능한 대상 목록을 View에 알린다.</summary>
        public event Action<Unit, IReadOnlyList<Unit>> ActionRequested;

        private readonly PendingSignal<Unit> _pending = new();

        /// <summary>
        /// 이번 선택의 유효 대상. <see cref="BattleState.AliveEnemiesOf"/>가 돌려주는 재사용 버퍼를 그대로 들면
        /// 입력 대기(await) 중에 다른 질의가 그 버퍼를 덮어쓸 수 있으므로 자체 리스트에 복사해 소유한다.
        /// </summary>
        private readonly List<Unit> _validTargets = new();

        public async Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken)
        {
            _validTargets.Clear();
            _validTargets.AddRange(state.AliveEnemiesOf(actor));

            ActionRequested?.Invoke(actor, _validTargets);

            Unit target = await _pending.WaitAsync(cancellationToken);
            _validTargets.Clear();
            return new ActionPlan(actor, ActionKind.Attack, new[] { target }, 1f);
        }

        /// <summary>View가 클릭한 대상을 제출한다. 대기 중이 아니거나 유효하지 않은 대상은 무시한다.</summary>
        public void SubmitTarget(Unit target)
        {
            if (!_pending.IsWaiting) return;
            if (target == null || !target.IsAlive) return;
            if (!_validTargets.Contains(target)) return;

            _pending.Complete(target);
        }
    }
}
