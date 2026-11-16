using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    public class GameUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private TitlePanelUI _titlePanel;
        [SerializeField] private CharacterSelectPanelUI _characterSelectPanel;

        [Header("UI Popup")]
        [SerializeField] private OptionPopupUI _optionPopup;
        [SerializeField] private PointAllocationPopupUI _allocationPopup;
        [Tooltip("저장된 이어하기 데이터가 있는데 새 런을 시작할 때 뜨는 경고 팝업.")]
        [SerializeField] private NewRunWarningPopupUI _newRunWarningPopup;

        private const string BattleSceneName = "BattleScene"; // 실제 턴제 전투가 일어날 씬 이름

        private bool _isValid;
        private GameFlowFSM _flowFSM;

        public TitlePanelUI TitlePanel => _titlePanel;
        public CharacterSelectPanelUI CharacterSelectPanel => _characterSelectPanel;

        private void Awake()
        {
            _flowFSM = new GameFlowFSM(this);

            _isValid = ValidateReferences();
            if (!_isValid)
            {
                enabled = false;
            }
        }

        /// <summary>
        /// 필수 인스펙터 연결을 검사하고 누락을 보고한다.
        /// </summary>
        /// <returns>전부 연결돼 있으면 true</returns>
        private bool ValidateReferences()
        {
            bool hasError = false;
            hasError |= NullCheck.LogIfMissing(_titlePanel, nameof(_titlePanel), this);
            hasError |= NullCheck.LogIfMissing(_characterSelectPanel, nameof(_characterSelectPanel), this);
            hasError |= NullCheck.LogIfMissing(_optionPopup, nameof(_optionPopup), this);
            hasError |= NullCheck.LogIfMissing(_allocationPopup, nameof(_allocationPopup), this);
            hasError |= NullCheck.LogIfMissing(_newRunWarningPopup, nameof(_newRunWarningPopup), this);

            return !hasError;
        }

        private void OnEnable()
        {
            // 검증 실패로 Awake에서 꺼진 뒤 외부에서 되살린 경우 — 참조가 없으므로 배선하지 않는다
            if (!_isValid)
            {
                return;
            }

            _titlePanel.OnPlayClicked += GoToCharacterSelect;
            _titlePanel.OnContinueClicked += ContinueRun;
            _titlePanel.OnOptionClicked += _optionPopup.Show;
            _titlePanel.OnQuitClicked += QuitGame;

            _characterSelectPanel.OnBackClicked += GoToTitle;
            _characterSelectPanel.OnBattleClicked += StartBattle;
            _characterSelectPanel.OnAllocationClicked += _allocationPopup.Show;

            _newRunWarningPopup.OnConfirmed += BeginNewRun;
        }

        private void OnDisable()
        {
            // OnEnable에서 배선하지 않았으므로 해제할 것도 없다(가드를 양쪽에 대칭으로 둔다)
            if (!_isValid)
            {
                return;
            }

            _titlePanel.OnPlayClicked -= GoToCharacterSelect;
            _titlePanel.OnContinueClicked -= ContinueRun;
            _titlePanel.OnOptionClicked -= _optionPopup.Show;
            _titlePanel.OnQuitClicked -= QuitGame;

            _characterSelectPanel.OnBackClicked -= GoToTitle;
            _characterSelectPanel.OnBattleClicked -= StartBattle;
            _characterSelectPanel.OnAllocationClicked -= _allocationPopup.Show;

            _newRunWarningPopup.OnConfirmed -= BeginNewRun;
        }

        private void Start()
        {
            _titlePanel.Hide();
            _characterSelectPanel.Hide();
            _optionPopup.Hide();
            _allocationPopup.Hide();
            _newRunWarningPopup.Hide();

            _flowFSM.ChangeState(_flowFSM.TitleState);
        }

        private void GoToTitle() => _flowFSM.ChangeState(_flowFSM.TitleState);
        private void GoToCharacterSelect() => _flowFSM.ChangeState(_flowFSM.CharacterSelectState);
        private void QuitGame() => GameManager.Instance.GameExit();

        /// <summary>
        /// '전투 시작' — 이어할 런이 저장돼 있으면 먼저 경고한다(새로 시작하면 그 기록이 사라지므로).
        /// 경고에서 확인을 누르면 <see cref="BeginNewRun"/>이 이어서 처리한다.
        /// </summary>
        private void StartBattle()
        {
            if (SaveService.HasRun)
            {
                _newRunWarningPopup.Show();
                return;
            }

            BeginNewRun();
        }

        private void BeginNewRun()
        {
            SaveService.ClearRun(); // 새 런을 시작하면 이전 체크포인트는 더 이상 이어할 수 없다

            // 선택한 캐릭터로 새 런을 시작(파티 1명) → BattleScene에서 이 파티로 전투 구성
            GameManager.Instance.BeginRun(_characterSelectPanel.SelectedCharacter);
            GameManager.Instance.LoadScene(BattleSceneName);
        }

        /// <summary>'이어하기' — 저장된 체크포인트를 런으로 되돌려 BattleScene으로 넘긴다.</summary>
        private void ContinueRun()
        {
            RunData run = SaveService.Current.Run.ToRunData(_characterSelectPanel.Roster);
            if (run == null)
            {
                // 복원 실패(로스터에서 캐릭터를 못 찾음) — 이유는 RunSnapshot이 이미 로그로 남겼다.
                // 세이브는 지우지 않는다(에셋 설정을 고치면 되살아날 수 있으므로) 대신 버튼만 잠근다.
                _titlePanel.SetContinueEnabled(false);
                return;
            }

            GameManager.Instance.ResumeRun(run);
            GameManager.Instance.LoadScene(BattleSceneName);
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}
