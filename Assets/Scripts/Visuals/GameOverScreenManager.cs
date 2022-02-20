using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TMP_Text gameOverScore;

    private GameManager _gameManager;
    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }
    
    public void Hide()
    {
        gameOverScreen.SetActive(false);
    }

    public void Show()
    {
        gameOverScreen.SetActive(true);
    }

    public void SetScoreMessage(string t)
    {
        gameOverScore.SetText(t);
    }

    public void ReturnToMainMenu()
    {
        _gameManager.LoadMainMenu();
    }
}
