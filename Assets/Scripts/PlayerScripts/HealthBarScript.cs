using UnityEngine;
using UnityEngine.UI;

namespace PlayerScripts
{
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
            int newHealth = Mathf.RoundToInt(currentHealth - damage);
            SetCurrentHealth(newHealth);
        }
    }
}
