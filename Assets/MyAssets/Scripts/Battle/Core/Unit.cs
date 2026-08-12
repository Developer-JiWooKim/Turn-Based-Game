using System;
using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    public sealed class Unit
    {
        /// <summary>View가 자신에 대응하는 프리팹 인스턴스를 찾기 위한 고유 식별자</summary>
        public readonly int Id;
        public readonly string DisplayName;
        public readonly TeamSide Team;
        public readonly Stats Stats;

        // 스킬 정의
        public readonly SkillProfile Skill;

        /// <summary>
        /// 적이 대상을 고를 때 이 유닛이 추첨에서 차지하는 몫(전열이 높다).
        /// 절대값이 아니라 <b>같이 후보에 오른 유닛들과의 비율</b>이 확률을 정하므로,
        /// 누가 쓰러져 후보가 줄면 남은 유닛들끼리 자동으로 다시 정규화된다.
        ///
        /// 몬스터에게는 의미가 없어 기본값 1을 그대로 쓴다 — 몬스터를 고르는 건 플레이어의 수동 타겟팅이라
        /// AI 추첨(<see cref="MonsterAiSelector"/>)을 타지 않기 때문.
        /// </summary>
        public readonly float AggroWeight;

        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        private int _skillCooldownRemaining; // 남은 스킬 쿨타임(턴) - 0 이하이고 Skill이 있으면 사용 가능

        /// <param name="currentHp">
        /// 시작 HP. 0 이하를 주면 MaxHp로 시작한다(신규 스폰).
        /// 스테이지를 이어가는 파티는 직전 스테이지에서 깎인 HP를 그대로 물려받아야 하므로 런 데이터가 이 값을 넘겨준다.
        /// </param>
        /// <param name="aggroWeight">적의 대상 추첨에서 차지하는 몫. <see cref="AggroWeight"/> 참고.</param>
        public Unit(int id, string displayName, TeamSide team, Stats stats, SkillProfile skill = null,
                    int currentHp = 0, float aggroWeight = 1f)
        {
            Id = id;
            DisplayName = displayName;
            Team = team;
            Stats = stats;
            Skill = skill;
            AggroWeight = aggroWeight;
            CurrentHp = currentHp > 0 ? Math.Min(currentHp, stats.MaxHp) : stats.MaxHp;
            _skillCooldownRemaining = 0;
        }

        public bool IsSkillReady => Skill != null && IsAlive && _skillCooldownRemaining <= 0;

        /// <summary>
        /// 현재 걸려 있는 상태이상. 종류당 최대 1건만 유지한다(재부여 시 갱신).
        /// 스탯 감소형은 <see cref="Stats"/>를 직접 고치지 않고 읽는 시점에 반영한다 —
        /// 파티 시너지가 이미 Stats를 스냅샷/복원 방식으로 조작하고 있어, 양쪽이 같은 필드를 쓰면 서로를 덮어쓴다.
        /// </summary>
        private readonly List<ActiveStatus> _statuses = new();

        public IReadOnlyList<ActiveStatus> Statuses => _statuses;

        public bool HasAnyStatus => _statuses.Count > 0;

        /// <summary>이번 차례에 행동할 수 없는지</summary>
        public bool IsStunned => Find(StatusKind.Stun) != null;

        /// <summary>공격력/방어력/속도는 감소형 상태이상을 반영한 값을 쓴다(원본 <see cref="Stats"/>는 건드리지 않는다).</summary>
        public int EffectiveAtk => Reduced(Stats.Atk, StatusKind.AtkDown);
        public int EffectiveDef => Reduced(Stats.Def, StatusKind.DefDown);
        public int EffectiveSpd => Reduced(Stats.Spd, StatusKind.SpdDown);

        /// <summary>
        /// 상태이상 부여를 시도한다. 저항(RES)으로 실패하면 false.
        /// 최종 부여 확률 = ApplyChance × (1 − RES) 이므로 RES 1.0은 완전 면역이다.
        /// </summary>
        public bool TryApplyStatus(in StatusEffect effect, IRandom rng)
        {
            if (!IsAlive || !effect.IsValid)
            {
                return false;
            }

            float effectiveRes = Math.Clamp(Stats.Res, 0f, 1f);
            float chance = effect.ApplyChance * Math.Clamp(1f - effectiveRes, 0f, 1f);

            if (chance <= 0f || rng.Value01() >= chance)
            {
                return false;
            }

            AddOrRefresh(effect);
            return true;
        }

        /// <summary>
        /// 저항 판정 없이 무조건 부여한다. 로그라이크 선택지처럼 "플레이어가 대가를 치르고 얻은" 효과에 쓴다 —
        /// 몬스터 RES 때문에 선택지가 헛돌면 선택의 가치가 무너지기 때문.
        /// </summary>
        public void ApplyStatus(in StatusEffect effect)
        {
            if (!IsAlive || effect.Duration <= 0)
            {
                return;
            }

            AddOrRefresh(effect);
        }

        private void AddOrRefresh(in StatusEffect effect)
        {
            ActiveStatus existing = Find(effect.Kind);
            if (existing == null)
            {
                _statuses.Add(new ActiveStatus(effect.Kind, effect.Magnitude, effect.Duration));
                return;
            }

            // 재부여는 갱신 — 약한 효과가 덮어써서 기존 상태를 약화시키지 않도록 각각 더 큰 값을 남긴다.
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, effect.Duration);
            existing.Magnitude = Math.Max(existing.Magnitude, effect.Magnitude);
        }

        /// <summary>이번 차례에 받을 도트 피해. 도트가 없으면 0(최소 1 보장).</summary>
        public int GetDotDamage()
        {
            ActiveStatus poison = Find(StatusKind.Poison);
            if (poison == null)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(Stats.MaxHp * poison.Magnitude, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 지속 턴을 1 줄이고 만료된 상태를 제거한다. 만료된 종류를 반환하며, 없으면 null.
        /// 자기 차례에 1회만 호출한다(턴 수 = 자기 행동 기회 수).
        /// </summary>
        public List<StatusKind> TickStatuses()
        {
            List<StatusKind> expired = null;

            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                ActiveStatus status = _statuses[i];
                status.RemainingTurns--;
                if (status.RemainingTurns > 0)
                {
                    continue;
                }

                (expired ??= new List<StatusKind>()).Add(status.Kind);
                _statuses.RemoveAt(i);
            }

            return expired;
        }

        private ActiveStatus Find(StatusKind kind)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Kind == kind)
                {
                    return _statuses[i];
                }
            }

            return null;
        }

        /// <summary>감소형 상태이상(비율)을 적용한 스탯 값. 0 밑으로는 내려가지 않는다.</summary>
        private int Reduced(int baseValue, StatusKind kind)
        {
            ActiveStatus status = Find(kind);
            if (status == null)
            {
                return baseValue;
            }

            float ratio = 1f - Math.Min(1f, status.Magnitude);
            return Math.Max(0, (int)Math.Round(baseValue * ratio));
        }

        /// <summary>스킬 사용 처리 — 쿨타임을 다시 채운다.</summary>
        public void ConsumeSkill() => _skillCooldownRemaining = Skill?.Cooldown ?? 0;

        /// <summary>매 턴 시작 시 호출하여 쿨타임을 1 감소시킨다.</summary>
        public void TickCooldown()
        {
            if (_skillCooldownRemaining > 0)
            {
                _skillCooldownRemaining--;
            }
        }

        /// <summary>데미지를 적용하고 실제 감소량을 반환한다. HP는 0 미만으로 내려가지 않는다.</summary>
        public int ApplyDamage(int amount)
        {
            if (amount < 0)
            {
                amount = 0;
            }

            int before = CurrentHp;
            CurrentHp = before - amount;
            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }

            return before - CurrentHp;
        }

        /// <summary>
        /// 로그라이크 성장 효과를 스탯에 누적하고, 늘어난 최대치·회복량만큼 HP를 채운다.
        ///
        /// 비율 성장의 기준은 자기 스탯 자신이다 — 전투용 <see cref="Unit"/>은 성장의 출처(선택지/자동 성장)를
        /// 구분해 들고 있지 않기 때문이다. 런 지속 성장은 <c>RunMember</c>가 그 구분을 알고 처리하므로,
        /// 이 경로는 전투 중 일회성 효과에만 쓴다.
        /// </summary>
        public void ApplyGrowth(in RoguelikeEffect effect)
        {
            if (!IsAlive)
            {
                return;
            }

            ApplyHeal(effect.ApplyTo(Stats, Stats));
        }

        /// <summary>회복을 적용하고 실제 회복량을 반환한다. MaxHp를 넘지 않는다. (죽은 유닛은 부활 불가)</summary>
        public int ApplyHeal(int amount)
        {
            if (amount < 0 || !IsAlive)
            {
                return 0;
            }

            int before = CurrentHp;
            CurrentHp = before + amount;

            // 회복을 적용한 현재 Hp가 최대 Hp를 넘으면 현재 Hp를 최대 Hp로 적용
            if (CurrentHp > Stats.MaxHp)
            {
                CurrentHp = Stats.MaxHp;
            }

            // 회복이 적용된 현재 Hp에서 회복을 적용하기 전 Hp를 뺀 값(실제 회복할 수치)을 리턴
            return CurrentHp - before;
        }
    }
}