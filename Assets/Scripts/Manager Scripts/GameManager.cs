using System;
using System.Collections;
using PlayerScripts;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
struct MapData
{
    public string mapName;
    public Vector2[] spawnLocations;
}

public enum PlayerLifeStatus
{
    NOT_IN_GAME,
    ALIVE,
    DEAD
}
public class GameManager : MonoBehaviour
{
    [SerializeField] private MapData[] mapsData;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField]
    private float deathAnimationTime = 3.0f; // in seconds
    
    private PlayerController _playerController;
    
    private MapData _currentMapData;

    [HideInInspector] public PlayerLifeStatus playerLifeStatus { get; private set; }
    [HideInInspector] public float deathAnimationProgression = 0.0f; // a value 0 to 1 that shows the progress of the death screen animation

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        playerLifeStatus = PlayerLifeStatus.NOT_IN_GAME;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (GUILayout.Button("Respawn players")) RespawnPlayer();

        if (GUILayout.Button("Load the new map")) LoadMainMap();

        GUILayout.EndArea();
    }

    private void Update()
    {
        Debug.Log(deathAnimationProgression);
        if (playerLifeStatus == PlayerLifeStatus.DEAD)
            // start the death animation
            deathAnimationProgression = Mathf.Clamp01(deathAnimationProgression + Time.deltaTime / deathAnimationTime);
    }

    void RespawnPlayer()
    {
        Terrain currentTerrain = FindObjectOfType<Terrain>();
        
        int spawnLocationIndex = Random.Range(0, _currentMapData.spawnLocations.Length);
        Vector2 rawSpawnLocation = _currentMapData.spawnLocations[spawnLocationIndex];
        float height = currentTerrain.SampleHeight(new Vector3(rawSpawnLocation.x, 0, rawSpawnLocation.y));
        
        Vector3 spawnLocation = new Vector3(rawSpawnLocation.x, height, rawSpawnLocation.y);
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
        if (playerLifeStatus == PlayerLifeStatus.ALIVE) throw new Exception("Can not create a player because the player is still alive");
        var prefab = Instantiate(playerPrefab, pos, rot);
        _playerController = prefab.GetComponent<PlayerController>();
        playerLifeStatus = PlayerLifeStatus.ALIVE;
    }

    public void KillPlayer()
    {
        playerLifeStatus = PlayerLifeStatus.DEAD;
        // Lift the constraints on the rigidbody, to make it feel like a ragdoll
        var rb = _playerController.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        
        // change the centre of mass to prevent the player from rolling forever
        rb.centerOfMass = new Vector3(0, 0, -1.0f);
        rb.angularDrag = 0.9f;
        
        // Add more friction to prevent sliding
        var playerCollider = _playerController.GetComponent<CapsuleCollider>();
        playerCollider.material = new PhysicMaterial
        {
                bounciness = 0,
                dynamicFriction = 0.9f,
                staticFriction = 0.9f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                frictionCombine = PhysicMaterialCombine.Maximum
        };
    }
}
