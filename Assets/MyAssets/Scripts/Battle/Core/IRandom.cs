using System;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 전투 로직이 사용하는 난수 공급자
    /// UnityEngine.Random 대신 이 인터페이스에 의존하여 Core를 Unity 비의존으로 유지하고
    /// 테스트 시 결정적 난수를 주입할 수 있게
    /// </summary>
    public interface IRandom
    {
        /// <summary>[0.0, 1.0) 범위의 실수.</summary>
        float Value01();

        /// <summary>[minInclusive, maxExclusive) 범위의 정수.</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// System.Random 기반 기본 구현. 시드를 주면 재현 가능한 전투를 만들 수 있다.
    /// </summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom() : this(Environment.TickCount) { }
        public SystemRandom(int seed) => _random = new Random(seed);

        public float Value01() => (float)_random.NextDouble();

        public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
