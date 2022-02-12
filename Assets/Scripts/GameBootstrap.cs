using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;
    void Start()
    {
        if (!FindObjectOfType<GameManager>())
        {
            Instantiate(gameManagerPrefab);
        }
    }
}
