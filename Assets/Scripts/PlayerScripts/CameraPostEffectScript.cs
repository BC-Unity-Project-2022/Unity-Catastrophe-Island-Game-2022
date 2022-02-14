using System;
using UnityEngine;

namespace PlayerScripts
{
    public class CameraPostEffectScript : MonoBehaviour
    {
        [SerializeField] private Material wholeScreenShaderMaterial;

        private Material _material;

        private GameManager _gameManager;
        private static readonly int BloodColorPropertyId = Shader.PropertyToID("_BloodDeathColor");
        private static readonly int DrownColorPropertyId = Shader.PropertyToID("_DrownDeathColor");
        private static readonly int DeathColorSelectorPropertyId = Shader.PropertyToID("_DeathColorSelector");

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
            _material = new Material(wholeScreenShaderMaterial);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            float materialAlpha = _gameManager.deathAnimationProgression;
            
            Color bloodColor = _material.GetColor(BloodColorPropertyId);
            Color drownColor = _material.GetColor(DrownColorPropertyId);
            bloodColor.a = materialAlpha;
            drownColor.a = materialAlpha;
            
            _material.SetColor(BloodColorPropertyId, bloodColor);
            _material.SetColor(DrownColorPropertyId, drownColor);

            int damageTypeSelector = _gameManager.lastDamageType == DamageType.DROWNING ? 0 : 1;
            _material.SetInt(DeathColorSelectorPropertyId, damageTypeSelector);
            
            Graphics.Blit(src, dest, _material);
        }
    }
}
