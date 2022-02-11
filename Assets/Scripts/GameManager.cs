using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[Serializable]
struct MapData
{
    public string mapName;
    public Vector2[] spawnLocations;
}
public class GameManager : MonoBehaviour
{
    [SerializeField] private MapData[] mapsData;
    [SerializeField] private float playerHeight;
    [SerializeField] private GameObject playerPrefab;
    private PlayerController _playerController;
    
    private MapData _currentMapData;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (GUILayout.Button("Respawn players")) RespawnPlayer();

        if (GUILayout.Button("Load the new map")) LoadMainMap();

        GUILayout.EndArea();
    }

    void RespawnPlayer()
    {
        Terrain currentTerrain = FindObjectOfType<Terrain>();
        
        // TODO: delete all the players that are left
        
        int spawnLocationIndex = Random.Range(0, _currentMapData.spawnLocations.Length);
        Vector2 rawSpawnLocation = _currentMapData.spawnLocations[spawnLocationIndex];
        float height = currentTerrain.SampleHeight(new Vector3(rawSpawnLocation.x, 0, rawSpawnLocation.y));
        
        Vector3 spawnLocation = new Vector3(rawSpawnLocation.x, height + playerHeight / 2, rawSpawnLocation.y);
        spawnLocation = currentTerrain.transform.TransformVector(spawnLocation);
        
        // spawn the player
        SpawnPlayer(spawnLocation, Quaternion.identity);
    }

    void LoadMainMap()
    {
        MapData md = mapsData[0];
        _currentMapData = md;
        // load the map
        SceneManager.LoadSceneAsync(md.mapName, LoadSceneMode.Single);
    }

    void SpawnPlayer(Vector3 pos, Quaternion rot)
    {
        if (_playerController != null) throw new Exception("Can not create a player because one exists already");
        var prefab = Instantiate(playerPrefab, pos, rot);
        _playerController = prefab.GetComponent<PlayerController>();
    }
}
