using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 원거리 공격의 투사체(화살·마법탄 등)를 쏘고 도착까지 기다리는 담당.
    ///
    /// <see cref="HitEffectSpawner"/>와 같은 프리팹별 풀 구조지만, 이쪽은 <b>도착 시점을 호출자가 기다린다</b> —
    /// 투사체가 닿기 전에 피해 숫자와 피격 연출이 먼저 나오면 순서가 거꾸로 보이기 때문이다.
    /// 그래서 <see cref="FlyAsync"/>만 Task를 돌려주고, 총구 섬광은 장식이라 기다리지 않는다.
    ///
    /// 프리팹은 스크립트 없는 순수 <see cref="ParticleSystem"/>이면 된다(Master Stylized Projectiles의
    /// <c>*Bullet</c>/<c>*Muzzle</c> 프리팹이 그대로 맞는다). 비행은 이 스크립트가 직접 옮기므로
    /// 프리팹 자체에 이동 기능이 있으면 안 된다.
    /// </summary>
    public sealed class ProjectileSpawner : MonoBehaviour
    {
        [Header("비행")]
        [Tooltip("비행 속도(월드 단위/초). 거리가 멀수록 오래 걸린다.")]
        [SerializeField] private float _speed = 18f;

        [Tooltip("비행 궤적의 최고 높이(월드 단위). 0이면 직선, 올리면 곡사(활 쏘듯)로 날아간다.")]
        [SerializeField] private float _arcHeight = 0f;

        [Tooltip("아무리 멀어도 비행이 이 시간을 넘지 않게 하는 상한(초). 전투가 늘어지지 않도록.")]
        [SerializeField] private float _maxFlightSeconds = 1.5f;

        [Header("정리")]
        [Tooltip("도착 후 풀에 되돌리기까지의 여유(초). 트레일이 뚝 끊기지 않도록 잠시 남겨둔다.")]
        [SerializeField] private float _lingerSeconds = 0.5f;

        [Tooltip("총구 섬광이 재생되는 시간(초).")]
        [SerializeField] private float _muzzleSeconds = 0.6f;

        [Tooltip("총구 섬광이 터지고 투사체가 출발하기까지의 간격(초). 0이면 동시에 나간다.")]
        [SerializeField] private float _muzzleLeadSeconds = 0.05f;

        private const int PoolCapacity = 4;
        private const int PoolMaxSize = 16; // 라인 스킬로 여러 명에게 동시에 날아가도 넉넉하도록

        private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> _pools = new();

        /// <summary>
        /// 투사체를 쏘고 <b>도착할 때까지</b> 기다린다. 프리팹이 없으면 즉시 통과한다(원거리 연출이 없는 유닛).
        /// </summary>
        public async Task FlyAsync(ParticleSystem prefab, Vector3 from, Vector3 to, CancellationToken ct)
        {
            if (prefab == null)
            {
                return;
            }

            ObjectPool<ParticleSystem> pool = GetPool(prefab);
            ParticleSystem projectile = pool.Get();

            Vector3 direction = to - from;
            projectile.transform.SetPositionAndRotation(from, LookRotation(direction, prefab));
            projectile.Play(withChildren: true);

            float duration = ResolveFlightSeconds(direction.magnitude);
            try
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    await Awaitable.NextFrameAsync(ct);
                    elapsed += Time.deltaTime;

                    float t = Mathf.Clamp01(elapsed / duration);
                    Vector3 position = Vector3.Lerp(from, to, t);
                    position.y += Mathf.Sin(t * Mathf.PI) * _arcHeight; // 0 → 최고점 → 0
                    projectile.transform.position = position;
                }

                projectile.transform.position = to;
            }
            finally
            {
                // 취소로 빠져나가도 인스턴스가 공중에 멈춘 채 남지 않도록 반드시 회수한다.
                // 여기서는 이미 여러 프레임 날아온 뒤라 방출을 끊어도 안전하다(트레일만 남아 사라진다).
                Retire(pool, projectile, _lingerSeconds, stopEmitting: true);
            }
        }

        /// <summary>
        /// 발사 지점의 섬광. 진행 방향을 보도록 회전하므로 총구에서는 앞으로,
        /// 낙하 연출에서는 아래를 향한다. 장식이라 재생 완료를 기다리지 않는다.
        /// </summary>
        public void SpawnMuzzle(ParticleSystem prefab, Vector3 from, Vector3 to)
        {
            if (prefab == null)
            {
                return;
            }

            ObjectPool<ParticleSystem> pool = GetPool(prefab);
            ParticleSystem muzzle = pool.Get();

            muzzle.transform.SetPositionAndRotation(from, LookRotation(to - from, prefab));
            muzzle.Play(withChildren: true);

            // ⚠️ stopEmitting을 켜면 안 된다 — Play와 같은 프레임에 방출이 끊겨 입자가 하나도 안 나온다.
            Retire(pool, muzzle, _muzzleSeconds, stopEmitting: false);
        }

        /// <summary>
        /// 섬광이 먼저 보이고 투사체가 출발하도록 한 박자 기다린다.
        /// 대상이 여럿이어도 박자는 하나뿐이라 섬광 스폰과 분리해 뒀다.
        /// </summary>
        public Task WaitMuzzleLeadAsync(CancellationToken ct) =>
            _muzzleLeadSeconds > 0f ? WaitAsync(_muzzleLeadSeconds, ct) : Task.CompletedTask;

        private static async Task WaitAsync(float seconds, CancellationToken ct) =>
            await Awaitable.WaitForSecondsAsync(seconds, ct);

        /// <summary>거리에 비례한 비행 시간(상한 적용). 속도가 0이면 상한을 그대로 쓴다.</summary>
        private float ResolveFlightSeconds(float distance)
        {
            if (_speed <= 0f)
            {
                return _maxFlightSeconds;
            }

            return Mathf.Min(distance / _speed, _maxFlightSeconds);
        }

        /// <summary>진행 방향을 보는 회전. 방향이 0이면 프리팹 원본 회전을 그대로 쓴다.</summary>
        private static Quaternion LookRotation(Vector3 direction, ParticleSystem prefab) =>
            direction.sqrMagnitude < 0.0001f ? prefab.transform.rotation : Quaternion.LookRotation(direction);

        /// <summary>
        /// 잠시 두었다가 풀에 되돌린다. 즉시 반납하면 잔여 입자가 한 프레임에 사라져 뚝 끊겨 보인다.
        /// 호출자를 기다리게 하지 않는 fire-and-forget이라 예외를 여기서 모두 받는다.
        /// </summary>
        /// <param name="stopEmitting">
        /// 지금 즉시 방출을 끊을지. <b>이미 여러 프레임 재생된 뒤에만 true를 줄 것</b> —
        /// <see cref="ParticleSystem.Play()"/>와 같은 프레임에 끊으면 입자가 하나도 방출되지 않아
        /// 이펙트가 통째로 보이지 않는다(총구 섬광이 안 보이던 실제 버그).
        /// </param>
        private async void Retire(ObjectPool<ParticleSystem> pool, ParticleSystem effect, float seconds,
                                  bool stopEmitting)
        {
            try
            {
                if (stopEmitting)
                {
                    effect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                }

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
                // 스포너의 자식으로 만든다 — 유닛 밑에 두면 유닛이 풀에 반납될 때 날아가던 투사체가 함께 꺼진다.
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
