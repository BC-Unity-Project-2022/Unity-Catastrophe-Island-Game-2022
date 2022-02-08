using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[Serializable]
struct MapData
{
    public string mapName;
    public Vector2[] spawnLocations;
}
public class GameManager : NetworkBehaviour
{
    [SerializeField] private MapData[] mapsData;
    private readonly ArrayList _alivePlayers = new ArrayList();
    
    private MapData _currentMapData;

    public void AddPlayer(PersistentPlayer player)
    {
        if (IsServer)
            _alivePlayers.Add(player);
    }
    public void RemovePlayer(PersistentPlayer player)
    {
        if (IsServer)
            _alivePlayers.Remove(player);
    }
    
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }

        if (GUILayout.Button("Respawn players")) RespawnPlayers();

        if (GUILayout.Button("Load the new map")) LoadMainMap();

        GUILayout.EndArea();
    }

    void RespawnPlayers()
    {
        Terrain currentTerrain = FindObjectOfType<Terrain>();
        
        // TODO: delete all the players that are left
        // TODO: avoid spawning people inside each other
        foreach (PersistentPlayer alivePlayer in _alivePlayers)
        {
            int spawnLocationIndex = Random.Range(0, _currentMapData.spawnLocations.Length);
            Vector2 rawSpawnLocation = _currentMapData.spawnLocations[spawnLocationIndex];
            float height = currentTerrain.SampleHeight(new Vector3(rawSpawnLocation.x, 0, rawSpawnLocation.y));
            
            Vector3 spawnLocation = new Vector3(rawSpawnLocation.x, height, rawSpawnLocation.y);
            spawnLocation = currentTerrain.transform.TransformVector(spawnLocation);
            
            alivePlayer.SpawnPlayer(spawnLocation, Quaternion.identity);
        }
    }

    void LoadMainMap()
    {
        if (!IsServer) return;
        MapData md = mapsData[0];
        _currentMapData = md;
        // load the map
        NetworkManager.Singleton.SceneManager.LoadScene(md.mapName, LoadSceneMode.Single);
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
    }
}
