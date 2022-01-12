using System;
using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Purchasing.Extension;
using Vector3 = UnityEngine.Vector3;

struct UserInput {
    public Vector3 DesiredDirection;
    public bool isJumping;
}

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
     

     [Range(0.0f, 1.0f)]
     public float airAccelerationFraction = 0.5f;
     [Range(0.0f, 1.0f)]
     public float airFrictionRateFraction = 0.5f;

     private NetworkVariable<UserInput> userInput =
        new NetworkVariable<UserInput>();

    private float previousJumpSecs;

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (Camera.main != null)
                Camera.main.GetComponent<CameraFollow>().SetTarget(gameObject.transform);
        }
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

    private float ApplyFriction(float friction, float vel)
    {
        float proposedNewVel = vel - Mathf.Sign(vel) * friction * Time.fixedDeltaTime;
        if (Mathf.Sign(proposedNewVel) == Mathf.Sign(vel)) return proposedNewVel;
        
        return 0;
    }

    private void FixedUpdate()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            bool isInAir = false;
            
            Rigidbody rb = GetComponent<Rigidbody>();

            float acceleration = groundAcceleration;
            float frictionRate = groundFrictionRate;
            if (isInAir)
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
            
            scaledDesiredDirectionRelative = new Vector3(TweakMovementVelocity(scaledDesiredDirectionRelative.x, horizontalVelRelative.x, isInAir), 0,TweakMovementVelocity(scaledDesiredDirectionRelative.z, horizontalVelRelative.z, isInAir));
            
            Vector3 velocityChangeRelative = scaledDesiredDirectionRelative * acceleration;
            Vector3 velocityChange = transform.TransformDirection(velocityChangeRelative);
            
            finalVelocity += velocityChange * Time.fixedDeltaTime;
            // NOTE: slow down if already past the speed limit
            if (!isInAir)
            {
                if (initHorizontalVel.magnitude > movementSpeed)
                {
                    finalVelocity = initHorizontalVel.normalized * Mathf.Lerp(initHorizontalVel.magnitude, movementSpeed, highSpeedSlowingDownRate * Time.fixedDeltaTime);
                }
            }

            finalVelocity.y = rb.velocity.y;
            rb.velocity = finalVelocity;


                // Smooth it out
            // TODO: how does that work with different frame rates? Maybe use a curve?
            // Debug.Log(velocityChange);
            // Vector3 newVelocity = Vector3.Lerp(rb.velocity, velocityChange, accelerationRate * Time.fixedTime);
            // newVelocity.y = rb.velocity.y;
            //
            // rb.velocity = newVelocity;

            // time for drag
            rb.velocity = new Vector3(ApplyFriction(frictionRate, rb.velocity.x), rb.velocity.y, ApplyFriction(frictionRate, rb.velocity.z));
            // TODO: drag outside of the air

            // if (userInput.Value.isJumping)
            //     rb.velocity = new Vector3(30, 0, 0);

            // rb.AddForce(desiredVelocity);

            // if (isInAir)
            // {

            // }
            // limit the speed
            // else
            // {
            // Vector3 velocityCorrection =  desiredVelocity - rb.velocity;
            //
            // velocityCorrection.y = 0;
            //
            // // if there si a minuscule deviation, don't count it
            // if (velocityCorrection.sqrMagnitude <= velocityCorrectionEpsilon * velocityCorrectionEpsilon)
            // {
            //     rb.velocity = desiredVelocity;
            // }
            // else
            // {
            //     // TODO: fix the function of change of speed here to be more natural
            //     if (velocityCorrection.sqrMagnitude > maxMovementForce * maxMovementForce)
            //     {
            //         velocityCorrection = velocityCorrection.normalized * maxMovementForce;
            //     }
            //     rb.velocity += velocityCorrection;
            //     // rb.AddForce(velocityCorrection * Time.deltaTime);
            // }
            // }

            // if (userInput.Value.isJumping)
            // {
            //     // TODO: check if can jump
            //     bool canJump = isInAir && Time.fixedTime - previousJumpSecs >= jumpCooldownSecs;
            //     if (canJump)
            //     {
            //         // Vector3 vel = rb.velocity;
            //         // vel.y += jumpVelocity;
            //         // rb.velocity = vel;
            //         //
            //         // previousJumpSecs = Time.fixedTime;
            //     }
            // }
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
