using SpaceGame.SaveSystem;
using SpaceGame.SaveSystem.Dto;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceGame.UI
{
    public class MainPanel : MonoBehaviour
    {
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _loadGameButton;

        private SaveService _saveService;

        private void Start()
        {
            _startGameButton.onClick.AddListener(StartNewGame);
            _loadGameButton.onClick.AddListener(StartLoadedGame);


            _saveService = new SaveService();
            _loadGameButton.gameObject.SetActive(_saveService.HasSave());
        }

        private void StartNewGame()
        {
            _startGameButton.interactable = false;
            GameContext.CurrentGameData = new GameData();
            SceneManager.LoadScene((int)Scene.Main);
        }

        private void StartLoadedGame()
        {
            _loadGameButton.interactable = false;
            var gameData = _saveService.LoadGame();
            GameContext.CurrentGameData = gameData;
            SceneManager.LoadScene((int)Scene.Main);
        }

        private void EndGame()
        {

            Application.Quit();
        }

        private void OnDestroy()
        {
            _startGameButton.onClick.RemoveAllListeners();
            _loadGameButton.onClick.RemoveAllListeners();

        }
    }
}