using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HealthBarScript : MonoBehaviour
{
   
    [SerializeField] private Slider slider;
    private AudioSource _audioSource;
    public float maxHealth = 100;
    public float currentHealth;

    private void SetHealth(float health)
    {
        slider.value = health;
    }

    public void TakeDamage(float damage)
    {
        // if water touches player for certain amount of time they die
        currentHealth -= damage;
        SetHealth(currentHealth);
    }

    private void Update()
    {
        if (currentHealth < 50)
        {
            _audioSource.Play(); 
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        _audioSource = GetComponent<AudioSource>();
    }
}
