using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 흩어져 있던 플레이어 입력(마우스/키보드)을 한곳으로 모으는 허브.
    /// 입력을 두 갈래로 분리한다:
    ///  - UI 입력: 포인터/클릭/메뉴 네비게이션. 생성된 <see cref="InputSystem_Actions"/>의 UI 맵이 담당(메뉴 조작은 EventSystem).
    ///  - 배틀 입력: 방향키 타겟 순환·확정 등 전투 전용 키. 아래 별도 액션 그룹으로 두어 UI 네비게이션과 충돌하지 않게 한다.
    /// View(예: TargetingController)는 원시 디바이스(Mouse.current 등)를 만지지 않고 이 매니저만 참조한다.
    /// </summary>
    public sealed class InputManager : Singleton<InputManager>
    {
        private InputSystem_Actions _actions;

        // 배틀 전용 키(UI 입력과 분리). 방향키는 UI 네비게이션과 겹치므로 배틀 맥락 전용 액션으로 따로 잡는다.
        private InputAction _battleCyclePrev; // 왼쪽 방향키: 이전 대상
        private InputAction _battleCycleNext; // 오른쪽 방향키: 다음 대상
        private InputAction _battleConfirm;   // Enter/Space: 겨냥 대상 공격 확정

        // UI 네비게이션(메뉴/선택지). 배틀 입력과 분리하고 IsGameplayInputEnabled에 묶지 않는다(퍼즈 중에도 UI 조작 허용).
        private InputAction _uiNavigatePrev; // 왼쪽 방향키: 이전 항목
        private InputAction _uiNavigateNext; // 오른쪽 방향키: 다음 항목
        private InputAction _uiSubmit;       // Enter/Space: 확정/선택

        private InputAction _pauseToggle;    // ESC: 배틀 퍼즈 열기/닫기

        /// <summary>
        /// 퍼즈/팝업 중 게임플레이 입력(타겟팅 등)을 막을지 여부.
        /// false면 아래 게임플레이 입력 질의가 모두 false를 돌려준다(UI 입력은 영향 없음).
        /// </summary>
        public bool IsGameplayInputEnabled { get; set; } = true;

        /// <summary>현재 포인터(마우스) 화면 좌표.</summary>
        public Vector2 PointerPosition =>
            _actions != null ? _actions.UI.Point.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>이번 프레임에 주 상호작용(좌클릭)이 눌렸는지. 게임플레이 입력이 꺼져 있으면 항상 false.</summary>
        public bool PrimaryPressedThisFrame =>
            IsGameplayInputEnabled && _actions != null && _actions.UI.Click.WasPressedThisFrame();

        /// <summary>이번 프레임에 이전 대상(왼쪽 방향키)이 눌렸는지.</summary>
        public bool BattleCyclePrevPressed =>
            IsGameplayInputEnabled && (_battleCyclePrev?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 다음 대상(오른쪽 방향키)이 눌렸는지.</summary>
        public bool BattleCycleNextPressed =>
            IsGameplayInputEnabled && (_battleCycleNext?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 확정(Enter/Space)이 눌렸는지.</summary>
        public bool BattleConfirmPressed =>
            IsGameplayInputEnabled && (_battleConfirm?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 UI 이전 항목(왼쪽 방향키)이 눌렸는지. 게임플레이 게이트와 무관.</summary>
        public bool UiNavigatePrevPressed => _uiNavigatePrev?.WasPressedThisFrame() ?? false;

        /// <summary>이번 프레임에 UI 다음 항목(오른쪽 방향키)이 눌렸는지. 게임플레이 게이트와 무관.</summary>
        public bool UiNavigateNextPressed => _uiNavigateNext?.WasPressedThisFrame() ?? false;

        /// <summary>이번 프레임에 UI 확정(Enter/Space)이 눌렸는지. 게임플레이 게이트와 무관.</summary>
        public bool UiSubmitPressed => _uiSubmit?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// 이번 프레임에 퍼즈 토글(ESC)이 눌렸는지. 게임플레이 게이트와 **무관**하다 —
        /// 여기 묶으면 퍼즈 중에 게이트가 닫혀 있어 ESC로 퍼즈를 풀 수 없다.
        /// </summary>
        public bool PauseTogglePressed => _pauseToggle?.WasPressedThisFrame() ?? false;

        protected override void Awake()
        {
            base.Awake();
            if (!IsValidInstance) return;

            DontDestroyOnLoad(gameObject);

            _actions = new InputSystem_Actions();
            _actions.UI.Enable();

            _battleCyclePrev = new InputAction("BattleCyclePrev", InputActionType.Button, "<Keyboard>/leftArrow");
            _battleCycleNext = new InputAction("BattleCycleNext", InputActionType.Button, "<Keyboard>/rightArrow");
            _battleConfirm = new InputAction("BattleConfirm", InputActionType.Button);
            _battleConfirm.AddBinding("<Keyboard>/enter");
            _battleConfirm.AddBinding("<Keyboard>/space");

            _uiNavigatePrev = new InputAction("UiNavigatePrev", InputActionType.Button, "<Keyboard>/leftArrow");
            _uiNavigateNext = new InputAction("UiNavigateNext", InputActionType.Button, "<Keyboard>/rightArrow");
            _uiSubmit = new InputAction("UiSubmit", InputActionType.Button);
            _uiSubmit.AddBinding("<Keyboard>/enter");
            _uiSubmit.AddBinding("<Keyboard>/space");

            _pauseToggle = new InputAction("PauseToggle", InputActionType.Button, "<Keyboard>/escape");

            _battleCyclePrev.Enable();
            _battleCycleNext.Enable();
            _battleConfirm.Enable();
            _uiNavigatePrev.Enable();
            _uiNavigateNext.Enable();
            _uiSubmit.Enable();
            _pauseToggle.Enable();

            // TODO#: ESC로 일반 팝업(옵션/성향) 닫기까지 묶는 건 추후 — 지금은 배틀 퍼즈 전용이다.
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _actions?.Dispose();
            _actions = null;
            _battleCyclePrev?.Dispose();
            _battleCycleNext?.Dispose();
            _battleConfirm?.Dispose();
            _uiNavigatePrev?.Dispose();
            _uiNavigateNext?.Dispose();
            _uiSubmit?.Dispose();
            _pauseToggle?.Dispose();
        }
    }
}
