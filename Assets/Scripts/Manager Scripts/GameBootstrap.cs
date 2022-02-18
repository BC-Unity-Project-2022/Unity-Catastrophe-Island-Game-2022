using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject heldItemCameraPrefab;
    void Awake()
    {
        if (!FindObjectOfType<GameManager>())
            Instantiate(gameManagerPrefab);

        if(!GameObject.FindGameObjectWithTag("HeldItemCamera"))
            Instantiate(heldItemCameraPrefab);
    }
}
