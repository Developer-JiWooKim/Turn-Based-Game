using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// NullCheck 공용 헬퍼.
    ///
    /// 값이 <b>어디서 오는가</b>에 따라 셋으로 나뉜다 — 로그를 읽는 사람이 뒤져야 할 곳이 달라지기 때문이다.
    /// <list type="bullet">
    /// <item><see cref="LogIfMissing"/> — 인스펙터에서 연결하는 참조. "(인스펙터 확인)"이 붙는다.</item>
    /// <item><see cref="LogIfNullObject"/> — 런타임에 얻은 <see cref="Object"/>(GetComponent·Instantiate·Find 결과).
    ///       뒤질 곳은 인스펙터가 아니라 프리팹 계층이나 생성 코드다.</item>
    /// <item><see cref="LogIfNull(object, string, object, string)"/> — 순수 C# 참조(생성자 인자, UXML 조회 결과).</item>
    /// </list>
    ///
    /// 앞의 둘은 Unity의 == 오버로드를 유지하므로 <b>이미 파괴된 객체</b>도 잡아내고, 그 경우 "연결되지 않았다"가 아니라
    /// "파괴되었다"고 알린다 — 원인이 인스펙터가 아니라 수명 관리에 있다는 뜻이라 찾는 자리가 완전히 다르다.
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
        // ── 인스펙터 참조 ──

        /// <summary>인스펙터 참조가 비어 있으면 로그를 남긴다(메시지에 "인스펙터 확인"이 붙는다).</summary>
        /// <param name="target">검사할 참조. <see cref="Object"/>로 받아야 Unity의 == 오버로드가 유지된다(파괴된 객체도 null로 판정).</param>
        /// <param name="name">필드 이름. 호출부에서 <c>nameof</c>로 넘긴다.</param>
        /// <param name="owner">보고 주체(보통 <c>this</c>). 로그 접두어에 쓰이고, Unity 오브젝트면 콘솔 클릭 시 선택된다.</param>
        /// <param name="consequence">"페이드 없이 씬을 전환합니다"처럼 누락 시 무슨 일이 벌어지는지(선택).</param>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfMissing(Object target, string name, object owner, string consequence = null)
            => LogIfUnityNull(target, name, owner, consequence, inspector: true);

        // ── 런타임에 얻은 UnityEngine.Object ──

        /// <summary>
        /// 런타임에 얻은 <see cref="Object"/>가 비어 있으면 로그를 남긴다.
        /// <c>GetComponentInChildren</c>·<c>Instantiate</c>·<c>Find</c> 결과처럼 <b>인스펙터에서 채우는 값이 아닌</b> 참조에 쓴다
        /// — "(인스펙터 확인)"이 붙지 않으므로 빈 슬롯을 찾아 헤매지 않게 된다.
        /// </summary>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfNullObject(Object target, string name, object owner, string consequence = null)
            => LogIfUnityNull(target, name, owner, consequence, inspector: false);

        // ── 목록 ──

        /// <summary>목록이 null이거나 비어 있으면 로그를 남긴다(인스펙터 슬롯 목록, 선택지 풀 등).</summary>
        /// <param name="inspector">
        /// 인스펙터에서 채우는 값인지. UXML 조회 결과처럼 인스펙터와 무관한 목록은 false로 넘긴다
        /// — "(인스펙터 확인)"이 붙으면 엉뚱한 곳을 뒤지게 된다.
        /// </param>
        /// <returns>비어 있으면 true(= 문제 있음).</returns>
        public static bool LogIfEmpty<T>(IReadOnlyList<T> items, string name, object owner,
                                         string consequence = null, bool inspector = true)
        {
            if (items != null && items.Count > 0)
            {
                return false;
            }

            Log($"{name}가 비어 있습니다", owner, consequence, inspector);

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
        [Obsolete("UnityEngine.Object는 LogIfMissing(인스펙터 참조) 또는 LogIfNullObject(런타임 조회 결과)를 쓰세요 " +
                  "— LogIfNull은 == 오버로드를 우회해 파괴된 객체를 놓칩니다.", error: true)]
        public static bool LogIfNull(Object target, string name, object owner, string consequence = null)
            => throw new NotSupportedException();

        /// <summary>
        /// Unity의 == 오버로드로 판정하고, "빈 참조"와 "파괴된 참조"를 갈라 보고한다.
        /// 후자는 대입 자체는 됐던 것이라 인스펙터를 아무리 봐도 원인이 없으므로 힌트를 붙이지 않는다.
        /// </summary>
        private static bool LogIfUnityNull(Object target, string name, object owner, string consequence, bool inspector)
        {
            if (target != null)
            {
                return false;
            }

            // Unity의 ==만 null로 보고 실제 참조는 살아 있는 경우 = 네이티브 객체가 파괴됐거나 참조가 깨진 것.
            if (!ReferenceEquals(target, null))
            {
                Log($"{name}가 이미 파괴되었거나 참조가 깨졌습니다", owner, consequence, inspector: false);

                return true;
            }

            Log(inspector ? $"{name}가 연결되지 않았습니다" : $"{name}가 null입니다", owner, consequence, inspector);

            return true;
        }

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
