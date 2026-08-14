using System;
using System.Collections.Generic;
using System.Linq;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;

namespace Assets.MyAssets.Scripts.Progression.Run
{
    /// <summary>
    /// 파티가 보유한 시너지 1건(HUD 표시용). <b>발동 여부와 무관하게</b> 파티에 있는 시너지 보유 캐릭터마다 하나씩 만들어진다 —
    /// 아직 인원이 모자란 시너지도 흐리게 보여줘야 "한 명만 더 모으면 된다"가 화면에 드러나기 때문.
    /// </summary>
    public readonly struct PartySynergy
    {
        public readonly CharacterStatsSO Source;
        /// <summary>파티에 살아 있는 해당 캐릭터 수. 발동 판정에만 쓰고 화면에는 찍지 않는다.</summary>
        public readonly int Count;
        public readonly SynergyBonus Effect;

        /// <summary>지금 실제로 효과가 적용 중인지. 별도 필드로 저장하지 않고 파생시켜 값이 어긋날 여지를 없앤다.</summary>
        public bool IsActive => Source != null && Count >= Source.SynergyThreshold;

        public PartySynergy(CharacterStatsSO source, int count, in SynergyBonus effect)
        {
            Source = source;
            Count = count;
            Effect = effect;
        }
    }

    /// <summary>
    /// 지금까지 고른 선택지 1종의 누적 횟수(HUD 표기용).
    /// 성장·회복·몬스터 디버프·영입을 가리지 않고 **9종 전부** 센다 — 이 표의 이름이 "선택 기록"인 이유다.
    /// </summary>
    public readonly struct ChoicePick
    {
        public readonly RoguelikeCategory Category;
        public readonly int Count;

        public ChoicePick(RoguelikeCategory category, int count)
        {
            Category = category;
            Count = count;
        }
    }

    /// <summary>
    /// 한 번의 런(도전) 동안 유지되는 세션 데이터
    /// 매 런 시작 시 새로 만들고, 전멸(리타이어) 시 폐기
    /// 로그라이크 무한 타워의 런 단위 상태(파티, 진행 스테이지, 누적 성장) 담고 있음
    ///
    /// 선택지 효과의 적용 규칙도 여기(로직)에 있으며, View(BattleDirector)는 결과를 화면에 반영만 한다.
    /// </summary>
    public sealed class RunData
    {
        /// <summary>파티 최대 인원(1명으로 시작, 영입 선택지로 확장)</summary>
        public const int MaxPartySize = 4;

        /// <summary>현재 파티. HP가 0이 된 멤버는 스테이지 종료 시 영구 추방된다.</summary>
        public readonly List<RunMember> Members = new();

        /// <summary>다음 스테이지 몬스터에게 1회 적용될 디버프 예약</summary>
        public readonly RunModifiers PendingModifiers = new();

        /// <summary>현재 도전 중인 스테이지(1부터 시작)</summary>
        public int CurrentStage = 1;

        /// <summary>파티에 자리가 남아 있는지(영입 선택지 등장 조건)</summary>
        public bool CanRecruit => Members.Count < MaxPartySize;

        /// <summary>런 전체에서 유일한 Unit 식별자 발급기(파티/몬스터 공용)</summary>
        private int _nextUnitId;

        /// <summary>
        /// 카테고리별 선택지 선택 횟수(<see cref="RoguelikeCategory"/> 값이 곧 인덱스).
        /// 스탯에 반영되는 값이 아니라 <b>표기 전용 집계</b>라 성장 계산 어디에도 쓰이지 않는다.
        /// </summary>
        private readonly int[] _choicePicks = new int[ChoicePickSlots];

        private static readonly int ChoicePickSlots = Enum.GetValues(typeof(RoguelikeCategory)).Length;

        /// <summary>
        /// 마지막으로 발급한 Unit 식별자(세이브 복원용).
        /// 재개 시 이 값을 이어붙이지 않으면 새로 발급한 Id가 기존 파티원과 겹쳐
        /// <c>UnitViewRegistry</c>의 Id→View 조회가 엉킨다.
        /// </summary>
        public int UnitIdSeed => _nextUnitId;

        public RunData(CharacterStatsSO starter)
        {
            if (starter != null)
            {
                Members.Add(new RunMember(NextUnitId(), starter));
            }
        }

        /// <summary>세이브에서 복원한 런(체크포인트 재개). 파티는 이미 만들어진 멤버를 그대로 받는다.</summary>
        public RunData(int currentStage, int unitIdSeed, IReadOnlyList<RunMember> members)
        {
            CurrentStage = currentStage;
            _nextUnitId = unitIdSeed;

            for (int i = 0; i < members.Count; i++)
            {
                Members.Add(members[i]);
            }
        }

        /// <summary>새 Unit(파티원·몬스터)에 부여할 식별자를 발급한다.</summary>
        public int NextUnitId() => ++_nextUnitId;

        /// <summary>
        /// 선택한 로그라이크 효과를 런에 적용한다.
        /// 파티 강화는 생존 멤버 전체에 즉시, 몬스터 디버프는 다음 스테이지로 예약.
        /// 영입은 어느 캐릭터인지 플레이어가 고른 뒤 <see cref="Recruit"/>로 따로 처리한다.
        /// </summary>
        /// <param name="category">선택 횟수 집계용. 스탯 계산에는 쓰이지 않는다.</param>
        public void ApplyChoice(in RoguelikeEffect effect, RoguelikeCategory category)
        {
            foreach (RunMember member in Members)
            {
                member.ApplyChoiceGrowth(effect);
            }

            PendingModifiers.Add(effect);

            // 종류를 가리지 않고 전부 센다 — 회복·디버프·영입도 플레이어가 "고른" 것이기 때문.
            // ⚠️ 영입은 후보 화면에서 '영입 안 함'으로 물러나도 여기서 이미 세어진다(카드를 고른 시점 기준).
            _choicePicks[(int)category]++;
        }

        /// <summary>
        /// 지금까지 고른 선택지를 <b>한 번이라도 고른 것만</b> 카테고리 순으로 돌려준다(HUD 표기용).
        /// 0인 항목을 빼는 이유 — 아직 안 고른 선택지까지 줄로 늘어놓으면 초반에 0만 가득 찬 표가 된다.
        /// </summary>
        public List<ChoicePick> GetChoicePicks()
        {
            var picks = new List<ChoicePick>();
            for (int i = 0; i < _choicePicks.Length; i++)
            {
                if (_choicePicks[i] > 0)
                {
                    picks.Add(new ChoicePick((RoguelikeCategory)i, _choicePicks[i]));
                }
            }

            return picks;
        }

        /// <summary>저장된 선택 횟수로 되돌린다(체크포인트 복원). 길이가 달라도 겹치는 만큼만 채운다.</summary>
        public void RestoreChoicePicks(IReadOnlyList<int> saved)
        {
            if (saved == null)
            {
                return;
            }

            int count = Math.Min(saved.Count, _choicePicks.Length);
            for (int i = 0; i < count; i++)
            {
                _choicePicks[i] = saved[i];
            }
        }

        /// <summary>선택 횟수 원본(세이브용). 인덱스는 <see cref="RoguelikeCategory"/> 값이다.</summary>
        public IReadOnlyList<int> ChoicePickCounts => _choicePicks;

        /// <summary>
        /// 스테이지 진급에 따른 파티 자동 성장을 적용한다(스테이지를 올리기 직전에 1회 호출).
        /// 몬스터 쪽은 스폰 시 배율로 처리하므로 여기서는 파티만 다룬다.
        /// </summary>
        public void ApplyStageGrowth(in StageScaling scaling)
        {
            int step = CurrentStage - 1; // 진급 직후 호출되므로 이번이 몇 번째 성장인지와 같다
            foreach (RunMember member in Members)
            {
                member.ApplyStageGrowth(scaling.CreatePlayerGrowth(member.BaseStats, step));
            }
        }

        /// <summary>파티에 새 멤버를 추가한다(자리가 없으면 null).</summary>
        public RunMember AddMember(CharacterStatsSO source)
        {
            if (source == null || !CanRecruit)
            {
                return null;
            }

            var member = new RunMember(NextUnitId(), source);
            Members.Add(member);
            return member;
        }

        /// <summary>
        /// 플레이어가 고른 캐릭터를 영입한다(같은 캐릭터 중복 허용). 자리가 없으면 아무 일도 하지 않는다.
        /// </summary>
        public RunMember Recruit(CharacterStatsSO source, in StageScaling scaling)
        {
            // ⚠️ 기증자는 AddMember 전에 만든다 — 새 멤버가 목록에 들어간 뒤에 평균을 내면
            //    선택지 누계가 0인 본인이 평균을 끌어내려 소급분이 조용히 줄어든다.
            Stats donor = CreateCatchUpDonor();
            return RecruitWith(source, scaling, donor);
        }

        /// <summary>기증자 스냅샷을 정해서 영입한다(교체 경로가 "나가기 전" 파티를 기준으로 삼기 위해 분리).</summary>
        private RunMember RecruitWith(CharacterStatsSO source, in StageScaling scaling, Stats donor)
        {
            RunMember member = AddMember(source);
            if (member == null)
            {
                return null;
            }

            ApplyCatchUp(member, scaling, donor);
            return member;
        }

        /// <summary>
        /// 영입자가 물려받을 기준이 되는 <b>현재 파티원들의 선택지 누계 평균</b>.
        /// 특정 1명이 아니라 평균을 쓰는 이유 — 선택지는 파티 전원에게 함께 적용되므로 멤버별 누계 차이는
        /// "언제 합류했는가"에서만 오고, 평균이 곧 "이 파티가 지금까지 벌어들인 몫"에 가장 가깝다.
        /// </summary>
        private Stats CreateCatchUpDonor()
        {
            if (Members.Count == 0)
            {
                return null;
            }

            int n = Members.Count;
            long hp = 0, atk = 0, spd = 0, def = 0;
            float critRate = 0f, critDmg = 0f, res = 0f;

            foreach (RunMember member in Members)
            {
                hp += member.ChoiceGrowth.MaxHp;
                atk += member.ChoiceGrowth.Atk;
                spd += member.ChoiceGrowth.Spd;
                def += member.ChoiceGrowth.Def;
                critRate += member.ChoiceGrowth.CritRate;
                critDmg += member.ChoiceGrowth.CritDmg;
                res += member.ChoiceGrowth.Res;
            }

            return new Stats((int)(hp / n), (int)(atk / n), (int)(spd / n), (int)(def / n),
                             critRate / n, critDmg / n, res / n);
        }

        /// <summary>
        /// 지금 영입하면 갖게 될 스탯(영입 후보 카드 미리보기용). 파티에는 아무 영향을 주지 않는다.
        /// <see cref="Recruit"/>과 같은 계산 경로를 타므로 카드에 뜬 값과 실제 합류 결과가 어긋나지 않는다.
        /// </summary>
        public Stats PreviewRecruitStats(CharacterStatsSO source, in StageScaling scaling)
        {
            if (source == null)
            {
                return null;
            }

            var preview = new RunMember(0, source); // Id 0 = 미리보기 전용(파티에 넣지 않으므로 식별자를 소모하지 않는다)
            ApplyCatchUp(preview, scaling, CreateCatchUpDonor());
            return preview.Stats;
        }

        /// <summary>
        /// 후반에 합류해도 뒤처지지 않도록 소급 성장을 적용한다. 두 갈래이며 출처가 다르다.
        ///  1. <b>스테이지 자동 성장</b> — 스테이지 번호에서 파생되므로 항상 <b>전부</b> 되짚는다
        ///  2. <b>선택지 성장</b> — 파티가 벌어들인 몫이라 <see cref="StageScaling.RecruitChoiceCatchUpRate"/> 비율만 물려받는다
        ///
        /// <para>
        /// 2번은 원래 0(전혀 소급하지 않음)이었고 "그건 그 시점 파티가 벌어들인 몫"이라는 확정된 규칙이었다.
        /// 선택지가 비율로 바뀌면서 격차가 감당할 수 없이 벌어져(36스테이지 영입자가 기존 파티원 ATK의 25%,
        /// 그 결과 다른 파티원까지 연달아 쓰러지는 연쇄가 생겼다) 2026-08-12에 부분 소급으로 바꿨다.
        /// <b>여전히 전부 소급하지는 않는다</b> — 파티를 살려두는 데 가치를 두는 설계는 그대로다.
        /// </para>
        /// </summary>
        private void ApplyCatchUp(RunMember member, in StageScaling scaling, Stats donor)
        {
            // 기존 파티원이 진급마다 받아온 것과 같은 순서로 되짚어야 누적 결과가 정확히 일치한다.
            for (int step = 1; step < CurrentStage; step++)
            {
                member.ApplyStageGrowth(scaling.CreatePlayerGrowth(member.BaseStats, step));
            }

            member.ApplyRecruitCatchUp(donor, scaling.RecruitChoiceCatchUpRate);
        }

        /// <summary>
        /// 파티가 꽉 찼을 때 기존 멤버 하나를 내보내고 새 캐릭터를 영입한다.
        /// outgoing이 현재 파티에 없으면 아무 일도 하지 않는다.
        /// </summary>
        public RunMember ReplaceMember(RunMember outgoing, CharacterStatsSO source, in StageScaling scaling)
        {
            if (source == null || outgoing == null || !Members.Contains(outgoing))
            {
                return null;
            }

            // ⚠️ 기증자는 내보내기 전에 만든다 — 교체 대상도 그 성장을 함께 벌어들인 파티원이라
            //    빼고 평균을 내면 "오래 버틴 멤버를 교체할수록 후임이 약해지는" 반대 결과가 나온다.
            //    영입 후보 카드의 미리보기(PreviewRecruitStats)도 파티가 꽉 찬 상태에서 계산되므로 기준이 같아진다.
            Stats donor = CreateCatchUpDonor();
            Members.Remove(outgoing);

            return RecruitWith(source, scaling, donor); // 자리가 방금 비었으므로 기존 영입 경로를 그대로 탄다
        }

        /// <summary>전투 결과(각 Unit의 HP)를 파티에 반영한다.</summary>
        public void SyncFromBattle(IEnumerable<Unit> units)
        {
            foreach (Unit unit in units)
            {
                RunMember member = Members.FirstOrDefault(m => m.UnitId == unit.Id);
                member?.SyncFrom(unit);
            }
        }

        /// <summary>HP가 0이 된 파티원을 영구 추방하고, 추방된 멤버 목록을 반환한다(View 정리용).</summary>
        public List<RunMember> RemoveFallen()
        {
            List<RunMember> fallen = Members.Where(m => !m.IsAlive).ToList();
            foreach (RunMember member in fallen)
            {
                Members.Remove(member);
            }

            return fallen;
        }
    }
}
