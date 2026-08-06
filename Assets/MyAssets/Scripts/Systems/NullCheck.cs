using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// "비어 있으면 로그를 남긴다"를 한 양식으로 모은 공용 헬퍼.
    /// 프로젝트의 모든 null 보고가 같은 형태(<c>[클래스명] 무엇이 어떻다 — 결과</c>)로 나오게 한다.
    ///
    /// 반환값을 누적해 "빠진 것을 한 번에 모두" 알린 뒤 한 번만 판정하는 것이 기본형이다:
    /// <code>
    /// bool hasError = false;
    /// hasError |= NullCheck.LogIfMissing(_registry, nameof(_registry), this);
    /// hasError |= NullCheck.LogIfMissing(_presenter, nameof(_presenter), this);
    /// return !hasError;
    /// </code>
    ///
    /// 로그 접두어는 <paramref name="owner"/>의 실제 타입에서 뽑는다.
    /// </summary>
    public static class NullCheck
    {
        /// <summary>인스펙터 참조가 비어 있으면 로그를 남긴다(메시지에 "인스펙터 확인"이 붙는다).</summary>
        /// <param name="target">검사할 참조. <see cref="Object"/>로 받아야 Unity의 == 오버로드가 유지된다(파괴된 객체도 null로 판정).</param>
        /// <param name="name">필드 이름. 호출부에서 <c>nameof</c>로 넘긴다.</param>
        /// <param name="owner">보고 주체(보통 <c>this</c>). 로그 접두어에 쓰이고, Unity 오브젝트면 콘솔 클릭 시 선택된다.</param>
        /// <param name="consequence">"페이드 없이 씬을 전환합니다"처럼 누락 시 무슨 일이 벌어지는지(선택).</param>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfMissing(Object target, string name, object owner, string consequence = null)
        {
            if (target != null)
            {
                return false;
            }

            Log($"{name}가 연결되지 않았습니다", owner, consequence, inspector: true);

            return true;
        }

        /// <summary>인스펙터 배열이 null이거나 비어 있으면 로그를 남긴다(슬롯 목록, 선택지 풀 등).</summary>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfEmpty<T>(T[] array, string name, object owner, string consequence = null)
        {
            if (array != null && array.Length > 0)
            {
                return false;
            }

            Log($"{name}가 비어 있습니다", owner, consequence, inspector: true);

            return true;
        }

        // ── 일반 참조 (생성자 인자, UXML 조회 결과 등 — 인스펙터와 무관) ──

        /// <summary>
        /// 순수 C# 참조가 null이면 로그를 남긴다. 생성자로 받은 협력 객체나
        /// <c>Q&lt;Slider&gt;()</c> 같은 UXML 조회 결과처럼 인스펙터와 무관한 값에 쓴다.
        /// </summary>
        /// <returns>null이면 true(= 문제 있음).</returns>
        public static bool LogIfNull(object target, string name, object owner, string consequence = null)
        {
            if (target != null)
            {
                return false;
            }

            Log($"{name}가 null입니다", owner, consequence, inspector: false);

            return true;
        }

        /// <summary>
        /// <see cref="Object"/>를 <see cref="LogIfNull(object, string, object, string)"/>로 넘기는 실수를 컴파일 에러로 막는다.
        /// 일반 <c>object</c>로 받으면 Unity의 == 오버로드가 사라져 <b>파괴된 객체가 null이 아닌 것으로 통과</b>한다.
        /// </summary>
        [Obsolete("UnityEngine.Object는 LogIfMissing을 쓰세요 — LogIfNull은 == 오버로드를 우회해 파괴된 객체를 놓칩니다.", error: true)]
        public static bool LogIfNull(Object target, string name, object owner, string consequence = null)
            => throw new NotSupportedException();

        private static void Log(string what, object owner, string consequence, bool inspector)
        {
            string ownerName = owner != null ? owner.GetType().Name : nameof(NullCheck);
            string tail = string.IsNullOrEmpty(consequence) ? string.Empty : $" — {consequence}";
            string hint = inspector ? "(인스펙터 확인)" : string.Empty;

            // owner가 Unity 오브젝트면 콘솔에서 클릭했을 때 해당 오브젝트가 선택된다(순수 C# 클래스면 null).
            Debug.LogError($"[{ownerName}] {what}{tail}{hint}.", owner as Object);
        }
    }
}
