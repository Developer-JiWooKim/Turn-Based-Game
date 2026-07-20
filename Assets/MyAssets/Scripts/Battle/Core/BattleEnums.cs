namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 유닛이 속한 진영.
    /// </summary>
    public enum TeamSide
    {
        Player,
        Enemy
    }

    /// <summary>
    /// 한 번의 행동 종류. 플레이어는 Attack만 사용하고, 보스 몬스터만 Skill을 사용한다.
    /// </summary>
    public enum ActionKind
    {
        Attack,
        Skill
    }

    /// <summary>
    /// 스킬/공격이 노리는 대상 범위. 가로 일렬 대형이므로 Line은 사실상 상대 진영 전체를 의미한다.
    /// </summary>
    public enum TargetScope
    {
        Single,
        Line
    }

    /// <summary>
    /// 전투 종료 결과.
    /// </summary>
    public enum BattleOutcome
    {
        /// <summary>아군이 적을 전멸시킴 → 다음 스테이지 진행.</summary>
        Victory,
        /// <summary>아군이 전멸 → 리타이어.</summary>
        Defeat
    }
}
