using System;
using Cinemachine;
using UnityEngine;

namespace PlayerScripts
{
    struct MouseMovementData
    {
        public float h;
        public float v;

        public override string ToString()
        {
            return $"h: {h}, v: {v}";
        }
    }

    public class CameraRotate : MonoBehaviour
    {
        public Transform playerTransform;

        public float rotationSpeedX = 10f;
        public float rotationSpeedY = 10f;

        public float maxLookUpRotation = 85;
        public float minLookDownRotation = -85;
        
        private GameManager _gameManager;

        public static void UnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        public static void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start()
        {
            LockCursor();
            
            _gameManager = FindObjectOfType<GameManager>();
        }

        void FixedUpdate()
        {
            // do not move after death
            if (_gameManager.playerLifeStatus != PlayerLifeStatus.ALIVE)
            {
                if (_gameManager.playerLifeStatus == PlayerLifeStatus.DEAD)
                {
                    // turn to the sky if drowning to make it look better
                    if (_gameManager.lastDamageType == DamageType.DROWNING)
                    {
                        Vector3 upEuler = transform.rotation.eulerAngles;
                        upEuler.x = -90;
                        
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(upEuler), 0.005f);
                    }
                }

                return;
            }
            
            MouseMovementData mouseMovementData = new MouseMovementData()
            {
                h = Input.GetAxis("Mouse X") * rotationSpeedX * Time.deltaTime,
                v = -Input.GetAxis("Mouse Y") * rotationSpeedY * Time.deltaTime
            };
            ApplyRotation(mouseMovementData);
        }

    
        private void ApplyRotation(MouseMovementData mouseMovementData)
        {
            Quaternion hQuaternion = Quaternion.AngleAxis(mouseMovementData.h, Vector3.up);
        
            playerTransform.rotation *= hQuaternion;
        
            Vector3 oldAimControlTransformRotation = transform.rotation.eulerAngles;

            float newRotX = oldAimControlTransformRotation.x + mouseMovementData.v;
            // deal with the angles resetting to 0<= x < 360
            if (newRotX > 180) newRotX = newRotX - 360;
            oldAimControlTransformRotation.x = Mathf.Clamp(newRotX, minLookDownRotation, maxLookUpRotation);
        
            transform.rotation = Quaternion.Euler(oldAimControlTransformRotation);
        }
    }
}