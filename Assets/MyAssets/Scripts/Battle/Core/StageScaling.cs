using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 스테이지가 올라갈수록 양측 스탯이 함께 강해지는 규칙(순수 계산, 수치는 SO에서 주입).
    ///
    /// 양측의 적용 방식이 다르다.
    ///  - 몬스터: 매 스테이지 새로 스폰되므로 기준 스탯에 <b>배율</b>을 곱한다(복리 가능 → 무한 타워의 벽).
    ///  - 플레이어: 현재 HP를 스테이지 간 이어받으므로 배율을 쓰면 HP가 어긋난다. 대신 진급할 때마다
    ///    기준 스탯 기반의 <b>flat 증가분</b>을 영구 누적한다(로그라이크 성장과 동일한 경로).
    ///
    /// 몬스터 성장률을 플레이어보다 높게 잡는 것이 전제다. 플레이어는 로그라이크 선택지로
    /// 원하는 스탯을 몰아서 올릴 수 있으므로, 자동 성장은 "완전히 뒤처지지 않을 정도"만 담당한다.
    ///
    /// SPD·치명타·저항은 스케일링 대상이 아니다. SPD는 양측이 같이 오르면 턴 순서가 그대로라 의미가 없고
    /// 몬스터만 올리면 선공을 계속 뺏겨 체감이 나쁘다. 비율 스탯은 곱하면 금방 상한에 붙는다.
    /// 이 스탯들은 로그라이크 선택지로만 성장한다.
    /// </summary>
    public readonly struct StageScaling
    {
        private readonly float _playerHpRate;
        private readonly float _playerAtkRate;
        private readonly float _playerDefRate;

        private readonly float _monsterHpRate;
        private readonly float _monsterAtkRate;
        private readonly float _monsterDefRate;

        /// <summary>true면 몬스터 배율이 복리((1+rate)^n), false면 선형(1+rate*n).</summary>
        private readonly bool _monsterCompound;

        public StageScaling(float playerHpRate, float playerAtkRate, float playerDefRate,
                            float monsterHpRate, float monsterAtkRate, float monsterDefRate,
                            bool monsterCompound)
        {
            _playerHpRate = playerHpRate;
            _playerAtkRate = playerAtkRate;
            _playerDefRate = playerDefRate;
            _monsterHpRate = monsterHpRate;
            _monsterAtkRate = monsterAtkRate;
            _monsterDefRate = monsterDefRate;
            _monsterCompound = monsterCompound;
        }

        /// <summary>
        /// 스테이지 1칸 진급분에 해당하는 플레이어 성장치를 만든다(기준 스탯 대비 비율 → flat).
        /// 로그라이크 효과와 같은 형태라 <see cref="Unit.ApplyGrowth"/> 경로를 그대로 탄다
        /// (= 늘어난 최대 HP만큼 현재 HP도 채워진다).
        /// </summary>
        /// <param name="baseStats">성장이 누적되기 전의 기준 스탯(SO 원본 수치).</param>
        /// <param name="step">몇 번째 성장인지(1 = 첫 진급). 누적 총량을 정확히 맞추는 데 쓰인다.</param>
        public RoguelikeEffect CreatePlayerGrowth(Stats baseStats, int step)
        {
            return new RoguelikeEffect(
                StepGrowth(baseStats.MaxHp, _playerHpRate, step),
                StepGrowth(baseStats.Atk, _playerAtkRate, step),
                0, // SPD는 스케일링 대상 아님
                StepGrowth(baseStats.Def, _playerDefRate, step),
                0, 0f, 0f, 0f,
                1f, 1f, false, false);
        }

        /// <summary>스폰된 몬스터 스탯에 스테이지 배율을 적용한다(1스테이지 = 배율 1).</summary>
        public void ApplyToMonster(Stats stats, int stage)
        {
            int steps = Math.Max(0, stage - 1);
            if (steps == 0)
            {
                return;
            }

            stats.MaxHp = Scale(stats.MaxHp, _monsterHpRate, steps);
            stats.Atk = Scale(stats.Atk, _monsterAtkRate, steps);
            stats.Def = Scale(stats.Def, _monsterDefRate, steps);
        }

        /// <summary>표시·디버깅용 몬스터 HP 배율.</summary>
        public float MonsterHpMultiplier(int stage) => Multiplier(_monsterHpRate, Math.Max(0, stage - 1));

        private int Scale(int value, float rate, int steps)
        {
            if (rate <= 0f)
            {
                return value;
            }

            return Math.Max(1, (int)Math.Round(value * Multiplier(rate, steps)));
        }

        private float Multiplier(float rate, int steps)
        {
            if (rate <= 0f || steps == 0)
            {
                return 1f;
            }

            return _monsterCompound ? (float)Math.Pow(1f + rate, steps) : 1f + rate * steps;
        }

        /// <summary>
        /// step번째 진급에서 더할 증가분. 누적 총량이 항상 <see cref="TotalGrowth"/>(= 기준값 × 비율 × step)이
        /// 되도록 직전 누적과의 차이를 돌려준다.
        ///
        /// 매 스테이지 따로 반올림하면 소수점 이하가 버려지거나(0.45 → 0) 최소 보정으로 부풀려져(→ +1),
        /// 기준값이 작은 스탯일수록 실제 비율에서 크게 어긋난다. 차분 방식은 그 오차가 누적되지 않는다.
        /// </summary>
        private static int StepGrowth(int baseValue, float rate, int step)
        {
            if (rate <= 0f || step <= 0)
            {
                return 0;
            }

            return TotalGrowth(baseValue, rate, step) - TotalGrowth(baseValue, rate, step - 1);
        }

        private static int TotalGrowth(int baseValue, float rate, int step) =>
            (int)Math.Round(baseValue * (double)rate * step);
    }
}
