using PlayerScripts;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;
    private bool wasCursorLocked;
    
    [SerializeField] GameObject pauseMenu;

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
        if (wasCursorLocked) CameraRotate.LockCursor();
        else CameraRotate.UnlockCursor();
        
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Pause()
    {
        wasCursorLocked = Cursor.lockState != CursorLockMode.None;
        
        CameraRotate.UnlockCursor();
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        _gameManager.LoadMainMenu();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Exited the game!");
    }
}
