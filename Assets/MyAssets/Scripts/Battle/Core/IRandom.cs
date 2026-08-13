namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>전투 로직이 사용하는 난수 공급자.</summary>
    public interface IRandom
    {
        /// <summary>[0.0 ~ 1.0] 범위의 실수.</summary>
        float Value01();

        /// <summary>[minInclusive, maxExclusive) 범위의 정수.</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// System.Random 기반 기본 구현. 시드를 주면 재현 가능한 전투를 만들 수 있다.
    /// </summary>
    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random _random;

        public SystemRandom() : this(System.Environment.TickCount) { }
        public SystemRandom(int seed) => _random = new System.Random(seed);

        public float Value01() => (float)_random.NextDouble();

        public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
