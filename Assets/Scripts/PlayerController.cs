using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

struct UserInput{
    public Vector3 DesiredDirection;
}
public class PlayerController : NetworkBehaviour
{
    // public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    // public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    private UserInput s_userInput;

    public const float WalkingSpeed = 1;

    private UserInput _userInput;

    private void FixedUpdate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        
        rb.AddForce(new Vector3(s_userInput.DesiredDirection.x, 0, s_userInput.DesiredDirection.z));
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
        // https://github.com/JetBrains/resharper-unity/wiki/Avoid-multiple-unnecessary-property-accesses
        var transform1 = transform;
        // transform1.position = position.Value;
        // transform1.rotation = rotation.Value;

        // incorporate the raw values of horizontal movement
        _userInput.DesiredDirection = transform1.forward * Input.GetAxis("Horizontal") +
                                      transform1.right * Input.GetAxis("Vertical");
        SyncUserInput();
    }
}
