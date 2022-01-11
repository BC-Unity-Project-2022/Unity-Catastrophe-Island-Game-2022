using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

struct UserInput {
    public Vector3 DesiredDirection;
}

public class PlayerController : NetworkBehaviour
{
    // public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    // public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    [SerializeField] private float movementSpeed = 1;

    private UserInput s_userInput;
    private UserInput _userInput;

    void Start()
    {
        Camera.main.GetComponent<CameraFollow>().SetTarget(gameObject.transform);
    }

    private void FixedUpdate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.MovePosition(transform.position + _userInput.DesiredDirection * movementSpeed * Time.deltaTime);
    }

    void SyncUserInput()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            ServerApplyUserInput(_userInput);
        }
        else
        {
            RequestInputUpdateServerRpc(_userInput);
        }
    }

    [ServerRpc]
    void RequestInputUpdateServerRpc(UserInput userInput)
    {
        ServerApplyUserInput(userInput);
    }

    void ServerApplyUserInput(UserInput userInput)
    {

        s_userInput = userInput;

        // position.Value += new Vector3(userInput.DesiredDirection.x, 0, userInput.DesiredDirection.z);
    }

    void Update()
    {
        _userInput.DesiredDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        SyncUserInput();
    }
}
