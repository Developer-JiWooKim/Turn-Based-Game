using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 한 전투(스테이지)의 유닛 구성과 생존 상태를 담는 컨테이너(순수 로직).
    /// 승패 판정과 대상 후보 조회의 기준이 된다.
    ///
    /// 조회 메서드들은 <b>모든 유닛의 모든 행동마다</b> 불리므로 LINQ 대신 직접 루프를 쓰고,
    /// 결과 리스트는 호출자가 즉시 소비한다는 전제로 버퍼를 재사용한다(무한 타워라 할당이 런 내내 누적된다).
    /// </summary>
    public sealed class BattleState
    {
        private readonly List<Unit> _players;
        private readonly List<Unit> _enemies;

        // 조회 결과용 재사용 버퍼. 서로 다른 질의가 같은 버퍼를 덮어쓰지 않도록 용도별로 나눠 둔다.
        private readonly List<Unit> _aliveBuffer = new();
        private readonly List<Unit> _allyBuffer = new();
        private readonly List<Unit> _enemyBuffer = new();

        public IReadOnlyList<Unit> Players => _players;
        public IReadOnlyList<Unit> Enemies => _enemies;

        public BattleState(IEnumerable<Unit> players, IEnumerable<Unit> enemies)
        {
            _players = new List<Unit>(players);
            _enemies = new List<Unit>(enemies);
        }

        /// <summary>살아있는 전체 유닛(턴 순서 계산용). 반환된 리스트는 다음 호출 때 재사용된다.</summary>
        public IReadOnlyList<Unit> AliveUnits
        {
            get
            {
                _aliveBuffer.Clear();
                AppendAlive(_players, _aliveBuffer);
                AppendAlive(_enemies, _aliveBuffer);
                return _aliveBuffer;
            }
        }

        /// <summary>지정 유닛 기준 살아있는 아군. 반환된 리스트는 다음 호출 때 재사용된다.</summary>
        public IReadOnlyList<Unit> AliveAlliesOf(Unit unit)
        {
            _allyBuffer.Clear();
            AppendAlive(unit.Team == TeamSide.Player ? _players : _enemies, _allyBuffer);
            return _allyBuffer;
        }

        /// <summary>지정 유닛 기준 살아있는 적(공격 대상 후보). 반환된 리스트는 다음 호출 때 재사용된다.</summary>
        public IReadOnlyList<Unit> AliveEnemiesOf(Unit unit)
        {
            _enemyBuffer.Clear();
            AppendAlive(unit.Team == TeamSide.Player ? _enemies : _players, _enemyBuffer);
            return _enemyBuffer;
        }

        public bool IsPartyWiped => !HasAnyAlive(_players);
        public bool AreEnemiesCleared => !HasAnyAlive(_enemies);
        public bool IsBattleOver => IsPartyWiped || AreEnemiesCleared;

        private static void AppendAlive(List<Unit> source, List<Unit> destination)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].IsAlive)
                {
                    destination.Add(source[i]);
                }
            }
        }

        private static bool HasAnyAlive(List<Unit> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
