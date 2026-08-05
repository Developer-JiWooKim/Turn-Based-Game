using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 유닛의 행동 계획을 정의 
    ///</summary>
    public sealed class ActionPlan
    {
        public readonly Unit Actor;
        public readonly ActionKind Kind;
        public readonly IReadOnlyList<Unit> Targets;
        public readonly float PowerMultiplier;

        public ActionPlan(Unit actor, ActionKind kind, IReadOnlyList<Unit> targets, float powerMultiplier)
        {
            Actor = actor;
            Kind = kind;
            Targets = targets;
            PowerMultiplier = powerMultiplier;
        }
    }

    public readonly struct HitResult
    {
        public readonly Unit Target;

        /// <summary>
        /// 계산된 피해량. 남은 HP로 깎지 않은 값이라 <b>과잉 피해(오버킬)를 그대로 담는다</b> —
        /// 데미지 표기가 "이 캐릭터가 실제로 얼마나 세게 때렸는지"를 보여줘야 하기 때문이다
        /// (HP 20 남은 적을 크리 300으로 잡으면 20이 아니라 300이 뜬다).
        /// 체력 감소는 이 값이 아니라 <see cref="Unit.CurrentHp"/>가 반영하므로 표기와 게이지는 어긋나지 않는다.
        /// </summary>
        public readonly int Damage;

        public readonly bool IsCritical;
        public readonly bool WasLethal; // 타격의 결과로 유닛이 죽었는지 체크

        public HitResult(Unit target, int damage, bool isCritical, bool wasLethal)
        {
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
            WasLethal = wasLethal;
        }
    }

    public sealed class ActionResult
    {
        public readonly Unit Actor;
        public readonly ActionKind Kind;
        public readonly IReadOnlyList<HitResult> Hits;
        public ActionResult(Unit actor, ActionKind kind, IReadOnlyList<HitResult> hits)
        {
            Actor = actor;
            Kind = kind;
            Hits = hits;
        }
    }

    public interface IActionSelector
    {
        Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken);
    }
}