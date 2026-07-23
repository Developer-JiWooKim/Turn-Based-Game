namespace Assets.MyAssets.Scripts.UI.States
{
    /// <summary>캐릭터 선택 화면 UI에 들어와 있는 상태</summary>
    public sealed class CharacterSelectState : IGameFlowState
    {
        public void Enter(GameUIController controller)
        {
            controller.CharacterSelectPanel.Show();
        }

        public void Exit(GameUIController controller)
        {
            controller.CharacterSelectPanel.Hide();
        }
    }
}
