using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 몬스터의 행동을 규칙 기반으로 즉시 결정하는 셀렉터.
    /// - 스킬이 없는 유닛(Normal): 살아있는 적 중 무작위 1명 일반 공격
    /// - 스킬이 있는 유닛(Elite/Boss): 스킬이 준비되면 무조건 스킬 우선 사용(단일/라인은 SkillProfile.Scope로 결정), 아니면 일반 공격
    /// </summary>
    public sealed class MonsterAiSelector : IActionSelector
    {
        private readonly IRandom _rng;

        public MonsterAiSelector(IRandom rng) => _rng = rng;

        public Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken)
        {
            IReadOnlyList<Unit> enemies = state.AliveEnemiesOf(actor);
            if (enemies.Count == 0)
                return Task.FromResult<ActionPlan>(null); // 이론상 없음(시뮬레이션이 종료 판정). 방어적 처리.

            if (actor.IsSkillReady)
            {
                SkillProfile skill = actor.Skill;
                IReadOnlyList<Unit> targets = skill.Scope == TargetScope.Line
                    ? enemies
                    : new[] { enemies[_rng.Range(0, enemies.Count)] };

                actor.ConsumeSkill();
                return Task.FromResult(new ActionPlan(actor, ActionKind.Skill, targets, skill.PowerMultiplier));
            }

            Unit target = enemies[_rng.Range(0, enemies.Count)];
            return Task.FromResult(new ActionPlan(actor, ActionKind.Attack, new[] { target }, 1f));
        }
    }
}
