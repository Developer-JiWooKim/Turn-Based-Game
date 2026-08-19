using System.Collections.Generic;
using Assets.MyAssets.Scripts.Systems;
using Assets.MyAssets.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View.Panels
{
    /// <summary>
    /// Tab으로 여닫는 몬스터 정보 창(UI Toolkit). 이번 웨이브에 서 있는 몬스터의
    /// 이름·아이콘·스탯과, 스킬이 있으면 그 내용까지 보여준다.
    ///
    /// <b>전투를 멈추지 않는 단순 조회 창이다</b> — 퍼즈(<see cref="BattlePausePanel"/>)와 달리
    /// <c>IPauseGate</c>를 구현하지 않고, 몬스터 차례의 연출도 그대로 흘러간다.
    /// 대신 열려 있는 동안 <b>배틀 입력만</b> 닫는다 — UI Toolkit 오버레이는 3D 레이캐스트를 막지 않아서,
    /// 그냥 두면 창 위를 클릭한 것이 뒤에 선 몬스터를 겨냥하는 클릭으로 새어 들어간다.
    ///
    /// 그 입력 게이트를 퍼즈와 나눠 쓰므로 <b>둘이 동시에 열리지 않게</b> 한다 —
    /// 퍼즈가 열리면 <see cref="SetSuspended"/>로 이 창을 닫고 Tab을 잠근다(이유는 그 메서드 주석 참고).
    /// </summary>
    public sealed class MonsterInfoPanel : BasePanelUI
    {
        protected override string RootElementName => "monster-info-panel";

        [Tooltip("우상단 몬스터 정보 버튼을 제공하는 HUD. 비워두면 Tab으로만 열 수 있다.")]
        [SerializeField] private BattleHUD _hud;

        /// <summary>카드 목록. 화면 일부의 표현 전담이라 순수 C# View로 두고 여기서 들고 쓴다.</summary>
        private readonly MonsterInfoCardsView _cards = new();

        /// <summary>이번 웨이브의 몬스터(<see cref="BattleDirector"/>가 스폰 직후 밀어넣는다). 비어 있으면 Tab이 먹지 않는다.</summary>
        private IReadOnlyList<SpawnedMonster> _wave;

        private bool _isOpen;

        /// <summary>퍼즈가 열려 있는 동안. 이 사이에는 Tab을 무시한다(<see cref="SetSuspended"/> 참고).</summary>
        private bool _suspended;

        /// <summary>
        /// 배틀 입력을 되살릴 프레임(-1이면 예약 없음).
        /// 닫기 버튼을 <b>클릭</b>해서 닫으면 같은 프레임에 <see cref="TargetingController"/>가 그 클릭을
        /// 타겟팅 클릭으로 다시 읽는다 — <see cref="BattlePausePanel"/>의 '계속하기'와 똑같은 이유로 한 프레임 미룬다.
        /// </summary>
        private int _enableInputAtFrame = -1;

        protected override void InitPanel()
        {
            _cards.Build(Require<VisualElement>("monster-info-cards", "몬스터 카드가 표시되지 않습니다"));
            BindButton("monster-info-close", Close);

            Hide();
        }

        /// <summary>
        /// 이번 웨이브의 몬스터를 등록한다. null이나 빈 목록을 넘기면 창을 닫고 Tab을 잠근다 —
        /// 전투가 끝나면 몬스터 View가 정리되므로 성장 선택지 화면에서 낡은 정보가 뜨지 않게 하기 위함이다.
        /// </summary>
        public void SetWave(IReadOnlyList<SpawnedMonster> monsters)
        {
            _wave = monsters;

            if (monsters == null || monsters.Count == 0)
            {
                Close();
            }
        }

        /// <summary>
        /// 퍼즈가 열리고 닫힐 때 <see cref="BattlePausePanel"/>이 알린다.
        ///
        /// 잠글 때는 창을 닫되 <b>입력 게이트는 되돌리지 않는다</b> — 이제 그 게이트의 주인은 퍼즈이고,
        /// 여기서 되살리면 퍼즈 중에 타겟팅 클릭이 새어 들어간다. 예약해 둔 복구도 함께 취소한다.
        /// </summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
            if (!suspended)
            {
                return;
            }

            _isOpen = false;
            _enableInputAtFrame = -1;
            Hide();
        }

        private void Update()
        {
            InputManager input = InputManager.Instance;
            if (input == null)
            {
                return;
            }

            if (_enableInputAtFrame >= 0 && Time.frameCount >= _enableInputAtFrame)
            {
                _enableInputAtFrame = -1;
                input.IsGameplayInputEnabled = true;
            }

            if (input.MonsterInfoTogglePressed)
            {
                ToggleWindow();
            }
        }

        /// <summary>Tab 키와 우상단 HUD 버튼이 함께 쓰는 여닫기(두 경로가 같은 규칙을 타도록 한곳에 둔다).</summary>
        private void ToggleWindow()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            // 퍼즈 중이거나 보여줄 몬스터가 없으면 아무 일도 하지 않는다.
            if (_isOpen || _suspended || _wave == null || _wave.Count == 0)
            {
                return;
            }

            _isOpen = true;
            _cards.Show(_wave);

            _enableInputAtFrame = -1; // 직전에 닫으며 걸어둔 복구 예약이 남아 있으면 취소
            if (InputManager.Instance != null)
            {
                InputManager.Instance.IsGameplayInputEnabled = false;
            }

            Show();
        }

        private void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _enableInputAtFrame = Time.frameCount + 1; // 이유는 필드 주석 참고
            Hide();
        }

        protected override void OnEnable()
        {
            base.OnEnable(); // 언어 변경 구독(BasePanelUI)

            if (_hud != null)
            {
                _hud.MonsterInfoClicked += ToggleWindow;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_hud != null)
            {
                _hud.MonsterInfoClicked -= ToggleWindow;
            }

            // InputManager는 DontDestroyOnLoad라, 창이 열린 채 씬이 바뀌면 배틀 입력이 잠긴 채로 다음 런까지
            // 이어진다(퍼즈 패널과 같은 이유). 복구 예약도 Update가 더 돌지 않으면 소멸하므로 여기서 확정한다.
            bool needsRestore = _isOpen || _enableInputAtFrame >= 0;

            _isOpen = false;
            _enableInputAtFrame = -1;

            if (needsRestore && InputManager.Instance != null)
            {
                InputManager.Instance.IsGameplayInputEnabled = true;
            }
        }

        /// <summary>카드 문구는 코드가 채우므로 언어가 바뀌면 열려 있는 창을 다시 그린다.</summary>
        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();

            if (_isOpen)
            {
                _cards.Show(_wave);
            }
        }
    }
}
