using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Progression.Run
{
    /// <summary>
    /// 런 동안 유지되는 파티원 1명의 상태. SO는 "기준 수치"일 뿐이므로, 로그라이크 성장으로 변한
    /// 스탯과 스테이지 간 이어지는 현재 HP는 여기(런 데이터)가 소유한다.
    /// 전투가 시작될 때마다 이 상태로 Core의 Unit을 만들고, 전투가 끝나면 결과를 다시 받아 적는다.
    /// </summary>
    public sealed class RunMember
    {
        /// <summary>런 내내 고정되는 식별자. View(프리팹 인스턴스)와의 매칭에 사용하므로 매 스테이지 유지된다.</summary>
        public readonly int UnitId;

        /// <summary>원본 SO — 표시 이름과 프리팹(View 데이터) 참조용.</summary>
        public readonly CharacterStatsSO Source;

        /// <summary>성장이 누적된 실제 스탯(SO 원본을 오염시키지 않는 독립 인스턴스).</summary>
        public readonly Stats Stats;

        /// <summary>성장 전 기준 스탯. 스테이지 자동 성장의 증가분을 계산하는 기준이며 변하지 않는다.</summary>
        public readonly Stats BaseStats;

        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        public string DisplayName => Source != null ? Source.DisplayName : "Unknown";
        public GameObject Prefab => Source != null ? Source.Prefab : null;

        public RunMember(int unitId, CharacterStatsSO source)
        {
            UnitId = unitId;
            Source = source;
            Stats = source.CreateStats();
            BaseStats = source.CreateStats();
            CurrentHp = Stats.MaxHp;
        }

        /// <summary>
        /// 현재 상태로 이번 스테이지의 전투용 Unit을 만든다(HP를 그대로 물려줌).
        /// 스탯은 복사본을 넘겨, 전투 중 버프/디버프가 런 데이터에 영구히 남지 않게 한다.
        /// 파티 시너지는 여기서 얹지 않는다 — <see cref="PartySynergyTracker"/>가 적용 전 스냅샷을
        /// 남겨야 사망으로 조건이 깨졌을 때 클램프 오차 없이 되돌릴 수 있기 때문.
        /// </summary>
        public Unit CreateUnit() => new Unit(UnitId, DisplayName, TeamSide.Player, Stats.Clone(), skill: null, CurrentHp);

        /// <summary>전투가 끝난 뒤 Unit의 HP를 런 데이터에 반영한다.</summary>
        public void SyncFrom(Unit unit) => CurrentHp = unit.CurrentHp;

        /// <summary>성장 효과를 적용하고 늘어난 최대치·회복량만큼 HP를 채운다(사망자는 제외).</summary>
        public void ApplyGrowth(in RoguelikeEffect effect)
        {
            if (!IsAlive) return;

            int heal = effect.ApplyTo(Stats);
            CurrentHp = System.Math.Min(Stats.MaxHp, CurrentHp + heal);
        }
    }
}
