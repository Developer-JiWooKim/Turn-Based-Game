using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 몬스터의 행동을 규칙 기반으로 즉시 결정하는 셀렉터.
    /// - 스킬이 없는 유닛(Normal): 살아있는 적 중 1명 일반 공격
    /// - 스킬이 있는 유닛(Elite/Boss): 스킬이 준비되면 무조건 스킬 우선 사용(단일/라인은 SkillProfile.Scope로 결정), 아니면 일반 공격
    ///
    /// 단일 대상은 균등이 아니라 <b>어그로 가중 추첨</b>으로 고른다(<see cref="PickTarget"/>) —
    /// 전열 캐릭터가 더 자주 맞아야 파티 구성에 "누구를 앞에 세울까"라는 판단이 생긴다.
    /// </summary>
    public sealed class MonsterAiSelector : IActionSelector
    {
        private readonly IRandom _rng;

        /// <summary>대상 추첨용 가중치 버퍼. 행동마다 새로 만들지 않도록 재사용한다.</summary>
        private readonly List<float> _weights = new();

        public MonsterAiSelector(IRandom rng) => _rng = rng;

        public Task<ActionPlan> SelectAsync(Unit actor, BattleState state, CancellationToken cancellationToken)
        {
            IReadOnlyList<Unit> enemies = state.AliveEnemiesOf(actor);
            if (enemies.Count == 0)
            {
                return Task.FromResult<ActionPlan>(null); // 이론상 없음(시뮬레이션이 종료 판정). 방어적 처리.
            }

            if (actor.IsSkillReady)
            {
                SkillProfile skill = actor.Skill;

                // 라인 스킬도 버퍼를 그대로 넘기지 않고 복사한다 —
                // AliveEnemiesOf는 재사용 버퍼를 돌려주므로, ActionPlan이 들고 있는 동안 다른 질의가 덮어쓸 수 있다.
                IReadOnlyList<Unit> targets = skill.Scope == TargetScope.Line
                    ? new List<Unit>(enemies)
                    : new[] { PickTarget(actor, enemies) };

                actor.ConsumeSkill();
                return Task.FromResult(new ActionPlan(actor, ActionKind.Skill, targets, skill.PowerMultiplier));
            }

            Unit target = PickTarget(actor, enemies);
            return Task.FromResult(new ActionPlan(actor, ActionKind.Attack, new[] { target }, 1f));
        }

        /// <summary>
        /// 단일 대상 하나를 고른다 — 후보들의 <see cref="Unit.AggroWeight"/>에 비례하며,
        /// 후보 목록이 곧 생존자라 전열이 쓰러지면 남은 유닛들끼리 알아서 다시 정규화된다.
        ///
        /// "누구를 때릴지"를 정하는 유일한 지점이라 <b>도발(Taunt) 상태이상이 들어올 자리</b>이기도 하다 —
        /// <paramref name="actor"/>는 그래서 미리 받아 두었고 <b>아직 읽지 않는다</b>. 도발이 생기면
        /// 여기서 "actor에게 도발이 걸려 있으면 시전자를 반환"만 앞에 붙이면 된다
        /// (그때 <see cref="ActiveStatus"/>에 시전자 Id 필드가 필요해진다).
        /// 광역기(<see cref="TargetScope.Line"/>)는 이 함수를 타지 않으므로 "도발에 걸려도 광역기는
        /// 아군 전체가 맞는다"는 규칙이 분기 없이 성립한다.
        /// </summary>
        private Unit PickTarget(Unit actor, IReadOnlyList<Unit> candidates)
        {
            // ⚠️ candidates는 BattleState의 재사용 버퍼다 — 가중치를 즉시 읽어 소비하고 들고 있지 않는다.
            _weights.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                _weights.Add(candidates[i].AggroWeight);
            }

            int index = WeightedPicker.PickOne(_weights, _rng);
            return candidates[index >= 0 ? index : 0]; // 후보가 있는 건 호출부가 이미 확인했다
        }
    }
}
