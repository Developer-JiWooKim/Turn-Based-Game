using System;
using System.Collections.Generic;
using System.Linq;
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

        private TaskCompletionSource<Unit> _pending;
        private IReadOnlyList<Unit> _validTargets;

        public async Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken)
        {
            _validTargets = state.AliveEnemiesOf(actor);
            _pending = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);

            ActionRequested?.Invoke(actor, _validTargets);

            using (cancellationToken.Register(() => _pending.TrySetCanceled(cancellationToken)))
            {
                Unit target = await _pending.Task;
                _pending = null;
                _validTargets = null;
                return new ActionPlan(actor, ActionKind.Attack, new[] { target }, 1f);
            }
        }

        /// <summary>View가 클릭한 대상을 제출한다. 대기 중이 아니거나 유효하지 않은 대상은 무시한다.</summary>
        public void SubmitTarget(Unit target)
        {
            var pending = _pending;
            if (pending == null) return;
            if (target == null || !target.IsAlive) return;
            if (_validTargets == null || !_validTargets.Contains(target)) return;

            pending.TrySetResult(target);
        }
    }
}
