using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Progression.Save
{
    /// <summary>
    /// 진행 중이던 런의 체크포인트. 
    /// 보스를 막 처치한 시점의 상태이며, 성장 선택지를 받기 전이라 재개하면 그 보스의 선택지부터 다시 진행한다.
    ///
    /// <see cref="RunData"/>를 그대로 직렬화하지 않는 이유 —
    /// 1. SO 참조(<see cref="RunMember.Source"/>)는 JSON으로 남길 수 없음.
    /// 2. <c>Game.Core</c>의 <see cref="Stats"/>에 직렬화 속성을 붙이면 세이브 포맷이 순수 로직 클래스의 필드 배치에 묶이기 때문.
    /// </summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        /// <summary>클리어한(= 재개 시 승리 처리부터 이어갈) 스테이지</summary>
        public int Stage = 1;

        /// <summary>마지막으로 발급된 Unit 식별자. (<see cref="RunData.UnitIdSeed"/>)</summary>
        public int UnitIdSeed;

        public float EnemyHpMultiplier = 1f;
        public float EnemyAtkMultiplier = 1f;
        public bool EnemySkipFirstTurn;

        public List<RunMemberSnapshot> Members = new();

        /// <summary>이어할 런이 있는지.</summary>
        public bool HasParty => Members != null && Members.Count > 0;

        /// <summary>현재 런 상태를 스냅샷으로 뜬다.</summary>
        public static RunSnapshot Capture(RunData run)
        {
            var snapshot = new RunSnapshot
            {
                Stage = run.CurrentStage,
                UnitIdSeed = run.UnitIdSeed,
                EnemyHpMultiplier = run.PendingModifiers.EnemyHpMultiplier,
                EnemyAtkMultiplier = run.PendingModifiers.EnemyAtkMultiplier,
                EnemySkipFirstTurn = run.PendingModifiers.EnemySkipFirstTurn
            };

            foreach (RunMember member in run.Members)
            {
                snapshot.Members.Add(RunMemberSnapshot.Capture(member));
            }

            return snapshot;
        }

        /// <summary>
        /// 스냅샷을 런 데이터로 되돌린다. 
        /// 캐릭터를 로스터에서 찾지 못하면(에셋 이름 변경·삭제) null을 반환한다.
        /// </summary>
        public RunData ToRunData(CharacterRosterSO roster)
        {
            if (!HasParty)
            {
                return null;
            }

            if (roster == null)
            {
                Debug.LogError("[RunSnapshot] 로스터가 없어 저장된 런을 복원할 수 없습니다.");
                return null;
            }

            var members = new List<RunMember>();
            foreach (RunMemberSnapshot saved in Members)
            {
                CharacterStatsSO source = roster.Find(saved.CharacterId);
                if (source == null)
                {
                    Debug.LogError($"[RunSnapshot] 로스터에서 '{saved.CharacterId}'를 찾지 못해 저장된 런을 복원할 수 없습니다.");
                    return null;
                }

                members.Add(saved.ToMember(source));
            }

            var run = new RunData(Stage, UnitIdSeed, members);
            run.PendingModifiers.Restore(EnemyHpMultiplier, EnemyAtkMultiplier, EnemySkipFirstTurn);
            return run;
        }
    }

    /// <summary>체크포인트 시점의 파티원 1명.</summary>
    [Serializable]
    public sealed class RunMemberSnapshot
    {
        /// <summary>
        /// 캐릭터 SO의 <b>에셋 이름</b>. 표시 이름은 번역을 거친 값이라 언어에 따라 달라져 식별자로 쓸 수 없다.
        /// </summary>
        public string CharacterId;

        public int UnitId;
        public int CurrentHp;

        public StatsSnapshot Stats = new();
        public StatsSnapshot BaseStats = new();
        public StatsSnapshot ChoiceGrowth = new();

        public static RunMemberSnapshot Capture(RunMember member) => new()
        {
            CharacterId = member.Source != null ? member.Source.name : string.Empty,
            UnitId = member.UnitId,
            CurrentHp = member.CurrentHp,
            Stats = StatsSnapshot.Capture(member.Stats),
            BaseStats = StatsSnapshot.Capture(member.BaseStats),
            ChoiceGrowth = StatsSnapshot.Capture(member.ChoiceGrowth)
        };

        public RunMember ToMember(CharacterStatsSO source) =>
            new(UnitId, source, Stats.ToStats(), BaseStats.ToStats(), ChoiceGrowth.ToStats(), CurrentHp);
    }

    /// <summary><see cref="Battle.Core.Stats"/>의 직렬화용 사본.</summary>
    [Serializable]
    public sealed class StatsSnapshot
    {
        public int MaxHp;
        public int Atk;
        public int Spd;
        public int Def;
        public float CritRate;
        public float CritDmg;
        public float Res;

        public static StatsSnapshot Capture(Stats stats) => new()
        {
            MaxHp = stats.MaxHp,
            Atk = stats.Atk,
            Spd = stats.Spd,
            Def = stats.Def,
            CritRate = stats.CritRate,
            CritDmg = stats.CritDmg,
            Res = stats.Res
        };

        public Stats ToStats() => new(MaxHp, Atk, Spd, Def, CritRate, CritDmg, Res);
    }
}
