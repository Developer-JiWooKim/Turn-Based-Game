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

            if (_statusText == null) return;

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
                for (int i = 0; i < statuses.Count; i++)
                {
                    // 한 줄에 나열하면 폭이 좁은 월드스페이스 텍스트에서 뒤쪽이 잘려 안 보인다 — 항목마다 줄을 나눈다.
                    if (_statusBuilder.Length > 0)
                    {
                        _statusBuilder.Append('\n');
                    }

                    _statusBuilder.Append(Label(statuses[i].Kind)).Append(' ').Append(statuses[i].RemainingTurns);
                }
            }

            // 비활성 상태에서 SetText하면 TMP가 메시를 다시 만들지 않는 경우가 있어 활성화를 먼저 한다.
            _statusText.gameObject.SetActive(true);
            _statusText.SetText(_statusBuilder);
        }

        /// <summary>상태이상/스폰 디버프 아이콘의 공용 크기·기준선 보정. 체감상 작거나 텍스트보다 낮춰 보이면 이 둘만 조정.</summary>
        private const string IconScale = "1.6";
        private const string IconVOffset = "0.12em";

        /// <summary>
        /// TMP 인라인 스프라이트 태그를 만든다 — 이름은 상태이상 표기(`_statusText`)에 지정된
        /// Sprite Asset(Debuff 아이콘 + Fallback 체인)의 스프라이트 이름과 일치해야 한다.
        /// Sprite Asset이 지정되지 않으면 TMP가 태그를 그대로 텍스트로 보여준다(폴백).
        /// 스폰 디버프 라벨(<see cref="MonsterSpawner"/>)도 같은 태그 형식을 써서 크기·기준선이 어긋나지 않는다.
        /// </summary>
        public static string IconTag(string spriteName) =>
            $"<voffset={IconVOffset}><sprite name=\"{spriteName}\" scale={IconScale}></voffset>";

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
            if (_billboardRoot == null) return;

            // Camera.main은 태그 검색이라 유닛마다 매 프레임 부르면 비용이 쌓인다 — 캐시를 통해 조회한다.
            Transform camera = MainCameraCache.CurrentTransform;
            if (camera == null) return;

            _billboardRoot.forward = camera.forward;
        }
    }
}
