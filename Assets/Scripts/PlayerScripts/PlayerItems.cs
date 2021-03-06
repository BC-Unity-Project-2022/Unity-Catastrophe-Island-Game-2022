using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PlayerScripts
{
    enum AttackState
    {
        NOT_ATTACKING,
        RISING,
        FALLING
    }
    public class PlayerItems : MonoBehaviour
    {
        [SerializeField] private GameObject axePrefab;
        [SerializeField] private float risingTime;
        [SerializeField] private float fallingTime;

        [SerializeField] private float axeDamage;

        [SerializeField] private float castRadius = 0.2f;
        [SerializeField] private float castDistance = 1f;

        private Camera _mainCamera;
        private Camera _heldItemCamera;
        private Transform _hand;
        private Transform _handBeforeAttack;
        private Transform _handDuringAttack;
        
        private GameManager _gameManager;
        private Builder _builder;

        private GameObject _heldItem;

        private AttackState _attackState = AttackState.NOT_ATTACKING;
        private float _attackActionProgress;
        private int _allLayersButPlayers = ~(1 << 6);

        private int _woodLeft = 0;
        private TMP_Text _woodLeftText;

        public void Hide()
        {
            if(_heldItem != null) Destroy(_heldItem);
            _heldItem = null;
            _woodLeftText.enabled = false;
        }

        public void Show()
        {
            _woodLeftText.enabled = true;
            ResetWoodDisplay();
        }

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
            _builder = GetComponent<Builder>();
            _heldItemCamera = GameObject.FindGameObjectWithTag("HeldItemCamera").GetComponent<Camera>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            _woodLeftText = GameObject.Find("Wood text").GetComponent<TMP_Text>();
            
            _hand = _heldItemCamera.transform.GetChild(0);
            _handBeforeAttack = _heldItemCamera.transform.GetChild(1);
            _handDuringAttack = _heldItemCamera.transform.GetChild(2);

            if (_hand.gameObject.name != "Hand")
                throw new Exception("A wrong gameobject has been referenced as the hand");
            if (_handBeforeAttack.gameObject.name != "HandBeforeAttack")
                throw new Exception("A wrong gameobject has been referenced as the hand before the attack");
            if (_handDuringAttack.gameObject.name != "HandDuringAttack")
                throw new Exception("A wrong gameobject has been referenced as the hand during the attack");
        }

        [CanBeNull]
        private Collider CheckHit()
        {
            RaycastHit hit;
            if (Physics.SphereCast(_mainCamera.transform.position, castRadius, _mainCamera.transform.forward, out hit,
                castDistance, _allLayersButPlayers))
                return hit.collider;
            return null;
        }

        private void ResetWoodDisplay()
        {
            _woodLeftText.text = $"Wood: {_woodLeft}";
        }

        public int GetWoodLeft()
        {
            return _woodLeft;
        }

        public void RemoveWood(int wood)
        {
            int newWood = _woodLeft - wood;
            if (newWood < 0) throw new Exception("Tried taking away more wood that we own");
            SetWoodLeft(newWood);
        }
        private void SetWoodLeft(int wood)
        {
            _woodLeft = wood;
            ResetWoodDisplay();
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

            // follow the main camera
            _heldItemCamera.transform.position = _mainCamera.transform.position;
            _heldItemCamera.transform.rotation = _mainCamera.transform.rotation;

            // start the attack
            if (Input.GetMouseButton(0) && _attackState == AttackState.NOT_ATTACKING)
            {
                _attackState = AttackState.RISING;
                _attackActionProgress = 0f;
            }

            // change to the next state
            if (_attackActionProgress >= 1f)
            {
                _attackActionProgress = 0f;
                if (_attackState == AttackState.RISING)
                {
                    Collider hitResult = CheckHit();
                    if (hitResult != null)
                    {
                        TreeHealth treeHealthScript = hitResult.GetComponent<TreeHealth>();
                        if (treeHealthScript) 
                            if (treeHealthScript.Damage(axeDamage))
                                SetWoodLeft(_woodLeft + treeHealthScript.CalculateWood());
                    }
                    _attackState = AttackState.FALLING;
                }
                else if (_attackState == AttackState.FALLING)
                {
                    _attackState = AttackState.NOT_ATTACKING;
                    // make sure that we return to the initial position
                    _hand.position = _handBeforeAttack.position;
                    _hand.rotation = _handBeforeAttack.rotation;
                }
            }

            // actually show the attacking state
            if (_attackState != AttackState.NOT_ATTACKING)
            {
                bool isRising = _attackState == AttackState.RISING;
                Transform from = isRising ? _handBeforeAttack : _handDuringAttack;
                Transform to = isRising ? _handDuringAttack : _handBeforeAttack;

                Vector3 pos = Vector3.Slerp(from.position, to.position, _attackActionProgress);
                Quaternion rot = Quaternion.Slerp(from.rotation, to.rotation, _attackActionProgress);

                _hand.position = pos;
                _hand.rotation = rot;

                _attackActionProgress += Time.deltaTime / (isRising ? risingTime : fallingTime);
            }
        }
    }
}
