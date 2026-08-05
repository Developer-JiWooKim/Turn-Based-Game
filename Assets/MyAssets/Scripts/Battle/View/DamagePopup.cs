using System;
using Assets.MyAssets.Scripts.Systems;
using TMPro;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 데미지 팝업의 표시 종류. 색과 크기만 다르고 동작은 같으므로
    /// <see cref="Core.RoguelikeEffect"/>·<see cref="Core.StatusEffect"/> 선례대로 종류별 클래스로 쪼개지 않는다.
    /// </summary>
    public enum DamageKind
    {
        Normal,
        Critical,
        /// <summary>중독 등 자기 차례 시작 시 들어오는 도트 피해.</summary>
        DoT
    }

    /// <summary>
    /// 피격 지점 위로 떠오르며 사라지는 피해량 숫자 1개.
    /// <see cref="DamagePopupSpawner"/>가 풀에서 꺼내 위치를 잡고 <see cref="Play"/>를 호출하며,
    /// 연출이 끝나면 콜백으로 스스로 반납을 요청한다.
    ///
    /// 유닛의 자식이 아니라 스포너 아래에 두는 점이 중요하다 — 유닛 View는 풀에 반납될 때
    /// 비활성화되므로, 자식으로 붙이면 재생 중인 팝업이 함께 꺼진다.
    /// </summary>
    public sealed class DamagePopup : MonoBehaviour
    {
        [Header("Reference Components")]
        [Tooltip("인스펙터가 비어 있으면 자식에서 찾아 채운다. 숫자만 표기하므로 폰트의 ASCII 제약에 걸리지 않는다.")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("카메라를 향하게 회전시킬 루트. 비워두면 자기 트랜스폼을 쓴다.")]
        [SerializeField] private Transform _billboardRoot;

        [Header("색")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _criticalColor = new(1f, 0.29f, 0.24f);
        [Tooltip("도트 피해(중독 등) — 타격과 구분되도록 다른 색을 쓴다.")]
        [SerializeField] private Color _dotColor = new(0.55f, 0.95f, 0.45f);

        [Header("연출")]
        [Tooltip("크리티컬일 때 글자 확대 배율.")]
        [SerializeField] private float _criticalScale = 1.6f;
        [Tooltip("사라질 때까지 떠오르는 높이(월드 단위).")]
        [SerializeField] private float _riseDistance = 1.2f;
        [Tooltip("떠올라 사라지기까지의 시간(초).")]
        [SerializeField] private float _duration = 0.8f;

        /// <summary>연출 중 크기를 바꾸므로 프리팹 원래 스케일을 1회만 기억해둔다(풀 재사용 시 복원 기준).</summary>
        private Vector3 _baseScale;

        private Action<DamagePopup> _onFinished;

        private void Awake()
        {
            // UnitView와 같은 자동 탐색 — 프리팹마다 일일이 연결하지 않아도 되도록.
            if (_text == null) _text = GetComponentInChildren<TMP_Text>(true);
            if (_billboardRoot == null) _billboardRoot = transform;

            _baseScale = transform.localScale;

            // 인스펙터 공란이 정상이라(자동 탐색) OnValidate는 두지 않는다 — UnitView와 같은 이유.
            NullCheck.LogIfMissing(_text, nameof(_text), this, "피해량 숫자가 표시되지 않습니다");
        }

        /// <summary>
        /// 숫자를 띄우고 연출을 시작한다. 연출이 끝나면 <paramref name="onFinished"/>로 반납을 알린다.
        /// 시뮬레이션을 대기시키지 않는 순수 장식이라 Task를 반환하지 않는다.
        /// </summary>
        public void Play(int amount, DamageKind kind, Action<DamagePopup> onFinished)
        {
            _onFinished = onFinished;

            if (_text == null)
            {
                Finish(); // 누락은 Awake에서 보고 완료 — 풀에 새지 않도록 즉시 반납한다
                return;
            }

            _text.SetText("{0}", amount);
            _text.color = ColorOf(kind);
            transform.localScale = _baseScale * (kind == DamageKind.Critical ? _criticalScale : 1f);

            _ = RiseRoutine();
        }

        private Color ColorOf(DamageKind kind) => kind switch
        {
            DamageKind.Critical => _criticalColor,
            DamageKind.DoT => _dotColor,
            _ => _normalColor
        };

        /// <summary>
        /// 떠오르며 서서히 사라진다. <see cref="CameraShake"/>와 같은 프레임 루프 방식이며
        /// <c>Time.timeScale</c>은 쓰지 않는다(프로젝트 규약 — 퍼즈는 게이트 대기로 처리).
        /// </summary>
        private async Awaitable RiseRoutine()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * _riseDistance;
            Color color = _text.color;
            float elapsed = 0f;

            try
            {
                while (elapsed < _duration)
                {
                    float t = elapsed / _duration;

                    // 처음엔 빠르게 솟았다가 잦아들도록(ease-out) — 등속보다 타격감이 산다.
                    float ease = 1f - (1f - t) * (1f - t);
                    transform.position = Vector3.Lerp(start, end, ease);

                    // 절반까지는 또렷하게 두고 그 뒤부터 사라진다 — 숫자를 읽을 시간을 준다.
                    color.a = Mathf.Clamp01((1f - t) * 2f);
                    _text.color = color;

                    elapsed += Time.deltaTime;
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return; // 씬 전환 등으로 파괴됨 — 반납할 풀도 이미 사라졌다
            }

            Finish();
        }

        /// <summary>반납 콜백을 1회만 호출한다(중복 반납은 풀을 망가뜨린다).</summary>
        private void Finish()
        {
            Action<DamagePopup> callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(this);
        }

        private void LateUpdate()
        {
            if (_billboardRoot == null) return;

            // 체력바와 같은 방식 — Camera.main은 태그 검색이라 캐시를 통해 조회한다.
            Transform camera = MainCameraCache.CurrentTransform;
            if (camera == null) return;

            _billboardRoot.forward = camera.forward;
        }
    }
}
