using System;

namespace Assets.MyAssets.Scripts.Localization
{
    /// <summary>
    /// 문자열 조회의 단일 진입점. <c>SaveService</c>·<c>GameSettings</c>와 같은 static이며,
    /// 이유도 같다 — 표를 주입할 <c>GameManager</c>가 없는 상황(BattleScene 단독 실행)에서도
    /// 호출이 안전해야 하기 때문이다. 표가 없으면 키(= SO 표시 이름이면 원문)를 그대로 돌려주고 조용히 넘어간다.
    ///
    /// 언어 값의 주인은 세이브(<c>OptionsData.Language</c>)이고, 여기는 "지금 어느 언어로 그리는가"만 들고 있다.
    /// 둘을 잇는 곳은 <c>GameSettings.Language</c> 한 곳뿐이다.
    ///
    /// ⚠️ 이름이 <c>Localization</c>이 아니라 <c>Loc</c>인 이유: 이 파일의 네임스페이스가
    /// <c>…Scripts.Localization</c>이라, 같은 이름의 타입을 두면 <c>…Scripts</c> 아래의 모든 코드에서
    /// 단순 이름 <c>Localization</c>이 <b>타입이 아니라 네임스페이스로</b> 해석돼 컴파일 에러가 난다
    /// (네임스페이스 멤버가 using 임포트보다 먼저 잡힌다). 호출부가 40곳 가까이라 짧은 이름이 읽기에도 낫다.
    /// </summary>
    public static class Loc
    {
        /// <summary>
        /// UXML/코드에 직접 적는 UI 문구 키의 접두어.
        /// <c>BasePanelUI</c>가 UXML을 훑을 때 이 접두어로 "번역할 문자열"과
        /// "코드가 나중에 채우는 값"(캐릭터 이름·스탯 숫자 등)을 구분한다.
        /// </summary>
        public const string UiKeyPrefix = "ui.";

        /// <summary>세이브에 기록되는 언어 문자열(<c>OptionsData.Language</c>의 기본값과 같아야 한다).</summary>
        private const string KoreanSaveValue = "ko";
        private const string EnglishSaveValue = "en";

        /// <summary>언어(또는 표)가 바뀌었을 때 화면을 다시 그리라는 신호.</summary>
        public static event Action LanguageChanged;

        private static LocalizationTableSO _table;
        private static LanguageCode _language = LanguageCode.Ko;

        public static LanguageCode Language
        {
            get => _language;
            set
            {
                if (_language == value)
                {
                    return;
                }

                _language = value;
                LanguageChanged?.Invoke();
            }
        }

        /// <summary>문자열 표를 주입한다(<c>GameManager.Awake</c>). 늦게 들어와도 화면이 다시 그려지도록 신호를 낸다.</summary>
        public static void SetTable(LocalizationTableSO table)
        {
            if (_table == table)
            {
                return;
            }

            _table = table;
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// 키에 해당하는 현재 언어 문자열. 표가 없거나 행이 없으면 <paramref name="key"/>를 그대로 돌려준다 —
        /// SO 표시 이름은 원문이 곧 키라 이 폴백이 곧 "번역 전 원문"이 되고,
        /// <c>ui.</c> 키는 화면에 키가 그대로 보여 빠진 행을 바로 알아챌 수 있다.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_table != null && _table.TryGet(key, _language, out string value))
            {
                return value;
            }

            return key;
        }

        /// <summary>
        /// 표에 행이 없을 때 <paramref name="fallback"/>으로 물러서는 조회.
        /// 키를 에셋 밖에서 만드는 경우(예: 선택지 카테고리에서 파생)에 쓴다 —
        /// 행이 아직 없어도 화면에 <c>choice.…</c> 같은 키가 아니라 에셋에 적힌 원문이 나온다.
        /// </summary>
        public static string Get(string key, string fallback)
        {
            if (_table != null && _table.TryGet(key, _language, out string value))
            {
                return value;
            }

            return fallback;
        }

        /// <summary>서식 문자열 조회 + 값 채우기. 번역문이 <c>{0}</c> 자리를 갖는 경우에 쓴다.</summary>
        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        /// <summary>UXML에서 읽은 문자열이 번역 대상 키인지(= <see cref="UiKeyPrefix"/>로 시작하는지).</summary>
        public static bool IsUiKey(string text) =>
            !string.IsNullOrEmpty(text) && text.StartsWith(UiKeyPrefix, StringComparison.Ordinal);

        /// <summary>세이브에 저장된 문자열을 언어로 해석한다(알 수 없는 값은 한국어).</summary>
        public static LanguageCode Parse(string saved) =>
            string.Equals(saved, EnglishSaveValue, StringComparison.OrdinalIgnoreCase) ? LanguageCode.En : LanguageCode.Ko;

        /// <summary>세이브에 기록할 문자열. enum을 그대로 직렬화하지 않는 이유는 기존 세이브(<c>"ko"</c>)와 호환하기 위함이다.</summary>
        public static string ToSaveValue(LanguageCode language) =>
            language == LanguageCode.En ? EnglishSaveValue : KoreanSaveValue;
    }
}
