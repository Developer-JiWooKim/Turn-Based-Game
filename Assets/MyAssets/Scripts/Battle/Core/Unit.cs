namespace Assets.MyAssets.Scripts.Battle.Core
{
    public sealed class Unit
    {
        public readonly int Id; // View가 자신에 대응하는 프리팹 인스턴스를 찾기 위한 고유 식별자
        public readonly string DisplayName;
        public readonly TeamSide Team;
        public readonly Stats Stats;

        // 스킬 정의(현재 기획은 보스 몬스터만 스킬 사용, 추후 플레이어 캐릭터 측도 스킬 사용하게 되면 활성화 시키면 됨)
        public readonly SkillProfile Skill;

        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        private int _skillCooldownRemaining; // 남은 스킬 쿨타임(턴) - 0 이하이고 Skill이 있으면 사용 가능

        /// <param name="currentHp">
        /// 시작 HP. 0 이하를 주면 MaxHp로 시작한다(신규 스폰). 
        /// 스테이지를 이어가는 파티는 직전 스테이지에서 깎인 HP를 그대로 물려받아야 하므로 런 데이터가 이 값을 넘겨준다.
        /// </param>
        public Unit(int id, string displayName, TeamSide team, Stats stats, SkillProfile skill = null, int currentHp = 0)
        {
            Id = id;
            DisplayName = displayName;
            Team = team;
            Stats = stats;
            Skill = skill;
            CurrentHp = currentHp > 0 ? System.Math.Min(currentHp, stats.MaxHp) : stats.MaxHp;
            _skillCooldownRemaining = 0;
        }

        public bool IsSkillReady => Skill != null && IsAlive && _skillCooldownRemaining <= 0;

        /// <summary>스킬 사용 처리 — 쿨타임을 다시 채운다.</summary>
        public void ConsumeSkill() => _skillCooldownRemaining = Skill?.Cooldown ?? 0;

        /// <summary>매 턴 시작 시 호출하여 쿨타임을 1 감소시킨다.</summary>
        public void TickCooldown()
        {
            if (_skillCooldownRemaining > 0)
                _skillCooldownRemaining--;
        }

        /// <summary>데미지를 적용하고 실제 감소량을 반환한다. HP는 0 미만으로 내려가지 않는다.</summary>
        public int ApplyDamage(int amount)
        {
            if (amount < 0) amount = 0;
            int before = CurrentHp;
            CurrentHp = before - amount;
            if (CurrentHp < 0) CurrentHp = 0;
            return before - CurrentHp;
        }

        /// <summary>로그라이크 성장 효과를 스탯에 누적하고, 늘어난 최대치·회복량만큼 HP를 채운다.</summary>
        public void ApplyGrowth(in RoguelikeEffect effect)
        {
            if (!IsAlive) return;
            ApplyHeal(effect.ApplyTo(Stats));
        }

        /// <summary>회복을 적용하고 실제 회복량을 반환한다. MaxHp를 넘지 않는다. (죽은 유닛은 부활 불가)</summary>
        public int ApplyHeal(int amount)
        {
            if (amount < 0 || !IsAlive) return 0;

            int before = CurrentHp;
            CurrentHp = before + amount;

            // 회복을 적용한 현재 Hp가 최대 Hp를 넘으면 현재 Hp를 최대 Hp로 적용
            if (CurrentHp > Stats.MaxHp) CurrentHp = Stats.MaxHp;

            // 회복이 적용된 현재 Hp에서 회복을 적용하기 전 Hp를 뺀 값(실제 회복할 수치)을 리턴
            return CurrentHp - before;
        }
    }
}
