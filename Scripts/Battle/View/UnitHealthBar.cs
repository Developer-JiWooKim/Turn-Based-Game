using System;
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

        [Header("게이지 연출")]
        [Tooltip("체력 변화가 게이지에 반영되기까지의 시간(초). 0이면 즉시 반영한다.\n" +
                 "짧으면 눈에 들어오기 전에 끝나 즉시 대입과 구분되지 않는다 — 0.25초에서 실제로 그랬다.")]
        [SerializeField] private float _drainDuration = 0.5f;

        private readonly StringBuilder _statusBuilder = new(); // 상태이상 줄 조합용 버퍼

        /// <summary>
        /// 진행 중인 게이지 연출의 세대 번호. 새 값이 들어오면 올려서 이전 연출이 스스로 물러나게 한다 —
        /// 풀에서 재사용될 때 이전 전투의 트윈이 살아남아 새 유닛의 게이지를 덮어쓰지 않도록.
        /// </summary>
        private int _drainVersion;

        /// <summary>지금 화면에 그려져 있는 값. 연출이 끝나기 전에 다음 피격이 들어오면 여기서 이어 간다.</summary>
        private float _shownRatio;
        private float _shownHp;

        /// <summary>마지막으로 <see cref="_hpText"/>에 써넣은 값. 같은 숫자를 매 프레임 다시 조판하지 않기 위함.</summary>
        private int _drawnHp = -1;
        private int _drawnMax = -1;

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

        /// <summary>
        /// 체력 표기를 갱신한다. 게이지는 <see cref="_drainDuration"/>에 걸쳐 목표까지 미끄러지고
        /// 숫자도 함께 따라간다 — 같은 값을 보여주는 두 표기가 따로 놀면 어긋난 것처럼 보인다.
        ///
        /// 히트 스톱을 걷어낸 뒤로 타격 순간에 눈이 가는 요소가 데미지 숫자뿐이라, 이 감속이 그 자리를 대신한다.
        /// 그래서 <see cref="_drainDuration"/>은 넉넉히 잡는다 — 짧으면 즉시 대입과 구분되지 않아 아무것도 아닌 게 된다.
        /// 시뮬레이션을 대기시키지 않는 순수 장식이라 Task를 반환하지 않는다(데미지 팝업과 같은 판단).
        /// </summary>
        public void Set(int current, int max)
        {
            _drainVersion++; // 진행 중이던 연출을 무효로 만든다 — 마지막에 들어온 값이 항상 이긴다

            // 숨겨져 있으면(공격 중 이동·사망) 연출이 보일 곳이 없으므로 값만 맞춰둔다.
            if (_drainDuration <= 0f || !isActiveAndEnabled)
            {
                Draw(Ratio(current, max), current, max);
                return;
            }

            _ = DrainRoutine(current, max, _drainVersion);
        }

        /// <summary>
        /// 연출 없이 즉시 반영한다(스폰·풀 재사용). 진행 중이던 연출도 함께 끊으므로
        /// 이전 유닛의 게이지가 새로 등장한 유닛 위에서 마저 흐르지 않는다.
        /// </summary>
        public void SetImmediate(int current, int max)
        {
            _drainVersion++;
            Draw(Ratio(current, max), current, max);
        }

        /// <summary>
        /// 게이지를 목표까지 미끄러뜨린다. <see cref="DamagePopup"/>·<see cref="CameraShake"/>와 같은
        /// 프레임 루프이며 <c>Time.timeScale</c>은 쓰지 않는다(프로젝트 규약).
        ///
        /// 이 연출이 눈에 들어오려면 <b>시간과 곡선이 함께</b> 맞아야 한다 — 짧거나 앞쪽에 몰린 곡선을 쓰면
        /// 즉시 대입과 구분되지 않는다(0.25초 + ease-out 조합에서 실제로 그랬다).
        /// </summary>
        private async Awaitable DrainRoutine(int targetHp, int max, int version)
        {
            float fromRatio = _shownRatio;
            float fromHp = _shownHp;
            float toRatio = Ratio(targetHp, max);
            float elapsed = 0f;

            try
            {
                while (elapsed < _drainDuration)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);

                    // 그 사이 새 값이 들어왔다면 이 연출은 낡았다 — 뒤늦게 그려서 최신 값을 덮지 않는다.
                    if (version != _drainVersion)
                    {
                        return;
                    }

                    elapsed += Time.deltaTime;

                    // 양 끝만 부드럽게 눌러 준다(ease-in-out). 데미지 팝업의 ease-out을 그대로 쓰면
                    // 변화량의 3/4이 앞 절반에 몰려 "확 줄고 멈춘" 것처럼 보인다 — 팝업은 위치 연출이라
                    // 튀어나왔다 잦아드는 게 맞지만, 게이지는 줄어드는 과정 자체를 읽혀야 한다.
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _drainDuration));

                    Draw(Mathf.Lerp(fromRatio, toRatio, t), Mathf.Lerp(fromHp, targetHp, t), max);
                }
            }
            catch (OperationCanceledException)
            {
                return; // 씬 전환 등으로 파괴됨
            }

            Draw(toRatio, targetHp, max); // 마지막 프레임 오차를 남기지 않는다
        }

        /// <summary>게이지와 숫자를 실제로 그린다. 연출 도중에는 <paramref name="hp"/>가 정수가 아니다.</summary>
        private void Draw(float ratio, float hp, int max)
        {
            _shownRatio = ratio;
            _shownHp = hp;

            if (_fill != null)
            {
                _fill.fillAmount = ratio;
            }

            if (_hpText == null)
            {
                return;
            }

            // 같은 숫자를 매 프레임 다시 조판하면 유닛 수만큼 TMP 메시 갱신이 쌓인다.
            int shown = Mathf.RoundToInt(hp);
            if (shown != _drawnHp || max != _drawnMax)
            {
                _drawnHp = shown;
                _drawnMax = max;
                _hpText.SetText("{0} / {1}", shown, max);
            }
        }

        private static float Ratio(int current, int max) => max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

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
