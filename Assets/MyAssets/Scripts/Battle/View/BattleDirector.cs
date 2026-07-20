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
        [Header("전투 구성")]
        [Tooltip("Title→캐릭터 선택을 거치지 않고 BattleScene을 직접 플레이할 때 쓰는 테스트 파티(폴백).")]
        [SerializeField] private CharacterStatsSO[] _testParty;
        [Tooltip("몬스터 구성. 스테이지 기반 스폰은 다음 청크에서 SO로 대체 예정.")]
        [SerializeField] private MonsterStatsSO[] _enemies;

        [Header("배치 슬롯 (가로 일렬)")]
        [SerializeField] private Transform[] _playerSlots;
        [SerializeField] private Transform[] _enemySlots;

        [Header("연출")]
        [Tooltip("공격 시작 후 타격이 적중하는 시점까지의 지연(초).")]
        [SerializeField] private float _impactDelay = 0.35f;
        [SerializeField] private CameraShake _cameraShake;

        [Header("연결")]
        [SerializeField] private TargetingController _targeting;

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

            var enemies = new List<Unit>();
            for (int i = 0; i < _enemies.Length; i++)
            {
                MonsterStatsSO so = _enemies[i];
                var unit = new Unit(++nextId, so.DisplayName, TeamSide.Enemy, so.CreateStats(), so.CreateSkill());
                enemies.Add(unit);
                SpawnView(unit, so.Prefab, _enemySlots, i);
            }

            // 몬스터의 Spawned 등장 모션 등 스폰 연출이 전부 끝난 뒤에야 첫 턴이 시작되도록 대기.
            // 이걸 빼먹으면 SPD가 높은 몬스터가 등장 연출 중에 바로 공격해버리는 문제가 생긴다.
            await Task.WhenAll(_views.Values.Select(v => v.PlaySpawnAsync(_cts.Token)));

            var state = new BattleState(players, enemies);
            _playerSelector = new PlayerActionSelector();
            var enemySelector = new MonsterAiSelector(rng);
            _simulation = new BattleSimulation(state, _playerSelector, enemySelector, rng);

            _simulation.ActionResolved += OnActionResolved;
            _simulation.UnitDied += OnUnitDied;
            _simulation.BattleEnded += OnBattleEnded;

            if (_targeting != null)
                _targeting.Initialize(_playerSelector);

            await _simulation.RunAsync(_cts.Token);
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

        private void SpawnView(Unit unit, GameObject prefab, Transform[] slots, int index)
        {
            if (prefab == null)
            {
                Debug.LogError($"[BattleDirector] '{unit.DisplayName}' 프리팹이 비어 있습니다.");
                return;
            }

            Transform slot = (slots != null && index < slots.Length && slots[index] != null) ? slots[index] : transform;
            GameObject go = Instantiate(prefab, slot.position, slot.rotation);

            UnitView view = go.GetComponentInChildren<UnitView>();
            if (view == null)
            {
                Debug.LogError($"[BattleDirector] '{unit.DisplayName}' 프리팹에 UnitView가 없습니다.");
                return;
            }

            view.Initialize(unit.Id, unit.CurrentHp, unit.Stats.MaxHp);
            _views[unit.Id] = view;
        }

        // ── Core 이벤트 → 연출 (연출 Task를 등록하면 시뮬레이션이 완료를 대기) ──

        private void OnActionResolved(object sender, ActionResolvedEventArgs e)
        {
            e.RegisterPlayback(PlayActionAsync(e.Result));
        }

        private void OnUnitDied(object sender, UnitDiedEventArgs e)
        {
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
                _simulation.ActionResolved -= OnActionResolved;
                _simulation.UnitDied -= OnUnitDied;
                _simulation.BattleEnded -= OnBattleEnded;
            }
        }
    }
}
