namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// "다음 스테이지 몬스터에게만" 적용되는 일회성 디버프 예약함.
    /// 선택지에서 쌓이고(<see cref="Add"/>), 몬스터 스폰 시 적용된 뒤(<see cref="ApplyTo"/>)
    /// 소모된다(<see cref="Consume"/>). 보스 직전까지 아껴두는 전략을 위해 값은 중첩 가능하다.
    /// </summary>
    public sealed class RunModifiers
    {
        public float EnemyHpMultiplier { get; private set; } = 1f;
        public float EnemyAtkMultiplier { get; private set; } = 1f;
        public bool EnemySkipFirstTurn { get; private set; }

        public bool HasAny => EnemyHpMultiplier != 1f || EnemyAtkMultiplier != 1f || EnemySkipFirstTurn;

        /// <summary>선택지 효과에 담긴 디버프를 예약한다(배율은 곱연산으로 중첩).</summary>
        public void Add(in RoguelikeEffect effect)
        {
            if (!effect.HasEnemyDebuff)
            {
                return;
            }

            EnemyHpMultiplier *= effect.EnemyHpMul;
            EnemyAtkMultiplier *= effect.EnemyAtkMul;
            EnemySkipFirstTurn |= effect.EnemySkipFirstTurn;
        }

        /// <summary>스폰된 몬스터 하나의 스탯에 배율을 적용한다(HP/ATK는 최소 1 보장).</summary>
        public void ApplyTo(Stats enemyStats)
        {
            if (EnemyHpMultiplier != 1f)
            {
                enemyStats.MaxHp = System.Math.Max(1, (int)(enemyStats.MaxHp * EnemyHpMultiplier));
            }

            if (EnemyAtkMultiplier != 1f)
            {
                enemyStats.Atk = System.Math.Max(1, (int)(enemyStats.Atk * EnemyAtkMultiplier));
            }
        }

        /// <summary>
        /// 저장된 값으로 예약을 되돌린다(체크포인트 복원).
        /// <see cref="Add"/>는 곱연산 누적이라 복원에 쓸 수 없어 따로 둔다.
        /// </summary>
        public void Restore(float enemyHpMultiplier, float enemyAtkMultiplier, bool enemySkipFirstTurn)
        {
            EnemyHpMultiplier = enemyHpMultiplier;
            EnemyAtkMultiplier = enemyAtkMultiplier;
            EnemySkipFirstTurn = enemySkipFirstTurn;
        }

        /// <summary>스테이지에 적용을 마친 뒤 호출하여 예약을 비운다.</summary>
        public void Consume()
        {
            EnemyHpMultiplier = 1f;
            EnemyAtkMultiplier = 1f;
            EnemySkipFirstTurn = false;
        }
    }
}
