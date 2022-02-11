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
    private HealthBarScript healthBar;

    private void Awake()
    {
        var canvas = FindObjectOfType<Canvas>();
        var go = Instantiate(healthBarPrefab, canvas.gameObject.transform, true);
        
        healthBar = go.GetComponent<HealthBarScript>();
    }

    void Update()
    {
        // Check if the health is below or equal to 0
        if (healthBar.currentHealth <= 0)
        {
            // TODO: kill the player
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ocean")) return;
        _timeSinceLastDrownDamage += Time.deltaTime;
        
        if (_timeSinceLastDrownDamage >= damagePeriod)
        {
            _timeSinceLastDrownDamage = 0.0f;
            healthBar.TakeDamage(damageFromOcean);
        }
    }
}
