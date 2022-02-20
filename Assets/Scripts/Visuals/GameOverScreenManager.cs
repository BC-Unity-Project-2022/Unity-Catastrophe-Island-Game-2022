using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TMP_Text gameOverScore;
    [SerializeField] private TMP_Text gameOverBestScore;

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

    public void UpdateBestScoreMessage()
    {
        var scores = _gameManager.LoadScores();
        float maxScore = 0;
        foreach (var saveData in scores)
        {
            if (maxScore < saveData.timeSurvived) maxScore = saveData.timeSurvived;
        }

        gameOverBestScore.text = $"Best score: {SaveData.GetTimeString(maxScore)}";
    }
}
