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

enum SpeedChangeTypes
{
    IDLE,
    ACCELERATING,
    ACTIVE_DECELERATING,
    CONSTANT_SPEED,
    PASSIVE_DECELERATING,
}

[Serializable]
public struct MovementAnimationSettings
{
    public float velEpsilon; 
    public float constSpeedVelEpsilon; 
    public float desiredEpsilon; 
    
    public AnimationCurve acceleratingSpeedCurve; 
    public AnimationCurve activeStoppingSpeedCurve; // going in the opposite direction
    public AnimationCurve passiveStoppingSpeedCurve; // not specifying a change in direction
    
    public float accelerationTime; 
    public float activeStoppingTime; 
    public float passiveStoppingTime; 
}

public class PlayerController : NetworkBehaviour
{
    // public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    // public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    [SerializeField] private float movementSpeed = 1; 
    [SerializeField] private float jumpVelocity = 10f; 
    [SerializeField] private float jumpCooldownSecs = 0.6f;

    public MovementAnimationSettings movementAnimationSettings;
    
    struct MovementAxis
    {
        private SpeedChangeTypes currentSpeedChangeType;
        private float lastChangeTime;

        private MovementAnimationSettings movementAnimationSettings;

        public MovementAxis(MovementAnimationSettings _movementAnimationSettings)
        {
            movementAnimationSettings = _movementAnimationSettings;
            lastChangeTime = 0f;
            currentSpeedChangeType = SpeedChangeTypes.IDLE;
        }
        public float ProposeNewVelocity(float input, float vel)
        {
            if (currentSpeedChangeType == SpeedChangeTypes.IDLE) return 0;
            if (currentSpeedChangeType == SpeedChangeTypes.CONSTANT_SPEED) return vel;

            float maxTime = currentSpeedChangeType == SpeedChangeTypes.ACCELERATING ? movementAnimationSettings.accelerationTime :
                currentSpeedChangeType == SpeedChangeTypes.ACTIVE_DECELERATING ? movementAnimationSettings.activeStoppingTime : movementAnimationSettings.passiveStoppingTime;
            
            // Lerp
            float timeScale = Math.Min(1, (Time.fixedTime - lastChangeTime) / maxTime); // a float 0 - 1
            
            AnimationCurve curve = currentSpeedChangeType == SpeedChangeTypes.ACCELERATING ? movementAnimationSettings.acceleratingSpeedCurve :
                currentSpeedChangeType == SpeedChangeTypes.ACTIVE_DECELERATING ? movementAnimationSettings.activeStoppingSpeedCurve : movementAnimationSettings.passiveStoppingSpeedCurve;

            float movementMagnitude = curve.Evaluate(timeScale);

            float direction;
            if (Mathf.Abs(vel) > movementAnimationSettings.velEpsilon)
                direction = Mathf.Sign(vel);
            else if (Mathf.Abs(input) > movementAnimationSettings.desiredEpsilon)
                direction = Mathf.Sign(input);
            else
                direction = 0;
            
            return direction * movementMagnitude;
        }
        public SpeedChangeTypes UpdateSpeedChangeType(float vel, float desired)
        {
            bool isVelSmall = Mathf.Abs(vel) <= movementAnimationSettings.velEpsilon;
            bool isDesiredSmall = Mathf.Abs(desired) <= movementAnimationSettings.velEpsilon;

            SpeedChangeTypes newSpeedChangeType;
            
            if (isDesiredSmall)
                if (isVelSmall)
                    newSpeedChangeType = SpeedChangeTypes.IDLE;
                else
                    newSpeedChangeType = SpeedChangeTypes.PASSIVE_DECELERATING;
            else
                if (isVelSmall)
                    newSpeedChangeType = SpeedChangeTypes.ACCELERATING;
                else
                    // if going in the same direction, then see what is applicable
                    if (Mathf.Sign(vel) == Mathf.Sign(desired))
                        // if it is close enough or we are slower than expected, start accelerating
                        if (Mathf.Abs(vel - desired) < movementAnimationSettings.constSpeedVelEpsilon)
                            newSpeedChangeType = SpeedChangeTypes.CONSTANT_SPEED;
                        else if (Mathf.Abs(vel) > Mathf.Abs(desired))
                            newSpeedChangeType = SpeedChangeTypes.PASSIVE_DECELERATING;
                        else
                            newSpeedChangeType = SpeedChangeTypes.ACCELERATING;
                    else
                        newSpeedChangeType = SpeedChangeTypes.ACTIVE_DECELERATING;

            if (newSpeedChangeType != currentSpeedChangeType)
            {
                lastChangeTime = Time.fixedTime;
                currentSpeedChangeType = newSpeedChangeType;
            }
            
            return newSpeedChangeType;
        }
    }

    private NetworkVariable<UserInput> userInput =
        new NetworkVariable<UserInput>();

    private float previousJumpSecs;

    private MovementAxis xAxis;
    private MovementAxis zAxis;

    void Start()
    {
        xAxis = new MovementAxis(movementAnimationSettings);
        zAxis = new MovementAxis(movementAnimationSettings);
        
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
            Vector3 desiredDirection = userInput.Value.DesiredDirection;
            desiredDirection.y = 0;
            desiredDirection.Normalize();
            Vector3 desiredVelocity = desiredDirection * movementSpeed;

            Rigidbody rb = GetComponent<Rigidbody>(); 

            bool isInAir = false;

            var a = xAxis.UpdateSpeedChangeType(rb.velocity.x, desiredVelocity.x);
            zAxis.UpdateSpeedChangeType(rb.velocity.z, desiredVelocity.z);


            Vector3 pureForward = transform.forward;
            pureForward.y = 0;
            pureForward.Normalize();
            
            Vector3 pureRight = transform.right;
            pureRight.y = 0;
            pureRight.Normalize();
            
            Vector3 horizontalVel = rb.velocity;
            horizontalVel.y = 0;

            Vector3 horizontalVelRelativeToTransform = transform.TransformDirection(horizontalVel);

            Vector3 desiredVel =
                new Vector3(
                    xAxis.ProposeNewVelocity(desiredDirection.x, horizontalVelRelativeToTransform.x),
                    0,
                    zAxis.ProposeNewVelocity(desiredDirection.z, horizontalVelRelativeToTransform.z));
            desiredVel *= movementSpeed;
            Debug.Log(desiredVel);

            Vector3 desiredVelChange = desiredVel - horizontalVel;

            // TODO: clamp
            // if (desiredVelChange.sqrMagnitude > movementSpeed * movementSpeed)
            // {
            //     desiredVelChange = desiredVelChange.normalized * movementSpeed;
            // }

            rb.velocity += desiredVelChange;

            if (userInput.Value.isJumping)
                rb.velocity = new Vector3(10, 0, 0);

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
