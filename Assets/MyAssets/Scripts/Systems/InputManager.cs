using System;
using Assets.MyAssets.Scripts.Progression.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 흩어져 있던 플레이어 입력(마우스/키보드)을 한곳으로 모으는 허브
    ///  - Battle 맵: 타겟 순환(Cycle)·확정(Confirm). 전투 전용이라 <see cref="IsGameplayInputEnabled"/> 게이트를 탄다.
    ///  - Menu 맵: 메뉴 방향키(Nav)·확정(Submit)·퍼즈(Pause). 게이트와 무관(퍼즈 중에도 메뉴 조작 허용).
    ///  - UI 맵(템플릿 기본): 마우스 Point/Click. EventSystem의 InputSystemUIInputModule도 이 맵을 공유한다.
    /// 방향키가 Battle·Menu에 겹치지만 두 맥락은 시간상 겹치지 않아(타겟팅=전투 턴, 메뉴=턴 밖) 충돌하지 않는다.
    ///
    /// 리바인딩: 플레이어가 키를 바꾸면 오버라이드를 <see cref="SaveData"/>에 JSON으로 저장하고, 시작 시 다시 적용한다.
    /// </summary>
    public sealed class InputManager : Singleton<InputManager>
    {
        [Header("InputSystem_Actions(에셋)")]
        [Tooltip("바인딩이 정의된 InputActionAsset(InputSystem_Actions)")]
        [SerializeField] private InputActionAsset _actions;

        /// <summary>바인드 기능을 가진 모듈(순수 C#)</summary>
        private InputBindingSaver _saver;
        private InputRebinder _rebinder;

        // Battle 맵(게이트 적용)
        private InputAction _battleCyclePrev;
        private InputAction _battleCycleNext;
        private InputAction _battleConfirm;

        // Menu 맵(게이트 무관)
        private InputAction _menuNavPrev;
        private InputAction _menuNavNext;
        private InputAction _menuSubmit;
        private InputAction _menuPause;

        // UI 맵(마우스)
        private InputAction _point;
        private InputAction _click;

        /// <summary>
        /// 퍼즈/팝업 중 게임플레이 입력(타겟팅 등)을 막을지 여부
        /// false면 아래 게임플레이 입력 질의가 모두 false를 돌려준다(UI 입력은 영향 없음).
        /// </summary>
        public bool IsGameplayInputEnabled { get; set; } = true;

        /// <summary>현재 포인터(마우스) 화면 좌표</summary>
        public Vector2 PointerPosition => _point?.ReadValue<Vector2>() ?? Vector2.zero;

        /// <summary>이번 프레임에 주 상호작용(좌클릭)이 눌렸는지. 게임플레이 입력이 꺼져 있으면 항상 false.</summary>
        public bool PrimaryPressedThisFrame => IsGameplayInputEnabled && (_click?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 이전 대상(왼쪽 방향키)이 눌렸는지</summary>
        public bool BattleCyclePrevPressed => IsGameplayInputEnabled && (_battleCyclePrev?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 다음 대상(오른쪽 방향키)이 눌렸는지</summary>
        public bool BattleCycleNextPressed => IsGameplayInputEnabled && (_battleCycleNext?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 확정(Enter/Space)이 눌렸는지</summary>
        public bool BattleConfirmPressed => IsGameplayInputEnabled && (_battleConfirm?.WasPressedThisFrame() ?? false);

        /// <summary>이번 프레임에 UI 이전 항목(왼쪽 방향키)이 눌렸는지. 게임플레이 게이트와 무관</summary>
        public bool UiNavigatePrevPressed => _menuNavPrev?.WasPressedThisFrame() ?? false;

        /// <summary>이번 프레임에 UI 다음 항목(오른쪽 방향키)이 눌렸는지. 게임플레이 게이트와 무관</summary>
        public bool UiNavigateNextPressed => _menuNavNext?.WasPressedThisFrame() ?? false;

        /// <summary>이번 프레임에 UI 확정(Enter/Space)이 눌렸는지. 게임플레이 게이트와 무관</summary>
        public bool UiSubmitPressed => _menuSubmit?.WasPressedThisFrame() ?? false;

        /// <summary>이번 프레임에 퍼즈 토글(ESC)이 눌렸는지. 게임플레이 게이트와 무관</summary>
        public bool PauseTogglePressed => _menuPause?.WasPressedThisFrame() ?? false;

        protected override void Awake()
        {
            base.Awake();
            if (!IsValidInstance) return;

            DontDestroyOnLoad(gameObject);

            if (NullCheck.LogIfMissing(_actions, nameof(_actions), this, "어떤 입력도 받을 수 없습니다"))
                return;

            _saver = new InputBindingSaver(_actions);
            _rebinder = new InputRebinder(_actions, _saver);
            _saver.LoadBindings();

            CacheActions();
            EnableActionMaps();
        }

        /// <summary>키 액션 캐싱. 경로가 에셋과 어긋나면 여기서 즉시 예외로 드러난다.</summary>
        private void CacheActions()
        {
            _battleCyclePrev = _actions.FindAction(InputControls.BattleCyclePrev, throwIfNotFound: true);
            _battleCycleNext = _actions.FindAction(InputControls.BattleCycleNext, throwIfNotFound: true);
            _battleConfirm = _actions.FindAction(InputControls.BattleConfirm, throwIfNotFound: true);

            _menuNavPrev = _actions.FindAction(InputControls.MenuNavPrev, throwIfNotFound: true);
            _menuNavNext = _actions.FindAction(InputControls.MenuNavNext, throwIfNotFound: true);
            _menuSubmit = _actions.FindAction(InputControls.MenuSubmit, throwIfNotFound: true);
            _menuPause = _actions.FindAction(InputControls.MenuPause, throwIfNotFound: true);

            _point = _actions.FindAction(InputControls.UiPoint, throwIfNotFound: true);
            _click = _actions.FindAction(InputControls.UiClick, throwIfNotFound: true);
        }

        /// <summary>키 액션 활성화</summary>
        private void EnableActionMaps()
        {
            _actions.FindActionMap(InputControls.BattleMap).Enable();
            _actions.FindActionMap(InputControls.MenuMap).Enable();

            // UI 맵의 마우스만 필요(방향키 Navigate는 Menu 맵이 담당). EventSystem이 UI 맵을 켜지만,
            // 단독 실행 등으로 켜지지 않은 경우에도 마우스가 동작하도록 개별 액션을 확실히 켠다.
            _point.Enable();
            _click.Enable();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _rebinder?.Dispose(); // 리바인딩 세션 정리
        }

        /// <summary>
        /// 리바인딩 UI용 창구(InputBindingSaver, InputRebinder)
        /// </summary>
        public int RebindControlCount => InputControls.Rebindable.Length; // 재설정 UI가 나열할 논리 컨트롤 개수

        public string GetRebindLabel(int index) => InputControls.Rebindable[index].Label;

        /// <summary>논리 컨트롤에 현재 할당된 키의 표시 문자열(예: "Left Arrow")</summary>
        public string GetRebindDisplay(int index)
        {
            if (_actions == null) return "-";

            string path = InputControls.Rebindable[index].ActionPaths[0];
            InputAction action = _actions.FindAction(path);
            if (action == null)
            {
                Debug.LogError($"[InputManager] 액션 '{path}'를 찾을 수 없습니다" +
                               $"({nameof(InputControls)}와 .inputactions 에셋이 어긋났는지 확인).");
                return "-";
            }

            return action.GetBindingDisplayString(bindingIndex: 0);
        }

        public void StartRebind(int index, Action onFinished)
            => _rebinder.StartRebind(InputControls.Rebindable[index], onFinished);

        /// <summary>모든 리바인딩을 기본값으로 되돌리고 저장</summary>
        public void ResetBindings()
        {
            _rebinder.Cancel();
            _saver.ResetAllBindings();
        }
    }
}