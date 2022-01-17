using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRotate : MonoBehaviour
{
    private Transform playerTransform;
    
    public float rotationSpeedX = 10f;
    public float rotationSpeedY = 10f;
    public Transform aimControlTransform;

    public float maxLookUpRotation = 85;
    public float minLookDownRotation = -85;

    void Update()
    {
        if (playerTransform == null) return;
        float h = Input.GetAxis("Mouse X") * rotationSpeedX * Time.deltaTime;
        float v = -Input.GetAxis("Mouse Y") * rotationSpeedY * Time.deltaTime; 
        
        Quaternion hQuaternion = Quaternion.AngleAxis(h, Vector3.up);
        
        playerTransform.rotation *= hQuaternion;
        
        Vector3 oldAimControlTransformRotation = aimControlTransform.rotation.eulerAngles;

        float newRotX = oldAimControlTransformRotation.x + v;
        // deal with the angles resetting to 0<= x < 360
        if (newRotX > 180) newRotX = newRotX - 360;
        oldAimControlTransformRotation.x = Mathf.Clamp(newRotX, minLookDownRotation, maxLookUpRotation);
        
        aimControlTransform.rotation = Quaternion.Euler(oldAimControlTransformRotation);
    }

    public void SetTarget(Transform followTarget)
    {
        playerTransform = followTarget;
    }

    public void SetMainCamera()
    {
        GetComponent<Camera>().enabled = true;
    }
}
