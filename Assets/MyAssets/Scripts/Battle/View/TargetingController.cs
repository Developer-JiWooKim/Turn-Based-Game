using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 플레이어 턴에 몬스터를 지정해 공격 대상을 정한다(README: 몬스터 직접 클릭 타겟팅).
    /// 마우스: 2단계 클릭 — 첫 클릭은 대상을 "겨냥"만 하고(빨강 아웃라인), 같은 대상을 다시 클릭하면 공격 확정.
    /// 키보드: 좌/우 방향키로 유효 대상을 순환 겨냥하고, Enter/Space로 겨냥 대상 공격 확정.
    /// 겨냥 표시는 대상 모델 렌더러의 레이어를 TargetMonster로 바꿔 아웃라인 색을 빨강으로 바꾸는 방식.
    /// </summary>
    public sealed class TargetingController : MonoBehaviour
    {
        [Tooltip("타겟팅 레이캐스트에 사용할 카메라. 비우면 Camera.main.")]
        [SerializeField] private Camera _camera;
        [Tooltip("클릭 판정 대상 레이어(몬스터 콜라이더).")]
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private float _rayDistance = 1000f;
        [Tooltip("겨냥된 몬스터에 씌울 레이어 이름(빨강 아웃라인). 렌더러만 이 레이어로 옮긴다.")]
        [SerializeField] private string _targetLayerName = "TargetMonster";

        private PlayerActionSelector _selector;
        private UnitViewRegistry _registry; // 방향키 순환 시 Unit → UnitView 조회에 사용
        private IReadOnlyList<Unit> _validTargets;
        private readonly List<Unit> _orderBuffer = new(); // 화면 좌→우 정렬 결과 재사용 버퍼
        private bool _awaitingInput;
        private int _targetLayer;

        // 현재 겨냥된 대상. 마우스로 같은 대상 재클릭 또는 Enter/Space로 공격 확정.
        private Unit _selectedTarget;
        private UnitView _selectedView;

        /// <summary>BattleDirector가 셀렉터와 View 레지스트리를 주입한다.</summary>
        public void Initialize(PlayerActionSelector selector, UnitViewRegistry registry)
        {
            _selector = selector;
            _registry = registry;
            _selector.ActionRequested += OnActionRequested;
        }

        private void Awake() => _targetLayer = LayerMask.NameToLayer(_targetLayerName);

        private void OnActionRequested(Unit actor, IReadOnlyList<Unit> validTargets)
        {
            ClearSelection();
            _validTargets = validTargets;
            _awaitingInput = true;
        }

        private void Update()
        {
            if (!_awaitingInput) return;

            InputManager input = InputManager.Instance;
            if (input == null) return;

            // 방향키: 유효 대상을 순환 겨냥(대기 유지)
            if (input.BattleCycleNextPressed)
            {
                CycleTarget(+1);
            }
            else if (input.BattleCyclePrevPressed)
            {
                CycleTarget(-1);
            }

            // Enter/Space: 겨냥된 대상이 있으면 공격 확정
            if (input.BattleConfirmPressed && _selectedTarget != null)
            {
                ConfirmTarget(_selectedTarget);
                return;
            }

            // 마우스 좌클릭: 2단계 클릭 타겟팅
            if (input.PrimaryPressedThisFrame)
            {
                HandleClick(input.PointerPosition);
            }
        }

        private void HandleClick(Vector2 pointer)
        {
            Camera cam = _camera != null ? _camera : MainCameraCache.Current;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _targetLayers)) return;

            UnitView view = hit.collider.GetComponentInParent<UnitView>();
            if (view == null) return;

            Unit target = FindValidTarget(view.UnitId);
            if (target == null) return; // 유효하지 않은 대상(아군/사망 등) 클릭은 무시

            if (target == _selectedTarget)
            {
                ConfirmTarget(target); // 2차 클릭: 같은 대상 → 공격 확정
                return;
            }

            // 1차 클릭(또는 다른 대상으로 변경): 겨냥만 표시하고 대기 유지
            SelectTarget(target, view);
        }

        /// <summary>
        /// 방향키로 유효 대상을 순환하며 겨냥을 옮긴다(+1 오른쪽 / -1 왼쪽).
        /// 순환 순서는 리스트 순서가 아니라 화면상의 좌→우 위치를 따른다.
        /// </summary>
        private void CycleTarget(int direction)
        {
            List<Unit> ordered = BuildScreenOrderedTargets();
            if (ordered.Count == 0) return;

            int index = ordered.IndexOf(_selectedTarget);
            int next = index < 0
                ? (direction > 0 ? 0 : ordered.Count - 1) // 겨냥 없던 상태: 왼쪽 끝/오른쪽 끝부터 시작
                : (index + direction + ordered.Count) % ordered.Count;

            Unit target = ordered[next];
            if (_registry != null && _registry.TryGet(target.Id, out UnitView view))
            {
                SelectTarget(target, view);
            }
        }

        /// <summary>
        /// 유효 대상 중 View가 있는 것들을 화면 X좌표 기준(좌→우)으로 정렬해 돌려준다.
        /// 매 입력마다 다시 계산하되(대상 수가 적음) 버퍼를 재사용해 할당을 피한다.
        /// </summary>
        private List<Unit> BuildScreenOrderedTargets()
        {
            _orderBuffer.Clear();
            if (_validTargets == null) return _orderBuffer;

            Camera cam = _camera != null ? _camera : MainCameraCache.Current;
            for (int i = 0; i < _validTargets.Count; i++)
            {
                Unit unit = _validTargets[i];
                if (_registry != null && _registry.TryGet(unit.Id, out UnitView view) && view != null)
                {
                    _orderBuffer.Add(unit);
                }
            }

            if (cam != null)
            {
                _orderBuffer.Sort((a, b) => ScreenX(cam, a).CompareTo(ScreenX(cam, b)));
            }

            return _orderBuffer;
        }

        private float ScreenX(Camera cam, Unit unit)
        => _registry != null && _registry.TryGet(unit.Id, out UnitView view) && view != null
                ? cam.WorldToScreenPoint(view.transform.position).x
                : 0f;

        /// <summary>겨냥을 지우고 대기를 끝낸 뒤 공격 대상을 확정 제출한다(마우스 2차 클릭·키보드 확정 공통).</summary>
        private void ConfirmTarget(Unit target)
        {
            ClearSelection();
            _awaitingInput = false;
            _validTargets = null;
            AudioManager.Confirm();
            _selector.SubmitTarget(target);
        }

        private void SelectTarget(Unit target, UnitView view)
        {
            if (_selectedView != null)
            {
                _selectedView.ResetOutlineLayer();
            }

            _selectedTarget = target;
            _selectedView = view;

            if (_targetLayer >= 0)
            {
                view.SetOutlineLayer(_targetLayer);
            }
        }

        /// <summary>겨냥 표시를 지우고 대상 아웃라인을 원래대로 되돌린다.</summary>
        private void ClearSelection()
        {
            if (_selectedView != null)
            {
                _selectedView.ResetOutlineLayer();
            }

            _selectedTarget = null;
            _selectedView = null;
        }

        private Unit FindValidTarget(int unitId)
        {
            if (_validTargets == null) return null;
            for (int i = 0; i < _validTargets.Count; i++)
            {
                if (_validTargets[i].Id == unitId)
                {
                    return _validTargets[i];
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            if (_selector != null)
            {
                _selector.ActionRequested -= OnActionRequested;
            }
        }
    }
}
