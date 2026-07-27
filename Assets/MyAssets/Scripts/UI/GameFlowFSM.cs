using Assets.MyAssets.Scripts.UI.States;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>현재 활성 UI 상태를 관리하는 단순 상태 머신 (View 레이어 내비게이션)</summary>
    public sealed class GameFlowFSM
    {
        private readonly GameUIController _controller;
        private IGameFlowState _current;
        public IGameFlowState Current => _current;

        public readonly TitleState TitleState = new TitleState();
        public readonly CharacterSelectState CharacterSelectState = new CharacterSelectState();

        public GameFlowFSM(GameUIController controller) => _controller = controller;

        /// <summary>상태 전환 메소드, 이전 상태의 Exit와 새 상태의 Enter 호출</summary>
        public void ChangeState(IGameFlowState next)
        {
            if (_current == next) return;

            _current?.Exit(_controller);
            _current = next;
            _current?.Enter(_controller);
        }
    }
}
