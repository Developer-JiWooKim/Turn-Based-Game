namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 보스 몬스터의 스킬 정의(Core 표현). 실제 수치/연출은 추후 BossSkillSO에서 주입한다.
    /// 스킬을 가진 유닛만 이 값을 갖고, 없으면 null(일반 공격만 수행).
    /// </summary>
    public sealed class SkillProfile
    {
        /// <summary>스킬 사용 후 다시 쓰기까지 대기할 턴 수. 0이면 매 턴 사용 가능.</summary>
        public readonly int Cooldown;

        /// <summary>단일 대상 / 라인(상대 진영 전체).</summary>
        public readonly TargetScope Scope;

        /// <summary>일반 공격 대비 데미지 배율.</summary>
        public readonly float PowerMultiplier;

        public SkillProfile(int cooldown, TargetScope scope, float powerMultiplier)
        {
            Cooldown = cooldown;
            Scope = scope;
            PowerMultiplier = powerMultiplier;
        }
    }
}
