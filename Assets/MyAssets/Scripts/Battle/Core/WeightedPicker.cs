using System;
using System.Collections.Generic;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary> 가중치에 비례해 중복 없이 뽑는 추첨기 </summary>
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
            if (weights == null || weights.Count == 0 || count <= 0) // 가중치 X, 뽑아야 될 개수가 0개 이하면 빈 리스트 반환
                return picked;

            var remaining = new List<int>(weights.Count);
            for (int i = 0; i < weights.Count; i++)
                remaining.Add(i); // 원본 데이터 리스트(weights)의 인덱스를 관리하는 리스트(remaining) 생성 후 인덱스 매칭

            while (picked.Count < count && remaining.Count > 0)
            {
                float total = 0f;
                foreach (int index in remaining)
                {
                    total += Math.Max(0f, weights[index]); // 전체 가중치 값의 합
                }

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