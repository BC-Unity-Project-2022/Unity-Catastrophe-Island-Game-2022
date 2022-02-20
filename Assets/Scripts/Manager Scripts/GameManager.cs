using System;
using System.Collections;
using Cinemachine;
using PlayerScripts;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
public struct SaveData
{
    public float timeSurvived;

    public SaveData(float time)
    {
        this.timeSurvived = time;
    }

    public override string ToString()
    {
        return $"Save Data (time: {this.timeSurvived})";
    }

    public static string ToString(SaveData[] dat)
    {
        string ret = "{";
        foreach (var saveData in dat)
        {
            ret += saveData;
        }

        return ret + "}";
    }
    public static string GetTimeString(float timeTaken)
    {
        timeTaken = Mathf.Floor(timeTaken);
        
        string mins = Mathf.Floor(timeTaken / 60).ToString();
        string secs = (timeTaken % 60).ToString();

        mins = mins.PadLeft(2, '0');
        secs = secs.PadLeft(2, '0');
        
        return $"{mins}:{secs}";
    }
}

[Serializable]
struct MapData
{
    public string mapName;
    public Vector2[] spawnLocations;
    public GameObject Flyby;
}

public enum PlayerLifeStatus
{
    NOT_IN_GAME,
    ALIVE,
    DEAD
}
public class GameManager : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName;
    [SerializeField] private MapData[] mapsData;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float playerHeight;

    [SerializeField]
    private float deathAnimationTime = 3.0f; // in seconds
    [SerializeField] private float mapIntoTime;
    [SerializeField] private float mapCooldownAfterIntoTime;
    
    [SerializeField] private float ragdollRotationEffectStrength;
    
    private PlayerController _playerController;
    
    private MapData _currentMapData;

    [HideInInspector] public PlayerLifeStatus playerLifeStatus;
    [HideInInspector] public float deathAnimationProgression = 0.0f; // a value 0 to 1 that shows the progress of the death screen animation

    [HideInInspector]
    public DamageType lastDamageType = DamageType.UNKNOWN;

    private float _introProgression;
    private bool _isPresentingMap;

    private GameObject _currentFlyBy;
    private CinemachineSmoothPath _currentFlyByPath;
    private CinemachineTrackedDolly _currentFlyByCamera;

    private float _gameBeginTime;
    private float _gameOverTime;

    private OverlayManager _overlayManager;
    private HealthBarScript _healthBar;
    private GameOverScreenManager _gameOverScreen;
    private Builder _builder;
    private PlayerItems _playerItems;

    private bool _isDeathAnimationOver = false;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        playerLifeStatus = PlayerLifeStatus.NOT_IN_GAME;
    }

    private void Update()
    {
        if (_isPresentingMap)
        {
            // finish a bit early so that the camera has enough time to go to the correct place due to z-damping on the camera
            // this is what the cooldown time is for!
            _introProgression = Mathf.Clamp01(_introProgression + Time.deltaTime / mapIntoTime);

            if (_currentFlyByCamera != null)
            {
                _currentFlyByCamera.m_PathPosition = Mathf.Lerp(0, _currentFlyByPath.m_Waypoints.Length, _introProgression);
            }

            return;
        }
        if (playerLifeStatus == PlayerLifeStatus.DEAD)
        {
            // start the death animation
            deathAnimationProgression = Mathf.Clamp01(deathAnimationProgression + Time.deltaTime / deathAnimationTime);
            if (!_isDeathAnimationOver && Mathf.Abs(1 - deathAnimationProgression) < 0.01f)
            {
                _isDeathAnimationOver = true;
                
                // show the stats
                ClearUIAndItems();
                _gameOverScreen.Show();
                _gameOverScreen.SetScoreMessage($"Time survived: {SaveData.GetTimeString(_gameOverTime - _gameBeginTime)}");
                _gameOverScreen.UpdateBestScoreMessage();

                KillPlayerImmediate();
            }
        }

        if(_overlayManager != null) _overlayManager.SetTimerText(SaveData.GetTimeString(Time.time - _gameBeginTime));
    }

    void ClearUIAndItems()
    {
        _gameOverScreen.Hide();
        _healthBar.Hide();
        _overlayManager.Hide();
        
        if(_playerItems != null) _playerItems.Hide();
        if(_builder != null) _builder.Hide();
    }

    Vector3 FindSpawnLocation()
    {
        Terrain currentTerrain = GameObject.FindGameObjectWithTag("Ground").GetComponent<Terrain>();
        
        int spawnLocationIndex = Random.Range(0, _currentMapData.spawnLocations.Length);
        Vector2 rawSpawnLocation = _currentMapData.spawnLocations[spawnLocationIndex];
        float height = currentTerrain.SampleHeight(new Vector3(rawSpawnLocation.x, 0, rawSpawnLocation.y));
        
        Vector3 spawnLocation = new Vector3(rawSpawnLocation.x, height + playerHeight / 2, rawSpawnLocation.y);
        return currentTerrain.transform.TransformVector(spawnLocation);
    }
    
    AsyncOperation LoadNewMap()
    {
        
        MapData md = mapsData[0];
        _currentMapData = md;
        playerLifeStatus = PlayerLifeStatus.NOT_IN_GAME;
        // load the map
        return SceneManager.LoadSceneAsync(md.mapName, LoadSceneMode.Single);
    }

    public void StartNewMap()
    {
        StartCoroutine(StartNewMapCoroutine());
    }
    IEnumerator StartNewMapCoroutine()
    {
        KillPlayerImmediate();
        
        AsyncOperation loadOperation = LoadNewMap();

        // wait for the load
        yield return loadOperation;

        _gameOverScreen = FindObjectOfType<GameOverScreenManager>();
        _overlayManager = FindObjectOfType<OverlayManager>();
        _healthBar = FindObjectOfType<HealthBarScript>();
    
        ClearUIAndItems();
        
        _introProgression = 0.0f;
        _isPresentingMap = true;

        Vector3 spawnLocation = FindSpawnLocation();
        
        var go = Instantiate(playerPrefab, spawnLocation, Quaternion.identity);
        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        
        // hide the player's mesh
        foreach (var meshRenderer in renderers)
        {
            meshRenderer.enabled = false;
        }


        if (_currentMapData.Flyby == null)
        {
            _currentFlyBy = null;
            _currentFlyByPath = null;
            _currentFlyByCamera = null;
        
            yield return new WaitForSeconds(mapCooldownAfterIntoTime + mapIntoTime); 
        }
        else
        {
            Vector3 lastWayPoint;
            
            _currentFlyBy = Instantiate(_currentMapData.Flyby);
            _currentFlyByPath = _currentFlyBy.GetComponentInChildren<CinemachineSmoothPath>();
            _currentFlyByCamera = _currentFlyBy.GetComponentInChildren<CinemachineTrackedDolly>();

            // wait a bit so that the player settles in place to set the last waypoint
            float waitUntilWaypointSet = 1.0f;
            if (mapIntoTime < 1.2f * waitUntilWaypointSet) throw new Exception("The map intro time can not be this small");
            yield return new WaitForSeconds(waitUntilWaypointSet);
            
            lastWayPoint = go.GetComponentInChildren<CinemachineVirtualCamera>().transform.position;
            _currentFlyByPath.m_Waypoints[_currentFlyByPath.m_Waypoints.Length - 1].position = lastWayPoint;
            
            yield return new WaitForSeconds(mapCooldownAfterIntoTime + mapIntoTime - waitUntilWaypointSet); 
        }

        _isPresentingMap = false;
        
        Vector3? cameraRot = null;
        if (_currentFlyBy != null)
        {
            cameraRot = _currentFlyBy.GetComponentInChildren<CinemachineVirtualCamera>().transform.rotation.eulerAngles;
        
            Destroy(_currentFlyBy);
            _currentFlyBy = null;
            _currentFlyByPath = null;
            _currentFlyByCamera = null;
        }
        
        // set up the camera rotation
        if (cameraRot is { } nonNullCameraRot)
        {
            Vector3 goRotation = go.transform.rotation.eulerAngles;
            goRotation.y = nonNullCameraRot.y;
            go.transform.rotation = Quaternion.Euler(goRotation);
            
            
            Transform cameraTransform = go.GetComponentInChildren<CameraRotate>().transform;
            Vector3 cameraRotation = cameraTransform.rotation.eulerAngles;
            cameraRotation.x = nonNullCameraRot.x;
            cameraTransform.rotation = Quaternion.Euler(cameraRotation);

            // Vector3 prefabRot = go.transform.rotation.eulerAngles;
            // prefabRot.y = nonNullCameraRot.y;
            // go.transform.rotation = Quaternion.Euler(prefabRot);
        }
        
        // re-enable the mesh (not really important)
        foreach (var meshRenderer in renderers)
        {
            meshRenderer.enabled = true;
        }

        // logic behind respawning the player
        _playerController = go.GetComponent<PlayerController>();
        playerLifeStatus = PlayerLifeStatus.ALIVE;
        _playerItems = _playerController.GetComponent<PlayerItems>();
        _builder = _playerController.GetComponent<Builder>();
        deathAnimationProgression = 0;
        _gameBeginTime = Time.time;
        _gameOverTime = 0;
        
        
        _overlayManager.Show();
        _healthBar.Show();
        _playerItems.Show();
    }

    void SaveScore(SaveData data)
    {
        Debug.Log("Saving");
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/scores.blob";

        SaveData[] previousData = {};
        
        if (File.Exists(path))
        {
            FileStream inFileStream = new FileStream(path, FileMode.Open);
            previousData = (SaveData[]) formatter.Deserialize(inFileStream);
            inFileStream.Close();
        }

        SaveData[] newData = previousData.Concat(new [] {data}).ToArray();
        
        Debug.Log("New data:");
        foreach (var saveData in newData)
        {
            Debug.Log(saveData);
        }

        FileStream outFileStream = new FileStream(path, FileMode.Create);
        
        formatter.Serialize(outFileStream, newData);
        
        outFileStream.Close();
        Debug.Log("Finished saving");
    }

    public SaveData[] LoadScores()
    {
        Debug.Log("Loading");
        string path = Application.persistentDataPath + "/scores.blob";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            
            FileStream inFileStream = new FileStream(path, FileMode.Open);
            var data = (SaveData[]) formatter.Deserialize(inFileStream);
            inFileStream.Close();
            
            return data;
        }
        
        SaveData[] ret = {};
        return ret;
    }

    public void LoadMainMenu()
    {
        KillPlayerImmediate();
        
        deathAnimationProgression = 0;
        _gameBeginTime = 0;
        _gameOverTime = 0;
        
        StopCoroutine(StartNewMapCoroutine());
        SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
    }

    public void KillPlayerImmediate()
    {
        // if hasn't been registered as a kill, change that
        if (_gameOverTime == 0f) _gameOverTime = Time.time;
        // Instantly kill
        playerLifeStatus = PlayerLifeStatus.NOT_IN_GAME;
        
        if(_playerController != null) Destroy(_playerController.gameObject);
        _playerController = null;
        
        CameraRotate.UnlockCursor();
    }
    
    public void KillPlayer(float damagePower, bool externalSource=false)
    {
        if (playerLifeStatus != PlayerLifeStatus.ALIVE) return;
        
        _gameOverTime = Time.time;

        playerLifeStatus = PlayerLifeStatus.DEAD;
        _isDeathAnimationOver = false;
        
        // guaranteed to not be not a number
        float scaledDamagePower = Mathf.Log(Mathf.Abs(damagePower) + 1);
        
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
        
        // If died of fall damage, apply a random rotation
        if (lastDamageType == DamageType.FALL_DAMAGE)
        {
            float magnitude = ragdollRotationEffectStrength * scaledDamagePower;
            
            rb.angularVelocity += new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)) * magnitude;
        }
        
        SaveScore(new SaveData(_gameOverTime - _gameBeginTime));
    }
}
