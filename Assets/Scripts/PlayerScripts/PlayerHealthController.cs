using UnityEngine;
using UnityEngine.Serialization;

namespace PlayerScripts
{
    public class PlayerHealthController : MonoBehaviour
    {
        private float _timeSinceLastDrownDamage = 0.0f;
    
        public float damageFromOcean;
        public float oceanDamagePeriod = 0.1f;

        public float minFallDamageVerticalVelocity = 15f;
        public AnimationCurve fallDamageCurve;
        public float minSpeedForMaxFallDamage;

        public GameObject healthBarPrefab;
        private HealthBarScript _healthBar;
        private Rigidbody _rb;
        private float _lastFrameVerticalVelocity;

        private GameManager _gameManager;

        private void Awake()
        {
            var canvas = FindObjectOfType<Canvas>();
            var go = Instantiate(healthBarPrefab, canvas.gameObject.transform, true);
        
            _healthBar = go.GetComponent<HealthBarScript>();
            _healthBar.currentHealth = _healthBar.maxHealth;

            _rb = GetComponent<Rigidbody>();
            
            _gameManager = FindObjectOfType<GameManager>();
        }

        void Update()
        {
            if (!_gameManager.isPlayerAlive) return;
            // Check if the health is below or equal to 0
            if (_healthBar.currentHealth <= 0)
            {
                Debug.Log("Game over");
                _gameManager.KillPlayer();
            }
            _lastFrameVerticalVelocity = _rb.velocity.y;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // fall damage
            // TODO: play a sound
            float impactVelocity = Mathf.Abs(_lastFrameVerticalVelocity);
            if (impactVelocity >= minFallDamageVerticalVelocity)
                _healthBar.TakeDamage(_healthBar.maxHealth * fallDamageCurve.Evaluate((impactVelocity - minFallDamageVerticalVelocity) / (minSpeedForMaxFallDamage - minFallDamageVerticalVelocity)));
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Ocean")) return;
            _timeSinceLastDrownDamage += Time.fixedDeltaTime;
        
            if (_timeSinceLastDrownDamage >= oceanDamagePeriod)
            {
                _timeSinceLastDrownDamage = 0.0f;
                _healthBar.TakeDamage(damageFromOcean);
            }
        }
    }
}
