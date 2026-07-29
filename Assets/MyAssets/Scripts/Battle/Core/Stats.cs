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

        /// <summary>
        /// 다른 스냅샷의 값으로 되돌린다(참조는 유지한 채 필드만 덮어씀).
        /// 임시 효과(파티 시너지 등)를 뺄셈이 아니라 스냅샷 복원으로 제거할 때 쓴다 —
        /// CritRate/Res처럼 1.0으로 클램프되는 값은 단순히 빼면 오차가 생기기 때문.
        /// </summary>
        public void CopyFrom(Stats other)
        {
            MaxHp = other.MaxHp;
            Atk = other.Atk;
            Spd = other.Spd;
            Def = other.Def;
            CritRate = other.CritRate;
            CritDmg = other.CritDmg;
            Res = other.Res;
        }
    }
}