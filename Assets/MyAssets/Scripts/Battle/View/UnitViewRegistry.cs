using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Run;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 전장에 서 있는 프리팹 인스턴스를 관리하는 레지스트리.
    /// "누가 어디에 서 있는가"만 알며, 전투 진행이나 연출 타이밍은 전혀 모른다.
    ///
    /// 파티 View는 런 내내 유지되고(Unit.Id = RunMember.UnitId 고정), 몬스터 View는 웨이브마다 스폰·정리된다.
    /// </summary>
    public sealed class UnitViewRegistry : MonoBehaviour
    {
        [Header("배치 슬롯 (가로 일렬)")]
        [SerializeField] private Transform[] _playerSlots;
        [SerializeField] private Transform[] _enemySlots;

        private readonly Dictionary<int, UnitView> _views = new();
        private readonly List<UnitView> _enemyViews = new();

        /// <summary>플레이어 슬롯 점유 현황(추방으로 중간이 비면 영입 시 그 자리를 재사용).</summary>
        private RunMember[] _slotOccupants;

        /// <summary>이번 웨이브에 살아 있는 몬스터 View들.</summary>
        public IReadOnlyList<UnitView> EnemyViews => _enemyViews;

        private void Awake()
        {
            _slotOccupants = new RunMember[_playerSlots != null ? _playerSlots.Length : 0];
        }

        public bool TryGet(int unitId, out UnitView view) => _views.TryGetValue(unitId, out view);

        /// <summary>파티원 View를 빈 슬롯에 스폰한다.</summary>
        public UnitView SpawnMember(RunMember member)
        {
            int slot = Array.IndexOf(_slotOccupants, null);
            if (slot < 0)
            {
                Debug.LogError($"[UnitViewRegistry] '{member.DisplayName}'을 배치할 플레이어 슬롯이 없습니다.");
                return null;
            }

            UnitView view = Spawn(member.UnitId, member.DisplayName, member.Prefab,
                                  member.CurrentHp, member.Stats.MaxHp, _playerSlots, slot);
            if (view != null)
                _slotOccupants[slot] = member;

            return view;
        }

        /// <summary>몬스터 View를 스폰하고 이번 웨이브 목록에 등록한다.</summary>
        public UnitView SpawnMonster(Unit unit, GameObject prefab, int index)
        {
            UnitView view = Spawn(unit.Id, unit.DisplayName, prefab,
                                  unit.CurrentHp, unit.Stats.MaxHp, _enemySlots, index);
            if (view != null)
                _enemyViews.Add(view);

            return view;
        }

        /// <summary>추방된 파티원의 View를 치우고 슬롯을 비운다.</summary>
        public void RemoveMember(RunMember member)
        {
            int slot = Array.IndexOf(_slotOccupants, member);
            if (slot >= 0)
                _slotOccupants[slot] = null;

            DespawnById(member.UnitId);
        }

        /// <summary>웨이브가 끝난 뒤 몬스터 View를 전부 정리한다.</summary>
        public void ClearMonsters()
        {
            foreach (UnitView view in _enemyViews)
                DespawnById(view.UnitId);

            _enemyViews.Clear();
        }

        /// <summary>성장 등으로 최대 HP/현재 HP가 바뀐 뒤 파티 체력바를 다시 그린다.</summary>
        public void RefreshHealth(IEnumerable<RunMember> members)
        {
            foreach (RunMember member in members)
            {
                if (_views.TryGetValue(member.UnitId, out UnitView view))
                    view.RefreshHealth(member.CurrentHp, member.Stats.MaxHp);
            }
        }

        /// <summary>지정한 View들의 등장 연출이 모두 끝날 때까지 기다린다.</summary>
        public Task WhenSpawnPlayed(IEnumerable<UnitView> views, CancellationToken ct) =>
            Task.WhenAll(views.Where(v => v != null).Select(v => v.PlaySpawnAsync(ct)));

        /// <summary>현재 등록된 모든 View의 등장 연출을 기다린다(전투 시작 전 파티 스폰용).</summary>
        public Task WhenAllSpawnPlayed(CancellationToken ct) => WhenSpawnPlayed(_views.Values.ToList(), ct);

        private UnitView Spawn(int unitId, string displayName, GameObject prefab,
                               int currentHp, int maxHp, Transform[] slots, int index)
        {
            if (prefab == null)
            {
                Debug.LogError($"[UnitViewRegistry] '{displayName}' 프리팹이 비어 있습니다.");
                return null;
            }

            Transform slot = (slots != null && index < slots.Length && slots[index] != null) ? slots[index] : transform;
            GameObject go = Instantiate(prefab, slot.position, slot.rotation);

            UnitView view = go.GetComponentInChildren<UnitView>();
            if (view == null)
            {
                Debug.LogError($"[UnitViewRegistry] '{displayName}' 프리팹에 UnitView가 없습니다.");
                Destroy(go);
                return null;
            }

            view.Initialize(unitId, currentHp, maxHp);
            _views[unitId] = view;
            return view;
        }

        private void DespawnById(int unitId)
        {
            if (!_views.TryGetValue(unitId, out UnitView view))
                return;

            _views.Remove(unitId);
            if (view != null)
                Destroy(view.gameObject);
        }
    }
}
