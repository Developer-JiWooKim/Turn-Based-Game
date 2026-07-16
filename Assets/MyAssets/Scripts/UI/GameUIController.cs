using Assets.MyAssets.Scripts.Singleton;
using UnityEngine;

namespace Assets.MyAssets.Scripts.UI
{
    public class GameUIController : MonoBehaviour
    {
        private const string BattleSceneName = "BattleScene";

        [Header("UI Panels")]
        [SerializeField] private TitlePanelUI _titlePanel;
        [SerializeField] private CharacterSelectPanelUI _characterSelectPanel;
        [SerializeField] private OptionPopupUI _optionPopup;

        public TitlePanelUI TitlePanel => _titlePanel;
        public CharacterSelectPanelUI CharacterSelectPanel => _characterSelectPanel;

        private GameFlowFSM _flowFSM;

        private void Awake()
        {
            _flowFSM = new GameFlowFSM(this);
        }

        private void OnEnable()
        {
            _titlePanel.OnPlayClicked += GoToCharacterSelect;
            _titlePanel.OnOptionClicked += _optionPopup.Show;
            _titlePanel.OnQuitClicked += QuitGame;

            _characterSelectPanel.OnBackClicked += GoToTitle;
            _characterSelectPanel.OnBattleClicked += StartBattle;
        }

        private void OnDisable()
        {
            _titlePanel.OnPlayClicked -= GoToCharacterSelect;
            _titlePanel.OnOptionClicked -= _optionPopup.Show;
            _titlePanel.OnQuitClicked -= QuitGame;

            _characterSelectPanel.OnBackClicked -= GoToTitle;
            _characterSelectPanel.OnBattleClicked -= StartBattle;
        }

        private void Start()
        {
            _titlePanel.Hide();
            _characterSelectPanel.Hide();
            _optionPopup.Hide();

            _flowFSM.ChangeState(_flowFSM.TitleState);
        }

        private void GoToTitle() => _flowFSM.ChangeState(_flowFSM.TitleState);
        private void GoToCharacterSelect() => _flowFSM.ChangeState(_flowFSM.CharacterSelectState);
        private void QuitGame() => GameManager.Instance.GameExit();
        private void StartBattle() => GameManager.Instance.LoadScene(BattleSceneName);
    }
}
