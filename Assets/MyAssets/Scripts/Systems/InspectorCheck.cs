using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// 인스펙터 연결 누락을 보고하는 공용 헬퍼.
    ///
    /// 각 컴포넌트의 <c>ValidateReferences()</c>가 이걸 호출하고, 반환값을 누적해
    /// "빠진 것을 한 번에 모두" 알린 뒤 한 번만 판정한다:
    /// <code>
    /// bool hasError = false;
    /// hasError |= InspectorCheck.LogIfMissing(_registry, nameof(_registry), this);
    /// hasError |= InspectorCheck.LogIfMissing(_presenter, nameof(_presenter), this);
    /// return !hasError;
    /// </code>
    ///
    /// 로그 접두어는 <paramref name="context"/>의 실제 타입에서 뽑는다 — 클래스명을 문자열로 적으면
    /// 복사·붙여넣기 과정에서 다른 클래스 이름이 남아 엉뚱한 파일을 뒤지게 된다(실제로 겪은 실수).
    /// </summary>
    public static class InspectorCheck
    {
        /// <summary>참조가 비어 있으면 로그를 남긴다.</summary>
        /// <param name="target">검사할 참조. <see cref="Object"/>로 받아야 Unity의 == 오버로드가 유지된다(파괴된 객체도 null로 판정).</param>
        /// <param name="name">필드 이름. 호출부에서 <c>nameof</c>로 넘긴다.</param>
        /// <param name="context">보고 주체. 로그 접두어와 콘솔 클릭 시 선택될 오브젝트로 쓰인다.</param>
        /// <param name="consequence">"— 페이드 없이 씬을 전환합니다"처럼 누락 시 무슨 일이 벌어지는지(선택).</param>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfMissing(Object target, string name, Object context, string consequence = null)
        {
            if (target != null) return false;

            Log($"{name}가 연결되지 않았습니다", context, consequence);
            return true;
        }

        /// <summary>배열이 null이거나 비어 있으면 로그를 남긴다(슬롯 목록 등).</summary>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfEmpty<T>(T[] array, string name, Object context, string consequence = null)
        {
            if (array != null && array.Length > 0) return false;

            Log($"{name}가 비어 있습니다", context, consequence);
            return true;
        }

        private static void Log(string what, Object context, string consequence)
        {
            string owner = context != null ? context.GetType().Name : nameof(InspectorCheck);
            string tail = string.IsNullOrEmpty(consequence) ? string.Empty : $" — {consequence}";

            Debug.LogError($"[{owner}] {what}{tail}(인스펙터 확인).", context);
        }
    }
}
