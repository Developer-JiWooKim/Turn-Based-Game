using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Localization
{
    /// <summary>지원 언어. 저장 형식은 <see cref="Localization.ToSaveValue"/> 참고.</summary>
    public enum LanguageCode
    {
        Ko,
        En
    }

    /// <summary>
    /// 화면에 나오는 문자열 전부를 담는 표. 언어당 열 하나이며 행 추가는 에디터에서 한다.
    ///
    /// 키 규약이 셋이다(섞인 것이 아니라 출처가 다르다) —
    /// <list type="bullet">
    /// <item><c>ui.…</c> — UI 문구. UXML/코드에 이 키를 직접 적는다.
    /// UXML을 훑어 치환하는 <c>BasePanelUI</c>가 이 접두어로 "번역할 문자열"과
    /// "코드가 채우는 값"을 구분하므로, 접두어를 빼면 조용히 번역되지 않는다.</item>
    /// <item><c>choice.&lt;카테고리&gt;.title/desc</c> — 로그라이크 선택지.
    /// 본문이 여러 줄이라 원문을 키로 쓰면 공백 하나에 깨지므로 카테고리 enum에서 키를 만든다.</item>
    /// <item>유닛 표시 이름(<c>UnitStatsSO.DisplayName</c>)은 <b>에셋에 적힌 원문 자체가 키</b>다.
    /// 한 줄짜리 짧은 이름이라 안전하고, 에셋을 건드리지 않고 번역을 얹을 수 있다.</item>
    /// </list>
    /// 어느 쪽이든 표에 행이 없으면 에셋의 원문이 그대로 나온다(<see cref="Loc.Get(string, string)"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Localization/String Table", fileName = "UiStringTable")]
    public sealed class LocalizationTableSO : ScriptableObject
    {
        /// <summary>문자열 1종의 언어별 값.</summary>
        [Serializable]
        public struct Entry
        {
            public string Key;
            [TextArea] public string Ko;
            [TextArea] public string En;

            /// <summary>요청한 언어의 값(비어 있으면 빈 문자열).</summary>
            public readonly string Value(LanguageCode language) => language == LanguageCode.En ? En : Ko;
        }

        [SerializeField] private Entry[] _entries;

        /// <summary>키 조회용 캐시. 항목 수가 늘어도 조회 비용이 그대로다.</summary>
        private Dictionary<string, Entry> _lookup;

        /// <summary>
        /// 키에 해당하는 문자열을 찾는다.
        /// 요청한 언어가 비어 있으면 한국어로 물러선다 — 번역이 아직 없는 항목이
        /// 화면에서 빈칸으로 사라지는 것보다 원문이 보이는 편이 낫기 때문이다.
        /// </summary>
        public bool TryGet(string key, LanguageCode language, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!Lookup.TryGetValue(key, out Entry entry))
            {
                return false;
            }

            value = entry.Value(language);
            if (string.IsNullOrEmpty(value))
            {
                value = entry.Ko;
            }

            return !string.IsNullOrEmpty(value);
        }

        private Dictionary<string, Entry> Lookup
        {
            get
            {
                if (_lookup != null)
                {
                    return _lookup;
                }

                _lookup = new Dictionary<string, Entry>(_entries != null ? _entries.Length : 0, StringComparer.Ordinal);
                if (_entries == null)
                {
                    return _lookup;
                }

                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry entry = _entries[i];
                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        continue;
                    }

                    // 같은 키가 둘이면 나중 것이 앞의 것을 조용히 가린다 — 어느 쪽이 쓰이는지 알 수 없으므로 보고한다.
                    if (!_lookup.TryAdd(entry.Key, entry))
                    {
                        Debug.LogWarning($"[{nameof(LocalizationTableSO)}] 키 '{entry.Key}'가 중복입니다 — 첫 번째 행을 씁니다.", this);
                    }
                }

                return _lookup;
            }
        }

#if UNITY_EDITOR
        /// <summary>에디터에서 행을 고치면 캐시를 버려 다음 조회 때 다시 만든다.</summary>
        private void OnValidate() => _lookup = null;
#endif
    }
}
