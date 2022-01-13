using System;
using System.Numerics;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Purchasing.Extension;
using Vector3 = UnityEngine.Vector3;

struct UserInput {
    public Vector3 DesiredDirection;
    public bool isJumping;
}

[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 1; 
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f;
    
    public float velEpsilon = 0.05f; 
    public float inputEpsilon = 0.05f; 
    
     public float highSpeedSlowingDownRate = 3f; 
     
     public float groundAcceleration = 30.0f;
     public float groundActiveStoppingRate = 4.0f;
     public float groundFrictionRate = 10.0f;

     public float groundCheckSphereDisplacement = 1.0f;
     public float groundCheckSphereRadius = 1.0f;

     [Range(0.0f, 1.0f)]
     public float airAccelerationFraction = 0.5f;
     [Range(0.0f, 1.0f)]
     public float airFrictionRateFraction = 0.5f;
     
     public float coyoteTime = 0.5f;

     private float lastTimeOnGround;

     private NetworkVariable<UserInput> userInput =
        new NetworkVariable<UserInput>(NetworkVariableReadPermission.Everyone);

    private float previousJumpSecs;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (IsServer)
        {
            var cameraRotate = GetComponentInChildren<CameraRotate>();
            if (cameraRotate != null)
            {
                cameraRotate.SetMainCamera();
                cameraRotate.SetTarget(gameObject.transform);
            }
        }
    }

    private bool isOnTheFloor()
    {
        int allLayersButPlayers = 1 << 6;
        allLayersButPlayers = ~allLayersButPlayers;
        
        // RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        // if (Physics.CheckSphere(transform.position + Vector3.down * groundCheckSphereDisplacement, groundCheckSphereRadius, allLayersButPlayers))
        // {
        //     Debug.DrawRay(transform.position + Vector3.down * groundCheckSphereDisplacement, Vector3.down * groundCheckSphereRadius, Color.yellow);
        //     Debug.DrawRay(transform.position + Vector3.down * groundCheckSphereDisplacement, Vector3.left * groundCheckSphereRadius, Color.yellow);
        //     Debug.Log("Hit");
        // }
        // else
        // {
        //     // debug.drawray(transform.position + vector3.down * groundcheckspheredisplacement, vector3.down * groundchecksphereradius, color.yellow);
        //     // debug.drawray(transform.position + vector3.down * groundcheckspheredisplacement, vector3.left * groundchecksphereradius, color.yellow);
        //     // debug.log("did not hit");
        // }
        
        // TODO: better ground detection and slopes

        bool isOnTheFloor = Physics.CheckSphere(transform.position + Vector3.down * groundCheckSphereDisplacement, groundCheckSphereRadius, allLayersButPlayers);
        if (isOnTheFloor) lastTimeOnGround = Time.fixedTime;
        
        return isOnTheFloor;
    }

    private float TweakMovementVelocity(float input, float vel, bool isInAir)
    {
        if (Mathf.Abs(input) <= inputEpsilon)
            return 0;
        if (Mathf.Abs(vel) <= velEpsilon)
            return input;
        if (Mathf.Sign(input) == Mathf.Sign(vel))
            return input;
        
        // trying to stop quickly
        if (isInAir) return input;

        return input * groundActiveStoppingRate;
    }

    private float CalculateFriction(float friction, float vel)
    {
        float proposedNewVel = vel - Mathf.Sign(vel) * friction * Time.fixedDeltaTime;
        if (Mathf.Sign(proposedNewVel) == Mathf.Sign(vel)) return proposedNewVel;
        
        return 0;
    }

    private void FixedUpdate()
    {
        if (IsServer && IsOwner)
        {
            bool isInAir = !isOnTheFloor();
            bool isInAirWithCoyoteTime = isInAir && lastTimeOnGround + coyoteTime < Time.fixedTime;
            
            float acceleration = groundAcceleration;
            float frictionRate = groundFrictionRate;
            if (isInAirWithCoyoteTime)
            {
                acceleration *= airAccelerationFraction;
                frictionRate *= airFrictionRateFraction;
            }
            
            Vector3 initHorizontalVel = rb.velocity;
            Vector3 finalVelocity = rb.velocity;
            initHorizontalVel.y = 0;

            // "relative" means relative to transform
            Vector3 horizontalVelRelative = transform.InverseTransformDirection(initHorizontalVel);
            Vector3 scaledDesiredDirectionRelative = userInput.Value.DesiredDirection;
            scaledDesiredDirectionRelative.y = 0;
            if (scaledDesiredDirectionRelative.sqrMagnitude > 1)
            {
                scaledDesiredDirectionRelative.Normalize();
            }
            
            scaledDesiredDirectionRelative = new Vector3(TweakMovementVelocity(scaledDesiredDirectionRelative.x, horizontalVelRelative.x, isInAirWithCoyoteTime), 0,TweakMovementVelocity(scaledDesiredDirectionRelative.z, horizontalVelRelative.z, isInAirWithCoyoteTime));
            
            Vector3 velocityChangeRelative = scaledDesiredDirectionRelative * acceleration;
            Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);
            
            // TODO: only when in air and pressing keys, slow down if moving faster than max speed in that direction
            // TODO: in that case, do not apply that direction at all
            
            // TODO: do something about not being able to strafe while moving full speed forwards
            finalVelocity += velocityChange * Time.fixedDeltaTime;
            // NOTE: slow down if already past the speed limit
            if (!isInAirWithCoyoteTime)
            {
                if (initHorizontalVel.magnitude > movementSpeed)
                {
                    finalVelocity = initHorizontalVel.normalized * Mathf.Lerp(initHorizontalVel.magnitude, movementSpeed, highSpeedSlowingDownRate * Time.fixedDeltaTime);
                }
            }

            finalVelocity.y = rb.velocity.y;
            rb.velocity = finalVelocity;

            // time for drag
            rb.velocity = new Vector3(CalculateFriction(frictionRate, rb.velocity.x), rb.velocity.y, CalculateFriction(frictionRate, rb.velocity.z));
            // TODO: drag outside of the air

            // limit the speed
            if (userInput.Value.isJumping)
            {
                bool canJump = !isInAirWithCoyoteTime && Time.fixedTime - previousJumpSecs >= jumpCooldownSecs;
                if (canJump)
                {
                    // Debug.Log($"Proposed coyote time: {Time.fixedTime - lastTimeOnGround}s");
                    // jump
                    
                    // disable coyote time
                    lastTimeOnGround = -10;
                    
                    Vector3 vel = rb.velocity;
                    vel.y = jumpVelocity;
                    rb.velocity = vel;
                    
                    previousJumpSecs = Time.fixedTime;
                }
            }
        }
    }

    void Update()
    {
        if (IsClient)
        {
            // "c_" stands for "client"
            UserInput c_userInput = new UserInput();
            c_userInput.DesiredDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

            c_userInput.isJumping = Input.GetButton("Jump");

            // maybe don't update it every tick?
            userInput.Value = c_userInput;
        }
    }
}
