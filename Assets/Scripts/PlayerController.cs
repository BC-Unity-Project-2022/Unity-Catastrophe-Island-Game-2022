using System;
using System.Numerics;
using Newtonsoft.Json.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Purchasing.Extension;
using Vector2 = UnityEngine.Vector2;
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
    
     public float groundAcceleration = 30.0f;
     public float groundActiveStoppingRate = 4.0f;
     public float groundPassiveStopping = 1.0f;
     
     public float fastAccelerationTimeAfterActiveStopping = 1.0f;

     public float groundCheckSphereDisplacement = 1.0f;
     public float groundCheckSphereRadius = 1.0f;

     [Range(0.0f, 1.0f)]
     public float airAccelerationFraction = 0.5f;
     
     public float coyoteTime = 0.5f;

     private float lastTimeOnGround;

     private NetworkVariable<UserInput> userInput =
        new NetworkVariable<UserInput>(NetworkVariableReadPermission.Everyone);

    private float previousJumpSecs;

    private Rigidbody rb;
    private Vector2 lastActiveStoppingTime;

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

    private bool IsVelocityPassingThreshold(float threshold, float before, float after)
    {
        return after + velEpsilon > threshold && before <= threshold + velEpsilon;
    }
    
    // make the movement feel nicer
    private float TweakVelocityChange(float input, float velRelativeToInput, float velRelativeToTransform, float lastActiveStoppingTime, bool isInAir)
    {
        float acceleration = groundAcceleration;
        if (isInAir)
            acceleration *= airAccelerationFraction;
        
        // if no input in this direction, but still moving there
        if (!isInAir && input == 0)
        {
            // Mathf.sign(0) == 1
            if (Mathf.Abs(velRelativeToTransform) < velEpsilon) return 0;
            
            float ret = -Mathf.Sign(velRelativeToTransform) * groundPassiveStopping * Time.fixedDeltaTime;
            // do not overshoot
            if (IsVelocityPassingThreshold(0, velRelativeToTransform, velRelativeToTransform + ret))
            {
                return -velRelativeToTransform;
            }
            return ret;
        }
        
        // increase the speed if moving in the opposite direction
        if (velRelativeToInput < 0 || lastActiveStoppingTime + fastAccelerationTimeAfterActiveStopping >= Time.fixedTime)
        {
            input *= groundActiveStoppingRate;
        }
        
        return input * acceleration * Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        if (IsServer && IsOwner)
        {
            bool isInAir = !isOnTheFloor() && (lastTimeOnGround + coyoteTime < Time.fixedTime);
            
            Vector3 initHorizontalVel = rb.velocity;
            Vector3 finalVelocity = initHorizontalVel;
            initHorizontalVel.y = 0;

            // "relative" means relative to transform
            Vector3 horizontalVelRelative = transform.InverseTransformDirection(initHorizontalVel);
            Vector3 scaledDesiredDirectionRelative = userInput.Value.DesiredDirection;
            
            scaledDesiredDirectionRelative.y = 0;

            if (scaledDesiredDirectionRelative.sqrMagnitude > 1)
            {
                scaledDesiredDirectionRelative.Normalize();
            }

            scaledDesiredDirectionRelative =
                (scaledDesiredDirectionRelative * movementSpeed - horizontalVelRelative).normalized * scaledDesiredDirectionRelative.magnitude;

            // normally, it is the same as the normalised desired direction. If the forward direction is Vector3.Zero, then it is transform.forward
            Vector3 forwardVector;
            Vector3 rightVector;
            if (scaledDesiredDirectionRelative == Vector3.zero)
            {
                forwardVector = transform.forward;
                rightVector = transform.right;
            }
            else
            {
                forwardVector =  scaledDesiredDirectionRelative.normalized;
                // a horizontal vector perpendicular to the normalisedDesiredDirectionRelative
                // rotate 90 degrees
                 rightVector = new Vector3(forwardVector.z, 0, -forwardVector.x);
            }
            
            // TODO: make it so that it doesn't wiggle when on a slope

            float velocityRelativeToDesiredDirectionX = Vector3.Dot(forwardVector, horizontalVelRelative);
            float velocityRelativeToDesiredDirectionZ = Vector3.Dot(rightVector, horizontalVelRelative);
            
            // Make sure that we come to stop quickly
            Vector3 velocityChangeRelative = new Vector3(TweakVelocityChange(scaledDesiredDirectionRelative.x, velocityRelativeToDesiredDirectionX, horizontalVelRelative.x, lastActiveStoppingTime.x, isInAir), 0,TweakVelocityChange(scaledDesiredDirectionRelative.z, velocityRelativeToDesiredDirectionZ, horizontalVelRelative.z, lastActiveStoppingTime.y, isInAir));

            if (velocityRelativeToDesiredDirectionX + velEpsilon < 0) lastActiveStoppingTime.x = Time.fixedTime;
            if (velocityRelativeToDesiredDirectionZ + velEpsilon < 0) lastActiveStoppingTime.y = Time.fixedTime;
            
            Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);
            
            // TODO: only when in air and pressing keys, slow down if moving faster than max speed in that direction
            // TODO: in that case, do not apply that direction at all
            
            // TODO: a more consistent feel to moving opposite of your velocity - is it not working if not at top v?
            finalVelocity += velocityChange;

            // if travelling at max speed, clamp to it
            // if (IsVelocityPassingThreshold(movementSpeed * movementSpeed, initHorizontalVel.sqrMagnitude, finalVelocity.sqrMagnitude))
            // {
            //     Debug.Log($"Max vel{initHorizontalVel.y}");
            //     finalVelocity = finalVelocity.normalized * movementSpeed;
            // }
            
            // apply the velocity
            finalVelocity.y = rb.velocity.y;
            rb.velocity = finalVelocity;

            // jumping
            if (userInput.Value.isJumping)
            {
                bool canJump = !isInAir && Time.fixedTime - previousJumpSecs >= jumpCooldownSecs;
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
