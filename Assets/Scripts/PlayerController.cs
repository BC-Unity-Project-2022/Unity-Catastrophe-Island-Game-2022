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
    private float lastActiveStoppingTime;

    private PhysicMaterial physMat;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        physMat = new PhysicMaterial();
        physMat.bounciness = 0;
        physMat.dynamicFriction = 0;
        physMat.staticFriction = 0;
        physMat.bounceCombine = PhysicMaterialCombine.Minimum;
        physMat.frictionCombine = PhysicMaterialCombine.Minimum;

        GetComponent<CapsuleCollider>().sharedMaterial = physMat;
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
        if (velRelativeToInput < -velEpsilon || lastActiveStoppingTime + fastAccelerationTimeAfterActiveStopping >= Time.fixedTime)
            input *= groundActiveStoppingRate;
        
        return input * acceleration * Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        if (IsServer && IsOwner)
        {
            bool isInAir = !isOnTheFloor() && (lastTimeOnGround + coyoteTime < Time.fixedTime);

            Vector3 initVelocityCache = rb.velocity;
            Vector3 initHorizontalVel = initVelocityCache;
            initHorizontalVel.y = 0;
            Vector3 newHorizontalVel = initHorizontalVel;

            Vector3 horizontalVelRelativeToTransform = transform.InverseTransformDirection(initHorizontalVel);
            
            Vector3 scaledDesiredDirectionRelativeToTransform = userInput.Value.DesiredDirection;
            scaledDesiredDirectionRelativeToTransform.y = 0;
            // put a speed cap as a protection against cheaters
            if (scaledDesiredDirectionRelativeToTransform.sqrMagnitude > 1)
                scaledDesiredDirectionRelativeToTransform.Normalize();

            // make sure that physics doesn't go wonky on slopes when stationary
            physMat.staticFriction = scaledDesiredDirectionRelativeToTransform == Vector3.zero ? 1 : 0;
            
            // try to counteract velocity to move in the desired dir
            Vector3 desiredVelocityChangeDirectionRelativeToTransform =
                (scaledDesiredDirectionRelativeToTransform * movementSpeed - horizontalVelRelativeToTransform).normalized * scaledDesiredDirectionRelativeToTransform.magnitude;
            
            // normally, it is the same as the normalised desired direction. If the forward direction is Vector3.Zero, then it is transform.forward
            Vector3 forwardVector;
            Vector3 rightVector;
            if (scaledDesiredDirectionRelativeToTransform == Vector3.zero)
            {
                var transformCache = transform;
                forwardVector = transformCache.forward;
                rightVector = transformCache.right;
            }
            else
            {
                forwardVector =  scaledDesiredDirectionRelativeToTransform.normalized;
                // a horizontal vector perpendicular to the normalisedDesiredDirectionRelative
                // rotate 90 degrees
                 rightVector = new Vector3(forwardVector.z, 0, -forwardVector.x);
            }
            
            float velocityRelativeToDesiredDirectionX = Vector3.Dot(forwardVector, horizontalVelRelativeToTransform);
            // float velocityRelativeToDesiredDirectionZ = Vector3.Dot(rightVector, horizontalVelRelativeToTransform);

            // Make sure that we come to stop quickly
            // NOTE: we are not giving velocityRelativeToDesiredDirectionZ to the function because that doesn't make much sense
            Vector3 velocityChangeRelative = new Vector3(TweakVelocityChange(desiredVelocityChangeDirectionRelativeToTransform.x, velocityRelativeToDesiredDirectionX, horizontalVelRelativeToTransform.x, lastActiveStoppingTime, isInAir), 0,TweakVelocityChange(desiredVelocityChangeDirectionRelativeToTransform.z, 0, horizontalVelRelativeToTransform.z, 0, isInAir));

            // if negligible velocity, assume we are stopped and record that time
            if (Mathf.Abs(velocityRelativeToDesiredDirectionX) < velEpsilon) lastActiveStoppingTime = Time.fixedTime;
            // if (Mathf.Abs(velocityRelativeToDesiredDirectionZ) + velEpsilon < 0) lastActiveStoppingTime.y = Time.fixedTime;
            
            Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);
            
            // TODO: a more consistent feel to moving opposite of your velocity - is it not working if not at top v?
            newHorizontalVel += velocityChange;

            // if travelling at max speed, clamp to it
            if (IsVelocityPassingThreshold(movementSpeed * movementSpeed, initHorizontalVel.sqrMagnitude, newHorizontalVel.sqrMagnitude))
            {
                newHorizontalVel = newHorizontalVel.normalized * movementSpeed;
            }

            // apply the velocity
            Vector3 newVelocity = newHorizontalVel;
            newVelocity.y = rb.velocity.y;
            rb.velocity = newVelocity;

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
