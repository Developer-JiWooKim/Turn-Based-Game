using System.Collections.Generic;
using System.Linq;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;

namespace Assets.MyAssets.Scripts.Progression.Run
{
    /// <summary>발동 중인 파티 시너지 1건(HUD 표시용).</summary>
    public readonly struct ActiveSynergy
    {
        public readonly CharacterStatsSO Source;
        /// <summary>파티에 있는 해당 캐릭터 수.</summary>
        public readonly int Count;
        public readonly RoguelikeEffect Effect;

        public ActiveSynergy(CharacterStatsSO source, int count, in RoguelikeEffect effect)
        {
            Source = source;
            Count = count;
            Effect = effect;
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
        /// <summary>파티 최대 인원(1명으로 시작, 영입 선택지로 확장).</summary>
        public const int MaxPartySize = 4;

        /// <summary>현재 파티. HP가 0이 된 멤버는 스테이지 종료 시 영구 추방된다.</summary>
        public readonly List<RunMember> Members = new();

        /// <summary>다음 스테이지 몬스터에게 1회 적용될 디버프 예약.</summary>
        public readonly RunModifiers PendingModifiers = new();

        /// <summary>현재 도전 중인 스테이지(1부터 시작).</summary>
        public int CurrentStage = 1;

        /// <summary>파티에 자리가 남아 있는지(영입 선택지 등장 조건).</summary>
        public bool CanRecruit => Members.Count < MaxPartySize;

        /// <summary>런 전체에서 유일한 Unit 식별자 발급기(파티/몬스터 공용).</summary>
        private int _nextUnitId;

        public RunData(CharacterStatsSO starter)
        {
            if (starter != null)
                Members.Add(new RunMember(NextUnitId(), starter));
        }

        /// <summary>새 Unit(파티원·몬스터)에 부여할 식별자를 발급한다.</summary>
        public int NextUnitId() => ++_nextUnitId;

        /// <summary>
        /// 선택한 로그라이크 효과를 런에 적용한다.
        /// 파티 강화는 생존 멤버 전체에 즉시, 몬스터 디버프는 다음 스테이지로 예약.
        /// 영입은 어느 캐릭터인지 플레이어가 고른 뒤 <see cref="Recruit"/>로 따로 처리한다.
        /// </summary>
        public void ApplyChoice(in RoguelikeEffect effect)
        {
            foreach (RunMember member in Members)
                member.ApplyGrowth(effect);

            PendingModifiers.Add(effect);
        }

        /// <summary>
        /// 스테이지 진급에 따른 파티 자동 성장을 적용한다(스테이지를 올리기 직전에 1회 호출).
        /// 몬스터 쪽은 스폰 시 배율로 처리하므로 여기서는 파티만 다룬다.
        /// </summary>
        public void ApplyStageGrowth(in StageScaling scaling)
        {
            int step = CurrentStage - 1; // 진급 직후 호출되므로 이번이 몇 번째 성장인지와 같다
            foreach (RunMember member in Members)
                member.ApplyGrowth(scaling.CreatePlayerGrowth(member.BaseStats, step));
        }

        /// <summary>파티에 새 멤버를 추가한다(자리가 없으면 null).</summary>
        public RunMember AddMember(CharacterStatsSO source)
        {
            if (source == null || !CanRecruit)
                return null;

            var member = new RunMember(NextUnitId(), source);
            Members.Add(member);
            return member;
        }

        /// <summary>
        /// 플레이어가 고른 캐릭터를 영입한다(같은 캐릭터 중복 허용). 자리가 없으면 아무 일도 하지 않는다.
        /// </summary>
        public RunMember Recruit(CharacterStatsSO source, in StageScaling scaling)
        {
            RunMember member = AddMember(source);
            if (member == null)
                return null;

            ApplyCatchUp(member, scaling);
            return member;
        }

        /// <summary>
        /// 지금 영입하면 갖게 될 스탯(영입 후보 카드 미리보기용). 파티에는 아무 영향을 주지 않는다.
        /// <see cref="Recruit"/>과 같은 계산 경로를 타므로 카드에 뜬 값과 실제 합류 결과가 어긋나지 않는다.
        /// </summary>
        public Stats PreviewRecruitStats(CharacterStatsSO source, in StageScaling scaling)
        {
            if (source == null)
                return null;

            var preview = new RunMember(0, source); // Id 0 = 미리보기 전용(파티에 넣지 않으므로 식별자를 소모하지 않는다)
            ApplyCatchUp(preview, scaling);
            return preview.Stats;
        }

        /// <summary>
        /// 후반에 합류해도 뒤처지지 않도록, 지금까지 쌓였을 스테이지 자동 성장을 소급 적용한다.
        /// (로그라이크 선택지로 받은 성장은 소급하지 않음 — 그건 그 시점 파티가 벌어들인 몫)
        /// </summary>
        private void ApplyCatchUp(RunMember member, in StageScaling scaling)
        {
            // 기존 파티원이 진급마다 받아온 것과 같은 순서로 되짚어야 누적 결과가 정확히 일치한다.
            for (int step = 1; step < CurrentStage; step++)
                member.ApplyGrowth(scaling.CreatePlayerGrowth(member.BaseStats, step));
        }

        /// <summary>
        /// 파티가 꽉 찼을 때 기존 멤버 하나를 내보내고 새 캐릭터를 영입한다.
        /// outgoing이 현재 파티에 없으면 아무 일도 하지 않는다.
        /// </summary>
        public RunMember ReplaceMember(RunMember outgoing, CharacterStatsSO source, in StageScaling scaling)
        {
            if (source == null || outgoing == null || !Members.Remove(outgoing))
                return null;

            return Recruit(source, scaling); // 자리가 방금 비었으므로 기존 영입 경로(소급 성장 포함)를 그대로 탄다
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
                Members.Remove(member);

            return fallen;
        }
    }
}
