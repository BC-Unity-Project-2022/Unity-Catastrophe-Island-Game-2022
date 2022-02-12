using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    private float _timeSinceLastDrownDamage = 0.0f;
    
    public float damageFromOcean;
    public float damagePeriod = 0.1f;

    public GameObject healthBarPrefab;
    private HealthBarScript _healthBar;

    private void Awake()
    {
        var canvas = FindObjectOfType<Canvas>();
        var go = Instantiate(healthBarPrefab, canvas.gameObject.transform, true);
        
        _healthBar = go.GetComponent<HealthBarScript>();
        _healthBar.currentHealth = _healthBar.maxHealth;
    }

    void Update()
    {
        // Check if the health is below or equal to 0
        if (_healthBar.currentHealth <= 0)
        {
            // TODO: kill the player
            Debug.Log("Game over");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ocean")) return;
        _timeSinceLastDrownDamage += Time.deltaTime;
        
        if (_timeSinceLastDrownDamage >= damagePeriod)
        {
            _timeSinceLastDrownDamage = 0.0f;
            _healthBar.TakeDamage(damageFromOcean);
        }
    }
}
