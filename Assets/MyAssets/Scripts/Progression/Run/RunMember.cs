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

        /// <summary>
        /// 로그라이크 선택지로 얻은 성장만의 누계(스테이지 자동 성장은 뺀 값).
        /// 둘 다 <see cref="Stats"/>에 함께 쌓이므로, 화면에서 출처를 구분하려면 선택지 몫을 따로 세어둘 수밖에 없다.
        /// </summary>
        public readonly Stats ChoiceGrowth = new(0, 0, 0, 0, 0f, 0f, 0f);

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
        /// 세이브에서 복원한다(체크포인트 재개). SO는 기준 수치일 뿐이므로 성장이 누적된 결과와
        /// 현재 HP를 그대로 받아야 재개 전후의 파티가 같아진다.
        /// </summary>
        public RunMember(int unitId, CharacterStatsSO source, Stats stats, Stats baseStats, Stats choiceGrowth, int currentHp)
        {
            UnitId = unitId;
            Source = source;
            Stats = stats;
            BaseStats = baseStats;
            ChoiceGrowth = choiceGrowth;
            CurrentHp = currentHp;
        }

        /// <summary>
        /// 현재 상태로 이번 스테이지의 전투용 Unit을 만든다(HP를 그대로 물려줌).
        /// 스탯은 복사본을 넘겨, 전투 중 버프/디버프가 런 데이터에 영구히 남지 않게 한다.
        /// 파티 시너지는 여기서 얹지 않는다 — <see cref="PartySynergyTracker"/>가 적용 전 스냅샷을
        /// 남겨야 사망으로 조건이 깨졌을 때 클램프 오차 없이 되돌릴 수 있기 때문.
        /// </summary>
        public Unit CreateUnit() => new Unit(UnitId, DisplayName, TeamSide.Player, Stats.Clone(), skill: null,
                                            CurrentHp, Source != null ? Source.AggroWeight : 1f);

        /// <summary>전투가 끝난 뒤 Unit의 HP를 런 데이터에 반영한다.</summary>
        public void SyncFrom(Unit unit) => CurrentHp = unit.CurrentHp;

        /// <summary>로그라이크 선택지로 받은 성장. 자동 성장과 구분해 보여줄 수 있도록 몫을 따로 집계한다.</summary>
        public void ApplyChoiceGrowth(in RoguelikeEffect effect) => ApplyGrowth(effect, ChoiceGrowth);

        /// <summary>스테이지 진급 자동 성장(후반 영입자의 소급 적용 포함). 집계 대상이 아니다.</summary>
        public void ApplyStageGrowth(in RoguelikeEffect effect) => ApplyGrowth(effect, bucket: null);

        /// <summary>
        /// 성장 효과를 적용하고 늘어난 최대치·회복량만큼 HP를 채운다(사망자는 제외).
        /// <paramref name="bucket"/>을 주면 <b>실제로 반영된 증분</b>을 거기에 누적한다 —
        /// 효과 값을 그대로 더하지 않는 이유는 <see cref="RoguelikeEffect.ApplyTo"/>가 치명타·저항을
        /// 1.0으로 클램프할 수 있어 넣은 값과 실제 증가분이 다를 수 있기 때문이다.
        /// </summary>
        private void ApplyGrowth(in RoguelikeEffect effect, Stats bucket)
        {
            if (!IsAlive)
            {
                return;
            }

            Stats before = bucket != null ? Stats.Clone() : null;
            int heal = effect.ApplyTo(Stats, CreateScaleBase());
            Accumulate(bucket, before);

            CurrentHp = System.Math.Min(Stats.MaxHp, CurrentHp + heal);
        }

        /// <summary>
        /// 영입 시 파티가 이미 쌓아둔 선택지 성장의 일부(<paramref name="rate"/> 비율)를 물려받는다.
        /// <paramref name="donor"/>는 합류 시점 파티원들의 <see cref="ChoiceGrowth"/> 평균이다.
        ///
        /// <para>
        /// ⚠️ <b>반드시 <see cref="ChoiceGrowth"/>에 누적해야 한다</b>(그래서 <see cref="ApplyStageGrowth"/>가 아니라 별도 경로다).
        /// <see cref="CreateScaleBase"/>가 <c>Stats − ChoiceGrowth</c>로 비율 성장의 기준을 만들기 때문에,
        /// 여기에 넣지 않으면 소급분만큼 기준이 부풀어 <b>이 멤버만 이후 선택지를 더 많이 받고 기존 파티원을 추월한다</b>.
        /// 하단 파티 스탯 표기의 파란 <c>(+선택지)</c> 괄호에 잡히는 것은 그 결과이지 목적이 아니다.
        /// </para>
        ///
        /// <para>
        /// 스테이지 자동 성장과 달리 <see cref="RoguelikeEffect"/>를 쓰지 않는다 — 그쪽 flat 생성자에는
        /// 치명타·치명피해·저항 칸이 없는데, 후반 파티 화력의 상당 부분이 치명피해 누계에 있어 빼놓을 수 없다.
        /// </para>
        /// </summary>
        public void ApplyRecruitCatchUp(Stats donor, float rate)
        {
            if (donor == null || rate <= 0f || !IsAlive)
            {
                return;
            }

            Stats before = Stats.Clone();

            Stats.MaxHp += Scale(donor.MaxHp, rate);
            Stats.Atk += Scale(donor.Atk, rate);
            Stats.Spd += Scale(donor.Spd, rate);
            Stats.Def += Scale(donor.Def, rate);
            Stats.CritRate = System.Math.Min(1f, Stats.CritRate + donor.CritRate * rate);
            Stats.CritDmg += donor.CritDmg * rate;
            Stats.Res = System.Math.Min(1f, Stats.Res + donor.Res * rate);

            Accumulate(ChoiceGrowth, before);
            CurrentHp = System.Math.Min(Stats.MaxHp, CurrentHp + (Stats.MaxHp - before.MaxHp));
        }

        /// <summary>
        /// <b>실제로 반영된 증분</b>(적용 전후 차분)을 집계함에 누적한다.
        /// 넣은 값을 그대로 더하지 않는 이유는 치명타·저항이 1.0으로 클램프되면 명목값과 실제 증가분이 달라지기 때문이다.
        /// </summary>
        private void Accumulate(Stats bucket, Stats before)
        {
            if (bucket == null)
            {
                return;
            }

            bucket.MaxHp += Stats.MaxHp - before.MaxHp;
            bucket.Atk += Stats.Atk - before.Atk;
            bucket.Spd += Stats.Spd - before.Spd;
            bucket.Def += Stats.Def - before.Def;
            bucket.CritRate += Stats.CritRate - before.CritRate;
            bucket.CritDmg += Stats.CritDmg - before.CritDmg;
            bucket.Res += Stats.Res - before.Res;
        }

        /// <summary>비율 증가분(정수). 반올림 규칙은 <see cref="RoguelikeEffect"/>·<see cref="SynergyBonus"/>와 같다.</summary>
        private static int Scale(int value, float rate) =>
            rate == 0f ? 0 : (int)System.Math.Round(value * rate, System.MidpointRounding.AwayFromZero);

        /// <summary>
        /// 선택지 비율 성장의 기준이 되는 스탯 = <b>기준값 + 스테이지 자동 성장</b>(= <see cref="Stats"/> − <see cref="ChoiceGrowth"/>).
        ///
        /// 현재 스탯을 그대로 기준으로 삼으면 선택지가 복리로 부풀어(같은 선택지를 반복할수록 증가폭이 커진다)
        /// 한 스탯에 몰아주는 것이 압도적으로 유리해진다. 반대로 <see cref="BaseStats"/>만 쓰면 기준이 런 내내
        /// 고정이라 고정 증분과 다를 바 없어져, 애초에 비율로 바꾼 이유(후반 희석)가 그대로 남는다.
        /// 자동 성장분까지만 기준에 넣으면 스테이지가 오를수록 선택지도 함께 커지되 폭주하지는 않는다.
        ///
        /// 파티 시너지는 전투용 <c>Unit</c>의 복사본에만 얹히므로 여기 <see cref="Stats"/>에는 애초에 섞이지 않는다.
        /// </summary>
        private Stats CreateScaleBase() =>
            new(Stats.MaxHp - ChoiceGrowth.MaxHp,
                Stats.Atk - ChoiceGrowth.Atk,
                Stats.Spd - ChoiceGrowth.Spd,
                Stats.Def - ChoiceGrowth.Def,
                Stats.CritRate - ChoiceGrowth.CritRate,
                Stats.CritDmg - ChoiceGrowth.CritDmg,
                Stats.Res - ChoiceGrowth.Res);
    }
}
