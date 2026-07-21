using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
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
        public readonly int Damage;
        public readonly bool IsCritical;
        public readonly bool WasLethal;

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