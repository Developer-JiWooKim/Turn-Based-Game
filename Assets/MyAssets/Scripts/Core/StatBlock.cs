using System;

namespace TurnBasedGame.Core
{
    public readonly struct StatBlock
    {
        public int MaxHp { get; }
        public int Atk { get; }
        public int Spd { get; }
        public int Def { get; }
        public int CritRate { get; }
        public float CritDmg { get; }
        public int Res { get; }

        public StatBlock(int maxHp, int atk, int spd, int def, int critRate, float critDmg, int res)
        {
            MaxHp = maxHp;
            Atk = atk;
            Spd = spd;
            Def = def;
            CritRate = Math.Clamp(critRate, 0, 100);
            CritDmg = critDmg;
            Res = res;
        }
    }
}
