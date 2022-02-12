using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public Slider slider;
    
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.RoundToInt(currentHealth - damage);
        if (currentHealth < 0)
        {
            EndGame();
        }
    }

    private void Update()
    {


    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    void EndGame()
    {
        // Application.Quit();
        Debug.Log("game over");
    }
}
