using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    public class GameUIController : MonoBehaviour
    {
        private const string BattleSceneName = "BattleScene"; // 실제 턴제 전투가 일어날 씬 이름

        [Header("UI Panels")]
        [SerializeField] private TitlePanelUI _titlePanel;
        [SerializeField] private CharacterSelectPanelUI _characterSelectPanel;

        [Header("UI Popup")]
        [SerializeField] private OptionPopupUI _optionPopup;
        [SerializeField] private PointAllocationPopupUI _allocationPopup;

        public TitlePanelUI TitlePanel => _titlePanel;
        public CharacterSelectPanelUI CharacterSelectPanel => _characterSelectPanel;

        private GameFlowFSM _flowFSM;

        private bool _isReady;

        private void Awake()
        {
            _flowFSM = new GameFlowFSM(this);

            _isReady = ValidateReferences();
            if (!_isReady)
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

            if (_titlePanel == null)
            {
                Debug.LogError($"[GameUIController] {nameof(_titlePanel)}가 연결되지 않았습니다(인스펙터 확인).", this);
                hasError = true;
            }

            if (_characterSelectPanel == null)
            {
                Debug.LogError($"[GameUIController] {nameof(_characterSelectPanel)}가 연결되지 않았습니다(인스펙터 확인).", this);
                hasError = true;
            }

            if (_optionPopup == null)
            {
                Debug.LogError($"[GameUIController] {nameof(_optionPopup)}가 연결되지 않았습니다(인스펙터 확인).", this);
                hasError = true;
            }

            if (_allocationPopup == null)
            {
                Debug.LogError($"[GameUIController] {nameof(_allocationPopup)}가 연결되지 않았습니다(인스펙터 확인).", this);
                hasError = true;
            }

            return !hasError;
        }

        private void OnEnable()
        {
            _titlePanel.OnPlayClicked += GoToCharacterSelect;
            _titlePanel.OnOptionClicked += _optionPopup.Show;
            _titlePanel.OnQuitClicked += QuitGame;

            _characterSelectPanel.OnBackClicked += GoToTitle;
            _characterSelectPanel.OnBattleClicked += StartBattle;
            _characterSelectPanel.OnAllocationClicked += _allocationPopup.Show;
        }

        private void OnDisable()
        {
            // 연결이 빠졌으면 컴포넌트 자체를 끈다 — OnEnable·Start가 아예 호출되지 않지만 OnDisable은 해당되지 않으므로 가드
            if (!_isReady) return;

            _titlePanel.OnPlayClicked -= GoToCharacterSelect;
            _titlePanel.OnOptionClicked -= _optionPopup.Show;
            _titlePanel.OnQuitClicked -= QuitGame;

            _characterSelectPanel.OnBackClicked -= GoToTitle;
            _characterSelectPanel.OnBattleClicked -= StartBattle;
            _characterSelectPanel.OnAllocationClicked -= _allocationPopup.Show;
        }

        private void Start()
        {
            _titlePanel.Hide();
            _characterSelectPanel.Hide();
            _optionPopup.Hide();
            _allocationPopup.Hide();

            _flowFSM.ChangeState(_flowFSM.TitleState);
        }

        private void GoToTitle() => _flowFSM.ChangeState(_flowFSM.TitleState);
        private void GoToCharacterSelect() => _flowFSM.ChangeState(_flowFSM.CharacterSelectState);
        private void QuitGame() => GameManager.Instance.GameExit();

        private void StartBattle()
        {
            // 선택한 캐릭터로 새 런을 시작(파티 1명) → BattleScene에서 이 파티로 전투 구성
            GameManager.Instance.BeginRun(_characterSelectPanel.SelectedCharacter);
            GameManager.Instance.LoadScene(BattleSceneName);
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}
