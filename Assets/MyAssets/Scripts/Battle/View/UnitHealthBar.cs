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
        [Tooltip("설정된 채력 게이지(Image Type = Filled)")]
        [SerializeField] private Image _fill;

        [Tooltip("체력을 숫자로 표기할 텍스트")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("걸려 있는 상태이상을 표기할 텍스트. 비워두면 표시하지 않는다.")]
        [SerializeField] private TMP_Text _statusText;

        [Tooltip("카메라를 향하게 회전시킬 루트")]
        [SerializeField] private Transform _billboardRoot;

        // 상태이상 줄 조합용 버퍼. static으로 공유하면 여러 유닛이 같은 버퍼를 번갈아 덮어써
        // 표시가 섞일 수 있으므로 인스턴스마다 따로 둔다.
        private readonly StringBuilder _statusBuilder = new();

        /// <summary>
        /// 스폰 시 적용된 로그라이크 디버프 표기(예: "HP -30%"). 이건 지속 턴이 있는 상태이상이 아니라
        /// 스폰 시점에 스탯에 녹아든 값이라, 상태이상 목록과 별도로 들고 있다가 함께 표시한다.
        /// </summary>
        private string _spawnDebuff;

        // 스폰 디버프만 따로 갱신될 때 기존 상태이상 표기를 잃지 않도록 마지막 목록을 기억해둔다.
        private IReadOnlyList<ActiveStatus> _lastStatuses;

        /// <summary>체력바 전체를 켜고 끈다(사망 시 숨김 → 스폰 시 복구). 체력바는 유닛 루트의 자식이라 모델에는 영향 없다.</summary>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>이번 전투 내내 유지되는 스폰 디버프 표기를 지정한다(null이면 없음).</summary>
        public void SetSpawnDebuff(string label)
        {
            _spawnDebuff = string.IsNullOrEmpty(label) ? null : label;
            SetStatuses(_lastStatuses);
        }

        public void Set(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

            if (_hpText != null)
                _hpText.SetText("{0} / {1}", current, max);
        }

        /// <summary>스폰 디버프 + 걸려 있는 상태이상을 줄마다 표기한다(둘 다 없으면 숨김).</summary>
        public void SetStatuses(IReadOnlyList<ActiveStatus> statuses)
        {
            _lastStatuses = statuses;

            if (_statusText == null)
                return;

            bool hasStatuses = statuses != null && statuses.Count > 0;
            if (!hasStatuses && _spawnDebuff == null)
            {
                _statusText.gameObject.SetActive(false);
                return;
            }

            _statusBuilder.Clear();

            // 스폰 디버프를 먼저 — 전투 내내 유지되므로 위쪽에 고정해두면 아래 줄만 턴마다 바뀐다.
            if (_spawnDebuff != null)
                _statusBuilder.Append(_spawnDebuff);

            if (hasStatuses)
            {
                for (int i = 0; i < statuses.Count; i++)
                {
                    // 한 줄에 나열하면 폭이 좁은 월드스페이스 텍스트에서 뒤쪽이 잘려 안 보인다 — 항목마다 줄을 나눈다.
                    if (_statusBuilder.Length > 0) _statusBuilder.Append('\n');
                    _statusBuilder.Append(Label(statuses[i].Kind)).Append(' ').Append(statuses[i].RemainingTurns);
                }
            }

            // 비활성 상태에서 SetText하면 TMP가 메시를 다시 만들지 않는 경우가 있어 활성화를 먼저 한다.
            _statusText.gameObject.SetActive(true);
            _statusText.SetText(_statusBuilder);
        }

        /// <summary>
        /// 상태이상 약어. **ASCII만 쓴다** — 월드스페이스 체력바는 TMP(uGUI)라 폰트 에셋의 글리프 아틀라스에
        /// 있는 문자만 렌더링되고, 기본 폰트에는 한글·화살표(↓)가 없어 네모로 깨진다.
        /// 한글로 바꾸려면 한글 글리프를 포함한 TMP Font Asset을 만들어 _statusText에 지정할 것.
        /// </summary>
        private static string Label(StatusKind kind) => kind switch
        {
            StatusKind.Stun => "STUN",
            StatusKind.Poison => "PSN",
            StatusKind.AtkDown => "ATK-",
            StatusKind.DefDown => "DEF-",
            StatusKind.SpdDown => "SPD-",
            _ => "?"
        };

        private void LateUpdate()
        {
            if (_billboardRoot == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            _billboardRoot.forward = camera.transform.forward;
        }
    }
}
