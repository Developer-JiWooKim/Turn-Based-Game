using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 전장에 서 있는 프리팹 인스턴스를 관리하는 레지스트리.
    /// "누가 어디에 서 있는가"만 알며, 전투 진행이나 연출 타이밍은 전혀 모른다.
    ///
    /// 파티 View는 런 내내 유지되고(Unit.Id = RunMember.UnitId 고정), 몬스터 View는 웨이브마다 스폰·정리된다.
    ///
    /// 스테이지가 무한히 이어지므로 인스턴스는 파괴하지 않고 프리팹별 풀에 반납해 재사용한다.
    /// 재사용 인스턴스는 이전 전투 상태를 그대로 들고 오기 때문에 스폰 시 <see cref="UnitView.ResetForSpawn"/>로 초기화한다.
    /// </summary>
    public sealed class UnitViewRegistry : MonoBehaviour
    {
        /// <summary>
        /// 화면에 배치된 파티원 한 명 — 슬롯 순서대로 줄 세울 때 쓴다.
        /// 멤버와 라벨을 함께 돌려주므로 "몇 번째 카드가 화면의 어느 자리인가"가 어긋날 수 없다.
        /// </summary>
        public readonly struct PartySlot
        {
            public readonly RunMember Member;

            /// <summary>체력바 왼쪽·턴 순서 칩과 같은 배치 라벨("A1").</summary>
            public readonly string Label;

            public PartySlot(RunMember member, string label)
            {
                Member = member;
                Label = label;
            }
        }

        /// <summary>프리팹 1종당 미리 잡아두는 풀 용량(슬롯 수 수준이면 충분하다)</summary>
        private const int PoolCapacityPerPrefab = 4;

        /// <summary>풀이 무한정 커지지 않도록 하는 상한. 넘긴 인스턴스는 반납 시 그냥 파괴된다.</summary>
        private const int PoolMaxPerPrefab = 8;

        [Header("배치 슬롯 (가로 일렬)")]
        [SerializeField] private Transform[] _playerSlots;
        [SerializeField] private Transform[] _enemySlots;

        private readonly Dictionary<int, UnitView> _views = new();
        private readonly List<UnitView> _enemyViews = new();

        /// <summary>프리팹별 인스턴스 풀. 무한 타워라 등장 몬스터 종류가 계속 바뀌므로 프리팹 단위로 캐시한다.</summary>
        private readonly Dictionary<GameObject, ObjectPool<UnitView>> _pools = new();

        /// <summary>살아 있는 View가 어느 프리팹에서 나왔는지(반납할 풀을 찾는 용도)</summary>
        private readonly Dictionary<UnitView, GameObject> _sourcePrefab = new();

        /// <summary>플레이어 슬롯 점유 현황(추방으로 중간이 비면 영입 시 그 자리를 재사용)</summary>
        private RunMember[] _slotOccupants;

        /// <summary>이번 웨이브에 살아 있는 몬스터 View들</summary>
        public IReadOnlyList<UnitView> EnemyViews => _enemyViews;

        private void Awake()
        {
            ValidateReferences(); // 배치 슬롯 누락을 시작 시 1회 보고한다.

            _slotOccupants = new RunMember[_playerSlots != null ? _playerSlots.Length : 0];
        }

        /// <summary>
        /// 유닛 수만큼 로그가 쏟아지기 전에 원인을 먼저 알린다.
        /// </summary>
        private void ValidateReferences()
        {
            NullCheck.LogIfEmpty(_playerSlots, nameof(_playerSlots), this, "파티원을 배치할 수 없습니다");
            NullCheck.LogIfEmpty(_enemySlots, nameof(_enemySlots), this, "몬스터가 한 자리에 겹쳐 스폰됩니다");
        }

        public bool TryGet(int unitId, out UnitView view) => _views.TryGetValue(unitId, out view);

        /// <summary>파티원 View를 빈 슬롯에 스폰한다.</summary>
        public UnitView SpawnMember(RunMember member)
        {
            int slotIndex = Array.IndexOf(_slotOccupants, null);
            if (slotIndex < 0)
            {
                Debug.LogError($"[UnitViewRegistry] '{member.DisplayName}'을 배치할 플레이어 슬롯이 없습니다.");
                return null;
            }

            UnitView view = Spawn(member.UnitId,
                                  member.DisplayName,
                                  member.Prefab,
                                  member.CurrentHp,
                                  member.Stats.MaxHp,
                                  _playerSlots,
                                  slotIndex,
                                  TeamSide.Player);
            if (view != null)
            {
                _slotOccupants[slotIndex] = member;
            }

            return view;
        }

        /// <summary>몬스터 View를 스폰하고 이번 웨이브 목록에 등록한다.</summary>
        public UnitView SpawnMonster(Unit unit, GameObject prefab, int index)
        {
            UnitView view = Spawn(unit.Id,
                                  unit.DisplayName,
                                  prefab,
                                  unit.CurrentHp,
                                  unit.Stats.MaxHp,
                                  _enemySlots,
                                  index,
                                  TeamSide.Enemy);

            if (view != null)
            {
                _enemyViews.Add(view);
            }

            return view;
        }

        /// <summary>
        /// 파티원을 <b>화면 배치 순서대로</b> 돌려준다(빈 슬롯은 건너뛴다).
        ///
        /// <see cref="RunData.Members"/>는 영입 순서라 화면 순서와 다를 수 있다 —
        /// <see cref="SpawnMember"/>가 앞쪽 빈자리를 재사용하는 반면 영입은 리스트 끝에 붙기 때문에,
        /// 추방으로 중간 슬롯이 빈 뒤 영입하면 그때부터 두 순서가 갈라진다.
        /// 화면에 보이는 대로 줄 세워야 하는 UI(교체 대상 선택 등)는 이걸 쓴다.
        /// </summary>
        public List<PartySlot> GetPartySlots()
        {
            var slots = new List<PartySlot>();
            for (int i = 0; i < _slotOccupants.Length; i++)
            {
                if (_slotOccupants[i] != null)
                {
                    slots.Add(new PartySlot(_slotOccupants[i], CreateSlotLabel(TeamSide.Player, i)));
                }
            }

            return slots;
        }

        /// <summary>
        /// 아군 진영 중앙과 적 진영 중앙의 <b>중간 지점</b>.
        /// 전체 공격처럼 "무대 한가운데로 나와서" 시전하는 연출의 목적지로 쓴다.
        ///
        /// 기준은 살아 있는 유닛이 아니라 <b>배치 슬롯</b>이다 — 유닛 위치의 평균을 쓰면
        /// 몬스터가 4마리인데 파티가 1명일 때 중심이 몬스터 쪽으로 쏠리고, 누가 죽을 때마다 자리가 달라진다.
        /// 슬롯 기준이면 웨이브 구성·생존자 수와 무관하게 늘 같은 자리가 나온다.
        /// 슬롯이 하나도 없는 진영은 계산에서 빠지고, 양쪽 다 없으면 레지스트리 위치를 돌려준다.
        /// </summary>
        public Vector3 GetBattlefieldCenter()
        {
            bool hasParty = TryGetSlotCenter(_playerSlots, out Vector3 party);
            bool hasEnemies = TryGetSlotCenter(_enemySlots, out Vector3 enemies);

            if (hasParty && hasEnemies)
            {
                return (party + enemies) * 0.5f;
            }

            if (hasParty)
            {
                return party;
            }

            return hasEnemies ? enemies : transform.position;
        }

        /// <summary>
        /// 슬롯 배열의 <b>첫 칸과 마지막 칸의 중간점</b>(비어 있는 칸은 건너뛴다).
        /// 전체 평균이 아니라 양 끝을 쓰는 이유 — 슬롯 간격이 고르지 않아도 줄의 한가운데가 나온다.
        /// </summary>
        private static bool TryGetSlotCenter(Transform[] slots, out Vector3 center)
        {
            center = Vector3.zero;
            if (slots == null)
            {
                return false;
            }

            Transform first = null;
            Transform last = null;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = slots[i];
                }

                last = slots[i];
            }

            if (first == null)
            {
                return false;
            }

            center = (first.position + last.position) * 0.5f;
            return true;
        }

        /// <summary>추방된 파티원의 View를 치우고 슬롯을 비운다.</summary>
        public void RemoveMember(RunMember member)
        {
            int slotIndex = Array.IndexOf(_slotOccupants, member);
            if (slotIndex >= 0)
            {
                _slotOccupants[slotIndex] = null;
            }

            DespawnById(member.UnitId);
        }

        /// <summary>웨이브가 끝난 뒤 몬스터 View를 전부 정리한다.</summary>
        public void ClearMonsters()
        {
            foreach (UnitView view in _enemyViews)
            {
                DespawnById(view.UnitId);
            }

            _enemyViews.Clear();
        }

        /// <summary>
        /// 유닛들의 현재 상태이상을 View에 반영한다.
        /// 파티 View는 런 내내 재사용되는데 파티 <see cref="Unit"/>은 스테이지마다 새로 만들어지므로,
        /// 스테이지 시작 시 이걸 호출하지 않으면 이전 스테이지의 상태 표기가 화면에 남는다(효과는 이미 사라졌는데도).
        /// </summary>
        public void RefreshStatuses(IEnumerable<Unit> units)
        {
            foreach (Unit unit in units)
            {
                if (_views.TryGetValue(unit.Id, out UnitView view))
                {
                    view.RefreshStatuses(unit.Statuses);
                }
            }
        }

        /// <summary>
        /// 전투가 끝나 상태이상이 소멸했음을 모든 View에 반영한다.
        /// 전투 종료 직후에 호출해야 이어지는 성장 선택지 화면에 이전 전투의 표기가 남지 않는다.
        /// </summary>
        public void ClearStatuses()
        {
            foreach (UnitView view in _views.Values)
            {
                if (view != null)
                {
                    view.RefreshStatuses(null);
                }
            }
        }

        /// <summary>성장 등으로 최대 HP/현재 HP가 바뀐 뒤 파티 체력바를 다시 그린다.</summary>
        public void RefreshHealth(IEnumerable<RunMember> members)
        {
            foreach (RunMember member in members)
            {
                if (_views.TryGetValue(member.UnitId, out UnitView view))
                {
                    view.RefreshHealth(member.CurrentHp, member.Stats.MaxHp);
                }
            }
        }

        /// <summary>지정한 View들의 등장 연출이 모두 끝날 때까지 기다린다.</summary>
        public Task WhenSpawnPlayed(IEnumerable<UnitView> views, CancellationToken ct) =>
            Task.WhenAll(views.Where(v => v != null).Select(v => v.PlaySpawnAsync(ct)));

        /// <summary>현재 등록된 모든 View의 등장 연출을 기다린다(전투 시작 전 파티 스폰용).</summary>
        public Task WhenAllSpawnPlayed(CancellationToken ct) => WhenSpawnPlayed(_views.Values.ToList(), ct);

        /// <summary>
        /// 화면 배치 라벨 — 진영 접두어(A=아군/E=적군) + 1부터 시작하는 번호("A1", "E2").
        /// 체력바 왼쪽 표기와 상단 턴 순서 칩이 같은 문자열을 쓰도록 **여기서만** 만든다.
        /// 배열 인덱스를 그대로 노출하지 않으려고 1부터 센다.
        /// </summary>
        private static string CreateSlotLabel(TeamSide team, int index) =>
            $"{(team == TeamSide.Player ? 'A' : 'E')}{index + 1}";

        private UnitView Spawn(int unitId, string displayName, GameObject prefab,
                               int currentHp, int maxHp, Transform[] slots, int index, TeamSide team)
        {
            if (prefab == null)
            {
                Debug.LogError($"[UnitViewRegistry] '{displayName}' 프리팹이 비어 있습니다.");
                return null;
            }

            ObjectPool<UnitView> pool = GetPool(prefab, displayName);
            if (pool == null)
            {
                return null;
            }

            UnitView view = pool.Get();

            Transform slot = ResolveSlot(slots, index, displayName);
            view.transform.SetPositionAndRotation(slot.position, slot.rotation);

            // 재사용 인스턴스에 남은 이전 전투 흔적을 지운 뒤 초기화한다(순서 이유는 ResetForSpawn 주석 참고).
            view.ResetForSpawn();
            view.Initialize(unitId, currentHp, maxHp, CreateSlotLabel(team, index));

            _views[unitId] = view;
            _sourcePrefab[view] = prefab;

            return view;
        }

        /// <summary>
        /// 배치할 슬롯을 고른다. 슬롯이 모자라면 레지스트리 위치로 폴백하는데,
        /// 그러면 유닛들이 한 자리에 겹쳐 보이기만 하고 원인이 드러나지 않으므로 로그를 남긴다
        /// (웨이브 SO에 슬롯 수보다 많은 몬스터를 넣으면 실제로 발생한다).
        /// </summary>
        private Transform ResolveSlot(Transform[] slots, int index, string displayName)
        {
            if (slots != null && index < slots.Length && slots[index] != null)
            {
                return slots[index];
            }

            Debug.LogError($"[UnitViewRegistry] '{displayName}'을 배치할 {index}번 슬롯이 없습니다" +
                           $"(슬롯 {(slots != null ? slots.Length : 0)}개) — 겹쳐서 스폰됩니다.", this);

            return transform;
        }

        private void DespawnById(int unitId)
        {
            if (!_views.TryGetValue(unitId, out UnitView view))
            {
                return;
            }

            _views.Remove(unitId);
            if (view == null)
            {
                return;
            }

            if (_sourcePrefab.TryGetValue(view, out GameObject prefab))
            {
                _sourcePrefab.Remove(view);
                if (_pools.TryGetValue(prefab, out ObjectPool<UnitView> pool))
                {
                    pool.Release(view); // 파괴 대신 비활성화 후 재사용 대기
                    return;
                }
            }

            Destroy(view.gameObject);
        }

        /// <summary>
        /// 프리팹 1종에 대응하는 풀을 가져온다(없으면 생성).
        /// UnitView가 없는 프리팹은 풀을 만들 가치가 없으므로 이 시점에 한 번 걸러낸다.
        /// </summary>
        private ObjectPool<UnitView> GetPool(GameObject prefab, string displayName)
        {
            if (_pools.TryGetValue(prefab, out ObjectPool<UnitView> pool))
            {
                return pool;
            }

            if (prefab.GetComponentInChildren<UnitView>(true) == null)
            {
                Debug.LogError($"[UnitViewRegistry] '{displayName}' 프리팹에 UnitView가 없습니다.");
                return null;
            }

            pool = new ObjectPool<UnitView>(
                createFunc: () => Instantiate(prefab).GetComponentInChildren<UnitView>(true),
                actionOnGet: v => v.gameObject.SetActive(true),
                actionOnRelease: v => v.gameObject.SetActive(false),
                actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
                defaultCapacity: PoolCapacityPerPrefab,
                maxSize: PoolMaxPerPrefab);

            _pools[prefab] = pool;
            return pool;
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}