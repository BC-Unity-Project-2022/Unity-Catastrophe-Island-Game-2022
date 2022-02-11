using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
   
    public Slider slider;
    

    public void setMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
    }

    public void setMinHealth()
    {
        slider.minValue = 0;
    }

    public void setHealth(int health)
    {
        slider.value = health;
    }

    public void takeDamage(int damage)
    {
        // if water touches player for certain amount of time they die
        currentHealth -= damage;
        setHealth(currentHealth);
    }

    private void Update()
    {
       

        if (currentHealth < 0)
        {
            endGame();
        }


    }

    public int maxHealth = 100;
    public int currentHealth;


    void Start()
    {
        currentHealth = maxHealth;
        // healthbar.setMaxHealth(maxHealth);

        

    }

    void endGame()
    {
        Application.Quit();
        Debug.Log("game over");
    }

    private float time = 0.0f;
    public float interpolationPeriod = 0.1f;

    private void OnTriggerStay(Collider other) 
    {
        Debug.Log("collision started");
        //InvokeRepeating("takeDamage(1)", 0, 1);
        //takeDamage(1);
        time += Time.deltaTime;
        
        if (time >= interpolationPeriod)
        {
            time = 0.0f;
            takeDamage(1);
        }


    }
}
