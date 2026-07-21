using System;
using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 가중치에 비례해 중복 없이 뽑는 추첨(순수 로직). 로그라이크 선택지 제시에 사용하며,
    /// 추후 영구 포인트로 카테고리별 가중치를 투자하는 시스템도 이 위에 얹는다.
    /// </summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// 가중치 목록에서 중복 없이 최대 count개의 인덱스를 뽑는다.
        /// 한 번 뽑힌 항목은 후보에서 빠지므로 남은 항목들끼리 다시 정규화된다.
        /// 가중치가 전부 0 이하면 균등 추첨으로 대체한다.
        /// </summary>
        public static List<int> PickDistinct(IReadOnlyList<float> weights, int count, IRandom rng)
        {
            var picked = new List<int>(count);
            if (weights == null || weights.Count == 0 || count <= 0)
                return picked;

            var remaining = new List<int>(weights.Count);
            for (int i = 0; i < weights.Count; i++)
                remaining.Add(i);

            while (picked.Count < count && remaining.Count > 0)
            {
                float total = 0f;
                foreach (int index in remaining)
                    total += Math.Max(0f, weights[index]);

                int slot;
                if (total <= 0f)
                {
                    slot = rng.Range(0, remaining.Count); // 가중치가 없으면 균등하게
                }
                else
                {
                    float roll = rng.Value01() * total;
                    slot = remaining.Count - 1; // 부동소수 오차로 끝까지 못 고르면 마지막 항목
                    for (int k = 0; k < remaining.Count; k++)
                    {
                        roll -= Math.Max(0f, weights[remaining[k]]);
                        if (roll <= 0f)
                        {
                            slot = k;
                            break;
                        }
                    }
                }

                picked.Add(remaining[slot]);
                remaining.RemoveAt(slot);
            }

            return picked;
        }
    }
}
