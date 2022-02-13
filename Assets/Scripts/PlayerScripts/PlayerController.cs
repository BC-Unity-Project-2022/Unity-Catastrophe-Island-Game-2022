using System;
using System.Diagnostics.SymbolStore;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

struct ColliderShapeParams
{
    public float Height;
    public float VerticalDisplacement;
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform aimController;

    [SerializeField] private float baseMovementSpeed = 1;
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCrouchVelocity = 15f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f;

    [SerializeField] private float velEpsilon = 0.05f; 
    [SerializeField] private float velocityClampingEpsilon = 0.2f; 

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
    
    [SerializeField] private float colliderCrouchHeight = 1;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float crouchAccelerationMultiplier = 0.5f;
    [SerializeField] private float timeToCrouch;
    
    [SerializeField] private float maxWaterMovementSpeed;
    public Vector3 desiredDirection;
    public bool isJumping;
    public bool isCrouching;
    
    private float _crouchTolerance = 0.0001f;
    private float _colliderHeight;
    private float _colliderVerticalDisplacement;
    
    private bool _crouchDisabledUntilReleased = false;

    private CapsuleCollider _mainCollider;

    private float _lastTimeOnGround;

    private float _previousJumpSecs;

    private Rigidbody _rb;
    private float _lastActiveStoppingTime;

    private PhysicMaterial _physMat;

    // 1 means not crouching, 0 means crouching
    private float _crouchState = 1;

    private ColliderShapeParams _colliderShapeParams;
    private int allLayersButPlayers = ~(1 << 6);

    private bool isUnderWater;

    private void OnEnable()
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

            _colliderHeight = _mainCollider.height;
            _colliderVerticalDisplacement = _mainCollider.center.y;
            
            _colliderShapeParams = GetColliderShapeParams(_crouchState, false);
    }

    private ColliderShapeParams GetColliderShapeParams(float crouchState, bool isInAir)
    {
        var ret = new ColliderShapeParams();
        ret.Height = Mathf.SmoothStep(colliderCrouchHeight, _colliderHeight, crouchState);
        ret.VerticalDisplacement = Mathf.SmoothStep(isInAir ? _colliderVerticalDisplacement :  (colliderCrouchHeight - _colliderHeight) / 2, _colliderVerticalDisplacement, crouchState);
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

    private bool IsTurning(float velRelativeToDesiredDirection)
    {
        return velRelativeToDesiredDirection < -velEpsilon;
    }

    // make the movement feel nicer
    private float ProposeAccelerationMultiplier(float rawInput, bool isInAir, float velRelativeToDesiredDirection, float lastActiveStoppingTime)
    {
        float acceleration = ProposeAccelerationMultiplier(rawInput, isInAir);
        
        // increase the speed if moving in the opposite direction
        if (IsTurning(velRelativeToDesiredDirection) ||
            lastActiveStoppingTime + fastAccelerationTimeAfterActiveStopping >= Time.fixedTime
            )
        {
            acceleration *= groundActiveStoppingAccelerationMultiplier;
        }

        return acceleration;
    }

    private float ProposeAccelerationMultiplier(float rawInput, bool isInAir)
    {
        float acceleration = groundAcceleration;
        if (isInAir)
            acceleration *= airAccelerationMultiplier;
        if (_crouchState < 1)
            acceleration *= crouchAccelerationMultiplier;
        
        // if the velocity is non-zero, but we want to stop
        if (!isInAir && rawInput == 0)
            acceleration *= groundPassiveStoppingAccelerationMultiplier;
        
        return acceleration;
    }

    private void HandleCrouch(bool wantToCrouch, bool isInAir)
    {
        if (_crouchDisabledUntilReleased)
        {
            if (wantToCrouch) wantToCrouch = false;
            else _crouchDisabledUntilReleased = false;
        }

        int desiredCrouchState = wantToCrouch ? 0 : 1;

        // if we need to change
        if (Math.Abs(_crouchState - desiredCrouchState) > _crouchTolerance)
        {
            // if we want to uncrouch, check that we can do that
            if (!wantToCrouch)
            {
                // check that the full collider would work
                Vector3 position = transform.position;

                float radius = _mainCollider.radius;

                Vector3 sphereCenterDisplacement = new Vector3(0, _colliderHeight / 2 - radius, 0);

                if (!Physics.CheckCapsule(position + sphereCenterDisplacement, position - sphereCenterDisplacement,
                    radius, allLayersButPlayers))
                {
                    wantToCrouch = false;
                    desiredCrouchState = 1;
                }
            }
            
            // update the height
            if (desiredCrouchState == 1)
                _crouchState += Time.fixedDeltaTime / timeToCrouch;
            else 
                _crouchState -= Time.fixedDeltaTime / timeToCrouch;
            
            _crouchState = Mathf.Clamp(_crouchState, 0, 1);
            _colliderShapeParams = GetColliderShapeParams(_crouchState, isInAir);
        }
            
        _mainCollider.height = _colliderShapeParams.Height;
        _mainCollider.center = new Vector3(0, _colliderShapeParams.VerticalDisplacement, 0);
        aimController.localPosition = _mainCollider.center;
    }

    private void HandleWalking(Vector3 scaledDesiredDirectionRelativeToTransform, bool isInAir)
    {
        //  for scaledDesiredDirectionRelativeToTransform, z represents forwards-backwards and x is left-right
        
        
        float currentMaxMovementSpeed = baseMovementSpeed;
        // moving while crouching is slower
        currentMaxMovementSpeed *= Mathf.Lerp(crouchSpeedMultiplier, 1, _crouchState);

        Vector3 initHorizontalVel = _rb.velocity;
        initHorizontalVel.y = 0;
        Vector3 newHorizontalVel = initHorizontalVel;

        Vector3 horizontalVelRelativeToTransform = transform.InverseTransformDirection(initHorizontalVel);

        // speed up when we are moving forwards
        if (scaledDesiredDirectionRelativeToTransform.z > 0 && Mathf.Abs(scaledDesiredDirectionRelativeToTransform.z) >
            Mathf.Abs(scaledDesiredDirectionRelativeToTransform.x))
            currentMaxMovementSpeed *= forwardMovementMovementSpeedMultiplier;

        // make sure that physics doesn't go wonky on slopes when stationary
        _physMat.staticFriction = scaledDesiredDirectionRelativeToTransform == Vector3.zero ? 1 : 0;

        Vector3 desiredVelocityRelative = scaledDesiredDirectionRelativeToTransform * currentMaxMovementSpeed;
        // try to counteract velocity to move in the desired direction
        // "Raw" because we do not take the max velocity into account
        Vector3 desiredVelocityChangeRelativeToTransformRaw =
            desiredVelocityRelative - horizontalVelRelativeToTransform;
        // maximal possible desired velocity change, disregarding the current velocity
        Vector3 maxVelocityChangeRelativeToTransform =
            desiredVelocityChangeRelativeToTransformRaw.normalized * currentMaxMovementSpeed;

        // do not overshoot in terms of velocity
        float desiredSpeedChange = Mathf.Min(desiredVelocityChangeRelativeToTransformRaw.magnitude, maxVelocityChangeRelativeToTransform.magnitude);
        Vector3 desiredVelocityChange = desiredVelocityChangeRelativeToTransformRaw.normalized * desiredSpeedChange;

        // normally, the forward vector is the same as the normalised desired direction. If the forward direction is Vector3.Zero, then it is transform.forward

        // NOTE: do not need desired direction X velocity because we are usually moving in the same axis as desired velocity
        float velocityRelativeToDesiredDirectionZ = 0f;
        if (scaledDesiredDirectionRelativeToTransform != Vector3.zero)
        {
            Vector3 forwardVector = scaledDesiredDirectionRelativeToTransform.normalized;
            
            velocityRelativeToDesiredDirectionZ = Vector3.Dot(forwardVector, horizontalVelRelativeToTransform);
        }

        // Make sure that we come to stop quickly
        float accelerationMultiplierZ = ProposeAccelerationMultiplier(
            scaledDesiredDirectionRelativeToTransform.z,
            isInAir,
            velocityRelativeToDesiredDirectionZ,
            _lastActiveStoppingTime
            );
        float accelerationMultiplierX = ProposeAccelerationMultiplier(
            scaledDesiredDirectionRelativeToTransform.x,
            isInAir,
            velocityRelativeToDesiredDirectionZ,
            _lastActiveStoppingTime
            );
        float GetDesiredVelocityChangeMultiplier(float desiredVelocityChangeOnAxis)
        {
            if (Mathf.Abs(desiredVelocityChangeOnAxis) < velEpsilon) return 0;
            return Mathf.Sign(desiredVelocityChangeOnAxis);
        }
        float desiredVelocityChangeX = GetDesiredVelocityChangeMultiplier(desiredVelocityChange.x) * accelerationMultiplierX;
        float desiredVelocityChangeZ = GetDesiredVelocityChangeMultiplier(desiredVelocityChange.z) * accelerationMultiplierZ;
        
        Vector3 velocityChangeRelative = new Vector3(desiredVelocityChangeX, 0, desiredVelocityChangeZ) * Time.fixedDeltaTime;
        
        if (velocityChangeRelative.sqrMagnitude > desiredSpeedChange * desiredSpeedChange)
            velocityChangeRelative =
                velocityChangeRelative.normalized * desiredSpeedChange;

        // if negligible velocity, assume we are stopped and record that time
        if (IsTurning(velocityRelativeToDesiredDirectionZ)) _lastActiveStoppingTime = Time.fixedTime;

        Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);

        newHorizontalVel += velocityChange;

        // if travelling close to the optimal velocity, clamp to it
        if ((desiredVelocityRelative - velocityChangeRelative - horizontalVelRelativeToTransform).sqrMagnitude < velocityClampingEpsilon * velocityClampingEpsilon)
            newHorizontalVel = transform.TransformDirection(desiredVelocityRelative);
        
            // apply the velocity
        Vector3 newVelocity = newHorizontalVel;
        newVelocity.y = _rb.velocity.y;
        _rb.velocity = newVelocity;
    }

    private void HandleMovement()
    {
        // get the walking user input
        Vector3 desiredDirectionRelativeToTransform = desiredDirection;
        desiredDirectionRelativeToTransform.y = 0;
        // put a speed cap as a protection against cheaters
        if (desiredDirectionRelativeToTransform.sqrMagnitude > 1)
            desiredDirectionRelativeToTransform.Normalize();

        bool isInAir = !isOnTheFloor() && _lastTimeOnGround + coyoteTime < Time.fixedTime;

        HandleCrouch(isCrouching, isInAir);
        HandleWalking(desiredDirectionRelativeToTransform, isInAir);

        // jumping
        if (isJumping)
        {
            // reset the flag
            isJumping = false;
            
            bool canJump = !isInAir;
            canJump &= Time.fixedTime - _previousJumpSecs >= jumpCooldownSecs;
            // can not be mid crouch transition
            canJump &= _crouchState < _crouchTolerance || _crouchState > 1 - _crouchTolerance;
            if (canJump)
            {
                float currentJumpVelocity = jumpVelocity;
                if (_crouchState < _crouchTolerance)
                {
                    currentJumpVelocity = jumpCrouchVelocity;
                    _crouchDisabledUntilReleased = true;
                }
                // Debug.Log($"Proposed coyote time: {Time.fixedTime - lastTimeOnGround}s");
                // jump
                
                // disable coyote time
                _lastTimeOnGround = -10;
                
                Vector3 vel = _rb.velocity;
                vel.y = currentJumpVelocity;
                _rb.velocity = vel;
                
                _previousJumpSecs = Time.fixedTime;
            }
        }
    }

    private void Update()
    {
        desiredDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        isJumping |= Input.GetButtonDown("Jump"); // This is so that we do not forget the input if multiple updates are called before the fixedUpdate
        isCrouching = Input.GetKey(KeyCode.LeftShift);
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ocean")) isUnderWater = true;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ocean")) isUnderWater = false;
    }
}
