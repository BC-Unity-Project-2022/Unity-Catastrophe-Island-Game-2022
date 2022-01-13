using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset;

    private Transform playerTransform;
    public float rotationSpeed = 10f;

    void Update()
    {
        if (playerTransform == null) return;
        if (NetworkManager.Singleton.IsServer)
        {
            transform.position = playerTransform.position + followOffset;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;
        float h = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float v = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime; 
        
        Quaternion hQuaternion = Quaternion.AngleAxis(h, Vector3.up);

        Quaternion vQuaternion = Quaternion.AngleAxis(v, Vector3.left); 
        
        playerTransform.rotation *= hQuaternion;
        transform.rotation *= vQuaternion;
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
