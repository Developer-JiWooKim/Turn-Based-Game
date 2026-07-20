using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Singleton;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// BattleScene의 진입점. Core(BattleSimulation)를 소유·구동하고, 이벤트를 구독해 연출을 재생한다.
    /// SO로부터 Core Unit과 프리팹 View를 생성하고 Id로 1:1 매칭한다.
    ///
    /// 전투 규칙은 전혀 갖지 않으며, 오직 "언제 무엇을 보여줄지"만 담당한다.
    /// </summary>
    public sealed class BattleDirector : MonoBehaviour
    {
        /// <summary>패배 시 되돌아갈 타이틀/캐릭터 선택 씬.</summary>
        private const string IntroSceneName = "IntroScene";

        [Header("전투 구성")]
        [Tooltip("Title→캐릭터 선택을 거치지 않고 BattleScene을 직접 플레이할 때 쓰는 테스트 파티(폴백).")]
        [SerializeField] private CharacterStatsSO[] _testParty;
        [Tooltip("스테이지별 몬스터 웨이브. 인덱스 0 = 1스테이지. RunData.CurrentStage로 순서대로 진행하며,\n" +
                 "배열 길이를 넘어가면 처음부터 순환한다(6스테이지 이후 랜덤 스폰 패턴 풀은 추후 구현).")]
        [SerializeField] private SpawnWaveSO[] _monsterWaves;

        [Header("배치 슬롯 (가로 일렬)")]
        [SerializeField] private Transform[] _playerSlots;
        [SerializeField] private Transform[] _enemySlots;

        [Header("연출")]
        [Tooltip("공격 시작 후 타격이 적중하는 시점까지의 지연(초).")]
        [SerializeField] private float _impactDelay = 0.35f;
        [SerializeField] private CameraShake _cameraShake;

        [Header("로그라이크")]
        [Tooltip("승리 시 이 풀에서 3개를 무작위로 뽑아 제시한다(가중치는 추후).")]
        [SerializeField] private RoguelikeChoiceSO[] _choicePool;
        [Tooltip("제시할 선택지 개수.")]
        [SerializeField] private int _choiceCount = 3;

        [Header("연결")]
        [SerializeField] private TargetingController _targeting;
        [SerializeField] private BattleHUD _hud;
        [SerializeField] private RoguelikeChoicePanel _rewardPanel;

        private readonly Dictionary<int, UnitView> _views = new();
        private PlayerActionSelector _playerSelector;
        private BattleSimulation _simulation;
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
            var rng = new SystemRandom();
            int nextId = 0;

            CharacterStatsSO[] party = ResolveParty();
            var players = new List<Unit>();
            for (int i = 0; i < party.Length; i++)
            {
                CharacterStatsSO so = party[i];
                var unit = new Unit(++nextId, so.DisplayName, TeamSide.Player, so.CreateStats());
                players.Add(unit);
                SpawnView(unit, so.Prefab, _playerSlots, i);
            }

            // 몬스터의 Spawned 등장 모션 등 스폰 연출이 전부 끝난 뒤에야 첫 턴이 시작되도록 대기.
            // 이걸 빼먹으면 SPD가 높은 몬스터가 등장 연출 중에 바로 공격해버리는 문제가 생긴다.
            await Task.WhenAll(_views.Values.Select(v => v.PlaySpawnAsync(_cts.Token)));

            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            int stage = run?.CurrentStage ?? 1;

            // 파티가 전멸하거나(패배) 몬스터 웨이브가 없을 때까지 스테이지를 계속 이어간다.
            // 각 승리 후에는 성장 선택지를 제시하고 다음 웨이브로 넘어간다.
            while (true)
            {
                if (_hud != null)
                    _hud.SetStage(stage);

                SpawnWaveSO wave = ResolveWave(stage);
                if (wave == null)
                {
                    Debug.LogError("[BattleDirector] 스폰할 몬스터 웨이브가 설정되지 않았습니다.");
                    return;
                }

                var enemies = new List<Unit>();
                var enemyViews = new List<UnitView>();
                for (int i = 0; i < wave.Monsters.Count; i++)
                {
                    MonsterStatsSO so = wave.Monsters[i];
                    var unit = new Unit(++nextId, so.DisplayName, TeamSide.Enemy, so.CreateStats(), so.CreateSkill());
                    enemies.Add(unit);
                    UnitView view = SpawnView(unit, so.Prefab, _enemySlots, i);
                    if (view != null)
                        enemyViews.Add(view);
                }

                await Task.WhenAll(enemyViews.Select(v => v.PlaySpawnAsync(_cts.Token)));

                List<Unit> aliveParty = players.Where(u => u.IsAlive).ToList();
                var state = new BattleState(aliveParty, enemies);
                _playerSelector = new PlayerActionSelector();
                var enemySelector = new MonsterAiSelector(rng);
                _simulation = new BattleSimulation(state, _playerSelector, enemySelector, rng);

                _simulation.TurnStarted += OnTurnStarted;
                _simulation.ActorTurnStarted += OnActorTurnStarted;
                _simulation.ActionResolved += OnActionResolved;
                _simulation.UnitDied += OnUnitDied;
                _simulation.BattleEnded += OnBattleEnded;

                if (_targeting != null)
                    _targeting.Initialize(_playerSelector);

                BattleOutcome outcome = await _simulation.RunAsync(_cts.Token);

                _simulation.TurnStarted -= OnTurnStarted;
                _simulation.ActorTurnStarted -= OnActorTurnStarted;
                _simulation.ActionResolved -= OnActionResolved;
                _simulation.UnitDied -= OnUnitDied;
                _simulation.BattleEnded -= OnBattleEnded;

                foreach (UnitView view in enemyViews)
                {
                    _views.Remove(view.UnitId);
                    Destroy(view.gameObject);
                }

                if (outcome == BattleOutcome.Defeat)
                {
                    HandleDefeat();
                    return;
                }

                // 승리 → 다음 스테이지 전, 성장 선택지 제시 후 살아있는 파티에 즉시 적용
                await PresentRewardAsync(players, rng);

                stage++;
                if (run != null)
                    run.CurrentStage = stage;
            }
        }

        /// <summary>런 데이터의 파티를 우선 사용하고, 없으면(직접 플레이) 인스펙터 테스트 파티로 폴백한다.</summary>
        private CharacterStatsSO[] ResolveParty()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run != null && run.Party.Count > 0)
                return run.Party.ToArray();

            Debug.Log("[BattleDirector] RunData 파티가 없어 인스펙터 테스트 파티로 진행합니다.");
            return _testParty;
        }

        /// <summary>스테이지 번호에 대응하는 웨이브를 찾는다. 설정된 웨이브 수를 넘어가면 순환한다.</summary>
        private SpawnWaveSO ResolveWave(int stage)
        {
            if (_monsterWaves == null || _monsterWaves.Length == 0)
                return null;

            int index = (stage - 1) % _monsterWaves.Length;
            return _monsterWaves[index];
        }

        /// <summary>선택지를 제시하고, 고른 보상을 살아있는 파티에 적용한 뒤 체력바/스탯을 갱신한다.</summary>
        private async Task PresentRewardAsync(List<Unit> party, IRandom rng)
        {
            if (_rewardPanel == null)
                return;

            List<RoguelikeChoiceSO> choices = PickChoices(_choiceCount, rng);
            if (choices.Count == 0)
                return;

            RoguelikeChoiceSO picked = await _rewardPanel.PresentAsync(choices, _cts.Token);
            if (picked == null)
                return;

            picked.CreateReward().Apply(party.Where(u => u.IsAlive));

            foreach (Unit unit in party)
            {
                if (unit.IsAlive && _views.TryGetValue(unit.Id, out UnitView view))
                    view.RefreshHealth(unit.CurrentHp, unit.Stats.MaxHp);
            }
        }

        /// <summary>선택지 풀에서 중복 없이 무작위로 count개를 뽑는다(가중치 없음 — 균등).</summary>
        private List<RoguelikeChoiceSO> PickChoices(int count, IRandom rng)
        {
            var pool = _choicePool == null
                ? new List<RoguelikeChoiceSO>()
                : _choicePool.Where(c => c != null).ToList();

            var picked = new List<RoguelikeChoiceSO>();
            while (picked.Count < count && pool.Count > 0)
            {
                int i = rng.Range(0, pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }

        private void HandleDefeat()
        {
            Debug.Log("[BattleDirector] 파티 전멸 — 리타이어");
            // TODO(다음 단계): 결과 화면 팝업 후 타이틀로 복귀
            GameManager.Instance.LoadScene(IntroSceneName);
        }

        private UnitView SpawnView(Unit unit, GameObject prefab, Transform[] slots, int index)
        {
            if (prefab == null)
            {
                Debug.LogError($"[BattleDirector] '{unit.DisplayName}' 프리팹이 비어 있습니다.");
                return null;
            }

            Transform slot = (slots != null && index < slots.Length && slots[index] != null) ? slots[index] : transform;
            GameObject go = Instantiate(prefab, slot.position, slot.rotation);

            UnitView view = go.GetComponentInChildren<UnitView>();
            if (view == null)
            {
                Debug.LogError($"[BattleDirector] '{unit.DisplayName}' 프리팹에 UnitView가 없습니다.");
                return null;
            }

            view.Initialize(unit.Id, unit.CurrentHp, unit.Stats.MaxHp);
            _views[unit.Id] = view;
            return view;
        }

        // ── Core 이벤트 → HUD 갱신 ──

        private void OnTurnStarted(object sender, TurnStartedEventArgs e)
        {
            if (_hud != null)
                _hud.ShowTurnOrder(e.Order);
        }

        private void OnActorTurnStarted(object sender, ActorTurnEventArgs e)
        {
            if (_hud != null)
                _hud.SetActiveUnit(e.Actor);
        }

        // ── Core 이벤트 → 연출 (연출 Task를 등록하면 시뮬레이션이 완료를 대기) ──

        private void OnActionResolved(object sender, ActionResolvedEventArgs e)
        {
            if (_hud != null)
                _hud.HidePrompt(); // 입력 완료 → 연출 단계이므로 "당신의 차례" 프롬프트 숨김

            e.RegisterPlayback(PlayActionAsync(e.Result));
        }

        private void OnUnitDied(object sender, UnitDiedEventArgs e)
        {
            if (_hud != null)
                _hud.MarkDead(e.Unit.Id);

            if (_views.TryGetValue(e.Unit.Id, out UnitView view))
                e.RegisterPlayback(view.PlayDeathAsync(_cts.Token));
        }

        private async Task PlayActionAsync(ActionResult result)
        {
            if (!_views.TryGetValue(result.Actor.Id, out UnitView actorView))
                return;

            CancellationToken ct = _cts.Token;

            // 1) 공격/스킬 애니메이션 시작
            Task actorAnim = result.Kind == ActionKind.Skill
                ? actorView.PlaySkillAsync(ct)
                : actorView.PlayAttackAsync(ct);

            // 2) 타격 시점까지 대기 후 피격 연출 + 체력바 갱신
            await Awaitable.WaitForSecondsAsync(_impactDelay, ct);

            var playback = new List<Task> { actorAnim };
            bool anyCritical = false;
            foreach (HitResult hit in result.Hits)
            {
                if (hit.IsCritical) anyCritical = true;
                if (_views.TryGetValue(hit.Target.Id, out UnitView targetView))
                    playback.Add(targetView.PlayHitAsync(hit.Target.CurrentHp, hit.Target.Stats.MaxHp, ct));
            }

            if (anyCritical && _cameraShake != null)
                _cameraShake.Shake();

            await Task.WhenAll(playback);
        }

        private void OnBattleEnded(object sender, BattleEndedEventArgs e)
        {
            // TODO(다음 청크): 승리 → 로그라이크 선택지 팝업 / 패배 → 리타이어 화면
            Debug.Log($"[BattleDirector] 전투 종료: {e.Outcome} (생존 아군 {e.Survivors.Count}명)");
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_simulation != null)
            {
                _simulation.TurnStarted -= OnTurnStarted;
                _simulation.ActorTurnStarted -= OnActorTurnStarted;
                _simulation.ActionResolved -= OnActionResolved;
                _simulation.UnitDied -= OnUnitDied;
                _simulation.BattleEnded -= OnBattleEnded;
            }
        }
    }
}
