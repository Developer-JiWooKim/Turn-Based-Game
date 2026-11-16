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
    /// <para>
    /// <b>난이도 가속</b>(2026-08-10): <see cref="_accelStartStage"/>까지는 기본 성장률을 그대로 쓰고,
    /// 그 뒤부터 <see cref="_accelMultiplier"/>를 곱한 성장률로 전환한다. "일정 지점까지는 편하게 도달하고
    /// 이후 난이도가 빠르게 오른다"는 곡선을 만들기 위한 것이며, 구간을 나누지 않고 성장률만 올리면
    /// 초반까지 함께 어려워진다.
    /// </para>
    ///
    /// <para>
    /// <b>SPD 스케일링</b>(2026-08-10에 방침 변경): 원래 SPD는 스케일링 대상이 아니었다 —
    /// "양측이 같이 오르면 턴 순서가 그대로라 의미가 없고, 몬스터만 올리면 선공을 계속 뺏겨 체감이 나쁘다"는
    /// 이유였다. 하지만 그 결과 <b>속도 강화 선택지가 사실상 빈 선택지</b>가 됐다(SPD를 올릴 이유가 없으므로).
    /// 그래서 몬스터 SPD를 조금씩 올려 "선공을 지키려면 투자해야 한다"는 압박을 만들고,
    /// 대신 보스를 잡을 때마다 플레이어에게도 SPD를 돌려준다(<see cref="_bossSpdRate"/>).
    ///
    /// <b>양측 모두 갱신 주기가 "보스 처치"다</b>(2026-08-12에 몬스터 쪽을 매 스테이지에서 옮겨왔다).
    /// 주기가 어긋나 있으면 플레이어는 보스마다 한 번 보상을 받는데 몬스터는 그사이 매 스테이지 빨라져
    /// <b>보상을 받은 직후에도 계속 밀리는</b> 체감이 된다. 같은 시점에 함께 올라야
    /// "이번 보스로 따라잡았다 / 못 따라잡았다"가 플레이어에게 읽힌다.
    /// ⚠️ 그래서 이제 <b>보상 비율(<see cref="_bossSpdRate"/>)이 몬스터 증가율(<see cref="_monsterSpdRate"/>)보다
    /// 작아야 한다</b>는 관계가 곧 난이도다 — 주기가 같아져 두 값을 직접 비교할 수 있게 됐다.
    /// 같은 값을 주면 선공 압박이 사라져 원래의 "SPD를 올릴 이유가 없다" 상태로 되돌아간다.
    /// 플레이어가 격차를 실제로 뒤집는 수단은 로그라이크 속도 강화 선택지이고, 이 보상은 완충재다.
    /// </para>
    ///
    /// 치명타·저항은 여전히 스케일링 대상이 아니다(곱하면 금방 상한에 붙는다). 선택지로만 성장한다.
    ///
    /// <para>
    /// <b>영입자 소급</b>(2026-08-12): 스테이지 자동 성장은 처음부터 전부 소급했지만 선택지 성장은 소급하지 않았다.
    /// 선택지가 비율로 바뀌면서 후반에 고른 한 장이 초반 것의 몇 배 값어치가 되자 그 격차가 감당할 수 없이 벌어져
    /// (36스테이지 영입자가 기존 파티원 ATK의 25%), <b>파티가 쌓아둔 몫의 일부를 물려받도록</b> 규칙을 바꿨다.
    /// 비율은 <see cref="RecruitChoiceCatchUpRate"/>이며 근거는 <c>BalanceCurve.md</c> G절.
    /// </para>
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

        /// <summary>이 스테이지까지는 기본 성장률. 0 이하면 가속 없음.</summary>
        private readonly int _accelStartStage;

        /// <summary>가속 구간에서 성장률에 곱하는 배수. 1 이하면 가속 없음.</summary>
        private readonly float _accelMultiplier;

        /// <summary>몬스터 SPD 성장률(<b>보스 처치 1회당</b>). 0이면 SPD를 올리지 않는다.</summary>
        private readonly float _monsterSpdRate;

        /// <summary>
        /// 몬스터 SPD 증가를 세기 시작하는 기준 스테이지(그 전까지는 기준값 그대로).
        /// 보스 간격의 위상까지 정하므로 <b>보스 스테이지 번호가 아니라 그 다음 스테이지</b>를 가리켜야
        /// "보스를 잡은 직후부터 빨라진다"가 된다 — 자세한 것은 <see cref="ScaleSpd"/> 참고.
        /// </summary>
        private readonly int _spdStartStage;

        /// <summary>보스 1회 처치당 플레이어가 얻는 SPD(기준 SPD 대비 비율). 0이면 보상 없음.</summary>
        private readonly float _bossSpdRate;

        /// <summary>보스가 등장하는 스테이지 간격. 출처는 <c>MonsterSpawner</c>이며 생성 시 주입된다.</summary>
        private readonly int _bossStageInterval;

        /// <summary>
        /// 영입자가 물려받는 <b>파티의 선택지 성장 비율</b>(0.6 = 60%). 0이면 소급하지 않는다.
        ///
        /// 스테이지 자동 성장(위 성장률)과 달리 이 값은 스테이지 번호가 아니라 <b>현재 파티가 쌓아둔 몫</b>에서
        /// 나오므로 여기서 계산하지 않고 비율만 들고 있는다 — 실제 적용은 <c>RunData.ApplyCatchUp</c>이 한다.
        /// </summary>
        public readonly float RecruitChoiceCatchUpRate;

        public StageScaling(float playerHpRate, float playerAtkRate, float playerDefRate,
                            float monsterHpRate, float monsterAtkRate, float monsterDefRate,
                            bool monsterCompound,
                            int accelStartStage, float accelMultiplier,
                            float monsterSpdRate, int spdStartStage,
                            float bossSpdRate, int bossStageInterval,
                            float recruitChoiceCatchUpRate)
        {
            RecruitChoiceCatchUpRate = recruitChoiceCatchUpRate;
            _playerHpRate = playerHpRate;
            _playerAtkRate = playerAtkRate;
            _playerDefRate = playerDefRate;
            _monsterHpRate = monsterHpRate;
            _monsterAtkRate = monsterAtkRate;
            _monsterDefRate = monsterDefRate;
            _monsterCompound = monsterCompound;
            _accelStartStage = accelStartStage;
            _accelMultiplier = accelMultiplier;
            _monsterSpdRate = monsterSpdRate;
            _spdStartStage = spdStartStage;
            _bossSpdRate = bossSpdRate;
            _bossStageInterval = bossStageInterval;
        }

        /// <summary>
        /// 스테이지 1칸 진급분에 해당하는 플레이어 성장치를 만든다(기준 스탯 대비 비율 → flat).
        /// 로그라이크 효과와 같은 형태라 <see cref="Unit.ApplyGrowth"/> 경로를 그대로 탄다
        /// (= 늘어난 최대 HP만큼 현재 HP도 채워진다).
        ///
        /// ⚠️ 여기서 만드는 건 <b>비율이 아니라 이미 계산된 고정 증분</b>이라
        /// <see cref="RoguelikeEffect"/>의 flat 생성자를 쓴다(선택지 쪽은 비율 생성자).
        /// 아래 <see cref="StepGrowth"/>처럼 누적 총량의 차분을 돌려주는 것이 목적이므로,
        /// 비율로 넘기면 매 스테이지 따로 반올림되어 그 정밀도가 사라진다.
        /// </summary>
        /// <param name="baseStats">성장이 누적되기 전의 기준 스탯(SO 원본 수치).</param>
        /// <param name="step">몇 번째 성장인지(1 = 첫 진급). 누적 총량을 정확히 맞추는 데 쓰인다.
        /// 진급 직후에 호출되므로 <b>방금 클리어한 스테이지 번호와 같고</b>, 보스 보상 판정에 그대로 쓴다.</param>
        public RoguelikeEffect CreatePlayerGrowth(Stats baseStats, int step)
        {
            return new RoguelikeEffect(
                StepGrowth(baseStats.MaxHp, _playerHpRate, step),
                StepGrowth(baseStats.Atk, _playerAtkRate, step),
                BossSpdStep(baseStats.Spd, step), // SPD는 보스를 잡았을 때만 오른다
                StepGrowth(baseStats.Def, _playerDefRate, step));
        }

        /// <summary>스폰된 몬스터 스탯에 스테이지 배율을 적용한다(1스테이지 = 배율 1).</summary>
        public void ApplyToMonster(Stats stats, int stage)
        {
            int steps = Math.Max(0, stage - 1);
            if (steps > 0)
            {
                stats.MaxHp = Scale(stats.MaxHp, _monsterHpRate, steps);
                stats.Atk = Scale(stats.Atk, _monsterAtkRate, steps);
                stats.Def = Scale(stats.Def, _monsterDefRate, steps);
            }

            // SPD는 시작 스테이지와 기준점이 달라 위 셋과 함께 묶지 않는다.
            stats.Spd = ScaleSpd(stats.Spd, stage);
        }

        /// <summary>표시·디버깅용 몬스터 HP 배율.</summary>
        public float MonsterHpMultiplier(int stage) => Multiplier(_monsterHpRate, Math.Max(0, stage - 1));

        private int Scale(int value, float rate, int steps)
        {
            if (rate <= 0f)
            {
                return value;
            }

            return Math.Max(1, (int)Math.Round(value * Multiplier(rate, steps), MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 몬스터 SPD 배율. 난이도 가속(<see cref="_accelMultiplier"/>)을 <b>일부러 적용하지 않는다</b> —
        /// 가속 구간에서 속도까지 함께 튀면 선공이 한순간에 뒤집혀, 플레이어가 대응할 여지가 없어진다.
        ///
        /// <para>
        /// 증가 단위는 스테이지가 아니라 <b>지나온 보스 수</b>다(<c>(stage − start) / 보스간격</c>).
        /// 플레이어 쪽 보상(<see cref="BossSpdStep"/>)이 보스 처치 단위라 주기를 맞춘 것이며,
        /// 덕분에 <see cref="_monsterSpdRate"/>와 <see cref="_bossSpdRate"/>를 같은 축에서 비교할 수 있다.
        /// </para>
        ///
        /// ⚠️ 나누기 전에 0으로 먼저 클램프한다 — C#의 정수 나눗셈은 0쪽으로 버림이라
        /// 음수를 그대로 나누면 시작 스테이지 이전 구간이 0이 아닌 값으로 새어 나올 수 있다.
        /// </summary>
        private int ScaleSpd(int value, int stage)
        {
            if (_monsterSpdRate <= 0f || _bossStageInterval <= 0)
            {
                return value;
            }

            int steps = Math.Max(0, stage - _spdStartStage) / _bossStageInterval;
            if (steps == 0)
            {
                return value;
            }

            float multiplier = _monsterCompound
                ? (float)Math.Pow(1f + _monsterSpdRate, steps)
                : 1f + _monsterSpdRate * steps;

            return Math.Max(1, (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 구간별 성장 배율. <see cref="_accelStartStage"/>까지는 기본 성장률, 그 뒤는 가속 성장률을 쓴다.
        /// 두 구간을 이어 붙이므로 경계 이전 구간의 누적값이 그대로 보존된다.
        /// </summary>
        private float Multiplier(float rate, int steps)
        {
            if (rate <= 0f || steps == 0)
            {
                return 1f;
            }

            // 가속이 꺼져 있으면(배수 1 이하 또는 시작 스테이지 미지정) 전 구간 동일 성장률.
            bool hasAccel = _accelMultiplier > 1f && _accelStartStage > 0;
            int normalSteps = hasAccel ? Math.Min(steps, Math.Max(0, _accelStartStage - 1)) : steps;
            int accelSteps = steps - normalSteps;
            float accelRate = rate * _accelMultiplier;

            if (!_monsterCompound)
            {
                return 1f + rate * normalSteps + accelRate * accelSteps;
            }

            return (float)(Math.Pow(1f + rate, normalSteps) * Math.Pow(1f + accelRate, accelSteps));
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

        /// <summary>
        /// 이번 진급에서 얻는 보스 처치 SPD 보상. 보스 스테이지가 아니면 0이다.
        ///
        /// <see cref="StepGrowth"/>와 같은 <b>누적 차분</b> 방식이라(횟수만 step 대신 처치한 보스 수),
        /// 보스마다 따로 반올림해 오차가 쌓이지 않는다. 덕분에 영입자 소급 적용(<c>RunData.ApplyCatchUp</c>)이
        /// 같은 step들을 되짚기만 해도 기존 파티원과 정확히 같은 값이 된다 —
        /// ⚠️ 이벤트("보스를 잡았다")로 만들면 소급 경로가 그 사실을 알 수 없어 어긋난다.
        /// </summary>
        private int BossSpdStep(int baseSpd, int step)
        {
            if (_bossSpdRate <= 0f || _bossStageInterval <= 0 || step <= 0)
            {
                return 0;
            }

            return TotalGrowth(baseSpd, _bossSpdRate, BossesCleared(step))
                 - TotalGrowth(baseSpd, _bossSpdRate, BossesCleared(step - 1));
        }

        /// <summary><paramref name="step"/>스테이지까지 클리어했을 때 지나온 보스 수.</summary>
        private int BossesCleared(int step) => step <= 0 ? 0 : step / _bossStageInterval;

        private static int TotalGrowth(int baseValue, float rate, int step) =>
            (int)Math.Round(baseValue * (double)rate * step);
    }
}
