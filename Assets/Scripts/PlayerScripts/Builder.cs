using System;
using System.Runtime.CompilerServices;
using Building;
using UnityEngine;

namespace PlayerScripts
{
    [Serializable]
    public struct Building
    {
        public GameObject prefab;
        public int woodRequirements;
    }

    public enum State
    {
        NOT_BUILDING,
        IN_BUILDING_MENU,
        SHOWING_A_HOLOGRAM
    }


    public class Builder : MonoBehaviour
    {
        [SerializeField] private Building[] buildings;
        [SerializeField] private float maxDistanceToBuildingInitialPlacingSpot;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private Material baseHologramShaderMaterial;
        [HideInInspector] public State state = State.NOT_BUILDING;

        private GameManager _gameManager;

        private Camera _camera;
        private GameObject _buildingHologramGameObject;

        private int _groundLayerMask = 1 << 7;

        private float _buildingRotation;
        private Building _selectedBuilding;

        private Material _hologramMaterial;
        private static readonly int HologramColorPropertyId = Shader.PropertyToID("_Color");

        private PlayerItems _playerItems;

        private void CenterOnTransform(Transform parent, Transform center)
        {
            Vector3 displacement = center.position;
            for(int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.transform.GetChild(i);
                child.position -= displacement;
            }
        }

        private void Start()
        {
            _camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            _gameManager = FindObjectOfType<GameManager>();
            _playerItems = GetComponent<PlayerItems>();
            _hologramMaterial = new Material(baseHologramShaderMaterial);
        }

        public void Hide()
        {
            state = State.NOT_BUILDING;
            if(_buildingHologramGameObject) Destroy(_buildingHologramGameObject);
        }

        public void SetAllMaterials(GameObject go, Material m)
        {
            // see this for more details https://answers.unity.com/questions/124794/how-to-replace-materials-in-the-materials-array.html
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
                {
                    int materialNum = renderer.materials.Length;
                    Material[] newMats = new Material[materialNum];
                    for (int i = 0; i < materialNum; i++) newMats[i] = m;
                    renderer.materials = newMats;
                }
        }

        public void PickMaterialColor(bool isValid)
        {
            _hologramMaterial.SetColor(HologramColorPropertyId, isValid ? Color.green : Color.red);
        }
        
        /**
         * Returns -1 if no number keys were pressed, otherwise returns the key's number. Note that the "0" key is returned as 10
         */
        public int GetNumberKeyPressed()
        { 
            KeyCode[] codes =
            {
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Alpha0,
            };
            for (int i = 1; i <= 10; i++)
                if (Input.GetKeyDown(codes[i - 1]))
                    return i;
        
            return -1;
        }

        Vector3 FindHologramPlacement()
        {
            RaycastHit hit;
            var cameraTransform = _camera.transform;
            Vector3 initialPos = cameraTransform.position;
            Vector3 lookDir = cameraTransform.forward;
            Ray ray = new Ray(initialPos, lookDir);

            bool isOnGround = Physics.Raycast(ray, out hit, maxDistanceToBuildingInitialPlacingSpot, _groundLayerMask);

            if (isOnGround)
                return hit.point;

            Vector3 finalPos = initialPos + maxDistanceToBuildingInitialPlacingSpot * lookDir;
            finalPos.y = 1000;
            
            // raycast vertically down
            Physics.Raycast(new Ray(finalPos, Vector3.down), out hit, Mathf.Infinity, _groundLayerMask);

            return hit.point;
        }

        bool ValidatePlacement(GameObject go)
        {
            foreach (var groundLevelValidator in go.GetComponentsInChildren<GroundLevelValidator>())
            {
                Vector3 rayCastPos = groundLevelValidator.transform.position;

                rayCastPos.y = 1000;
                
                // raycast vertically down
                RaycastHit hit;
                bool hasHit = Physics.Raycast(new Ray(rayCastPos, Vector3.down), out hit, Mathf.Infinity, _groundLayerMask);

                if (hasHit)
                {
                    bool isBelowGround = hit.point.y > groundLevelValidator.transform.position.y;
                    if (isBelowGround && !groundLevelValidator.belowGround) return false;
                    if (!isBelowGround && groundLevelValidator.belowGround) return false;
                }
            }

            return true;
        }

        bool CheckBuildingMaterials(Building b)
        {
            return b.woodRequirements <= _playerItems.GetWoodLeft();
        }
        
        void Update()
        {
            if (_gameManager.playerLifeStatus != PlayerLifeStatus.ALIVE) return;
            
            // rotate
            if (Input.GetKey("[")) _buildingRotation += rotationSpeed * Time.deltaTime;
            if (Input.GetKey("]")) _buildingRotation -= rotationSpeed * Time.deltaTime;
            
            // close the menu
            if (state != State.NOT_BUILDING && 
                (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape)))
            {
                Hide();
                return;
            }
            
            switch (state)
            {
                case State.NOT_BUILDING:
                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        // Enter the building menu
                        state = State.IN_BUILDING_MENU;
                    }
                    break;
                case State.IN_BUILDING_MENU:
                    int numberKeyPressed = GetNumberKeyPressed();
                    if (numberKeyPressed == -1)
                        break;
                    // spawn a hologram
                    if (numberKeyPressed <= buildings.Length)
                    {
                        _selectedBuilding = buildings[numberKeyPressed - 1];
                        
                        // check if we can afford to build it
                        if (!CheckBuildingMaterials(_selectedBuilding)) return;
                        
                        // find a place to spawn one in
                        Vector3 newBuildingPlacement = FindHologramPlacement();

                        Transform center = _selectedBuilding.prefab
                            .GetComponentInChildren<BuildablePrefabCenterAnchor>().transform;
                        
                        // center the actual prefab
                        CenterOnTransform(_selectedBuilding.prefab.transform, center);
                        
                        _buildingHologramGameObject =
                            Instantiate(_selectedBuilding.prefab, newBuildingPlacement, Quaternion.identity);
                        
                        // remove all the colliders
                        foreach (var collider in _buildingHologramGameObject.GetComponentsInChildren<MeshCollider>())
                            collider.enabled = false;
                        
                        foreach (var collider in _buildingHologramGameObject.GetComponentsInChildren<BoxCollider>())
                            collider.enabled = false;
                        
                        // apply the shader
                        bool isValid = ValidatePlacement(_buildingHologramGameObject);
                        PickMaterialColor(isValid);
                        SetAllMaterials(_buildingHologramGameObject, _hologramMaterial);
                        
                        state = State.SHOWING_A_HOLOGRAM;
                        
                        // reset the rotation
                        _buildingRotation = 0.0f;
                    }
                    break;
                case State.SHOWING_A_HOLOGRAM:
                    
                    // display the hologram
                    Vector3 placement = FindHologramPlacement();
                    
                    _buildingHologramGameObject.transform.position = placement;
                    Vector3 rot = _buildingHologramGameObject.transform.rotation.eulerAngles;
                    rot.y = _buildingRotation;
                    Quaternion rotQuaternion = Quaternion.Euler(rot);
                    _buildingHologramGameObject.transform.rotation = rotQuaternion;

                    bool isPlacementValid = ValidatePlacement(_buildingHologramGameObject);
                    
                    // apply the shader
                    PickMaterialColor(isPlacementValid);
                    SetAllMaterials(_buildingHologramGameObject, _hologramMaterial);

                    // place the building
                    if (isPlacementValid && Input.GetMouseButtonDown(0))
                    {
                        // check if we can afford to build it
                        if (!CheckBuildingMaterials(_selectedBuilding)) return;
                        _playerItems.RemoveWood(_selectedBuilding.woodRequirements);
                        
                        Destroy(_buildingHologramGameObject);
                        var go = Instantiate(_selectedBuilding.prefab, placement, rotQuaternion);
                        state = State.NOT_BUILDING;
                    }
                        
                    break;
            }
        }
    }
}