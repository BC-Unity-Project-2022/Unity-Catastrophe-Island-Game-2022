using UnityEngine;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
    public int maxHealth = 100;

    public int currentHealth = 1;

    public Slider slider;

    public void SetCurrentHealth(int health)
    {
        currentHealth = health;
        slider.value = health;
    }
    
    public void TakeDamage(float damage)
    {
        SetCurrentHealth(Mathf.RoundToInt(currentHealth - damage));
    }
}
