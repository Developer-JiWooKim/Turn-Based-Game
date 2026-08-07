using System.Collections.Generic;
using System.Text;
using Assets.MyAssets.Scripts.Battle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.MyAssets.Scripts.Battle.View
{
    public sealed class UnitHealthBar : MonoBehaviour
    {
        [Header("HealthBar HUD(Canvas)")]
        [Tooltip("설정된 채력 게이지(Image Type = Filled)")]
        [SerializeField] private Image _fill;

        [Tooltip("체력을 숫자로 표기할 텍스트")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("걸려 있는 상태이상을 표기할 텍스트. 비워두면 표시하지 않는다.")]
        [SerializeField] private TMP_Text _statusText;

        [Tooltip("화면 배치 인덱스를 표기할 텍스트([A1] 등). 비워두면 표시하지 않는다.")]
        [SerializeField] private TMP_Text _indexText;

        [Tooltip("카메라를 향하게 회전시킬 루트")]
        [SerializeField] private Transform _billboardRoot;

        private readonly StringBuilder _statusBuilder = new(); // 상태이상 줄 조합용 버퍼

        /// <summary>
        /// 스폰 시 적용된 로그라이크 디버프 표기(예: "HP -30%"). 
        /// 이건 지속 턴이 있는 상태이상이 아니라 스폰 시점에 스탯에 녹아든 값이라, 상태이상 목록과 별도로 들고 있다가 함께 표시한다.
        /// </summary>
        private string _spawnDebuff;

        /// <summary>
        /// 스폰 디버프만 따로 갱신될 때 기존 상태이상 표기를 잃지 않도록 마지막 목록을 기억해둔다.
        /// </summary>
        private IReadOnlyList<ActiveStatus> _lastStatuses;

        /// <summary>체력바 전체를 켜고 끈다(사망 시 숨김 → 스폰 시 복구). 체력바는 유닛 루트의 자식이라 모델에는 영향 없다.</summary>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>
        /// 화면 배치 인덱스를 표기한다(예: "A1" → <c>[A1]</c>). 진영은 A/E 접두어로 구분되며
        /// 문자열은 <see cref="UnitViewRegistry"/>가 만든다 — 상단 턴 순서 칩과 같은 값을 쓰기 위함.
        /// 풀에서 재사용된 인스턴스에 이전 번호가 남지 않도록 스폰마다 다시 지정된다.
        /// </summary>
        public void SetSlotLabel(string label)
        {
            if (_indexText == null)
            {
                return;
            }

            bool has = !string.IsNullOrEmpty(label);
            _indexText.gameObject.SetActive(has);

            if (has)
            {
                _indexText.text = $"{label}";
            }
        }

        /// <summary>이번 전투 내내 유지되는 스폰 디버프 표기를 지정한다(null이면 없음).</summary>
        public void SetSpawnDebuff(string label)
        {
            _spawnDebuff = string.IsNullOrEmpty(label) ? null : label;
            SetStatuses(_lastStatuses);
        }

        public void Set(int current, int max)
        {
            if (_fill != null)
            {
                _fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            }

            if (_hpText != null)
            {
                _hpText.SetText("{0} / {1}", current, max);
            }
        }

        /// <summary>스폰 디버프 + 걸려 있는 상태이상을 줄마다 표기한다(둘 다 없으면 숨김).</summary>
        public void SetStatuses(IReadOnlyList<ActiveStatus> statuses)
        {
            _lastStatuses = statuses;

            if (_statusText == null)
            {
                return;
            }

            bool hasStatuses = statuses != null && statuses.Count > 0;
            if (!hasStatuses && _spawnDebuff == null)
            {
                _statusText.gameObject.SetActive(false);
                return;
            }

            _statusBuilder.Clear();

            // 스폰 디버프를 먼저 — 전투 내내 유지되므로 위쪽에 고정해두면 아래 줄만 턴마다 바뀐다.
            if (_spawnDebuff != null)
            {
                _statusBuilder.Append(_spawnDebuff);
            }

            if (hasStatuses)
            {
                // 상태이상은 개수와 무관하게 "한 줄"에 나열한다.
                // 과거엔 항목마다 줄을 나눴는데(ASCII 약어 시절엔 폭이 넓어 뒤가 잘렸다) 표기가 아이콘으로 바뀌어
                // 항목 하나가 훨씬 좁아졌고, 무엇보다 줄이 늘면 글자 블록이 아래로 자라 체력바를 덮는 문제가 있었다.
                // 종류가 5개뿐이라 전부 걸려도 가로 폭에 들어간다(넘치면 TMP가 알아서 다음 줄로 접는다).
                if (_statusBuilder.Length > 0)
                {
                    _statusBuilder.Append('\n');
                }

                for (int i = 0; i < statuses.Count; i++)
                {
                    if (i > 0)
                    {
                        _statusBuilder.Append(EntrySeparator);
                    }

                    _statusBuilder.Append(Label(statuses[i].Kind)).Append(' ').Append(statuses[i].RemainingTurns);
                }
            }

            // 비활성 상태에서 SetText하면 TMP가 메시를 다시 만들지 않는 경우가 있어 활성화를 먼저 한다.
            _statusText.gameObject.SetActive(true);
            _statusText.SetText(_statusBuilder);
        }

        /// <summary>아이콘 크기(폰트 크기 대비)와 기준선 보정. 작아 보이거나 텍스트와 높이가 안 맞으면 이 둘만 조정.</summary>
        private const string IconSize = "160%";
        private const string IconVOffset = "0.12em";

        /// <summary>
        /// 같은 줄에 여러 항목을 나열할 때의 간격.
        /// 상태이상 목록과 스폰 디버프 라벨(<see cref="MonsterSpawner.DescribeDebuff"/>)이 같이 쓴다 —
        /// <see cref="IconTag"/>와 같은 이유로, 두 표기의 간격이 어긋나지 않게 한곳에 둔다.
        /// </summary>
        public const string EntrySeparator = "   ";

        /// <summary>
        /// TMP 인라인 스프라이트 태그를 만든다 — 이름은 상태이상 표기(`_statusText`)에 지정된
        /// Sprite Asset(Debuff 아이콘 + Fallback 체인)의 스프라이트 이름과 일치해야 한다.
        /// 스폰 디버프 라벨(<see cref="MonsterSpawner"/>)도 이 메서드를 써서 크기·기준선이 어긋나지 않는다.
        ///
        /// ⚠️ 크기는 반드시 바깥의 <c>&lt;size&gt;</c>로 준다 — <c>&lt;sprite&gt;</c>가 인식하는 속성은
        /// name/index/anim/color/tint뿐이라 <c>scale=</c> 같은 걸 넣으면 TMP가 태그 전체를 무효로 보고
        /// <b>태그 문자열을 그대로 화면에 출력</b>한다(이름·Fallback이 멀쩡한데 전부 텍스트로 나왔던 실제 버그).
        /// 이름을 못 찾을 때도 같은 증상이 나므로, 글자로 보이면 이 둘부터 의심할 것.
        /// </summary>
        public static string IconTag(string spriteName) =>
            $"<size={IconSize}><voffset={IconVOffset}><sprite name=\"{spriteName}\"></voffset></size>";

        /// <summary>상태이상 표기.</summary>
        private static string Label(StatusKind kind) => kind switch
        {
            StatusKind.Stun => IconTag("Debuff_Stun"),
            StatusKind.Poison => IconTag("Debuff_Poison"),
            StatusKind.AtkDown => IconTag("Debuff_AttackDown"),
            StatusKind.DefDown => IconTag("Debuff_DefenseDown"),
            StatusKind.SpdDown => IconTag("Debuff_SpeedDown"),
            _ => "?"
        };

        private void LateUpdate()
        {
            if (_billboardRoot == null)
            {
                return;
            }

            // Camera.main은 태그 검색이라 유닛마다 매 프레임 부르면 비용이 쌓인다 — 캐시를 통해 조회한다.
            Transform camera = MainCameraCache.CurrentTransform;
            if (camera == null)
            {
                return;
            }

            _billboardRoot.forward = camera.forward;
        }
    }
}
