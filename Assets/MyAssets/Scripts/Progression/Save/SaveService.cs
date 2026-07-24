using System;
using System.IO;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Progression.Save
{
    /// <summary>영구 데이터를 JSON 파일로 읽고 쓰는 창구</summary>
    public static class SaveService
    {
        private const string FileName = "save.json";

        /// <summary>에디터에서 세이브를 모아둘 프로젝트 루트 하위 폴더명.</summary>
        private const string EditorFolderName = "SaveData";

        private static SaveData _current;

        /// <summary>현재 세이브 데이터. 최초 접근 시 파일에서 1회 로드한 뒤 캐싱한다.</summary>
        public static SaveData Current => _current ??= Load();

        /// <summary>
        /// 세이브를 둘 폴더
        /// 에디터에서는 확인·삭제가 쉽도록 프로젝트 루트의 <c>SaveData/</c>를 쓰고,
        /// 빌드에서는 <see cref="Application.persistentDataPath"/>(플랫폼별 사용자 데이터 폴더)를 쓴다.
        /// 빌드된 게임은 프로젝트 폴더와 무관하고 설치 경로가 읽기 전용일 수 있어 루트 방식을 쓸 수 없다.
        /// </summary>
        public static string SaveFolder
        {
#if UNITY_EDITOR
            // Application.dataPath = <프로젝트 루트>/Assets 이므로, 한 단계 위가 프로젝트 루트
            // Assets 바깥에 두어야 Unity가 에셋으로 임포트하지 않는다(.meta 생성 방지).
            get => Path.Combine(Directory.GetParent(Application.dataPath).FullName, EditorFolderName);
#else
            get => Application.persistentDataPath;
#endif
        }

        /// <summary>세이브 파일 전체 경로(디버깅·수동 확인용)</summary>
        public static string FilePath => Path.Combine(SaveFolder, FileName);

        /// <summary>
        /// 파일에서 세이브를 읽어 캐시를 교체
        /// 파일이 없거나 깨져 있어도 게임이 멈추지 않도록 기본값으로 복구한다.
        /// </summary>
        public static SaveData Load()
        {
            SaveData data = null;
            try
            {
                if (File.Exists(FilePath))
                    data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception ex)
            {
                // 세이브가 깨진 경우 — 진행 기록은 잃지만 게임은 계속 실행
                Debug.LogWarning($"[SaveService] 세이브를 읽지 못해 기본값으로 시작합니다: {ex.Message}");
            }

            _current = Normalize(data);
            return _current;
        }

        /// <summary>현재 세이브 데이터를 파일에 기록</summary>
        public static void Save()
        {
            try
            {
                // persistentDataPath와 달리 에디터용 SaveData 폴더는 자동으로 생기지 않는다.
                Directory.CreateDirectory(SaveFolder);
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] 세이브 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 런 종료 시 도달 스테이지를 기록
        /// 기록을 새로 세웠을 때만 파일에 쓴다.
        /// </summary>
        /// <returns>신기록이면 true</returns>
        public static bool RecordStage(int reachedStage)
        {
            if (reachedStage <= Current.BestStage)
                return false;

            Current.BestStage = reachedStage;
            Save();
            return true;
        }

        /// <summary>세이브를 삭제하고 기본값으로 되돌림(개발·테스트용)</summary>
        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] 세이브 삭제 실패: {ex.Message}");
            }

            _current = new SaveData();
        }

        /// <summary>
        /// 로드 결과를 안전한 상태로 맞춘다.
        /// JSON에 필드가 없으면 초기값이 유지되지만, 값이 명시적으로 null이면 null로 들어올 수 있다.
        /// </summary>
        private static SaveData Normalize(SaveData data)
        {
            data ??= new();
            data.CategoryPoints ??= new();
            data.Options ??= new();

            // 포맷이 바뀌면 여기서 data.Version을 보고 이전 버전 데이터를 변환한다.
            data.Version = SaveData.CurrentVersion;
            return data;
        }
    }
}
