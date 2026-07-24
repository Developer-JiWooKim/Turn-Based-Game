namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>유닛이 속한 진영</summary>
    public enum TeamSide
    {
        Player,
        Enemy
    }

    /// <summary>한 번의 행동 종류</summary>
    public enum ActionKind
    {
        Attack,
        Skill
    }

    /// <summary>스킬/공격이 노리는 대상 범위</summary>
    public enum TargetScope
    {
        Single,
        Line
    }

    /// <summary>전투 종료 결과</summary>
    public enum BattleOutcome
    {
        Victory,
        Defeat
    }
}
