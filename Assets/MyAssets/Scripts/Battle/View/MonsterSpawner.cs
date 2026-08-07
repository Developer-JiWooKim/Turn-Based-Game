using System.Collections.Generic;
using System.Linq;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// "이번 스테이지에 어떤 몬스터가 나오는가"만 책임지는 컴포넌트.
    /// 웨이브 선택(수동 설계 구간 / 랜덤 풀)과 몬스터 <see cref="Unit"/> 생성 + View 스폰까지 담당
    ///
    /// 파티 쪽은 <see cref="UnitViewRegistry.SpawnMember"/>가 이미 스폰을 담당하므로 여기서 다루지 않는다.
    /// </summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        [Header("스테이지별 웨이브 (수동 설계)")]
        [Tooltip("인덱스 0 = 1스테이지. 이 배열 길이까지는 순서대로 진행하고,\n" +
                 "그 이후 스테이지는 _randomWavePool에서 뽑는다.")]
        [SerializeField] private SpawnWaveSO[] _monsterWaves;

        [Header("랜덤 스폰 (수동 설계 웨이브 이후)")]
        [Tooltip("보스/일반 풀을 나누지 않고 한 배열에 등록한다 — 각 웨이브의 Tier(Boss 포함 여부)로 자동 구분한다.")]
        [SerializeField] private SpawnWaveSO[] _randomWavePool;
        [Tooltip("이 배수 스테이지마다 보스 웨이브를 강제한다(수동 설계 구간에는 영향 없음). 0이면 보스 강제 없음.")]
        [SerializeField] private int _bossStageInterval = 5;

        private UnitViewRegistry _registry;

        /// <summary>스폰할 레지스트리를 주입받는다(<see cref="BattleDirector"/>가 전투 시작 시 1회 호출).</summary>
        public void Initialize(UnitViewRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// 스테이지 번호에 대응하는 웨이브를 찾는다. 수동 설계 배열 범위 안이면 그대로 사용하고,
        /// 그 이후는 보스 배수 여부에 따라 랜덤 풀에서 뽑는다. 풀이 비어 있으면 기존 배열을 순환한다.
        /// </summary>
        public SpawnWaveSO ResolveWave(int stage, IRandom rng)
        {
            if (_monsterWaves == null || _monsterWaves.Length == 0)
            {
                return null;
            }

            if (stage <= _monsterWaves.Length)
            {
                return _monsterWaves[stage - 1];
            }

            bool wantBoss = _bossStageInterval > 0 && stage % _bossStageInterval == 0;
            return PickFromPool(wantBoss, rng)
                ?? PickFromPool(!wantBoss, rng)
                ?? _monsterWaves[(stage - 1) % _monsterWaves.Length];
        }

        /// <summary>
        /// 웨이브의 몬스터를 만들어 스폰한다. 
        /// 스탯은 기준값 → 스테이지 배율 → 로그라이크 디버프 순.
        /// </summary>
        public List<Unit> SpawnWave(SpawnWaveSO wave, RunData run, in StageScaling scaling, int stage)
        {
            // Consume() 전에 읽어둔다 — 스폰 루프가 끝나면 예약이 비워진다.
            RunModifiers modifiers = run.PendingModifiers;
            string debuffLabel = DescribeDebuff(modifiers);
            bool skipFirstTurn = modifiers.EnemySkipFirstTurn;

            var enemies = new List<Unit>();
            for (int i = 0; i < wave.Monsters.Count; i++)
            {
                MonsterStatsSO so = wave.Monsters[i];
                if (so == null)
                {
                    continue;
                }

                Stats stats = so.CreateStats();
                scaling.ApplyToMonster(stats, stage);
                modifiers.ApplyTo(stats);

                var unit = new Unit(run.NextUnitId(), so.DisplayName, TeamSide.Enemy, stats, so.CreateSkill());

                // '몬스터 행동불가' 선택지는 1턴 기절로 표현한다 — 저항 없이 확정 적용(플레이어가 선택으로 얻은 효과).
                if (skipFirstTurn)
                {
                    unit.ApplyStatus(new StatusEffect(StatusKind.Stun, 1, 0f, 1f));
                }

                enemies.Add(unit);

                UnitView view = _registry.SpawnMonster(unit, so.Prefab, i);
                if (view != null)
                {
                    view.SetSpawnDebuff(debuffLabel);
                    view.RefreshStatuses(unit.Statuses);
                }
            }

            modifiers.Consume(); // 이번 스테이지에 소모 — 다음 스테이지로 넘어가지 않는다
            return enemies;
        }

        /// <summary>
        /// 스탯에 녹아든 로그라이크 디버프를 체력바 표기용 문자열로 만든다(없으면 null).
        /// 기절은 실제 상태이상이라 여기 포함하지 않는다 — 상태이상 목록이 알아서 표시한다.
        /// TMP 인라인 스프라이트 태그를 쓴다 — 이름은 <see cref="UnitHealthBar"/>와 같은 Sprite Asset(Fallback 포함)을 참조한다.
        /// </summary>
        private static string DescribeDebuff(RunModifiers modifiers)
        {
            string label = null;

            if (modifiers.EnemyHpMultiplier != 1f)
            {
                label = $"{UnitHealthBar.IconTag("Debuff_HealthDown")} -{Mathf.RoundToInt((1f - modifiers.EnemyHpMultiplier) * 100f)}%";
            }

            if (modifiers.EnemyAtkMultiplier != 1f)
            {
                string atk = $"{UnitHealthBar.IconTag("Debuff_AttackDown")} -{Mathf.RoundToInt((1f - modifiers.EnemyAtkMultiplier) * 100f)}%";

                // 줄을 나누지 않고 옆에 붙인다 — 줄이 늘면 글자 블록이 아래로 자라 체력바를 덮는다(SetStatuses 주석 참고).
                label = label == null ? atk : $"{label}{UnitHealthBar.EntrySeparator}{atk}";
            }

            return label;
        }

        /// <summary>랜덤 풀에서 보스 여부가 일치하는 웨이브 하나를 가중치 비례로 뽑는다.</summary>
        private SpawnWaveSO PickFromPool(bool boss, IRandom rng)
        {
            if (_randomWavePool == null)
            {
                return null;
            }

            var candidates = _randomWavePool.Where(w => w != null && w.IsBossWave == boss).ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            List<int> picked = WeightedPicker.PickDistinct(candidates.Select(c => c.Weight).ToList(), 1, rng);
            return picked.Count > 0 ? candidates[picked[0]] : null;
        }
    }
}
