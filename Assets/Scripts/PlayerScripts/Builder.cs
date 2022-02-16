using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PlayerScripts
{
    [Serializable]
    public struct Building
    {
        public GameObject prefab;
    }

    struct HologramPlacementResult
    {
        public Vector3 position;
        public bool isOnGround;

        public HologramPlacementResult(Vector3 pos, bool isOnGround)
        {
            this.position = pos;
            this.isOnGround = isOnGround;
        }
    }

    enum State
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
        [SerializeField] private Material hologramShaderMaterial;
        private State _state = State.NOT_BUILDING;

        private Camera _camera;
        private GameObject _buildingGameObject;

        private int _groundLayerMask = 1 << 7;

        private float _buildingRotation;
        private Building _selectedBuilding;

        private void Start()
        {
            _camera = FindObjectOfType<Camera>();
        }

        public void Hide()
        {
            _state = State.NOT_BUILDING;
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

        HologramPlacementResult FindHologramPlacement()
        {
            RaycastHit hit;
            var cameraTransform = _camera.transform;
            Vector3 initialPos = cameraTransform.position;
            Vector3 lookDir = cameraTransform.forward;
            Ray ray = new Ray(initialPos, lookDir);

            bool isOnGround = Physics.Raycast(ray, out hit, maxDistanceToBuildingInitialPlacingSpot, _groundLayerMask);

            if (isOnGround)
                return new HologramPlacementResult(hit.point, true);

            Vector3 finalPos = initialPos + maxDistanceToBuildingInitialPlacingSpot * lookDir;
            
            // raycast vertically down
            Physics.Raycast(new Ray(finalPos, Vector3.down), out hit, Mathf.Infinity, _groundLayerMask);

            return new HologramPlacementResult(hit.point, false);
        }
        void Update()
        {
            // rotate
            if (Input.GetKey("[")) _buildingRotation += rotationSpeed * Time.deltaTime;
            if (Input.GetKey("]")) _buildingRotation -= rotationSpeed * Time.deltaTime;
            
            // close the menu
            if (_state != State.NOT_BUILDING && 
                (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape)))
            {
                Debug.Log("Escaped the menu");
                _state = State.NOT_BUILDING;
                return;
            }
            
            switch (_state)
            {
                case State.NOT_BUILDING:
                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        // Enter the building menu
                        _state = State.IN_BUILDING_MENU;
                        Debug.Log("In the building menu");
                    }
                    break;
                case State.IN_BUILDING_MENU:
                    int numberKeyPressed = GetNumberKeyPressed();
                    if (numberKeyPressed == -1)
                        break;
                    if (numberKeyPressed <= buildings.Length)
                    {
                        Debug.Log("Showing a hologram");
                        _selectedBuilding = buildings[numberKeyPressed - 1];
                        
                        // spawn a hologram
                        
                        // find a place to spawn one in
                        HologramPlacementResult newBuildingPlacement = FindHologramPlacement();
                        _buildingGameObject =
                            Instantiate(_selectedBuilding.prefab, newBuildingPlacement .position, Quaternion.identity);
                        
                        // remove all the colliders
                        foreach (var collider in _buildingGameObject.GetComponentsInChildren<MeshCollider>())
                            collider.enabled = false;
                        
                        foreach (var collider in _buildingGameObject.GetComponentsInChildren<BoxCollider>())
                            collider.enabled = false;
                        
                        // apply the shader
                        // see this for more details https://answers.unity.com/questions/124794/how-to-replace-materials-in-the-materials-array.html
                        foreach (var renderer in _buildingGameObject.GetComponentsInChildren<MeshRenderer>())
                            {
                                int materialNum = renderer.materials.Length;
                                Material[] newMats = new Material[materialNum];
                                for (int i = 0; i < materialNum; i++) newMats[i] = hologramShaderMaterial;
                                renderer.materials = newMats;
                            }
                        
                        _state = State.SHOWING_A_HOLOGRAM;
                        // reset the rotation
                        _buildingRotation = 0.0f;
                    }
                    break;
                case State.SHOWING_A_HOLOGRAM:
                    
                    HologramPlacementResult placement = FindHologramPlacement();
                    
                    _buildingGameObject.transform.position = placement.position;
                    Vector3 rot = _buildingGameObject.transform.rotation.eulerAngles;
                    rot.y = _buildingRotation;
                    Quaternion rotQuaternion = Quaternion.Euler(rot);
                    _buildingGameObject.transform.rotation = rotQuaternion;

                    // place the building
                    if (Input.GetMouseButtonDown(0))
                    {
                        Destroy(_buildingGameObject);
                        var go = Instantiate(_selectedBuilding.prefab, placement.position, rotQuaternion);
                        _state = State.NOT_BUILDING;
                    }
                        
                    break;
            }
        }
    }
}