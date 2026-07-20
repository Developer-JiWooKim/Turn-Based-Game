using System.Collections.Generic;
using System.Linq;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 한 전투(스테이지)의 유닛 구성과 생존 상태를 담는 컨테이너(순수 로직).
    /// 승패 판정과 대상 후보 조회의 기준이 된다.
    /// </summary>
    public sealed class BattleState
    {
        private readonly List<Unit> _players;
        private readonly List<Unit> _enemies;

        public IReadOnlyList<Unit> Players => _players;
        public IReadOnlyList<Unit> Enemies => _enemies;

        public BattleState(IEnumerable<Unit> players, IEnumerable<Unit> enemies)
        {
            _players = players.ToList();
            _enemies = enemies.ToList();
        }

        public IEnumerable<Unit> AllUnits => _players.Concat(_enemies);
        public IEnumerable<Unit> AliveUnits => AllUnits.Where(u => u.IsAlive);

        /// <summary>지정 유닛 기준 살아있는 아군.</summary>
        public IReadOnlyList<Unit> AliveAlliesOf(Unit unit) =>
            (unit.Team == TeamSide.Player ? _players : _enemies).Where(u => u.IsAlive).ToList();

        /// <summary>지정 유닛 기준 살아있는 적(공격 대상 후보).</summary>
        public IReadOnlyList<Unit> AliveEnemiesOf(Unit unit) =>
            (unit.Team == TeamSide.Player ? _enemies : _players).Where(u => u.IsAlive).ToList();

        public bool IsPartyWiped => !_players.Any(u => u.IsAlive);
        public bool AreEnemiesCleared => !_enemies.Any(u => u.IsAlive);
        public bool IsBattleOver => IsPartyWiped || AreEnemiesCleared;
    }
}
