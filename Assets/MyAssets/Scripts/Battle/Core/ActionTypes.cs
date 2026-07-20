using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 셀렉터가 결정한 "무엇을 누구에게" — 아직 데미지 계산 전의 행동 계획.
    /// </summary>
    public sealed class ActionPlan
    {
        public readonly Unit Actor;
        public readonly ActionKind Kind;
        public readonly IReadOnlyList<Unit> Targets;
        /// <summary>데미지 배율. 일반 공격은 1.0, 스킬은 SkillProfile 배율.</summary>
        public readonly float PowerMultiplier;

        public ActionPlan(Unit actor, ActionKind kind, IReadOnlyList<Unit> targets, float powerMultiplier)
        {
            Actor = actor;
            Kind = kind;
            Targets = targets;
            PowerMultiplier = powerMultiplier;
        }
    }

    /// <summary>대상 1명에 대한 타격 결과(연출/UI 갱신용 순수 값).</summary>
    public readonly struct HitResult
    {
        public readonly Unit Target;
        public readonly int Damage;
        public readonly bool IsCritical;
        /// <summary>이 타격으로 대상이 사망했는가.</summary>
        public readonly bool WasLethal;

        public HitResult(Unit target, int damage, bool isCritical, bool wasLethal)
        {
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
            WasLethal = wasLethal;
        }
    }

    /// <summary>한 행동이 실제로 해소된 뒤의 전체 결과. View가 이 정보로 연출을 재생한다.</summary>
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

    /// <summary>
    /// 행동 주체가 무엇을 할지 결정하는 전략. 플레이어(입력 대기)와 몬스터 AI가 각각 구현한다.
    /// 비동기이므로 플레이어의 클릭 입력을 자연스럽게 await할 수 있다.
    /// </summary>
    public interface IActionSelector
    {
        Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken);
    }
}
