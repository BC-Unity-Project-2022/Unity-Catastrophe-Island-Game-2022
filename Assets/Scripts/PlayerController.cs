using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

struct ColliderShapeParams
{
    public float height;
    public float verticalDisplacement;
}
struct UserInput {
    public Vector3 DesiredDirection;
    public bool isJumping;
    public bool isCrouching;
}

[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float baseMovementSpeed = 1;
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCrouchVelocity = 15f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f;

    [SerializeField] private float velEpsilon = 0.05f; 

    [SerializeField] private float groundAcceleration = 30.0f;
    [SerializeField] private float groundActiveStoppingAccelerationMultiplier = 4.0f;
    [SerializeField] private float groundPassiveStoppingAccelerationMultiplier = 2.0f;
    
    [SerializeField] private float forwardMovementMovementSpeedMultiplier = 1.5f;

    [SerializeField] private float fastAccelerationTimeAfterActiveStopping = 1.0f;

    [SerializeField] private float groundCheckSphereDisplacement = 1.0f;
    [SerializeField] private float groundCheckSphereRadius = 1.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float airAccelerationMultiplier = 0.5f;

    [SerializeField] private float coyoteTime = 0.5f;
    
    [SerializeField] private float colliderHeight = 2;
    [SerializeField] private float colliderCrouchHeight = 1;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float crouchAccelerationMultiplier = 0.5f;
    

    private bool _crouchDisableUntilReleased = false;

    private CapsuleCollider _mainCollider;

    private float _lastTimeOnGround;

    private float _previousJumpSecs;

    private Rigidbody _rb;
    private float _lastActiveStoppingTime;

    private PhysicMaterial _physMat;

    private bool _isCrouching = false;

    private ColliderShapeParams _colliderShapeParams;
    private int allLayersButPlayers = ~(1 << 6);

    private void OnEnable()
    {
        if (IsClient && IsOwner)
        {
            _rb = GetComponent<Rigidbody>();
            _physMat = new PhysicMaterial
            {
                bounciness = 0,
                dynamicFriction = 0,
                staticFriction = 0,
                bounceCombine = PhysicMaterialCombine.Minimum,
                frictionCombine = PhysicMaterialCombine.Minimum
            };

            _mainCollider = GetComponent<CapsuleCollider>();
            _mainCollider.sharedMaterial = _physMat;

            _colliderShapeParams = GetColliderShapeParams(false, false);
        }
    }

    private ColliderShapeParams GetColliderShapeParams(bool useCrouchingCollider, bool isInAir)
    {
        var ret = new ColliderShapeParams();
        if (useCrouchingCollider)
        { 
            // spawn it in th middle if in air
            ret.verticalDisplacement= isInAir ? 0 :  (colliderCrouchHeight - colliderHeight) / 2;
            ret.height = colliderCrouchHeight;
        }
        else
        {
            ret.height = colliderHeight;
            ret.verticalDisplacement = 0f;
        }
        return ret;
    }

    private bool isOnTheFloor()
    {
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
        if (isOnTheFloor) _lastTimeOnGround = Time.fixedTime;
        
        return isOnTheFloor;
    }

    // make the movement feel nicer
    private float ProposeAccelerationMultiple(float rawInput, bool isInAir, float velRelativeToDesiredDirection, float lastActiveStoppingTime)
    {
        float acceleration = ProposeAccelerationMultiple(rawInput, isInAir);
        
        // Debug.Log(velRelativeToDesiredDirection);
        // increase the speed if moving in the opposite direction
        // if (velRelativeToDesiredDirection < -velEpsilon ||
        //     lastActiveStoppingTime + fastAccelerationTimeAfterActiveStopping >= Time.fixedTime)
        // {
        //     Debug.Log(velRelativeToDesiredDirection);
        //     acceleration *= groundActiveStoppingAccelerationMultiplier;
        // }

        return acceleration;
    }

    private float ProposeAccelerationMultiple(float rawInput, bool isInAir)
    {
        float acceleration = groundAcceleration;
        if (isInAir)
            acceleration *= airAccelerationMultiplier;
        if (_isCrouching)
            acceleration *= crouchAccelerationMultiplier;
        
        // if the velocity is non-zero, but we want to stop
        if (!isInAir && rawInput == 0)
            acceleration *= groundPassiveStoppingAccelerationMultiplier;
        
        return acceleration;
    }

    private bool HandleCrouch(bool wantToCrouch, bool isInAir)
    {
            if (_crouchDisableUntilReleased)
            {
                if (wantToCrouch) wantToCrouch = false;
                else _crouchDisableUntilReleased = false;
            }

            if (_isCrouching != wantToCrouch)
            {
                // if we want to uncrouch
                if (_isCrouching && !wantToCrouch)
                {
                    // check that the full collider would work
                    Vector3 position = transform.position;

                    float radius = _mainCollider.radius;

                    Vector3 sphereCenterDisplacement = new Vector3(0, colliderHeight / 2 - radius, 0);
                    
                    if (!Physics.CheckCapsule(position + sphereCenterDisplacement, position - sphereCenterDisplacement, radius, allLayersButPlayers))
                        _isCrouching = false;
                }
                // crouching
                else
                    _isCrouching = true;
                _colliderShapeParams = GetColliderShapeParams(_isCrouching, isInAir);
            }
            
            _mainCollider.height = _colliderShapeParams.height;
            _mainCollider.center = new Vector3(0, _colliderShapeParams.verticalDisplacement, 0);
            return _isCrouching;
    }

    private void HandleWalking(Vector3 scaledDesiredDirectionRelativeToTransform, bool isInAir)
    {
        //  for scaledDesiredDirectionRelativeToTransform, z represents forwards-backwards and x is left-right
        
        
        float currentMaxMovementSpeed = baseMovementSpeed;
        // moving while crouching is slower
        if (_isCrouching) currentMaxMovementSpeed *= crouchSpeedMultiplier;

        Vector3 initHorizontalVel = _rb.velocity;
        initHorizontalVel.y = 0;
        Vector3 newHorizontalVel = initHorizontalVel;

        Vector3 horizontalVelRelativeToTransform = transform.InverseTransformDirection(initHorizontalVel);

        // speed up when we are moving forwards
        // TODO: do not modify this, modify the desired velocity change maybe?
        // TODO: the player moves faster than crouch speed if slowing down from high speeds
        if (scaledDesiredDirectionRelativeToTransform.z > 0 && Mathf.Abs(scaledDesiredDirectionRelativeToTransform.z) >
            Mathf.Abs(scaledDesiredDirectionRelativeToTransform.x))
            currentMaxMovementSpeed *= forwardMovementMovementSpeedMultiplier;

        // make sure that physics doesn't go wonky on slopes when stationary
        _physMat.staticFriction = scaledDesiredDirectionRelativeToTransform == Vector3.zero ? 1 : 0;

        Vector3 desiredVelocity = scaledDesiredDirectionRelativeToTransform * currentMaxMovementSpeed;
        // try to counteract velocity to move in the desired direction
        // "Raw" because we do not take the max velocity into account
        Vector3 desiredVelocityChangeRelativeToTransformRaw =
            desiredVelocity - horizontalVelRelativeToTransform;
        // maximal possible desired velocity change, disregarding the current velocity
        Vector3 maxVelocityChangeRelativeToTransform =
            desiredVelocityChangeRelativeToTransformRaw.normalized * currentMaxMovementSpeed;

        // do not overshoot in terms of velocity
        float desiredSpeedChange = Mathf.Min(desiredVelocityChangeRelativeToTransformRaw.magnitude, maxVelocityChangeRelativeToTransform.magnitude);
        Vector3 desiredVelocityChange = desiredVelocityChangeRelativeToTransformRaw.normalized * desiredSpeedChange;

        // normally, the forward vector is the same as the normalised desired direction. If the forward direction is Vector3.Zero, then it is transform.forward
        Vector3 forwardVector = scaledDesiredDirectionRelativeToTransform == Vector3.zero
            ? transform.forward
            : scaledDesiredDirectionRelativeToTransform.normalized;
        Vector3 rightVector = new Vector3(forwardVector.z, 0, -forwardVector.x);

        float velocityRelativeToDesiredDirectionZ = Vector3.Dot(forwardVector, horizontalVelRelativeToTransform);
        float velocityRelativeToDesiredDirectionX = Vector3.Dot(rightVector, horizontalVelRelativeToTransform);

        // Make sure that we come to stop quickly
        // NOTE: we are not giving velocityRelativeToDesiredDirectionZ to the function because that doesn't make much sense to stop quickly in that direction as the x direction is heavily favoured because we are frequently pressing keys to go in the direction of velocity, not perpendicularly to it.
        float accelerationMultipleZ = ProposeAccelerationMultiple(
            scaledDesiredDirectionRelativeToTransform.z,
            isInAir,
            velocityRelativeToDesiredDirectionZ,
            _lastActiveStoppingTime
            );
        float accelerationMultipleX = ProposeAccelerationMultiple(
            scaledDesiredDirectionRelativeToTransform.x,
            isInAir,
            velocityRelativeToDesiredDirectionX,
            _lastActiveStoppingTime
            );
        float GetDesiredVelocityChangeMultiplier(float desiredVelocityChange)
        {
            if (Mathf.Abs(desiredVelocityChange) < velEpsilon) return 0;
            return Mathf.Sign(desiredVelocityChange);
        }
        float desiredVelocityChangeX = GetDesiredVelocityChangeMultiplier(desiredVelocityChange.x) * accelerationMultipleX;
        float desiredVelocityChangeZ = GetDesiredVelocityChangeMultiplier(desiredVelocityChange.z) * accelerationMultipleZ;
        
        Vector3 velocityChangeRelative = new Vector3(desiredVelocityChangeX, 0, desiredVelocityChangeZ) * Time.fixedDeltaTime;
        
        // TODO: clamp so that we don't overshoot
        // if (velocityChangeRelative.sqrMagnitude > desiredSpeedChange * desiredSpeedChange)
        //     velocityChangeRelative =
        //         velocityChangeRelative.normalized * desiredSpeedChange;

        // if negligible velocity, assume we are stopped and record that time
        if (Mathf.Abs(velocityRelativeToDesiredDirectionZ) < velEpsilon) _lastActiveStoppingTime = Time.fixedTime;

        Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);

        newHorizontalVel += velocityChange;

        // TODO: if travelling at max speed, clamp to it
        
        // apply the velocity
        Vector3 newVelocity = newHorizontalVel;
        newVelocity.y = _rb.velocity.y;
        _rb.velocity = newVelocity;
    }

    [ServerRpc]
    private void HandleMovementServerRpc(UserInput userInput)
    {
        HandleMovement(userInput);
    }

    private void HandleMovement(UserInput userInput)
    {
        // TODO: slow down if above target velocity
        // TODO: quick stopping not working again
        // get the walking user input
        Vector3 desiredDirectionRelativeToTransform = userInput.DesiredDirection;
        desiredDirectionRelativeToTransform.y = 0;
        // put a speed cap as a protection against cheaters
        if (desiredDirectionRelativeToTransform.sqrMagnitude > 1)
            desiredDirectionRelativeToTransform.Normalize();

        bool isInAir = !isOnTheFloor() && (_lastTimeOnGround + coyoteTime < Time.fixedTime);

        HandleCrouch(userInput.isCrouching, isInAir);
        HandleWalking(desiredDirectionRelativeToTransform, isInAir);

        // jumping
        if (userInput.isJumping)
        {
            bool canJump = !isInAir && Time.fixedTime - _previousJumpSecs >= jumpCooldownSecs;
            if (canJump)
            {
                float thisJumpVelocity = jumpVelocity;
                if (_isCrouching)
                {
                    thisJumpVelocity = jumpCrouchVelocity;
                    _crouchDisableUntilReleased = true;
                }
                // Debug.Log($"Proposed coyote time: {Time.fixedTime - lastTimeOnGround}s");
                // jump
                
                // disable coyote time
                _lastTimeOnGround = -10;
                
                Vector3 vel = _rb.velocity;
                vel.y = thisJumpVelocity;
                _rb.velocity = vel;
                
                _previousJumpSecs = Time.fixedTime;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !IsClient) return;
        UserInput userInput = new UserInput
        {
            DesiredDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")),
            isJumping = Input.GetButton("Jump"),
            isCrouching = Input.GetKey(KeyCode.LeftShift)
        };
        
        if (IsServer) HandleMovement(userInput);
        else HandleMovementServerRpc(userInput);
    }

    void Update()
    {
        if (IsClient)
        {
        }
    }
}
