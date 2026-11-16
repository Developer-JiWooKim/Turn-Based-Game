using System;
using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;

namespace Assets.MyAssets.Scripts.Progression.Save
{
    /// <summary>런을 넘어 영구 보존되는 데이터.</summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>현재 세이브 포맷 버전.</summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        /// <summary>최고 도달 스테이지.</summary>
        public int BestStage;

        /// <summary>로그라이크 선택지 카테고리별로 투자한 영구 포인트(성향 커스터마이징).</summary>
        public List<CategoryPoint> CategoryPoints = new();

        /// <summary>옵션 메뉴 설정.</summary>
        public OptionsData Options = new();

        /// <summary> 진행 중이던 런의 체크포인트.</summary>
        public RunSnapshot Run = new();

        /// <summary>지금까지 획득한 영구 포인트 총량.</summary>
        public int GetEarnedPoints(int stagesPerPoint) => stagesPerPoint <= 0 ? 0 : BestStage / stagesPerPoint;

        /// <summary>카테고리에 투자되어 이미 쓰인 포인트 합계.</summary>
        public int GetSpentPoints()
        {
            int sum = 0;

            foreach (CategoryPoint entry in CategoryPoints)
            {
                sum += entry.Points;
            }

            return sum;
        }

        /// <summary>아직 투자하지 않고 남은 포인트. (획득 총량 - 투자 합계)</summary>
        public int GetAvailablePoints(int stagesPerPoint) => GetEarnedPoints(stagesPerPoint) - GetSpentPoints();

        /// <summary>해당 카테고리에 투자된 포인트. (투자한 적 없으면 0)</summary>
        public int GetPoints(RoguelikeCategory category) => Find(category)?.Points ?? 0;

        /// <summary>
        /// 카테고리 투자량을 <paramref name="delta"/>만큼 조정한다.
        /// 남은 포인트가 없으면 늘릴 수 없고, 투자량이 0이면 더 뺄 수 없다.
        /// </summary>
        /// <returns>실제로 값이 바뀌었으면 true</returns>
        public bool TryAdjustPoints(RoguelikeCategory category, int delta, int stagesPerPoint)
        {
            int current = GetPoints(category);

            if (delta > 0 && GetAvailablePoints(stagesPerPoint) <= 0)
            {
                return false;
            }

            if (delta < 0 && current <= 0)
            {
                return false;
            }

            SetPoints(category, current + delta);
            return true;
        }

        /// <summary>카테고리 투자 포인트를 설정. (0이면 항목 자체를 제거해 세이브를 깔끔히 유지)</summary>
        public void SetPoints(RoguelikeCategory category, int points)
        {
            CategoryPoint entry = Find(category);
            if (points <= 0)
            {
                if (entry is not null)
                {
                    CategoryPoints.Remove(entry);
                }

                return;
            }

            if (entry is not null)
            {
                entry.Points = points;
            }
            else
            {
                CategoryPoints.Add(new CategoryPoint { Category = category, Points = points });
            }
        }

        /// <summary>투자 내역을 전부 리셋.</summary>
        public void ResetPoints() => CategoryPoints.Clear();

        private CategoryPoint Find(RoguelikeCategory category) => CategoryPoints.Find(e => e.Category == category);
    }

    /// <summary>카테고리 1종에 투자된 영구 포인트.</summary>
    [Serializable]
    public sealed class CategoryPoint
    {
        public RoguelikeCategory Category;
        public int Points;
    }

    /// <summary>옵션 메뉴에서 조정하는 설정값.</summary>
    [Serializable]
    public sealed class OptionsData
    {
        /// <summary>마스터 볼륨(0~1).</summary>
        public float MasterVolume = 1f;

        /// <summary>BGM 볼륨(0~1).</summary>
        public float BgmVolume = 1f;

        /// <summary>효과음 볼륨(0~1).</summary>
        public float SfxVolume = 1f;

        /// <summary>
        /// 화면 모드 프리셋 인덱스. (<c>GameSettings.DisplayPresets</c>, -1이면 현재 해상도 유지)
        /// </summary>
        public int ResolutionIndex = -1;

        /// <summary>언어 코드(ko / en). (Default = ko)</summary>
        public string Language = "ko";

        public string InputBindingOverrides = string.Empty;
    }
}
