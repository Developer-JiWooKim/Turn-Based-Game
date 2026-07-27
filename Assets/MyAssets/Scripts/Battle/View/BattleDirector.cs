using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// BattleScene의 진입점이자 스테이지 루프의 오케스트레이터
    /// 웨이브를 구성해 Core(BattleSimulation)를 돌리고, 결과를 <see cref="RunData"/>에 반영한 뒤
    /// 다음 스테이지로 넘어가는 흐름만 담당한다.
    ///
    /// 실제 작업은 아래 컴포넌트들에게 위임한다.
    ///  - <see cref="BattleRunFlow"/>    : 런 시작(파티 해석)·종료(기록 저장·결과·씬 전환)
    ///  - <see cref="MonsterSpawner"/>   : 웨이브 선택·몬스터 생성
    ///  - <see cref="UnitViewRegistry"/> : 프리팹 스폰·정리·체력바
    ///  - <see cref="BattlePresenter"/>  : Core 이벤트 구독 → 연출·HUD
    ///  - <see cref="RoguelikeRewardService"/> : 성장 선택지 제시·적용
    /// </summary>
    public sealed class BattleDirector : MonoBehaviour
    {
        [Header("전투 구성")]
        [Tooltip("스테이지가 오를수록 양측 스탯이 얼마나 강해지는지. 비워두면 성장 없이 진행한다.")]
        [SerializeField] private StageScalingSO _stageScaling;

        [Header("연결")]
        [SerializeField] private BattleRunFlow _runFlow;
        [SerializeField] private UnitViewRegistry _registry;
        [SerializeField] private MonsterSpawner _spawner;
        [SerializeField] private BattlePresenter _presenter;
        [SerializeField] private RoguelikeRewardService _rewards;
        [SerializeField] private TargetingController _targeting;
        [Tooltip("ESC/HUD 버튼으로 여는 퍼즈 오버레이. 비워두면 퍼즈 없이 진행한다.")]
        [SerializeField] private BattlePausePanel _pausePanel;

        private RunData _run;
        private StageScaling _scaling;
        private CancellationTokenSource _cts;

        /// <summary>이번 스테이지 전용 취소원(씬 종료용 _cts와 링크). '배틀 중단'이 이걸 취소한다.</summary>
        private CancellationTokenSource _stageCts;

        /// <summary>플레이어가 배틀 중단을 선택했는지. 취소 사유를 씬 종료와 구분하는 데 쓴다.</summary>
        private bool _aborted;

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

            bool LogIfNull(UnityEngine.Object target, string name)
            {
                if (target != null) return false;

                Debug.LogError($"[BattleDirector] {name}가 연결되지 않았습니다(인스펙터 확인).");
                return true;
            }

            bool hasError = false;
            hasError |= LogIfNull(_registry, nameof(_registry));
            hasError |= LogIfNull(_presenter, nameof(_presenter));
            hasError |= LogIfNull(_spawner, nameof(_spawner));
            hasError |= LogIfNull(_runFlow, nameof(_runFlow));
            if (hasError) return;

            _run = _runFlow.ResolveRun();
            if (_run == null || _run.Members.Count == 0)
            {
                Debug.LogError("[BattleDirector] 파티가 비어 있어 전투를 시작할 수 없습니다.");
                return;
            }

            // SO가 비어 있으면 성장률 0인 기본값 — 스케일링 없이 그대로 진행된다.
            _scaling = _stageScaling != null ? _stageScaling.Create() : default;
            _presenter.Initialize(_cts.Token);
            _spawner.Initialize(_registry);

            if (_pausePanel != null)
            {
                _pausePanel.AbortRequested += OnAbortRequested;
                _pausePanel.SetBattleActive(false); // 전투 구간에 들어갈 때만 켠다
            }

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
                    await _runFlow.EndRunAsync(_run, _cts.Token);
                    return;
                }

                // 승리 → 다음 스테이지로 진급 후 자동 성장, 그다음 선택지 제시
                AudioManager.Sfx(AudioManager.Library?.VictoryStinger);
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
            if (_pausePanel != null)
                _pausePanel.SetStage(stage);

            SpawnWaveSO wave = _spawner.ResolveWave(stage, rng);
            if (wave == null)
            {
                Debug.LogError("[BattleDirector] 스폰할 몬스터 웨이브가 설정되지 않았습니다.");
                return false;
            }

            // 보스/일반 BGM으로 전환. 같은 클립이면 AudioManager가 무시하므로 매 스테이지 호출해도 안전하다.
            AudioManager.Bgm(AudioManager.Library?.GetBattleBgm(wave.IsBossWave));

            // 파티 Unit은 스테이지마다 런 데이터의 현재 상태(성장한 스탯 + 이어받은 HP)로 새로 만든다.
            // Id는 RunMember가 런 내내 유지하므로 이미 스폰된 View를 그대로 쓴다.
            // 파티 시너지는 트래커가 적용·추적한다 — 전투 중 대상 캐릭터가 죽으면 즉시 되돌리기 위함(README 규칙).
            var synergyTracker = new PartySynergyTracker(_run);
            List<Unit> players = synergyTracker.CreateBattleUnits();

            // 영입·교체·사망으로 파티 구성이 바뀌므로 시너지는 스테이지마다 다시 판정한다.
            // 표시도 트래커에서 받는다 — 전투 중 갱신과 같은 판정 기준을 쓰기 위함.
            _presenter.SetSynergies(synergyTracker.GetActiveSynergies());

            // 상태이상은 전투 단위라 스테이지가 바뀌면 사라진다(파티 Unit이 새로 생성되므로).
            // View는 런 내내 재사용되니 이전 스테이지의 표기를 여기서 지워준다.
            _registry.RefreshStatuses(players);

            List<Unit> enemies = _spawner.SpawnWave(wave, _run, _scaling, stage);
            await _registry.WhenSpawnPlayed(_registry.EnemyViews, _cts.Token);

            // 인스펙터에 연결되지 않았으면 진짜 null을 넘긴다(Unity의 == 오버로드는 인터페이스 캐스트에서 사라지므로).
            IPauseGate pauseGate = _pausePanel != null ? _pausePanel : null;

            var playerSelector = new PlayerActionSelector();
            var simulation = new BattleSimulation(new BattleState(players, enemies),
                                                  playerSelector, new MonsterAiSelector(rng), rng, pauseGate);
            _presenter.Bind(simulation);
            simulation.UnitDied += (_, e) =>
            {
                if (e.Unit.Team != TeamSide.Player) return;

                List<ActiveSynergy> updated = synergyTracker.OnAllyDied(e.Unit);
                if (updated != null)
                    _presenter.SetSynergies(updated);
            };

            if (_targeting != null)
                _targeting.Initialize(playerSelector, _registry);

            BattleOutcome outcome = await RunSimulationAsync(simulation);

            _presenter.Unbind();
            _registry.ClearMonsters();
            _registry.ClearStatuses(); // 상태이상은 전투와 함께 끝난다 — 선택지 화면에 표기가 남지 않도록 여기서 지운다

            // 전투 결과(HP)를 런 데이터에 반영하고, 쓰러진 파티원은 영구 추방한다(README 규칙)
            _run.SyncFromBattle(players);
            foreach (RunMember fallen in _run.RemoveFallen())
                _registry.RemoveMember(fallen);

            return outcome != BattleOutcome.Defeat;
        }

        /// <summary>
        /// 전투를 끝까지 돌린다. 이 구간에서만 퍼즈를 허용하며, '배틀 중단'으로 취소되면 패배로 처리해
        /// 전멸과 같은 결과 화면 경로를 타게 한다(씬 종료로 인한 취소는 그대로 위로 던진다).
        /// </summary>
        private async Task<BattleOutcome> RunSimulationAsync(BattleSimulation simulation)
        {
            _aborted = false;
            using CancellationTokenSource stageCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            _stageCts = stageCts;

            if (_pausePanel != null)
                _pausePanel.SetBattleActive(true);

            try
            {
                return await simulation.RunAsync(stageCts.Token);
            }
            catch (OperationCanceledException) when (_aborted)
            {
                Debug.Log("[BattleDirector] 플레이어가 배틀을 중단했습니다.");
                return BattleOutcome.Defeat;
            }
            finally
            {
                // Dispose(using 종료)보다 먼저 참조를 끊어야 중단 핸들러가 파기된 CTS를 취소하지 않는다.
                _stageCts = null;
                if (_pausePanel != null)
                    _pausePanel.SetBattleActive(false);
            }
        }

        /// <summary>퍼즈 패널의 '배틀 중단' — 진행 중인 전투를 취소한다.</summary>
        private void OnAbortRequested()
        {
            _aborted = true;
            _stageCts?.Cancel();
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

        private void OnDestroy()
        {
            if (_pausePanel != null)
                _pausePanel.AbortRequested -= OnAbortRequested;

            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
