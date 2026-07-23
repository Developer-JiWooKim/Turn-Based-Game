namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 상태이상 종류. 종류를 늘릴 때는 여기에 값을 추가하고 소비 지점만 손보면 된다
    /// (스탯 감소형은 <see cref="Unit"/>의 유효 스탯 계산, 그 외는 <see cref="BattleSimulation"/>의 행동 루프).
    /// </summary>
    public enum StatusKind
    {
        /// <summary>행동 불가 — 자기 차례를 통째로 건너뛴다.</summary>
        Stun,
        /// <summary>도트 — 자기 차례 시작 시 최대 HP 비율만큼 피해.</summary>
        Poison,
        AtkDown,
        DefDown,
        SpdDown
    }

    /// <summary>
    /// 스킬이 들고 있는 "상태이상 부여 정의"(순수 값). 실제로 유닛에 붙은 상태는 <see cref="ActiveStatus"/>다.
    /// 카테고리별 구현체로 쪼개지 않고 <see cref="RoguelikeEffect"/>와 같은 방식으로 한 값에 담는다.
    /// </summary>
    public readonly struct StatusEffect
    {
        public readonly StatusKind Kind;

        /// <summary>지속 턴 수. 0 이하면 부여되지 않는다.</summary>
        public readonly int Duration;

        /// <summary>
        /// 효과 크기. 의미는 종류마다 다르다 —
        /// <see cref="StatusKind.Poison"/>은 최대 HP 대비 비율(0.05 = 5%),
        /// *Down 계열은 해당 스탯의 감소 비율(0.3 = 30% 감소). <see cref="StatusKind.Stun"/>은 사용하지 않는다.
        /// </summary>
        public readonly float Magnitude;

        /// <summary>저항(RES) 적용 전 기본 부여 확률 0~1. 0 이하면 부여되지 않는다.</summary>
        public readonly float ApplyChance;

        public StatusEffect(StatusKind kind, int duration, float magnitude, float applyChance)
        {
            Kind = kind;
            Duration = duration;
            Magnitude = magnitude;
            ApplyChance = applyChance;
        }

        /// <summary>실제로 부여를 시도할 값인지(둘 중 하나라도 0이면 효과 없음).</summary>
        public bool IsValid => Duration > 0 && ApplyChance > 0f;
    }

    /// <summary>유닛에 실제로 걸려 있는 상태이상 1건. 지속 턴이 0이 되면 제거된다.</summary>
    public sealed class ActiveStatus
    {
        public readonly StatusKind Kind;

        /// <summary>같은 종류가 재부여되면 더 강한 값으로 갱신된다.</summary>
        public float Magnitude;

        public int RemainingTurns;

        public ActiveStatus(StatusKind kind, float magnitude, int remainingTurns)
        {
            Kind = kind;
            Magnitude = magnitude;
            RemainingTurns = remainingTurns;
        }
    }

    /// <summary>상태이상 변화 사유(View 피드백 구분용).</summary>
    public enum StatusChangeReason
    {
        Applied,
        /// <summary>RES 저항으로 부여 실패.</summary>
        Resisted,
        /// <summary>지속 턴이 1 줄었다(표시 갱신용). 이게 없으면 남은 턴 수가 화면에서 멈춰 보인다.</summary>
        Ticked,
        Expired
    }
}
