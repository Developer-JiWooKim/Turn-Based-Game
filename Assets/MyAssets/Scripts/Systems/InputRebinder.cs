using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    public sealed class InputRebinder : IDisposable
    {
        private readonly InputActionAsset _inputActions;
        private readonly InputBindingSaver _bindingSaver;
        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        private bool IsInputActionAssetNull { get; set; } = false;
        private bool IsBindingSaverNull { get; set; } = false;

        public InputRebinder(InputActionAsset inputActions, InputBindingSaver bindingSaver)
        {
            _inputActions = inputActions;
            _bindingSaver = bindingSaver;

            if (_inputActions == null)
            {
                Debug.LogError("[InputRebinder] _inputActions is null");
                IsInputActionAssetNull = true;
            }

            if (_bindingSaver == null)
            {
                Debug.LogError("[InputRebinder] _bindingSaver is null");
                IsBindingSaverNull = true;
            }
        }

        public void StartRebind(RebindControl control, Action onFinished)
        {
            // asset이나 saver 중 하나라도 없으면(초기화 실패) 재설정을 진행하지 않는다.
            if (IsInputActionAssetNull || IsBindingSaverNull)
            {
                onFinished?.Invoke();
                return;
            }

            // 이미 재설정 대기 중이면 이전 세션을 취소하고 새로 시작한다(버튼 중복 클릭 방어).
            if (_activeRebind != null)
                Cancel();

            InputAction primary = _inputActions.FindAction(control.ActionPaths[0]);
            if (primary == null)
            {
                onFinished?.Invoke();
                return;
            }

            primary.Disable();
            _activeRebind = primary.PerformInteractiveRebinding(0)
                .WithControlsExcluding("<Mouse>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => FinishRebind(control, primary, onFinished, changed: true))
                .OnCancel(op => FinishRebind(control, primary, onFinished, changed: false))
                .Start();
        }

        private void FinishRebind(RebindControl control, InputAction primary, Action onFinished, bool changed)
        {
            _activeRebind?.Dispose();
            _activeRebind = null;
            primary.Enable();

            if (changed)
            {
                string path = primary.bindings[0].effectivePath;
                for (int i = 1; i < control.ActionPaths.Length; i++)
                {
                    InputAction sibling = _inputActions.FindAction(control.ActionPaths[i]);
                    sibling?.ApplyBindingOverride(0, path);
                }
                _bindingSaver.SaveBindings();
            }

            onFinished?.Invoke();
        }

        public void Cancel()
        {
            _activeRebind?.Cancel();
            _activeRebind?.Dispose();
            _activeRebind = null;
        }
        public void Dispose()
        {
            Cancel();
        }
    }
}
