using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 플레이어 턴에 몬스터를 클릭해 공격 대상을 지정한다(README: 몬스터 직접 클릭 타겟팅).
    /// PlayerActionSelector가 대상 선택을 요청하면 입력을 받고, 클릭한 UnitView를 유효 대상과
    /// 대조해 <see cref="PlayerActionSelector.SubmitTarget"/>로 되돌린다(View→Core 콜백).
    /// </summary>
    public sealed class TargetingController : MonoBehaviour
    {
        [Tooltip("타겟팅 레이캐스트에 사용할 카메라. 비우면 Camera.main.")]
        [SerializeField] private Camera _camera;
        [Tooltip("클릭 판정 대상 레이어(몬스터 콜라이더).")]
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private float _rayDistance = 1000f;

        private PlayerActionSelector _selector;
        private IReadOnlyList<Unit> _validTargets;
        private bool _awaitingInput;

        /// <summary>BattleDirector가 셀렉터를 주입한다.</summary>
        public void Initialize(PlayerActionSelector selector)
        {
            _selector = selector;
            _selector.ActionRequested += OnActionRequested;
        }

        private void OnActionRequested(Unit actor, IReadOnlyList<Unit> validTargets)
        {
            _validTargets = validTargets;
            _awaitingInput = true;
            // TODO(다음 청크): 유효 대상 하이라이트 / 커서 피드백
        }

        private void Update()
        {
            if (!_awaitingInput) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _targetLayers)) return;

            UnitView view = hit.collider.GetComponentInParent<UnitView>();
            if (view == null) return;

            Unit target = FindValidTarget(view.UnitId);
            if (target == null) return; // 유효하지 않은 대상(아군/사망 등) 클릭은 무시

            _awaitingInput = false;
            _validTargets = null;
            _selector.SubmitTarget(target);
        }

        private Unit FindValidTarget(int unitId)
        {
            if (_validTargets == null) return null;
            for (int i = 0; i < _validTargets.Count; i++)
                if (_validTargets[i].Id == unitId)
                    return _validTargets[i];
            return null;
        }

        private void OnDestroy()
        {
            if (_selector != null)
                _selector.ActionRequested -= OnActionRequested;
        }
    }
}
