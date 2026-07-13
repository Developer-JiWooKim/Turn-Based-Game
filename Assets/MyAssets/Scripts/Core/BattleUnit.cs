using System;

namespace TurnBasedGame.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public sealed class BattleUnit
    {
        public int Id { get; }
        public string Name { get; }
        public Team Team { get; }
        public StatBlock BaseStats { get; }
        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        public BattleUnit(int id, string name, Team team, StatBlock baseStats)
        {
            Id = id;
            Name = name;
            Team = team;
            BaseStats = baseStats;
            CurrentHp = baseStats.MaxHp;
        }

        internal void ApplyDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
            }

            CurrentHp = Math.Max(0, CurrentHp - amount);
        }
    }
}
