using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    public sealed class InputRebinder : IDisposable
    {
        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

        private readonly InputActionAsset _inputActions;
        private readonly InputBindingSaver _bindingSaver;

        /// <summary>생성 시점에 협력 객체가 다 있었는지. 없으면 <see cref="StartRebind"/>가 조용히 넘어간다(보고는 생성자에서 1회).</summary>
        private readonly bool _isValid = true;

        public InputRebinder(InputActionAsset inputActions, InputBindingSaver bindingSaver)
        {
            _inputActions = inputActions;
            _bindingSaver = bindingSaver;

            // 에셋은 UnityEngine.Object라 LogIfMissing(== 오버로드 유지), saver는 순수 C# 클래스라 LogIfNull.
            bool hasError = false;
            hasError |= NullCheck.LogIfMissing(inputActions, nameof(inputActions), this, "키를 재설정할 수 없습니다");
            hasError |= NullCheck.LogIfNull(bindingSaver, nameof(bindingSaver), this, "재설정한 키가 저장되지 않습니다");
            _isValid = !hasError;
        }

        public void StartRebind(RebindControl control, Action onFinished)
        {
            // asset이나 saver 중 하나라도 없으면(초기화 실패) 재설정을 진행하지 않는다.
            if (!_isValid)
            {
                onFinished?.Invoke();
                return;
            }

            // 이미 재설정 대기 중이면 이전 세션을 취소하고 새로 시작한다(버튼 중복 클릭 방어).
            if (_activeRebind != null)
            {
                Cancel();
            }

            InputAction primary = _inputActions.FindAction(control.ActionPaths[0]);
            if (primary == null)
            {
                // 조용히 빠져나가면 키 버튼을 눌러도 아무 일이 없는 것처럼만 보인다.
                Debug.LogError($"[InputRebinder] 대표 액션 '{control.ActionPaths[0]}'를 찾을 수 없어 재설정할 수 없습니다" +
                               $"({nameof(InputControls)}와 .inputactions 에셋이 어긋났는지 확인).");
                onFinished?.Invoke();
                return;
            }

            primary.Disable();
            _activeRebind = primary.PerformInteractiveRebinding(bindingIndex: 0)
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
                    if (sibling == null)
                    {
                        // 조용히 건너뛰면 새 키가 한쪽 맵에만 적용된다 —
                        // 예를 들어 전투 타겟팅에서는 먹히는데 메뉴에서는 안 먹히는 상태가 된다.
                        Debug.LogError($"[InputRebinder] 함께 재설정할 액션 '{control.ActionPaths[i]}'를 찾을 수 없습니다" +
                                       $" — '{control.Label}' 키가 일부 맵에만 적용됩니다.");
                        continue;
                    }

                    sibling.ApplyBindingOverride(bindingIndex: 0, path);
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
