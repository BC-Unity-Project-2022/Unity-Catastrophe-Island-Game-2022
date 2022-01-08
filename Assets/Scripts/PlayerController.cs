using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

struct UserInput{
    public Vector3 DesiredDirection;
}
public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    public const float WalkingSpeed = 1;

    private UserInput _userInput;

    void SyncUserInput()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SeverApplyUserInput(_userInput);
        }
        else
        {
            RequestInputUpdateServerRpc(_userInput);
        }
    }

    [ServerRpc]
    void RequestInputUpdateServerRpc(UserInput userInput)
    {
        SeverApplyUserInput(userInput);
    }

    void SeverApplyUserInput(UserInput userInput)
    {
        position.Value += new Vector3(userInput.DesiredDirection.x, 0, userInput.DesiredDirection.z);
    }

    void Update()
    {
        // https://github.com/JetBrains/resharper-unity/wiki/Avoid-multiple-unnecessary-property-accesses
        var transform1 = transform;
        transform1.position = position.Value;
        transform1.rotation = rotation.Value;

        // incorporate the raw values of horizontal movement
        _userInput.DesiredDirection = transform1.forward * Input.GetAxis("Horizontal") +
                                      transform1.right * Input.GetAxis("Vertical");
        SyncUserInput();
    }
}
