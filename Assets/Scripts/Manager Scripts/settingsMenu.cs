using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class settingsMenu : MonoBehaviour
{
    private GameManager _gameManager;

    public void setSensitivity (float sensitivity)
        {
        _gameManager = FindObjectOfType<GameManager>();

        Debug.Log(_gameManager);
        _gameManager.mouseSensitivityMultiplier = sensitivity;
        Debug.Log(sensitivity);
        }
    
}
