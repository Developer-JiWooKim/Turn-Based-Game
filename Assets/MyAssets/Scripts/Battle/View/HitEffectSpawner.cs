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
        [SerializeField] private float _scale = 1f;

        [Tooltip("타격 지점에서 밀어낼 오프셋(월드 단위). 이펙트가 유닛에 파묻히면 앞으로 당겨 쓴다.")]
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;

        [Tooltip("풀에 되돌리기까지의 시간(초). 0이면 파티클 설정(duration + startLifetime)에서 자동 계산한다.")]
        [SerializeField] private float _lifetime = 0f;

        [Tooltip("크리티컬일 때 크기에 곱하는 배율(1이면 일반 타격과 같다).")]
        [SerializeField] private float _criticalScaleMultiplier = 1.4f;

        private const int PoolCapacity = 4;
        private const int PoolMaxSize = 16; // 라인 스킬로 여러 명이 동시에 맞아도 넉넉하도록

        /// <summary>프리팹별 인스턴스 풀(일반/크리티컬 이펙트가 서로 다른 프리팹일 수 있다).</summary>
        private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _pools = new();

        /// <summary>
        /// 지정한 월드 좌표에 타격 이펙트를 띄운다.
        /// 연출 완료는 기다리지 않는다 — 데미지 팝업과 같이 장식이라 전투 진행과 무관하다.
        /// </summary>
        public void Spawn(Vector3 worldPosition, bool critical)
        {
            ParticleSystem prefab = critical && _criticalEffect != null ? _criticalEffect : _hitEffect;
            if (prefab == null)
            {
                return; // 이펙트를 아직 연결하지 않았다 — 조용히 넘어간다(다른 연출은 그대로 진행)
            }

            ObjectPool<ParticleSystem> pool = GetPool(prefab);
            ParticleSystem effect = pool.Get();

            float scale = critical ? _scale * _criticalScaleMultiplier : _scale;
            effect.transform.SetPositionAndRotation(worldPosition + _positionOffset, prefab.transform.rotation);
            effect.transform.localScale = Vector3.one * scale;

            effect.Play(withChildren: true);
            ReleaseWhenFinished(pool, effect, ResolveLifetime(prefab));
        }

        /// <summary>
        /// 인스펙터 값이 0이면 파티클 설정에서 재생 길이를 뽑는다.
        /// 커브형 수명은 constantMax가 0이라 최소 1초를 바닥으로 둔다(너무 일찍 반납해 잘리지 않도록).
        /// </summary>
        private float ResolveLifetime(ParticleSystem prefab)
        {
            if (_lifetime > 0f)
            {
                return _lifetime;
            }

            ParticleSystem.MainModule main = prefab.main;
            return Mathf.Max(1f, main.duration + main.startLifetime.constantMax);
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

        private ObjectPool<ParticleSystem> GetPool(ParticleSystem prefab)
        {
            if (_pools.TryGetValue(prefab, out ObjectPool<ParticleSystem> pool))
            {
                return pool;
            }

            pool = new ObjectPool<ParticleSystem>(
                // 스포너의 자식으로 만든다 — 유닛 밑에 두면 유닛이 풀에 반납될 때 재생 중인 이펙트가 함께 꺼진다.
                createFunc: () => Instantiate(prefab, transform),
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
