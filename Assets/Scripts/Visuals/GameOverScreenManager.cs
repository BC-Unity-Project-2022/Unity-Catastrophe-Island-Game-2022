using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreenManager : MonoBehaviour
{
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text gameOverScore;
    
    [SerializeField] private TMP_Text mainMenuButtonText;
    [SerializeField] private Image mainMenuButtonImage;
    [SerializeField] private Button mainMenuButton;
    public void Hide()
    {
        gameOverText.enabled = false;
        gameOverScore.enabled = false;
        
        mainMenuButtonText.enabled = false;
        mainMenuButtonImage.enabled = false;
        mainMenuButton.enabled = false;
    }

    public void Show()
    {
        gameOverText.enabled = true;
        gameOverScore.enabled = true;
        
        mainMenuButtonText.enabled = true;
        mainMenuButtonImage.enabled = true;
        mainMenuButton.enabled = true;
    }

    public void SetScoreMessage(string t)
    {
        gameOverScore.SetText(t);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Return to main menu");
    }
}
