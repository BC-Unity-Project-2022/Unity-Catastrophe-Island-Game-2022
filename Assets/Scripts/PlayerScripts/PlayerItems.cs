using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayerScripts
{
    public class PlayerItems : MonoBehaviour
    {
        [SerializeField] private GameObject axePrefab;

        private Camera _mainCamera;
        private Camera _heldItemCamera;
        private Transform _hand;
        
        private GameManager _gameManager;
        private Builder _builder;

        private GameObject _heldItem;

        public void Hide()
        {
            if(_heldItem != null) Destroy(_heldItem);
            _heldItem = null;
        }

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
            _builder = GetComponent<Builder>();
            _heldItemCamera = GameObject.FindGameObjectWithTag("HeldItemCamera").GetComponent<Camera>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            _hand = _heldItemCamera.transform.GetChild(0);

            if (_hand.gameObject.name != "Hand")
                throw new Exception("A wrong gameobject has been referenced as the hand");
        }

        private void Update()
        {
            if (_gameManager.playerLifeStatus != PlayerLifeStatus.ALIVE)
            {
                Hide();
                return;
            }
            if (_builder.state != State.NOT_BUILDING) return;
            // automatically give an item if none are held
            if (_heldItem == null)
            {
                _heldItem = Instantiate(axePrefab, _hand.position, _hand.rotation, _hand);
                _heldItem.layer = 8;
            }

            _heldItemCamera.transform.position = _mainCamera.transform.position;
            _heldItemCamera.transform.rotation = _mainCamera.transform.rotation;

            _heldItem.transform.position = _hand.transform.position;
            _heldItem.transform.rotation = _hand.transform.rotation;
        }
    }
}
