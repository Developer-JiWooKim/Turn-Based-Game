using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 타격 순간 재생되는 파티클 이펙트의 풀을 소유하고 요청받은 위치에 하나씩 띄우는 담당.
    /// <see cref="DamagePopupSpawner"/>와 같은 구조이며, 이펙트 프리팹이 여러 종류일 수 있어
    /// <see cref="UnitViewRegistry"/>처럼 <b>프리팹별로</b> 풀을 나눠 갖는다.
    ///
    /// 이펙트 프리팹은 스크립트가 붙지 않은 순수 <see cref="ParticleSystem"/>이면 된다 —
    /// 구해온 에셋을 그대로 슬롯에 넣고 크기·수명·위치를 인스펙터에서 조정하는 것을 전제로 설계했다.
    /// </summary>
    public sealed class HitEffectSpawner : MonoBehaviour
    {
        [Header("이펙트 프리팹")]
        [Tooltip("일반 타격 이펙트. 비워두면 이펙트 없이 진행한다.")]
        [SerializeField] private ParticleSystem _hitEffect;

        [Tooltip("크리티컬 전용 이펙트. 비워두면 일반 타격 이펙트를 그대로 쓴다.")]
        [SerializeField] private ParticleSystem _criticalEffect;

        [Header("연출")]
        [Tooltip("이펙트 크기 배율(프리팹 크기가 유닛에 비해 크거나 작을 때 조정).")]
        [SerializeField] private float _scale = 1.5f;

        [Tooltip("타격 지점에서 카메라 쪽으로 당기는 거리(월드 단위). " +
                 "0이면 몸 한가운데에서 터져 메시에 가려진다 — 캐릭터 앞으로 빼내는 값이다.")]
        [SerializeField] private float _cameraOffset = 0.6f;

        [Tooltip("추가 위치 보정(월드 단위). 좌우·높이를 미세 조정할 때만 쓴다.")]
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;

        [Tooltip("풀에 되돌리기까지의 시간(초). 0이면 파티클 설정(duration + startLifetime)에서 자동 계산한다.")]
        [SerializeField] private float _lifetime = 0f;

        [Tooltip("크리티컬일 때 크기에 곱하는 배율(1이면 일반 타격과 같다).")]
        [SerializeField] private float _criticalScaleMultiplier = 1.4f;

        private const int PoolCapacity = 4;
        private const int PoolMaxSize = 16; // 라인 스킬로 여러 명이 동시에 맞아도 넉넉하도록

        /// <summary>프리팹별 인스턴스 풀(일반/크리티컬 이펙트가 서로 다른 프리팹일 수 있다).</summary>
        private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _pools = new();

        /// <summary>프리팹별 자동 계산된 재생 길이(<see cref="ResolveLifetime"/> 참고).</summary>
        private readonly Dictionary<ParticleSystem, float> _autoLifetimes = new();

        /// <summary>
        /// 지정한 월드 좌표에 타격 이펙트를 띄운다.
        /// 연출 완료는 기다리지 않는다 — 데미지 팝업과 같이 장식이라 전투 진행과 무관하다.
        /// </summary>
        /// <param name="overridePrefab">
        /// 공격자(<see cref="UnitView.HitEffect"/>)가 가진 전용 이펙트. 있으면 아래 기본 이펙트보다 우선한다 —
        /// 화살은 화살대로, 마법은 마법대로 명중 이펙트가 달라야 하기 때문.
        /// </param>
        public void SpawnHit(Vector3 worldPosition, bool critical, ParticleSystem overridePrefab = null)
        {
            ParticleSystem prefab = ResolveHitPrefab(critical, overridePrefab);
            Play(prefab, worldPosition, critical ? _criticalScaleMultiplier : 1f);
        }

        /// <summary>
        /// 유닛 전용 → 크리티컬 기본 → 일반 기본 순으로 고른다.
        /// 유닛 전용에는 크리티컬 변형을 따로 두지 않고 크기만 키운다(슬롯을 유닛마다 둘씩 늘리지 않기 위함).
        /// </summary>
        private ParticleSystem ResolveHitPrefab(bool critical, ParticleSystem overridePrefab)
        {
            if (overridePrefab != null)
            {
                return overridePrefab;
            }

            return critical && _criticalEffect != null ? _criticalEffect : _hitEffect;
        }

        /// <summary>프리팹 하나를 그 자리에서 재생한다(풀 관리 포함).</summary>
        private void Play(ParticleSystem prefab, Vector3 worldPosition, float scaleMultiplier = 1f)
        {
            if (prefab == null)
            {
                return; // 이펙트를 아직 연결하지 않았다 — 조용히 넘어간다(다른 연출은 그대로 진행)
            }

            ObjectPool<ParticleSystem> pool = GetPool(prefab);
            ParticleSystem effect = pool.Get();

            effect.transform.SetPositionAndRotation(ResolvePosition(worldPosition), prefab.transform.rotation);
            effect.transform.localScale = Vector3.one * (_scale * scaleMultiplier);

            effect.Play(withChildren: true);
            ReleaseWhenFinished(pool, effect, ResolveLifetime(prefab));
        }

        /// <summary>
        /// 타격 지점을 카메라 쪽으로 당겨 캐릭터 메시에 가려지지 않게 한다.
        ///
        /// 고정 방향 오프셋을 쓰지 않는 이유 — 아군은 화면 아래쪽, 적군은 위쪽에 서 있어서
        /// 한 방향으로 밀면 한쪽 진영에서는 오히려 몸 안쪽으로 들어간다.
        /// 카메라를 향해 당기면 양쪽 모두 항상 캐릭터 앞으로 나온다.
        /// </summary>
        private Vector3 ResolvePosition(Vector3 worldPosition)
        {
            Vector3 position = worldPosition + _positionOffset;

            Transform camera = MainCameraCache.CurrentTransform;
            if (camera == null || Mathf.Approximately(_cameraOffset, 0f))
            {
                return position;
            }

            Vector3 toCamera = camera.position - worldPosition;
            return position + toCamera.normalized * _cameraOffset;
        }

        /// <summary>
        /// 인스펙터 값이 0이면 파티클 설정에서 재생 길이를 뽑는다.
        /// 자식 시스템까지 훑어 <b>가장 오래 사는</b> 것에 맞춘다 — 루트만 보면 더 긴 자식이 재생 도중 반납된다.
        /// 커브형 수명은 constantMax가 0이라 최소 1초를 바닥으로 둔다(너무 일찍 반납해 잘리지 않도록).
        /// 계층 탐색은 프리팹당 1회만 하고 캐싱한다(스폰마다 훑을 이유가 없다).
        /// </summary>
        private float ResolveLifetime(ParticleSystem prefab)
        {
            if (_lifetime > 0f)
            {
                return _lifetime; // 인스펙터에서 직접 준 값이 항상 우선(플레이 중 조정도 바로 반영된다)
            }

            if (!_autoLifetimes.TryGetValue(prefab, out float seconds))
            {
                seconds = 1f;
                foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = system.main;
                    seconds = Mathf.Max(seconds, main.duration + main.startLifetime.constantMax);
                }

                _autoLifetimes[prefab] = seconds;
            }

            return seconds;
        }

        /// <summary>
        /// 재생이 끝날 시간에 맞춰 풀에 되돌린다.
        /// 호출자를 기다리게 하지 않는 fire-and-forget이라 예외가 밖으로 나가지 않도록 여기서 모두 받는다.
        /// </summary>
        private async void ReleaseWhenFinished(ObjectPool<ParticleSystem> pool, ParticleSystem effect, float seconds)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(seconds, destroyCancellationToken);
                pool.Release(effect);
            }
            catch (OperationCanceledException)
            {
                // 씬이 끝나 스포너째 사라지는 중 — 인스턴스도 함께 파괴되므로 반납할 필요가 없다.
            }
        }

        /// <summary>
        /// 풀에 넣을 인스턴스를 만든다.
        ///
        /// ⚠️ 스케일링 모드를 <see cref="ParticleSystemScalingMode.Hierarchy"/>로 바꿔 두는 것이 핵심이다 —
        /// 파티클 시스템의 기본값인 <c>Shape</c>는 transform 스케일을 <b>방출 모양에만</b> 적용해서
        /// 입자 크기와 속도는 그대로다. 그 상태로는 <c>localScale</c>(= 인스펙터 크기 배율)을
        /// 아무리 올려도 이펙트가 커지지 않는다(실제로 겪은 증상).
        ///
        /// 자식 파티클 시스템까지 전부 바꾼다 — 스파클류는 여러 시스템이 겹쳐 하나의 이펙트를 이루는 경우가 많고,
        /// 루트만 바꾸면 자식들만 원래 크기로 남아 어긋난다.
        /// (자식이 루트의 배율을 물려받아야 하므로 자기 스케일만 보는 Local이 아니라 Hierarchy를 쓴다.)
        /// </summary>
        private ParticleSystem CreateInstance(ParticleSystem prefab)
        {
            ParticleSystem effect = Instantiate(prefab, transform);

            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }

            return effect;
        }

        private ObjectPool<ParticleSystem> GetPool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out ObjectPool<ParticleSystem> pool))
            {
                return pool;
            }

            pool = new ObjectPool<ParticleSystem>(
                // 스포너의 자식으로 만든다 — 유닛 밑에 두면 유닛이 풀에 반납될 때 재생 중인 이펙트가 함께 꺼진다.
                createFunc: () => CreateInstance(prefab),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p =>
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    p.gameObject.SetActive(false);
                },
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                defaultCapacity: PoolCapacity,
                maxSize: PoolMaxSize);

            _pools[prefab] = pool;
            return pool;
        }
    }
}
