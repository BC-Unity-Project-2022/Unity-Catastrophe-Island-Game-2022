using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;
    void Awake()
    {
        if (!FindObjectOfType<GameManager>())
        {
            Instantiate(gameManagerPrefab);
        }
    }
}
