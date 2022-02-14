using System;
using UnityEngine;

namespace PlayerScripts
{
    public class CameraPostEffectScript : MonoBehaviour
    {
        [SerializeField] private Material wholeScreenShaderMaterial;

        private GameManager _gameManager;
        private static readonly int BloodColorPropertyId = Shader.PropertyToID("_BloodDeathColor");
        private static readonly int DrownColorPropertyId = Shader.PropertyToID("_DrownDeathColor");
        private static readonly int DeathColorSelectorPropertyId = Shader.PropertyToID("_DeathColorSelector");

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            float materialAlpha = _gameManager.deathAnimationProgression;
            
            Color bloodColor = wholeScreenShaderMaterial.GetColor(BloodColorPropertyId);
            Color drownColor = wholeScreenShaderMaterial.GetColor(DrownColorPropertyId);
            bloodColor.a = materialAlpha;
            drownColor.a = materialAlpha;
            
            wholeScreenShaderMaterial.SetColor(BloodColorPropertyId, bloodColor);
            wholeScreenShaderMaterial.SetColor(DrownColorPropertyId, drownColor);

            int damageTypeSelector = _gameManager.lastDamageType == DamageType.DROWNING ? 0 : 1;
            wholeScreenShaderMaterial.SetInt(DeathColorSelectorPropertyId, damageTypeSelector);
            
            Graphics.Blit(src, dest, wholeScreenShaderMaterial);
        }
    }
}
