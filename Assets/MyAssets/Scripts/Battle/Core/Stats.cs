namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 유닛의 전투 스탯 묶음. SO의 기준 수치로 생성한 뒤, 로그라이크 버프/디버프로 전투 중 변동될 수 있다.
    /// 순수 데이터이며 밸런싱 수치는 외부(SO)에서 주입한다.
    /// </summary>
    public sealed class Stats
    {
        public int MaxHp;
        public int Atk;
        /// <summary>턴 순서 결정. 버프/디버프로 전투 중 변동 가능.</summary>
        public int Spd;
        public int Def;
        /// <summary>치명타 확률 0~1 (최대 1.0 = 100%).</summary>
        public float CritRate;
        /// <summary>치명타 데미지 배율 (예: 1.5 = 150%).</summary>
        public float CritDmg;
        /// <summary>디버프 저항력 0~1.</summary>
        public float Res;

        public Stats(int maxHp, int atk, int spd, int def, float critRate, float critDmg, float res)
        {
            MaxHp = maxHp;
            Atk = atk;
            Spd = spd;
            Def = def;
            CritRate = critRate;
            CritDmg = critDmg;
            Res = res;
        }

        /// <summary>동일 수치의 독립 복사본. 원본(SO 캐시 등)을 공유·오염시키지 않기 위해 사용.</summary>
        public Stats Clone()
        {
            return new Stats(MaxHp, Atk, Spd, Def, CritRate, CritDmg, Res);
        }
    }
}
