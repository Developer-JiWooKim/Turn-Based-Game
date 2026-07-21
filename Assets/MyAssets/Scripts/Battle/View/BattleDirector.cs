using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Run;
using Assets.MyAssets.Scripts.Save;
using Assets.MyAssets.Scripts.Singleton;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// BattleScene의 진입점이자 스테이지 루프의 오케스트레이터
    /// 웨이브를 구성해 Core(BattleSimulation)를 돌리고, 결과를 <see cref="RunData"/>에 반영한 뒤
    /// 다음 스테이지로 넘어가는 흐름만 담당한다.
    ///
    /// 실제 작업은 아래 셋에게 위임한다.
    ///  - <see cref="UnitViewRegistry"/> : 프리팹 스폰·정리·체력바
    ///  - <see cref="BattlePresenter"/>  : Core 이벤트 구독 → 연출·HUD
    ///  - <see cref="RoguelikeRewardService"/> : 성장 선택지 제시·적용
    /// </summary>
    public sealed class BattleDirector : MonoBehaviour
    {
        /// <summary>패배 시 되돌아갈 타이틀/캐릭터 선택 씬</summary>
        private const string IntroSceneName = "IntroScene";

        [Header("전투 구성")]
        [Tooltip("Title→캐릭터 선택을 거치지 않고 BattleScene을 직접 플레이할 때 쓰는 테스트 파티(폴백)")]
        [SerializeField] private CharacterStatsSO[] _testParty;
        [Tooltip("스테이지별 몬스터 웨이브(수동 설계). 인덱스 0 = 1스테이지. 이 배열 길이까지는 순서대로 진행하고,\n" +
                 "그 이후 스테이지는 _randomWavePool에서 뽑는다.")]
        [SerializeField] private SpawnWaveSO[] _monsterWaves;
        [Tooltip("스테이지가 오를수록 양측 스탯이 얼마나 강해지는지. 비워두면 성장 없이 진행한다.")]
        [SerializeField] private StageScalingSO _stageScaling;

        [Header("랜덤 스폰 (수동 설계 웨이브 이후)")]
        [Tooltip("보스/일반 풀을 나누지 않고 한 배열에 등록한다 — 각 웨이브의 Tier(Boss 포함 여부)로 자동 구분한다.")]
        [SerializeField] private SpawnWaveSO[] _randomWavePool;
        [Tooltip("이 배수 스테이지마다 보스 웨이브를 강제한다(수동 설계 구간에는 영향 없음). 0이면 보스 강제 없음.")]
        [SerializeField] private int _bossStageInterval = 5;

        [Header("연결")]
        [SerializeField] private UnitViewRegistry _registry;
        [SerializeField] private BattlePresenter _presenter;
        [SerializeField] private RoguelikeRewardService _rewards;
        [SerializeField] private TargetingController _targeting;
        [Tooltip("전멸 시 도달 스테이지를 보여주는 결과 팝업. 비워두면 바로 씬을 전환한다.")]
        [SerializeField] private BattleResultPanel _resultPanel;

        private RunData _run;
        private StageScaling _scaling;
        private CancellationTokenSource _cts;

        private async void Start()
        {
            try
            {
                await BeginBattleAsync();
            }
            catch (OperationCanceledException)
            {
                // 씬 종료 등으로 취소됨 — 정상 흐름
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private async Task BeginBattleAsync()
        {
            _cts = new CancellationTokenSource();
            SystemRandom rng = new SystemRandom();

            if (_registry == null || _presenter == null)
            {
                Debug.LogError("[BattleDirector] Registry/Presenter가 연결되지 않았습니다(인스펙터 확인).");
                return;
            }

            _run = ResolveRun();
            if (_run == null || _run.Members.Count == 0)
            {
                Debug.LogError("[BattleDirector] 파티가 비어 있어 전투를 시작할 수 없습니다.");
                return;
            }

            // SO가 비어 있으면 성장률 0인 기본값 — 스케일링 없이 그대로 진행된다.
            _scaling = _stageScaling != null ? _stageScaling.Create() : default;
            _presenter.Initialize(_cts.Token);

            foreach (RunMember member in _run.Members)
                _registry.SpawnMember(member);

            // 파티 등장 모션이 끝난 뒤에야 첫 웨이브가 스폰되도록 대기.
            await _registry.WhenAllSpawnPlayed(_cts.Token);

            // 파티가 전멸할 때까지(패배) 스테이지를 계속 이어간다.
            // 각 승리 후에는 자동 성장 + 성장 선택지를 거쳐 다음 웨이브로 넘어간다.
            while (true)
            {
                bool survived = await RunStageAsync(rng);
                if (!survived)
                {
                    await HandleDefeatAsync();
                    return;
                }

                // 승리 → 다음 스테이지로 진급 후 자동 성장, 그다음 선택지 제시
                _run.CurrentStage++;
                _run.ApplyStageGrowth(_scaling);
                _registry.RefreshHealth(_run.Members); // 선택지 패널의 카드와 체력바가 같은 값을 보이도록 먼저 갱신
                await PresentRewardAsync(rng);
                _registry.RefreshHealth(_run.Members); // 선택지로 늘어난 체력 반영
            }
        }

        /// <summary>한 스테이지(웨이브 1회)를 끝까지 진행한다. 파티가 살아남았으면 true.</summary>
        private async Task<bool> RunStageAsync(IRandom rng)
        {
            int stage = _run.CurrentStage;
            _presenter.SetStage(stage);
            // 영입·교체·사망으로 파티 구성이 바뀌므로 시너지는 스테이지마다 다시 판정한다
            _presenter.SetSynergies(_run.GetActiveSynergies());

            SpawnWaveSO wave = ResolveWave(stage, rng);
            if (wave == null)
            {
                Debug.LogError("[BattleDirector] 스폰할 몬스터 웨이브가 설정되지 않았습니다.");
                return false;
            }

            // 파티 Unit은 스테이지마다 런 데이터의 현재 상태(성장한 스탯 + 이어받은 HP)로 새로 만든다.
            // Id는 RunMember가 런 내내 유지하므로 이미 스폰된 View를 그대로 쓴다.
            // 파티 시너지는 트래커가 적용·추적한다 — 전투 중 대상 캐릭터가 죽으면 즉시 되돌리기 위함(README 규칙).
            var synergyTracker = new PartySynergyTracker(_run);
            List<Unit> players = synergyTracker.CreateBattleUnits();

            bool enemySkipFirstTurn = _run.PendingModifiers.EnemySkipFirstTurn;
            List<Unit> enemies = SpawnWave(wave, stage);
            await _registry.WhenSpawnPlayed(_registry.EnemyViews, _cts.Token);

            var playerSelector = new PlayerActionSelector();
            var simulation = new BattleSimulation(new BattleState(players, enemies),
                                                  playerSelector, new MonsterAiSelector(rng), rng, enemySkipFirstTurn);
            _presenter.Bind(simulation);
            simulation.UnitDied += (_, e) =>
            {
                if (e.Unit.Team != TeamSide.Player) return;

                List<ActiveSynergy> updated = synergyTracker.OnAllyDied(e.Unit);
                if (updated != null)
                    _presenter.SetSynergies(updated);
            };

            if (_targeting != null)
                _targeting.Initialize(playerSelector);

            BattleOutcome outcome = await simulation.RunAsync(_cts.Token);

            _presenter.Unbind();
            _registry.ClearMonsters();

            // 전투 결과(HP)를 런 데이터에 반영하고, 쓰러진 파티원은 영구 추방한다(README 규칙)
            _run.SyncFromBattle(players);
            foreach (RunMember fallen in _run.RemoveFallen())
                _registry.RemoveMember(fallen);

            return outcome != BattleOutcome.Defeat;
        }

        /// <summary>웨이브의 몬스터를 만들어 스폰한다. 스탯은 기준값 → 스테이지 배율 → 로그라이크 디버프 순.</summary>
        private List<Unit> SpawnWave(SpawnWaveSO wave, int stage)
        {
            var enemies = new List<Unit>();
            for (int i = 0; i < wave.Monsters.Count; i++)
            {
                MonsterStatsSO so = wave.Monsters[i];
                if (so == null) continue;

                Stats stats = so.CreateStats();
                _scaling.ApplyToMonster(stats, stage);
                _run.PendingModifiers.ApplyTo(stats);

                var unit = new Unit(_run.NextUnitId(), so.DisplayName, TeamSide.Enemy, stats, so.CreateSkill());
                enemies.Add(unit);
                _registry.SpawnMonster(unit, so.Prefab, i);
            }

            _run.PendingModifiers.Consume(); // 이번 스테이지에 소모 — 다음 스테이지로 넘어가지 않는다
            return enemies;
        }

        /// <summary>성장 선택지를 제시하고, 영입되었으면(교체 포함) 파티원 View를 갱신한다.</summary>
        private async Task PresentRewardAsync(IRandom rng)
        {
            if (_rewards == null)
                return;

            RecruitResult result = await _rewards.PresentAsync(_run, rng, _scaling, _cts.Token);
            if (result.Recruited == null)
                return;

            if (result.ReplacedOut != null)
                _registry.RemoveMember(result.ReplacedOut);

            UnitView view = _registry.SpawnMember(result.Recruited);
            if (view != null)
                await view.PlaySpawnAsync(_cts.Token);
        }

        /// <summary>런 데이터를 가져오고, 없으면(BattleScene 직접 플레이) 테스트 파티로 임시 런을 만든다.</summary>
        private RunData ResolveRun()
        {
            RunData run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run != null && run.Members.Count > 0)
                return run;

            if (_testParty == null || _testParty.Length == 0)
                return null;

            Debug.Log("[BattleDirector] RunData가 없어 인스펙터 테스트 파티로 진행합니다.");
            var fallback = new RunData(_testParty[0]);
            for (int i = 1; i < _testParty.Length; i++)
                fallback.AddMember(_testParty[i]);

            return fallback;
        }

        /// <summary>
        /// 스테이지 번호에 대응하는 웨이브를 찾는다. 수동 설계 배열 범위 안이면 그대로 사용하고,
        /// 그 이후는 보스 배수 여부에 따라 랜덤 풀에서 뽑는다. 풀이 비어 있으면 기존 배열을 순환한다.
        /// </summary>
        private SpawnWaveSO ResolveWave(int stage, IRandom rng)
        {
            if (_monsterWaves == null || _monsterWaves.Length == 0)
                return null;

            if (stage <= _monsterWaves.Length)
                return _monsterWaves[stage - 1];

            bool wantBoss = _bossStageInterval > 0 && stage % _bossStageInterval == 0;
            return PickFromPool(wantBoss, rng)
                ?? PickFromPool(!wantBoss, rng)
                ?? _monsterWaves[(stage - 1) % _monsterWaves.Length];
        }

        /// <summary>랜덤 풀에서 보스 여부가 일치하는 웨이브 하나를 가중치 비례로 뽑는다.</summary>
        private SpawnWaveSO PickFromPool(bool boss, IRandom rng)
        {
            if (_randomWavePool == null)
                return null;

            var candidates = _randomWavePool.Where(w => w != null && w.IsBossWave == boss).ToList();
            if (candidates.Count == 0)
                return null;

            List<int> picked = WeightedPicker.PickDistinct(candidates.Select(c => c.Weight).ToList(), 1, rng);
            return picked.Count > 0 ? candidates[picked[0]] : null;
        }

        /// <summary>전멸 시 결과를 보여주고, 플레이어가 확인하면 타이틀로 돌아간다.</summary>
        private async Task HandleDefeatAsync()
        {
            int reachedStage = _run.CurrentStage;
            Debug.Log($"[BattleDirector] 파티 전멸 — {reachedStage}스테이지에서 리타이어");

            // 패널에는 이번 런 이전까지의 기록을 보여줘야 하므로 저장(RecordStage)보다 먼저 읽어둔다.
            // 신기록 여부는 저장 계층의 판정 결과를 그대로 쓴다(동점은 신기록이 아님).
            int previousBest = SaveService.Current.BestStage;
            bool isNewRecord = SaveService.RecordStage(reachedStage);

            if (_resultPanel != null)
                await _resultPanel.PresentAsync(reachedStage, previousBest, isNewRecord, _cts.Token);

            if (GameManager.Instance != null)
                GameManager.Instance.LoadScene(IntroSceneName);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
