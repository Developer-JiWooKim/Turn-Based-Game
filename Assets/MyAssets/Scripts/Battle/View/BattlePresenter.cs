using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.View.Panels;
using Assets.MyAssets.Scripts.Progression.Run;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// BattleSimulation의 이벤트를 구독해 연출과 HUD를 갱신하는 연출 담당.
    /// 전투 규칙은 전혀 모르고 "언제 무엇을 보여줄지"만 담당하며,
    /// 애니메이션이 끝날 때까지의 Task를 이벤트에 등록해 시뮬레이션을 대기시킨다.
    ///
    /// 시뮬레이션은 웨이브마다 새로 만들어지므로 <see cref="Bind"/>/<see cref="Unbind"/>로 구독을 갈아끼운다.
    ///
    /// <b>행동 1회를 어떻게 보여줄지는 <see cref="ActionPlayback"/>이 맡는다.</b> 여기 남은 것은
    /// "이벤트를 받아 화면 어디에 반영할지"와 시뮬레이션을 대기시키는 <see cref="RegisterPlayback"/>이며,
    /// 연출 참조(이펙트·투사체·팝업·쉐이크)는 인스펙터 배선을 유지하려고 이 클래스가 계속 들고 있다가
    /// <see cref="Initialize"/>에서 넘겨준다.
    /// </summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private BattleHUD _hud;
        [SerializeField] private CameraShake _cameraShake;
        [Tooltip("피해량 숫자 팝업. 비워두면 숫자 없이 진행한다.")]
        [SerializeField] private DamagePopupSpawner _damagePopups;

        [Tooltip("타격 파티클 이펙트. 비워두면 이펙트 없이 진행한다.")]
        [SerializeField] private HitEffectSpawner _hitEffects;

        [Tooltip("원거리 유닛의 투사체. 비워두면 투사체 없이 즉시 명중한다.")]
        [SerializeField] private ProjectileSpawner _projectiles;

        [Header("연출")]
        [Tooltip("공격 시작 후 타격이 적중하는 시점까지의 지연(초). " +
                 "유닛의 클립에 타격 이벤트를 심어 뒀다면 그쪽이 우선이고 이 값은 쓰이지 않는다.")]
        [SerializeField] private float _impactDelay = 0.35f;
        [Tooltip("현재 행동 중인 유닛에 씌울 레이어 이름(파랑 아웃라인). 렌더러만 이 레이어로 옮긴다.")]
        [SerializeField] private string _activeLayerName = "ActiveUnit";

        private UnitViewRegistry _registry;
        private BattleSimulation _simulation;
        private UnitView _activeView; // 현재 파랑 강조 중인 행동 유닛 View(다음 행동 유닛으로 넘어갈 때 원복).

        /// <summary>행동 1회의 연출 시퀀스. Initialize에서 만들며 인스펙터 참조를 그대로 넘겨받는다.</summary>
        private ActionPlayback _playback;

        private int _activeLayer;

        private CancellationToken _ct;

        /// <summary>재생 중인 연출 수(0이면 시뮬레이션이 연출을 기다리고 있지 않다).</summary>
        private int _playbackCount;

        /// <summary>
        /// 연출이 하나라도 재생 중인가.
        ///
        /// 시뮬레이션이 연출을 기다리는 <c>WhenPlaybackComplete()</c>는 취소 토큰을 받지 않아,
        /// 이 동안에는 '배틀 중단'을 눌러도 다음 유닛 행동 직전까지 반영되지 않는다.
        /// <see cref="BattlePausePanel"/>이 그 사이 중단 버튼을 잠그는 데 쓴다.
        /// </summary>
        public bool IsPlayingBack => _playbackCount > 0;

        private void Awake() => _activeLayer = LayerMask.NameToLayer(_activeLayerName);

        /// <summary>
        /// 씬 종료 시 연출을 취소할 토큰과 View 레지스트리를 받아둔다(전투 시작 전 1회).
        /// 연출 시퀀스(<see cref="ActionPlayback"/>)도 여기서 만든다 — 취소 토큰이 이 시점에 정해지기 때문이다.
        /// </summary>
        public void Initialize(CancellationToken ct, UnitViewRegistry registry)
        {
            _ct = ct;
            _registry = registry;
            _playback = new ActionPlayback(registry, ct, _impactDelay,
                                           _damagePopups, _hitEffects, _projectiles, _cameraShake);
        }

        public void SetStage(int stage)
        {
            if (_hud != null)
            {
                _hud.SetStage(stage);
            }
        }

        /// <summary>
        /// 전투 구간에 들어가고 나갈 때 HUD 버튼 줄(시너지·스탯 토글 / 몬스터 정보 / 퍼즈)을 켜고 끈다.
        /// 퍼즈 패널이 아니라 여기가 맡는 이유 — 그쪽은 인스펙터 <b>선택</b> 참조라,
        /// 비워두면 퍼즈뿐 아니라 나머지 버튼까지 함께 사라진다.
        /// </summary>
        public void SetBattleActive(bool active)
        {
            if (_hud != null)
            {
                _hud.SetButtonBarVisible(active);
            }
        }

        /// <summary>이번 스테이지에 발동 중인 파티 시너지를 HUD에 표시한다.</summary>
        public void SetSynergies(IReadOnlyList<PartySynergy> synergies)
        {
            if (_hud != null)
            {
                _hud.ShowSynergies(synergies);
                _hud.RefreshPartyStats(); // 시너지는 Stats를 직접 고치므로 하단 스탯 바도 같이 갱신
            }
        }

        /// <summary>
        /// 지금까지 고른 성장 선택지 횟수를 HUD에 표시한다.
        /// 값이 바뀌는 시점은 선택지를 고른 직후(= 스테이지 사이)뿐이라 스테이지 시작에 한 번만 밀어 넣으면 된다 —
        /// 전투 중에는 변할 일이 없어 <see cref="SetSynergies"/>처럼 사망 시 갱신할 필요도 없다.
        /// </summary>
        public void SetChoicePicks(IReadOnlyList<ChoicePick> picks)
        {
            if (_hud != null)
            {
                _hud.ShowChoicePicks(picks);
            }
        }

        /// <summary>
        /// 이번 스테이지의 파티를 하단 스탯 바에 넘긴다.
        /// 정렬 기준 좌표는 레지스트리의 View에서 얻는다 — View는 슬롯 Transform 위치에 그대로 놓이고,
        /// 추방·영입으로 슬롯이 재사용되므로 <see cref="RunData.Members"/> 순서로는 위치를 알 수 없다.
        /// <see cref="RunMember"/>도 함께 넘기는 이유는 증가량 분해(기준 스탯 대비 성장분) 때문이다.
        /// </summary>
        public void SetParty(RunData run, IReadOnlyList<Unit> players)
        {
            if (_hud == null)
            {
                return;
            }

            var slots = new List<PartyMemberSlot>(players.Count);
            foreach (Unit unit in players)
            {
                if (_registry.TryGet(unit.Id, out UnitView view))
                {
                    slots.Add(new PartyMemberSlot(unit, FindMember(run, unit.Id), view.transform.position));
                }
            }

            _hud.SetParty(slots);
        }

        /// <summary>전투 유닛에 대응하는 런 데이터(Id는 런 내내 고정이라 이걸로 짝을 찾는다).</summary>
        private static RunMember FindMember(RunData run, int unitId)
        {
            if (run == null)
            {
                return null;
            }

            foreach (RunMember member in run.Members)
            {
                if (member.UnitId == unitId)
                {
                    return member;
                }
            }

            return null;
        }

        /// <summary>이번 웨이브의 시뮬레이션에 연출을 연결한다.</summary>
        public void Bind(BattleSimulation simulation)
        {
            Unbind();

            _simulation = simulation;
            _simulation.TurnStarted += OnTurnStarted;
            _simulation.ActorTurnStarted += OnActorTurnStarted;
            _simulation.ActionResolved += OnActionResolved;
            _simulation.UnitDied += OnUnitDied;
            _simulation.StatusChanged += OnStatusChanged;
            _simulation.StatusTicked += OnStatusTicked;
            _simulation.BattleEnded += OnBattleEnded;
        }

        public void Unbind()
        {
            if (_simulation == null)
            {
                return;
            }

            _simulation.TurnStarted -= OnTurnStarted;
            _simulation.ActorTurnStarted -= OnActorTurnStarted;
            _simulation.ActionResolved -= OnActionResolved;
            _simulation.UnitDied -= OnUnitDied;
            _simulation.StatusChanged -= OnStatusChanged;
            _simulation.StatusTicked -= OnStatusTicked;
            _simulation.BattleEnded -= OnBattleEnded;
            _simulation = null;

            ClearActive();
        }

        private void OnDestroy() => Unbind();

        // ── HUD 갱신 ──

        private void OnTurnStarted(object sender, TurnStartedEventArgs e)
        {
            if (_hud == null)
            {
                return;
            }

            var chips = new List<TurnChipInfo>(e.Order.Count);
            foreach (Unit unit in e.Order)
            {
                // 칩 라벨은 View가 아는 화면 배치 인덱스다.
                // 못 찾으면 이름으로 폴백한다 — 라벨이 빈 칩이 뜨는 것보다 낫다.
                string label = _registry.TryGet(unit.Id, out UnitView view) && !string.IsNullOrEmpty(view.SlotLabel)
                    ? view.SlotLabel
                    : unit.DisplayName;

                chips.Add(new TurnChipInfo(unit.Id, label, unit.Team));
            }

            _hud.ShowTurnOrder(chips);
        }

        private void OnActorTurnStarted(object sender, ActorTurnEventArgs e)
        {
            if (_hud != null)
            {
                _hud.SetActiveUnit(e.Actor);
            }

            HighlightActive(e.Actor.Id);
        }

        /// <summary>이전 행동 유닛의 파랑 강조를 원복하고, 새 행동 유닛을 ActiveUnit 레이어로 옮긴다.</summary>
        private void HighlightActive(int unitId)
        {
            ClearActive();

            if (_activeLayer >= 0 && _registry.TryGet(unitId, out UnitView view))
            {
                view.SetOutlineLayer(_activeLayer);
                _activeView = view;
            }
        }

        private void ClearActive()
        {
            if (_activeView != null)
            {
                _activeView.ResetOutlineLayer();
            }

            _activeView = null;
        }

        // ── 연출 (연출 Task를 등록하면 시뮬레이션이 완료를 대기) ──

        /// <summary>
        /// 연출 Task를 이벤트에 등록하면서 <see cref="IsPlayingBack"/>을 함께 관리한다.
        /// 등록 경로가 셋(행동/사망/도트)이라 세는 자리를 한 곳으로 모아 짝이 어긋나지 않게 한다.
        /// </summary>
        private void RegisterPlayback(PlaybackEventArgs args, Task playback)
        {
            _playbackCount++;
            args.RegisterPlayback(TrackPlaybackAsync(playback));
        }

        /// <summary>연출이 끝나면(취소·예외로 끝나도) 카운트를 되돌린다.</summary>
        private async Task TrackPlaybackAsync(Task playback)
        {
            try
            {
                await playback;
            }
            finally
            {
                _playbackCount--;
            }
        }

        private void OnActionResolved(object sender, ActionResolvedEventArgs e)
        {
            if (_hud != null)
            {
                _hud.HidePrompt(); // 입력 완료 → 연출 단계이므로 "당신의 차례" 프롬프트 숨김
            }

            RegisterPlayback(e, _playback.PlayAsync(e.Result));
        }

        private void OnUnitDied(object sender, UnitDiedEventArgs e)
        {
            if (_hud != null)
            {
                _hud.MarkDead(e.Unit.Id);
            }


            if (_registry.TryGet(e.Unit.Id, out UnitView view))
            {
                RegisterPlayback(e, view.PlayDieAsync(_ct));
            }
        }

        /// <summary>상태이상이 붙거나 풀렸을 때 체력바 옆 표기를 다시 그린다(저항은 표기 변화가 없다).</summary>
        private void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Reason == StatusChangeReason.Resisted)
            {
                return;
            }

            if (_registry.TryGet(e.Unit.Id, out UnitView view))
            {
                view.RefreshStatuses(e.Unit.Statuses);
            }

            // 스탯 감소 계열이 붙거나 풀리면 유효 스탯이 바뀐다.
            if (_hud != null)
            {
                _hud.RefreshPartyStats();
            }
        }

        /// <summary>도트 피해는 일반 피격과 같은 연출을 재사용한다(체력바 갱신 포함).</summary>
        private void OnStatusTicked(object sender, StatusTickedEventArgs e)
        {
            if (_registry.TryGet(e.Unit.Id, out UnitView view))
            {
                RegisterPlayback(e, view.PlayHitAsync(e.Unit.CurrentHp, e.Unit.Stats.MaxHp, _ct));

                if (_damagePopups != null)
                {
                    _damagePopups.Spawn(view.PopupOrigin, e.Damage, DamageKind.DoT);
                }
            }
        }

        private void OnBattleEnded(object sender, BattleEndedEventArgs e)
        {
            ClearActive();
            Debug.Log($"[BattlePresenter] 전투 종료: {e.Outcome} (생존 아군 {e.Survivors.Count}명)");
        }
    }
}
