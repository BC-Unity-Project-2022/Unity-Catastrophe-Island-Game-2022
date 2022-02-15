using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    [SerializeField] GameObject pauseMenu;

    private int mainMenuSceneIndex = 0;
    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            } else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Pause()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void LoadMainMenu()
    {
        _gameManager.playerLifeStatus = PlayerLifeStatus.NOT_IN_GAME;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Exited the game!");
    }
}
