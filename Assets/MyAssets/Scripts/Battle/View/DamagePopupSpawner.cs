using Assets.MyAssets.Scripts.Systems;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 피해량 팝업의 풀을 소유하고 요청받은 위치에 하나씩 띄우는 담당.
    /// 무한 타워라 전투가 길어질수록 스폰 횟수가 계속 늘어나므로
    /// <see cref="UnitViewRegistry"/>와 같이 파괴 대신 <see cref="ObjectPool{T}"/>로 재사용한다
    /// (프리팹이 1종이라 레지스트리처럼 프리팹별 딕셔너리를 둘 필요는 없다).
    /// </summary>
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private DamagePopup _prefab;

        [Header("연출")]
        [Tooltip("같은 자리에 숫자가 겹쳐 읽기 어려워지지 않도록 좌우로 흩는 폭(월드 단위, ±).")]
        [SerializeField] private float _horizontalSpread = 0.35f;

        private const int PoolCapacity = 8;
        private const int PoolMaxSize = 32; // 라인 스킬로 여러 명이 동시에 맞아도 넉넉하도록

        private ObjectPool<DamagePopup> _pool;
        private bool _isValid;

        private void Awake()
        {
            _isValid = !NullCheck.LogIfMissing(_prefab, nameof(_prefab), this, "피해량 숫자가 표시되지 않습니다");
            if (!_isValid)
            {
                return;
            }

            _pool = new ObjectPool<DamagePopup>(
                // 스포너의 자식으로 만든다 — 유닛 밑에 두면 유닛이 풀에 반납될 때 함께 꺼진다.
                createFunc: () => Instantiate(_prefab, transform),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                defaultCapacity: PoolCapacity,
                maxSize: PoolMaxSize);
        }

        /// <summary>
        /// 지정한 월드 좌표에 피해량을 띄운다. 연출 완료는 기다리지 않는다(장식이라 전투 진행과 무관).
        /// </summary>
        public void Spawn(Vector3 worldPosition, int amount, DamageKind kind)
        {
            if (!_isValid || amount <= 0)
            {
                return; // 0은 보여줄 변화가 없다
            }

            DamagePopup popup = _pool.Get();
            popup.transform.position = worldPosition + Vector3.right * Random.Range(-_horizontalSpread, _horizontalSpread);
            popup.Play(amount, kind, Release);
        }

        private void Release(DamagePopup popup) => _pool.Release(popup);

#if UNITY_EDITOR
        private void OnValidate() => NullCheck.LogIfMissing(_prefab, nameof(_prefab), this, "피해량 숫자가 표시되지 않습니다");
#endif
    }
}
