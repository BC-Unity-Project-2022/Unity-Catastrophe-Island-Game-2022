using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private GameManager _gameManager;
    public void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }

    public void PlayGame()
    {
        _gameManager.StartNewMap();
    }

    public void Exit()
    {
        Application.Quit();
        Debug.Log("Exited the game!");
    }
}
