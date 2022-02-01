using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
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
    [SerializeField] private float movementSpeed = 1; 
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCrouchVelocity = 15f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f;

    [SerializeField] private float velEpsilon = 0.05f; 

    [SerializeField] private float groundAcceleration = 30.0f;
    [SerializeField] private float groundActiveStoppingRate = 4.0f;
    [SerializeField] private float groundPassiveStopping = 1.0f;
    
    [SerializeField] private float forwardMovementAccelerationMultiplier = 1.5f;
    [SerializeField] private float forwardMovementMovementSpeedMultiplier = 1.5f;

    [SerializeField] private float fastAccelerationTimeAfterActiveStopping = 1.0f;

    [SerializeField] private float groundCheckSphereDisplacement = 1.0f;
    [SerializeField] private float groundCheckSphereRadius = 1.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float airAccelerationFraction = 0.5f;

    [SerializeField] private float coyoteTime = 0.5f;
    
    [SerializeField] private float colliderHeight = 2;
    [SerializeField] private float colliderCrouchHeight = 1;

    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
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
        if (_isCrouching)
            acceleration *= crouchAccelerationMultiplier;
        
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

    private void HandleCrouch(bool wantToCrouch, bool isInAir)
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
        bool isInAir = !isOnTheFloor() && (_lastTimeOnGround + coyoteTime < Time.fixedTime);

        HandleCrouch(userInput.isCrouching, isInAir);

        Vector3 initVelocityCache = _rb.velocity;
        Vector3 initHorizontalVel = initVelocityCache;
        initHorizontalVel.y = 0;
        Vector3 newHorizontalVel = initHorizontalVel;

        Vector3 horizontalVelRelativeToTransform = transform.InverseTransformDirection(initHorizontalVel);
        
        Vector3 scaledDesiredDirectionRelativeToTransform = userInput.DesiredDirection;
        scaledDesiredDirectionRelativeToTransform.y = 0;
        // put a speed cap as a protection against cheaters
        if (scaledDesiredDirectionRelativeToTransform.sqrMagnitude > 1)
            scaledDesiredDirectionRelativeToTransform.Normalize();

        // speed up when we are moving forwards
        if (scaledDesiredDirectionRelativeToTransform.z > 0)
            scaledDesiredDirectionRelativeToTransform.z *= forwardMovementAccelerationMultiplier;

        // make sure that physics doesn't go wonky on slopes when stationary
        _physMat.staticFriction = scaledDesiredDirectionRelativeToTransform == Vector3.zero ? 1 : 0;
        
        // try to counteract velocity to move in the desired dir
        Vector3 desiredVelocityChangeDirectionRelativeToTransform =
            (scaledDesiredDirectionRelativeToTransform * movementSpeed - horizontalVelRelativeToTransform).normalized * scaledDesiredDirectionRelativeToTransform.magnitude;
        
        // normally, the forward vector is the same as the normalised desired direction. If the forward direction is Vector3.Zero, then it is transform.forward
        Vector3 forwardVector;
        // Vector3 rightVector;
        if (scaledDesiredDirectionRelativeToTransform == Vector3.zero)
            forwardVector = transform.forward;
        else
            forwardVector =  scaledDesiredDirectionRelativeToTransform.normalized;
        
        float velocityRelativeToDesiredDirectionX = Vector3.Dot(forwardVector, horizontalVelRelativeToTransform);

        // Make sure that we come to stop quickly
        // NOTE: we are not giving velocityRelativeToDesiredDirectionZ to the function because that doesn't make much sense to stop quickly in that direction as the x direction is heavily favoured because we are frequently pressing keys to go in the direction of velocity, not perpendicularly to it.
        Vector3 velocityChangeRelative = new Vector3(TweakVelocityChange(desiredVelocityChangeDirectionRelativeToTransform.x, velocityRelativeToDesiredDirectionX, horizontalVelRelativeToTransform.x, _lastActiveStoppingTime, isInAir), 0,TweakVelocityChange(desiredVelocityChangeDirectionRelativeToTransform.z, 0, horizontalVelRelativeToTransform.z, 0, isInAir));
        
        // if negligible velocity, assume we are stopped and record that time
        if (Mathf.Abs(velocityRelativeToDesiredDirectionX) < velEpsilon) _lastActiveStoppingTime = Time.fixedTime;
        
        Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);
        
        newHorizontalVel += velocityChange;

        // if travelling at max speed, clamp to it
        
        // make sure that if travelling forward, the max speed is larger
        float currentMaxMovementSpeed = movementSpeed;
        if (scaledDesiredDirectionRelativeToTransform.x == 0 && scaledDesiredDirectionRelativeToTransform.z > 0)
            currentMaxMovementSpeed *= forwardMovementMovementSpeedMultiplier;
        if (_isCrouching) currentMaxMovementSpeed *= crouchSpeedMultiplier;

        if (IsVelocityPassingThreshold(currentMaxMovementSpeed * currentMaxMovementSpeed, initHorizontalVel.sqrMagnitude, newHorizontalVel.sqrMagnitude))
            newHorizontalVel = newHorizontalVel.normalized * currentMaxMovementSpeed;
        
        // apply the velocity
        Vector3 newVelocity = newHorizontalVel;
        newVelocity.y = _rb.velocity.y;
        _rb.velocity = newVelocity;

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
