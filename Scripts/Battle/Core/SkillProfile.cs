namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// Elite/Boss 몬스터의 스킬 정의(Core 표현). 실제 수치는 MonsterStatsSO에서 주입한다.
    /// 스킬을 가진 유닛만 이 값을 갖고, 없으면 null(일반 공격만 수행).
    /// 관례상 Elite는 Scope=Single(단일 대상), Boss는 Scope=Line(적 진영 전체)로 설정한다.
    /// </summary>
    public sealed class SkillProfile
    {
        /// <summary>스킬 사용 후 다시 쓰기까지 대기할 턴 수. 0이면 매 턴 사용 가능.</summary>
        public readonly int Cooldown;

        /// <summary>단일 대상 / 라인(상대 진영 전체).</summary>
        public readonly TargetScope Scope;

        /// <summary>일반 공격 대비 데미지 배율.</summary>
        public readonly float PowerMultiplier;

        /// <summary>적중한 생존 대상에게 부여를 시도할 상태이상. null이면 데미지만 준다.</summary>
        public readonly StatusEffect? Status;

        public SkillProfile(int cooldown, TargetScope scope, float powerMultiplier, StatusEffect? status = null)
        {
            Cooldown = cooldown;
            Scope = scope;
            PowerMultiplier = powerMultiplier;
            Status = status;
        }
    }
}
