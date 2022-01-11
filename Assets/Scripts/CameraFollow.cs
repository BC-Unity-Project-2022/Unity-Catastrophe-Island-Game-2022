using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset;

    private Transform playerTransform;

    void Update()
    {
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + followOffset;
        }
    }

    public void SetTarget(Transform followTarget)
    {
        playerTransform = followTarget;
    }
}
