using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private GameManager _gameManager;
    void Start()
    {
        // record the initial settings and hide the settings menu
        _gameManager = FindObjectOfType<GameManager>();

        var mouseSensitivitySlider = GameObject.Find("MouseSensitivitySlider").GetComponent<Slider>();
        _gameManager.mouseSensitivityMultiplier = mouseSensitivitySlider.value;
        
        GameObject.Find("settingsMenu").SetActive(false);
    }
}
