using System;
using UnityEngine;

namespace PlayerScripts
{
    public class CameraPostEffectScript : MonoBehaviour
    {
        [SerializeField] private Material wholeScreenShaderMaterial;

        private GameManager _gameManager;
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            float materialAlpha = _gameManager.deathAnimationProgression;
            
            Color color = wholeScreenShaderMaterial.color;
            color.a = materialAlpha;
            wholeScreenShaderMaterial.SetColor(ColorPropertyId, color);
            
            Graphics.Blit(src, dest, wholeScreenShaderMaterial);
        }
    }
}
