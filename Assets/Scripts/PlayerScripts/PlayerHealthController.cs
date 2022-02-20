using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PlayerScripts
{
    public class PlayerHealthController : MonoBehaviour
    {
        private float _timeSinceLastDrownDamage = 0.0f;

        public float timeBeforeDrowningStarts;
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
        private bool _initialised = false;
        private float _oxygenTimeLeft;

        private void Awake()
        {
            _healthBar = FindObjectOfType<HealthBarScript>();
            
            _healthBar.currentHealth = _healthBar.maxHealth;

            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        void Update()
        {
            if (!_initialised && _gameManager.playerLifeStatus != PlayerLifeStatus.NOT_IN_GAME)
            {
                _initialised = true;
                var images = _healthBar.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    image.enabled = true;
                }
            }
            
            if (_gameManager.playerLifeStatus == PlayerLifeStatus.DEAD) return;
            
            _lastFrameVerticalVelocity = _rb.velocity.y;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // fall damage
            // TODO: play a sound
            float impactVelocity = Mathf.Abs(_lastFrameVerticalVelocity);
            if (impactVelocity >= minFallDamageVerticalVelocity)
                _healthBar.TakeDamage(_healthBar.maxHealth * fallDamageCurve.Evaluate((impactVelocity - minFallDamageVerticalVelocity) / (minSpeedForMaxFallDamage - minFallDamageVerticalVelocity)), DamageType.FALL_DAMAGE);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("NonBreathable")) return;
            _oxygenTimeLeft = timeBeforeDrowningStarts;
        }
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("NonBreathable")) return;
            _oxygenTimeLeft = timeBeforeDrowningStarts;
        }
        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("NonBreathable")) return;
            if (_oxygenTimeLeft > 0)
            {
                _oxygenTimeLeft -= Time.fixedDeltaTime;
                return;
            }
            
            _timeSinceLastDrownDamage += Time.fixedDeltaTime;
        
            if (_timeSinceLastDrownDamage >= oceanDamagePeriod)
            {
                _timeSinceLastDrownDamage = 0.0f;
                _healthBar.TakeDamage(damageFromOcean, DamageType.DROWNING);
            }
        }
    }
}
