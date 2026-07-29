using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    public sealed class InputBindingSaver
    {
        private readonly InputActionAsset _inputActions;

        /// <summary>생성 시점에 에셋이 있었는지. 없으면 아래 공개 메서드가 전부 조용히 넘어간다(보고는 생성자에서 1회).</summary>
        private readonly bool _isValid;

        public InputBindingSaver(InputActionAsset inputActions)
        {
            _inputActions = inputActions;
            _isValid = !NullCheck.LogIfMissing(inputActions, nameof(inputActions), this, "키 설정을 저장·복원할 수 없습니다");
        }

        /// <summary>저장소에서 바인딩 오버라이드 데이터를 불러와 적용</summary>
        public void LoadBindings()
        {
            if (!_isValid) return; // 누락은 생성자에서 보고 완료

            string overrides = SaveService.Current.Options.InputBindingOverrides;
            if (!string.IsNullOrEmpty(overrides))
            {
                _inputActions.LoadBindingOverridesFromJson(overrides);
            }
        }

        /// <summary>현재 적용된 바인딩 오버라이드를 저장소에 기록</summary>
        public void SaveBindings()
        {
            if (!_isValid) return;

            SaveService.Current.Options.InputBindingOverrides = _inputActions.SaveBindingOverridesAsJson();
            SaveService.Save();
        }

        public void ResetAllBindings()
        {
            if (!_isValid) return;

            _inputActions.RemoveAllBindingOverrides();
            SaveBindings();
        }
    }
}
