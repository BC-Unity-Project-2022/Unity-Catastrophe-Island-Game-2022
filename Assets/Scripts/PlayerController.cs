using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

struct UserInput {
    public Vector3 DesiredDirection;
    public bool isJumping;
}

public class PlayerController : NetworkBehaviour
{
    // public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    // public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    [SerializeField] private float movementSpeed = 1; 
    [SerializeField] private float movementSpeedChangeRate = 0.5f; // 0 to 1
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f; 

    private NetworkVariable<UserInput> userInput =
        new NetworkVariable<UserInput>();
    private Vector3 prevDisplacement;

    private float previousJumpSecs;

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (Camera.main != null)
                Camera.main.GetComponent<CameraFollow>().SetTarget(gameObject.transform);
        }
    }

    private void FixedUpdate()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Vector3 desiredDisplacement = userInput.Value.DesiredDirection; 
            desiredDisplacement.Normalize();
            desiredDisplacement *= movementSpeed * Time.fixedDeltaTime;
            
            // make it nice and smooth
            var displacement = Vector3.Lerp(prevDisplacement, desiredDisplacement, movementSpeedChangeRate);

            prevDisplacement = displacement;
            
            Rigidbody rb = GetComponent<Rigidbody>(); 
            rb.MovePosition(transform.position + displacement);

            Debug.Log(userInput.Value.isJumping);
            if (userInput.Value.isJumping)
            {
                // TODO: check if can jump
                bool canJump = true && Time.fixedTime - previousJumpSecs >= jumpCooldownSecs;
                if (canJump)
                {
                    Vector3 vel = rb.velocity;
                    vel.y += jumpVelocity;
                    rb.velocity = vel;

                    previousJumpSecs = Time.fixedTime;
                }
            }
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            // "c_" stands for "client"
            UserInput c_userInput = new UserInput();
            c_userInput.DesiredDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

            c_userInput.isJumping = Input.GetButton("Jump");

            userInput.Value = c_userInput;
        }
    }
}
