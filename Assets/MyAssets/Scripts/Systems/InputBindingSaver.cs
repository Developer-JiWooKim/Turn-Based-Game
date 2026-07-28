using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    public sealed class InputBindingSaver
    {
        private readonly InputActionAsset _inputActions;
        private readonly bool _isValid = true;

        public InputBindingSaver(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
            if (_inputActions == null)
            {
                Debug.LogError("[InputBindingSaver] _inputActions is null");
                _isValid = false;
            }
        }

        /// <summary>저장소에서 바인딩 오버라이드 데이터를 불러와 적용</summary>
        public void LoadBindings()
        {
            if (!_isValid)
            {
                Debug.LogError("[InputBindingSaver] LoadBindings(): _inputActions is null");
                return;
            }

            string overrides = SaveService.Current.Options.InputBindingOverrides;
            if (!string.IsNullOrEmpty(overrides))
            {
                _inputActions.LoadBindingOverridesFromJson(overrides);
            }
        }

        /// <summary>현재 적용된 바인딩 오버라이드를 저장소에 기록</summary>
        public void SaveBindings()
        {
            if (!_isValid)
            {
                Debug.LogError("[InputBindingSaver] SaveBindings(): _inputActions is null");
                return;
            }

            SaveService.Current.Options.InputBindingOverrides = _inputActions.SaveBindingOverridesAsJson();
            SaveService.Save();
        }

        public void ResetAllBindings()
        {
            if (!_isValid)
            {
                Debug.LogError("[InputBindingSaver] ResetAllBindings(): _inputActions is null");
                return;
            }

            _inputActions.RemoveAllBindingOverrides();
            SaveBindings();
        }
    }
}
