using System.Collections.Generic;
using System.Linq;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;

namespace Assets.MyAssets.Scripts.Progression.Run
{
    /// <summary>
    /// 한 스테이지 전투 동안 파티 시너지의 발동 상태를 추적한다.
    /// 전투 시작 시 조건(같은 캐릭터 임계 인원 이상)을 만족하는 캐릭터들에게 시너지를 적용하고,
    /// 그중 하나가 죽어 조건이 깨지면 생존자의 전투 스탯에서 시너지를 즉시 제거한다.
    ///
    /// 제거는 뺄셈이 아니라 적용 전 스냅샷 복원 방식이다 — CritRate/Res처럼 1.0으로 클램프되는 값은
    /// 단순히 시너지 수치만큼 빼면 클램프로 잘려나간 부분만큼 원래 값보다 더 깎이는 오차가 생긴다.
    /// 전투 중에는 파티가 줄어들기만 하므로(영입은 스테이지 사이에만 일어남) 조건은 항상 깨지는 방향으로만
    /// 바뀐다 — 되돌리는 경우만 고려하면 된다.
    /// </summary>
    public sealed class PartySynergyTracker
    {
        private readonly RunData _run;
        private List<Unit> _units;

        /// <summary>파티에 있는 같은 캐릭터(시너지 보유 종류만)의 현재 생존 수.</summary>
        private readonly Dictionary<CharacterStatsSO, int> _aliveCountBySource = new();
        /// <summary>지금 시너지가 적용돼 있는 유닛만 들어있다 — 적용 안 된 유닛은 되돌릴 것도 없다.</summary>
        private readonly Dictionary<int, CharacterStatsSO> _synergySourceByUnitId = new();
        private readonly Dictionary<int, Stats> _preSynergyByUnitId = new();

        public PartySynergyTracker(RunData run) => _run = run;

        /// <summary>전투용 Unit을 만들며, 발동 조건을 만족하는 캐릭터에 시너지를 적용하고 되돌릴 스냅샷을 남긴다.</summary>
        public List<Unit> CreateBattleUnits()
        {
            _aliveCountBySource.Clear();
            _synergySourceByUnitId.Clear();
            _preSynergyByUnitId.Clear();

            foreach (RunMember member in _run.Members)
            {
                if (member.Source == null || !member.Source.HasSynergy)
                {
                    continue;
                }
                _aliveCountBySource.TryGetValue(member.Source, out int count);
                _aliveCountBySource[member.Source] = count + 1;
            }

            _units = new List<Unit>(_run.Members.Count);
            foreach (RunMember member in _run.Members)
            {
                Unit unit = member.CreateUnit();
                CharacterStatsSO source = member.Source;

                if (source != null && _aliveCountBySource.TryGetValue(source, out int count) && count >= source.SynergyThreshold)
                {
                    _preSynergyByUnitId[unit.Id] = unit.Stats.Clone();
                    source.CreateSynergy().ApplyTo(unit.Stats);
                    _synergySourceByUnitId[unit.Id] = source;
                }

                _units.Add(unit);
            }
            return _units;
        }

        /// <summary>
        /// 파티원이 죽었을 때 호출한다. 그 캐릭터 때문에 유지되던 시너지가 깨지면
        /// 같은 시너지를 받던 생존자의 스탯을 즉시 원래대로 되돌린다.
        /// </summary>
        /// <returns>표시가 바뀔 수 있는 경우 현재 시너지 목록(HUD 갱신용), 시너지와 무관한 사망이면 null.</returns>
        public List<PartySynergy> OnAllyDied(Unit deadUnit)
        {
            CharacterStatsSO source = _run.Members.FirstOrDefault(m => m.UnitId == deadUnit.Id)?.Source;
            if (source == null || !_aliveCountBySource.TryGetValue(source, out int count))
            {
                return null; // 시너지가 없는 캐릭터라 다른 시너지에 영향 없음
            }

            int remaining = count - 1;
            _aliveCountBySource[source] = remaining;

            // 죽은 유닛은 더 이상 시너지 대상이 아니다 — 표시 인원수에서 빠지도록 먼저 지운다.
            _synergySourceByUnitId.Remove(deadUnit.Id);
            _preSynergyByUnitId.Remove(deadUnit.Id);

            // 남은 인원이 임계 밑으로 내려갔을 때만 생존자의 스탯을 되돌린다(3명 중 1명 사망 등은 유지).
            if (remaining < source.SynergyThreshold)
            {
                foreach (Unit unit in _units)
                {
                    if (!_synergySourceByUnitId.TryGetValue(unit.Id, out CharacterStatsSO s) || s != source)
                    {
                        continue;
                    }

                    unit.Stats.CopyFrom(_preSynergyByUnitId[unit.Id]);
                    _synergySourceByUnitId.Remove(unit.Id);
                    _preSynergyByUnitId.Remove(unit.Id);
                }
            }

            return GetSynergies();
        }

        /// <summary>
        /// 파티가 보유한 시너지 목록(HUD 표시용). <b>인원이 모자라 아직 발동하지 않은 것도 포함</b>하며,
        /// 발동 여부는 <see cref="PartySynergy.IsActive"/>가 인원과 임계치로 판정한다.
        /// <see cref="CreateBattleUnits"/> 직후에는 전투 시작 상태를, <see cref="OnAllyDied"/> 이후에는
        /// 갱신된 상태를 돌려준다 — 판정 기준이 한 곳이라 "전투 시작 표시"와 "전투 중 갱신"이 어긋날 수 없다.
        ///
        /// <see cref="_aliveCountBySource"/>는 시너지 보유 캐릭터만 담고 있으므로(<see cref="CreateBattleUnits"/> 참고)
        /// 그대로 순회하면 곧 "보여줄 시너지 전부"가 된다.
        /// </summary>
        public List<PartySynergy> GetSynergies()
        {
            var synergies = new List<PartySynergy>(_aliveCountBySource.Count);
            foreach (KeyValuePair<CharacterStatsSO, int> entry in _aliveCountBySource)
            {
                synergies.Add(new PartySynergy(entry.Key, entry.Value, entry.Key.CreateSynergy()));
            }

            return synergies;
        }
    }
}