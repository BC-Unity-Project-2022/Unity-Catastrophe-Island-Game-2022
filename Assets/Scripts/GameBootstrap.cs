using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameBootstrap : NetworkBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject gameManagerPrefab;
    void Start()
    {
        if (!FindObjectOfType<NetworkManager>())
        {
            Instantiate(networkManagerPrefab);
        }
        if (!FindObjectOfType<GameManager>())
        {
            Instantiate(gameManagerPrefab);
        }
    }

    // void Spawn(GameObject prefab)
    // {
    //     GameObject go = Instantiate(prefab);
    //     go.GetComponent<NetworkObject>().Spawn();
    // }
}
