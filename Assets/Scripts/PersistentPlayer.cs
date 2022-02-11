using System;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class PersistentPlayer : NetworkBehaviour
{
    [SerializeField] private float playerModelHeight;
    [SerializeField] private GameObject playerPrefab;
    [CanBeNull] public PlayerController player;

    private GameManager _gameManager;
    
    public void SpawnPlayer(Vector3 coords, Quaternion rotation)
    {
        if (!IsOwner || !IsServer) return;
        if (player != null) throw new Exception($"Can not spawn player from {gameObject.name} because it already exists");
        GameObject playerGameObject = Instantiate(playerPrefab, coords + new Vector3(0, playerModelHeight / 2, 0), rotation);
        player = playerGameObject.GetComponent<PlayerController>();
        playerGameObject.GetComponent<NetworkObject>().Spawn(false);
    }

    public void DestroyPlayer()
    {
        if (!IsOwner || !IsServer) return;
        if (player == null) throw new Exception($"Can not delete player form {gameObject.name} because it does not exist");
        
        player.gameObject.GetComponent<NetworkObject>().Despawn();
    }
    void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
        DontDestroyOnLoad(this);
    }

    public override void OnNetworkSpawn()
    {
        gameObject.name = "PersistentPlayer" + OwnerClientId;
        
        if (!IsServer) return;
        _gameManager.AddPlayer(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        RemovePersistentPlayer();
    }

    public override void OnNetworkDespawn()
    {
        RemovePersistentPlayer();
    }

    void RemovePersistentPlayer()
    {
        if (!IsServer) return;
        _gameManager.RemovePlayer(this);
    }
}
