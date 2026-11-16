using Assets.MyAssets.Scripts.Progression.Save;

namespace Assets.MyAssets.Scripts.UI.States
{
    /// <summary>타이틀 화면 UI에 들어와 있는 상태.</summary>
    public sealed class TitleState : IGameFlowState
    {
        public void Enter(GameUIController controller)
        {
            // 저장된 런이 있는지는 화면에 들어올 때마다 다시 확인.
            controller.TitlePanel.SetContinueEnabled(SaveService.HasRun);
            controller.TitlePanel.Show();
        }

        public void Exit(GameUIController controller)
        {
            controller.TitlePanel.Hide();
        }
    }
}
