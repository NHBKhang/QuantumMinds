using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class MultistateCharacterController : MonoBehaviour
{

    [Header("Local References:")]
    public LocalReferences localReferences;


    [Serializable]
    public class LocalReferences
    {
        [Tooltip("The transform of the actual character mesh.")]
        //The transform of the actual character mesh:
        public Transform characterRepresentation;

        [Tooltip("The child of the projected player.")]
        public Transform physicsGyro;

        [Tooltip("The animator component of the character.")]
        public Animator animator;

        [Tooltip("The character's gyro transform, this is a child of the character representation.")]
        public Transform characterGyroControl;

        [Tooltip("Every armature bone in the character with a rigidbody")]
        public List<Rigidbody> ragdoll = new List<Rigidbody>();

        [Tooltip("The armature's left foot transform.")]
        public Transform leftFoot;
        [Tooltip("The armature's right foot transform.")]
        public Transform rightFoot;
        [Tooltip("The armature's left hand transform.")]
        public Transform leftHand;
        [Tooltip("The armature's right hand transform.")]
        public Transform rightHand;
    }


    [Header("Profile:"), Tooltip("Refer to Section 0.A of the documentation.")]
    public MSCCProfile profile;
    private MSCCProfile changedProfile;

    [HideInInspector]
    public MultiStateSettings settings;

    //Events

    [Header("Events:")]
    public MSEvents mSEvents = new MSEvents();

    [Serializable]
    public class MSEvents
    {
        [Tooltip("This event is triggered on the player's transition from the falling state to grounded state, and it provides the time in seconds that the player has been in the falling state.")]
        public PlayerLandEvent OnPlayerLand;
        [Tooltip("This event is triggered when the player transitions into a different state.")]
        public PlayerStateChangeEvent OnPlayerMovementStateChange;
    }


    //Debug
    [Header("Debug:"), Tooltip("Enter Debug Mode?")]
    public bool debugModeEnabled = true;
    
    private void Start()
    {
        //Creates an instance of the profile   
        if (!profile)
        {
            UnityEngine.Debug.LogError("The player's profile has not been set!");
            this.enabled = false;
        }
        settings = profile.CreateInstanceOfProfile(new MultiStateSettings());

        SetReferencePosition();

        //Sets the rigidbody
        settings.rigidbodyForceModifiers.playerRigidbody = GetComponent<Rigidbody>();

        settings.basicControls.colliderHeight = localReferences.physicsGyro.GetComponent<CapsuleCollider>().height;
        settings.basicControls.colliderPosition = localReferences.physicsGyro.GetComponent<CapsuleCollider>().center;
        settings.physicsGyroSettings.normalColliderRadius = localReferences.physicsGyro.GetComponent<CapsuleCollider>().radius;

        //Determines the initial character rep speed
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            settings.visualRepresentation.repSpeed.targetCharacterRepSpeed = settings.visualRepresentation.repSpeed.flyingCharacterRepSpeed;
        }
        else
        {
            settings.visualRepresentation.repSpeed.targetCharacterRepSpeed = settings.visualRepresentation.repSpeed.fallingCharacterRepSpeed;
        }
        //Sets the start values
        SetGroundedMovementSubstate(settings.groundedSubstateModifiers.defaultGroundedMovementSubState);
        settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition;
        settings.raycastDistances.currentRaycastDistance = settings.raycastDistances.fallingRaycastDistance;
    }


    private void Update()
    {
        //Set's a reference position at the player's height.
        SetReferencePosition();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (settings.acv == null) return;
        //Checks if a MoveToTarget has been set
        IdentifyMoveToTarget();
        //Updates player rotation
        UpdateRotation();
        //Determines the movement state
        IdentifyMovementState();
        //Updates the movement vector based on the state
        UpdateMovementByState();
        //Sets the player friction based on the movement state and input
        UpdateFriction();
        //Actually moves the player
        MoveTowardsStepTransitionalValue();
        //Draws a line showing the direction of the externalTarget in front of the player
        DebugDirectionalVectors();
        UpdateAnimation();
        //Moves the character representation
        UpdateCharacterRep();
        //Records the current rotation
        RecordRotation();
        //Checks if the railSystem variable should be set to null
        CheckForRailSystemDetach();
    }

    //Set's a reference position at the player's height.
    void SetReferencePosition()
    {
        settings.directionalVectorModifiers.referencePosition = transform.position + Vector3.up* settings.basicControls.playerHeight;
    }

    private void LateUpdate()
    {
        //Updates the collider's height based on the movement state and grounded substate
        UpdateColliderHeight();
        //Updates the physics gyro
        CheckPhysicsGyro();
        //Checks for a profile change
        CheckForProfileChange();
    }

    //Starts the profile change
    public void AttemptProfileChange(MSCCProfile newProfile)
    {
        changedProfile = newProfile;
    }

    //Checks for a profile change
    void CheckForProfileChange()
    {
        if (changedProfile == null) return;
        settings.movementStateSettings.requestedState = MovementState.undefined;
        ChangeState(MovementState.undefined);
        settings = changedProfile.CreateInstanceOfProfile(settings);
        changedProfile = null;
    }

    //Updates the physics gyro
    void CheckPhysicsGyro()
    {
        if (!settings.physicsGyroSettings.enablePhysicsGyro) return;

        if (settings.movementStateSettings.movementState != MovementState.flying && settings.movementStateSettings.movementState != MovementState.swimming)
        {
            localReferences.physicsGyro.GetComponent<CapsuleCollider>().radius = settings.physicsGyroSettings.normalColliderRadius;
            localReferences.physicsGyro.localRotation = Quaternion.Lerp(localReferences.physicsGyro.localRotation, transform.rotation, 10 * Time.deltaTime);
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            UpdatePhysicsGyro(settings.physicsGyroSettings.flyingPhysicsGyroMaximumAnimationAngle, settings.physicsGyroSettings.flyingPhysicsGyroInputFactor, settings.physicsGyroSettings.flyingPhysicsGyroMagnitudeFactor, settings.physicsGyroSettings.flyingPhysicsGyroTransitionSpeed);
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.swimming)
        {
            if (settings.swimmingControls.swimmingFalseWalk)
            {
                localReferences.physicsGyro.GetComponent<CapsuleCollider>().radius = settings.physicsGyroSettings.normalColliderRadius;
                localReferences.physicsGyro.localRotation = Quaternion.Lerp(localReferences.physicsGyro.localRotation, transform.rotation, 10 * Time.deltaTime);
                return;
            }
            else
            {
                UpdatePhysicsGyro(settings.physicsGyroSettings.swimmingPhysicsGyroMaximumAnimationAngle, settings.physicsGyroSettings.swimmingPhysicsGyroInputFactor, settings.physicsGyroSettings.swimmingPhysicsGyroMagnitudeFactor, settings.physicsGyroSettings.swimmingPhysicsGyroTransitionSpeed);
            }
            return;
        }
    }

    //Updates the physics gyro
    void UpdatePhysicsGyro(float physicsGyroMaximumAnimationAngle, float physicsGyroInputFactor, float physicsGyroMagnitudeFactor, float physicsGyroTransitionSpeed)
    {
        localReferences.physicsGyro.GetComponent<CapsuleCollider>().radius = settings.physicsGyroSettings.normalColliderRadius * settings.physicsGyroSettings.physicsGyroRadiusMultiplier;

        Quaternion newRotation = localReferences.characterGyroControl.localRotation;

        Vector3 relativeFreeVector = localReferences.characterGyroControl.InverseTransformDirection(settings.acv.freeVector) * 10;
        Vector3 relativeVelocityVector = localReferences.characterGyroControl.InverseTransformDirection(settings.basicControls.referenceVelocity);

        Vector3 relativeVector = new Vector3((relativeVelocityVector.x - relativeFreeVector.x) * physicsGyroInputFactor + relativeFreeVector.x, (relativeVelocityVector.y - relativeFreeVector.y) * physicsGyroInputFactor + relativeFreeVector.y, (relativeVelocityVector.z - relativeFreeVector.z) * physicsGyroInputFactor + relativeFreeVector.z);

        //Vector3 relativeVector = characterGyroControl.InverseTransformDirection(acv.freeVector);
        Quaternion lookRot = Quaternion.LookRotation(relativeVector, Vector3.up);
        float adjustedMagnitude = relativeVector.magnitude;
        if (relativeFreeVector.z >= 0)
        {
            newRotation.eulerAngles = new Vector3(newRotation.eulerAngles.x + Mathf.Clamp((adjustedMagnitude / physicsGyroMagnitudeFactor) * physicsGyroMaximumAnimationAngle, 0, physicsGyroMaximumAnimationAngle), lookRot.eulerAngles.y, lookRot.eulerAngles.z);
        }
        else
        {
            newRotation.eulerAngles = new Vector3(-newRotation.eulerAngles.x + Mathf.Clamp((adjustedMagnitude / physicsGyroMagnitudeFactor) * physicsGyroMaximumAnimationAngle, 0, physicsGyroMaximumAnimationAngle), lookRot.eulerAngles.y, lookRot.eulerAngles.z);
        }

        localReferences.physicsGyro.localRotation = Quaternion.Lerp(localReferences.physicsGyro.localRotation, newRotation, physicsGyroTransitionSpeed * Time.deltaTime);
    }

    //Updates the physics gyro height
    void UpdateColliderHeight()
    {
        if (settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.recovery || settings.movementStateSettings.movementState == MovementState.onRails)
        {
            if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.crouching)
            {
                localReferences.physicsGyro.GetComponent<CapsuleCollider>().height = settings.basicControls.colliderHeight - settings.groundedSubstateModifiers.crouchingColliderDifference;
                localReferences.physicsGyro.GetComponent<CapsuleCollider>().center = settings.basicControls.colliderPosition + Vector3.down * (settings.groundedSubstateModifiers.crouchingColliderDifference * 0.5f);
                return;
            }
        }

        {
            localReferences.physicsGyro.GetComponent<CapsuleCollider>().height = settings.basicControls.colliderHeight;
            localReferences.physicsGyro.GetComponent<CapsuleCollider>().center = settings.basicControls.colliderPosition;
        }
    }

    //Updates the player's animation based on state
    void UpdateAnimation()
    {
        if (settings.movementStateSettings.movementState == MovementState.ragdoll)
        {
            localReferences.animator.enabled = false;
            return;
        }
        else
        {
            if (!localReferences.animator.enabled) localReferences.animator.enabled = true;
        }

        //Gyro variables 
        Vector3 transDirection = localReferences.characterRepresentation.InverseTransformDirection(settings.basicControls.referenceVelocity);
        Vector3 inverseTransDirection = settings.basicControls.referenceVelocity;
        Vector3 givenVelocity = localReferences.characterGyroControl.InverseTransformDirection(inverseTransDirection);
        float gyroAngleDirectionalMultiplier = 1;
        float freeVectorUpperVertMagLimit = 0.9f;
        float freeVectorLowerVertMagLimit = 0.9f;
        bool updateFreeVector = false;
        Quaternion targetGyroRotation = localReferences.characterRepresentation.rotation;

        //Smooth transition speed for the animator parameters
        float gyroSpeed = 50;
        float lerpSpeed = 50;

        bool disableAnimatorXZ = false;

        if (settings.basicAnimationSettings.commonAnimationTrigger != "")
        {
            localReferences.animator.SetTrigger(settings.basicAnimationSettings.commonAnimationTrigger);
            settings.basicAnimationSettings.commonAnimationTrigger = "";
        }

        if (settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.recovery)
        {
            //Sets common animator variables
            SetAnimatorVariables(settings.basicAnimationSettings.groundedMaximumAnimationSpeed, false, false, false, false, false, false);
        }
        if (settings.movementStateSettings.movementState == MovementState.falling)
        {
            //Sets common animator variables
            disableAnimatorXZ = true;
            SetAnimatorVariables(1, true, false, false, false, false, false);
        }
        if (settings.movementStateSettings.movementState == MovementState.onRails && settings.railSystemSettings.railSystem != null)
        {
            //Sets common animator variables
            SetAnimatorVariables(100, false, false, false, false, false, true);

            //Changes direction on rail to face the right direction
            if (settings.railSystemSettings.railSystem.primarySettings.keyControlled)
            {
                if (Input.GetAxis(settings.railSystemSettings.railSystem.primarySettings.keyAxis) > 0)
                {
                    localReferences.animator.SetFloat("directionalMagnitude", settings.basicControls.referenceVelocity.magnitude);
                }
                if (Input.GetAxis(settings.railSystemSettings.railSystem.primarySettings.keyAxis) < 0)
                {
                    localReferences.animator.SetFloat("directionalMagnitude", settings.basicControls.referenceVelocity.magnitude * -1);
                }
            }

            //Calls rail system animation trigger
            if (!settings.railSystemSettings.hasCalledOnRailsTrigger)
            {
                settings.railSystemSettings.hasCalledOnRailsTrigger = true;
                localReferences.animator.SetTrigger(settings.railSystemSettings.railSystem.primarySettings.animationTrigger);
            }

            //Gyro is not in use
            updateFreeVector = false;
        }
        if (settings.movementStateSettings.movementState == MovementState.sliding)
        {
            //Sets common animator variables
            SetAnimatorVariables(settings.basicAnimationSettings.groundedMaximumAnimationSpeed, false, true, false, false, false, false);
        }
        if (settings.movementStateSettings.movementState == MovementState.jumping)
        {
            //Sets common animator variables
            SetAnimatorVariables(1, true, false, true, false, false, false);
        }
        if (settings.movementStateSettings.movementState == MovementState.blink)
        {
            SetAnimatorVariables(1, false, false, false, false, false, false);
            localReferences.animator.SetFloat("BlinkDistance", Vector3.Distance(localReferences.characterRepresentation.position, settings.directionalVectorModifiers.referencePosition));

            Vector3 position1 = localReferences.characterRepresentation.position;
            position1.x = 0;
            position1.z = 0;
            float distance = Vector3.Distance(position1, settings.basicAnimationSettings.blinkAnimationHeight);
            if (position1.y < settings.basicAnimationSettings.blinkAnimationHeight.y)
                distance *= -1;

            localReferences.animator.SetFloat("BlinkHeight", distance - settings.blinkControls.blinkAnimationHeightOffset);
        }
        else
        {
            localReferences.animator.SetFloat("BlinkDistance", 0);
            localReferences.animator.SetFloat("BlinkHeight", 0);
        }
        if (settings.movementStateSettings.movementState == MovementState.swimming)
        {

            //Checks if player should be in the grounded animator state while in the swimming movement state
            if (settings.swimmingControls.swimmingFalseWalk)
            {
                //Sets common animator variables
                SetAnimatorVariables(settings.basicAnimationSettings.groundedMaximumAnimationSpeed, false, false, false, false, false, false);

                //gyro is not in use
                updateFreeVector = false;
            }
            else
            {
                //Sets common animator variables
                SetAnimatorVariables(settings.swimmingAnimationSettings.swimmingMaximumAnimationSpeed, false, false, false, true, false, false);
                gyroSpeed = settings.swimmingAnimationSettings.swimmingGyroTransitionSpeed;
                lerpSpeed = settings.swimmingAnimationSettings.swimmingAnimationTransitionSpeed;
                //checks if swimming at water surface
                float gyroDepth = settings.swimmingControls.waterLevel - (settings.directionalVectorModifiers.referencePosition.y - settings.basicControls.playerHeight);
                if (gyroDepth < settings.swimmingAnimationSettings.swimmingMinimumGyroDepth + settings.swimmingAnimationSettings.swimmingGyroFadeDistance)
                {
                    //sets transition speed and vertical magnitude limits
                    freeVectorUpperVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorUpperVertMagLimit + settings.swimmingAnimationSettings.swimmingGyroFadeMultiplier * (gyroDepth - settings.swimmingAnimationSettings.swimmingMinimumGyroDepth / (settings.swimmingAnimationSettings.swimmingGyroFadeDistance));
                    freeVectorLowerVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorLowerVertMagLimit + settings.swimmingAnimationSettings.swimmingGyroFadeMultiplier * (gyroDepth - settings.swimmingAnimationSettings.swimmingMinimumGyroDepth / (settings.swimmingAnimationSettings.swimmingGyroFadeDistance));
                }
                else
                {
                    //sets transition speed and vertical magnitude limits
                    freeVectorUpperVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorUpperVertMagLimit;
                    freeVectorLowerVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorLowerVertMagLimit;
                }

                if (gyroDepth >= settings.swimmingAnimationSettings.swimmingMinimumGyroDepth)
                {
                    //Calculates gyro directional vectors
                    GetFreeVectorAnimationDefaults(settings.basicAnimationSettings.gyroCalculationMethod, out transDirection, out inverseTransDirection, out givenVelocity, out gyroAngleDirectionalMultiplier, false);
                    //Gyro is in use
                    updateFreeVector = true;
                }
            }

        }
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            //Sets common animator variables
            SetAnimatorVariables(settings.flyingAnimationSettings.flyingMaximumAnimationSpeed, false, false, false, false, true, false);
            //sets transition speed and vertical magnitude limits
            freeVectorUpperVertMagLimit = settings.flyingAnimationSettings.flyingFreeVectorUpperVertMagLimit;
            freeVectorLowerVertMagLimit = settings.flyingAnimationSettings.flyingFreeVectorLowerVertMagLimit;
            gyroSpeed = settings.flyingAnimationSettings.flyingGyroTransitionSpeed;
            lerpSpeed = settings.flyingAnimationSettings.flyingAnimationTransitionSpeed;
            //Calculates gyro directional vectors
            GetFreeVectorAnimationDefaults(settings.basicAnimationSettings.gyroCalculationMethod, out transDirection, out inverseTransDirection, out givenVelocity, out gyroAngleDirectionalMultiplier, settings.flyingControls.flyingConstantForward);
            //Gyro is in use
            updateFreeVector = true;
        }

        float zVelocity = 0;
        float xVelocity = 0;

        //Calculates animator parameters and gyro rotation if its in use
        if (updateFreeVector)
        {

            Vector3 flatVector = GetFlatVector3(transDirection);
            float verticalMagnitude = transDirection.magnitude - flatVector.magnitude;
            float directionalPercentage = 0;
            if (transDirection.y > 0)
            {
                directionalPercentage = Mathf.Clamp01(verticalMagnitude / Mathf.Abs(freeVectorUpperVertMagLimit));
                directionalPercentage *= -1;
            }
            if (transDirection.y < 0)
            {
                directionalPercentage = Mathf.Clamp01(verticalMagnitude / Mathf.Abs(freeVectorLowerVertMagLimit));
            }

            targetGyroRotation.eulerAngles = new Vector3(90 * gyroAngleDirectionalMultiplier * directionalPercentage, targetGyroRotation.eulerAngles.y, targetGyroRotation.eulerAngles.z);

            if (debugModeEnabled) UnityEngine.Debug.DrawRay(localReferences.characterGyroControl.position, localReferences.characterGyroControl.forward, Color.yellow);

            zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), givenVelocity.z, lerpSpeed * Time.deltaTime));
            xVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), givenVelocity.x, lerpSpeed * Time.deltaTime));
        }
        else
        {
            //This adjusts the animator velocities to compensate for four possible movement directions
            Vector3 calcVector = CalculateAdjustedVelocities(transDirection);

            if (disableAnimatorXZ)
            {
                zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), 0, 10 * Time.deltaTime));
                xVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), 0, 10 * Time.deltaTime));
            }
            else
            {
                if (settings.rootMotionSettings.enableRootMotion)
                {
                    if (settings.rootMotionSettings.rootMotionCheckInputDirection)
                    {
                        RaycastHit raycastHit;
                        //Vector3 inputVector = transform.TransformDirection(acv.flatVector);
                        UnityEngine.Debug.DrawRay(settings.directionalVectorModifiers.referencePosition + Vector3.up * -(settings.basicControls.playerHeight + settings.rootMotionSettings.rootMotionCastHeightOffset), new Vector3(settings.acv.flatVector.x, 0, settings.acv.flatVector.z) * settings.rootMotionSettings.rootMotionCastDistance, Color.red);
                        if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition + Vector3.up * -(settings.basicControls.playerHeight - settings.rootMotionSettings.rootMotionCastHeightOffset), new Vector3(settings.acv.flatVector.x, 0, settings.acv.flatVector.z), out raycastHit, settings.rootMotionSettings.rootMotionCastDistance, settings.rootMotionSettings.rootMotionLayerMask))
                        {
                            zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), 0, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                            xVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), 0, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                            return;
                        }
                    }
                    if (settings.groundedSubstateModifiers.groundedMovementMethod == MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased)
                    {
                        Vector3 inputVector = transform.InverseTransformDirection(settings.acv.flatVector);
                        calcVector = new Vector3(inputVector.x, 0, inputVector.z);
                        zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), calcVector.z, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                        xVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), calcVector.x, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                    }
                    else
                    {
                        zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), 1, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                        xVelocity=(Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), 0, settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed * Time.deltaTime));
                    }
                }
                else
                {
                    if (settings.movementStateSettings.movementState != MovementState.swimming)
                    {
                        zVelocity = (calcVector.z);
                        xVelocity = calcVector.x;
                    }
                    else
                    {
                        zVelocity = (Mathf.Lerp(localReferences.animator.GetFloat("zVelocity"), calcVector.z, settings.swimmingAnimationSettings.swimmingAnimationTransitionSpeed * Time.deltaTime));
                        xVelocity=  Mathf.Lerp(localReferences.animator.GetFloat("xVelocity"), calcVector.x, settings.swimmingAnimationSettings.swimmingAnimationTransitionSpeed * Time.deltaTime);
                    }
                }
            }
        }

        //Gyro rotates towards calculated rotation or it's neutral position
        localReferences.characterGyroControl.rotation = Quaternion.Slerp(localReferences.characterGyroControl.rotation, targetGyroRotation, gyroSpeed * Time.deltaTime);

        localReferences.animator.SetFloat("zVelocity", zVelocity);
        localReferences.animator.SetFloat("xVelocity",xVelocity);



        SetAnimatorGroundedSubstate();
    }

    //Records the animator velocity
    public void UpdateRootMotionAnimatorVelocity(float velocity)
    {
        settings.rootMotionSettings.rootMotionSpeed = velocity;
    }

    //Calculates Gyro directional vectors based on the GyroCalculationMethod
    void GetFreeVectorAnimationDefaults(MultiStateSettings.BasicAnimationSettings.GyroCalculationMethod calculationMethod, out Vector3 transDirection, out Vector3 inverseTransDirection, out Vector3 givenVelocity, out float gyroAngleDirectionalMultiplier, bool constantForward)
    {
        transDirection = localReferences.characterRepresentation.InverseTransformDirection(settings.basicControls.referenceVelocity);
        inverseTransDirection = settings.basicControls.referenceVelocity;
        givenVelocity = localReferences.characterGyroControl.InverseTransformDirection(inverseTransDirection);
        gyroAngleDirectionalMultiplier = 1;

        //Velocity based calculation
        if (calculationMethod == MultiStateSettings.BasicAnimationSettings.GyroCalculationMethod.characterRepVelocity)
        {
            if (transDirection.z < 0)
            {
                gyroAngleDirectionalMultiplier = -1;
            }
            return;
        }

        //Player input based calculation
        if (calculationMethod == MultiStateSettings.BasicAnimationSettings.GyroCalculationMethod.freeVectorInput)
        {
            transDirection = localReferences.characterRepresentation.InverseTransformDirection(settings.acv.freeVector);
            if (constantForward)
            {
                transDirection = new Vector3(transDirection.x, transDirection.y, 1);
            }
            inverseTransDirection = localReferences.characterRepresentation.TransformDirection(transDirection);
            givenVelocity = localReferences.characterGyroControl.InverseTransformDirection(inverseTransDirection);
            if (settings.acv.verticalInput < 0)
            {
                gyroAngleDirectionalMultiplier = -1;
            }
            return;
        }
        //ExternalTarget based calculation
        if (calculationMethod == MultiStateSettings.BasicAnimationSettings.GyroCalculationMethod.externalTargetDirectional)
        {
            transDirection = localReferences.characterRepresentation.TransformDirection(new Vector3(settings.directionalVectorModifiers.externalTargetValue.x, settings.directionalVectorModifiers.externalTargetValue.y - settings.basicControls.playerHeight, settings.directionalVectorModifiers.externalTargetValue.z) - localReferences.characterRepresentation.position);
            inverseTransDirection = localReferences.characterRepresentation.InverseTransformDirection(transDirection);
            givenVelocity = localReferences.characterGyroControl.InverseTransformDirection(inverseTransDirection);
            if (localReferences.characterRepresentation.TransformPoint(settings.directionalVectorModifiers.externalTargetValue).z < localReferences.characterRepresentation.localPosition.z)
            {
                gyroAngleDirectionalMultiplier = -1;
            }
            return;
        }

        //Difference between character representation and projected player method
        if (calculationMethod == MultiStateSettings.BasicAnimationSettings.GyroCalculationMethod.characterRepPlayerOffset)
        {
            transDirection = new Vector3(settings.directionalVectorModifiers.referencePosition.x, settings.directionalVectorModifiers.referencePosition.y - settings.basicControls.playerHeight, settings.directionalVectorModifiers.referencePosition.z) - localReferences.characterRepresentation.position;
            inverseTransDirection = transDirection;
            givenVelocity = localReferences.characterGyroControl.InverseTransformDirection(inverseTransDirection);
            if (localReferences.characterRepresentation.TransformPoint(settings.directionalVectorModifiers.referencePosition).z < localReferences.characterRepresentation.localPosition.z)
            {
                gyroAngleDirectionalMultiplier = -1;
            }
            return;
        }
    }

    //Sets common animator state and speed related variables
    void SetAnimatorVariables(float maximumAnimationSpeed, bool isFalling, bool isSliding, bool isJumping, bool isSwimming, bool isFlying, bool isOnRails)
    {
        localReferences.animator.SetBool("falling", isFalling);
        localReferences.animator.SetBool("sliding", isSliding);
        localReferences.animator.SetBool("jumping", isJumping);
        localReferences.animator.SetBool("swimming", isSwimming);
        localReferences.animator.SetBool("flying", isFlying);
        localReferences.animator.SetBool("onRails", isOnRails);
        float modifiedRailPosition = settings.railSystemSettings.railPosition * settings.railSystemSettings.animationRailPositionMultiplier + settings.railSystemSettings.animationRailPositionOffset;
        localReferences.animator.SetFloat("railPosition", modifiedRailPosition - Mathf.Floor(modifiedRailPosition));
        localReferences.animator.SetFloat("waterDepth", settings.swimmingControls.waterLevel - settings.directionalVectorModifiers.referencePosition.y);
        localReferences.animator.SetFloat("magnitude", settings.basicControls.referenceVelocity.magnitude);
        settings.basicAnimationSettings.angularVelocities.Add(Mathf.DeltaAngle(settings.basicAnimationSettings.angularVelocity, transform.rotation.eulerAngles.y) * Time.deltaTime * 100);
        if (settings.basicAnimationSettings.angularVelocities.Count > 5) settings.basicAnimationSettings.angularVelocities.RemoveAt(0);
        localReferences.animator.SetFloat("AngularVelocity", Mathf.Lerp(localReferences.animator.GetFloat("AngularVelocity"), settings.basicAnimationSettings.angularVelocities.ToArray().Average(), 20 * Time.deltaTime));
        localReferences.animator.SetFloat("speedAdjustment", 1 + Mathf.Clamp(((settings.basicControls.referenceVelocity.magnitude - settings.basicAnimationSettings.animationBaseTransition) * settings.basicAnimationSettings.animationSpeedMultiplier), 0.01f, maximumAnimationSpeed - 1));
    }

    //This adjusts the animator velocities to compensate for four possible movement directions by checking the key input
    Vector3 CalculateAdjustedVelocities(Vector3 transDirection)
    {
        Vector3 returnValue = Vector3.zero;
        if (settings.acv.isClassic)
        {
            if (settings.acv.horizontalInput < 0 && settings.acv.sideStepInput < 0 && settings.acv.verticalInput < 0)
            {
                returnValue.z = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.z, -transDirection.magnitude / 4, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.z = returnValue.z;
                returnValue.x = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.x, -transDirection.magnitude / 4, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.x = returnValue.x;
                return returnValue;
            }
            if (settings.acv.horizontalInput > 0 && settings.acv.sideStepInput > 0 && settings.acv.verticalInput < 0)
            {
                returnValue.z = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.z, -transDirection.magnitude / 4, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.z = returnValue.z;
                returnValue.x = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.x, transDirection.magnitude / 4, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.x = returnValue.x;
                return returnValue;
            }
            if (settings.acv.horizontalInput < 0 && settings.acv.sideStepInput > 0 && settings.acv.verticalInput == 0)
            {
                returnValue.z = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.z, 0, 1 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.z = returnValue.z;
                returnValue.x = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.x, transDirection.magnitude, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.x = returnValue.x;
                return returnValue;
            }
            if (settings.acv.horizontalInput > 0 && settings.acv.sideStepInput < 0 && settings.acv.verticalInput == 0)
            {
                returnValue.z = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.z, 0, 1 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.z = returnValue.z;
                returnValue.x = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.x, -transDirection.magnitude, 2 * Time.deltaTime);
                settings.basicAnimationSettings.animationVelocity.x = returnValue.x;
                return returnValue;
            }
        }
        returnValue.z = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.z, transDirection.z, 15 * Time.deltaTime);
        settings.basicAnimationSettings.animationVelocity.z = returnValue.z;
        returnValue.x = Mathf.Lerp(settings.basicAnimationSettings.animationVelocity.x, transDirection.x, 15 * Time.deltaTime);
        settings.basicAnimationSettings.animationVelocity.x = returnValue.x;
        return returnValue;
    }

    //Sets Animator Grounded Substate
    void SetAnimatorGroundedSubstate()
    {
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.sprinting)
        {
            settings.basicAnimationSettings.currentAnimationSubstateValue = Mathf.Lerp(settings.basicAnimationSettings.currentAnimationSubstateValue, 0, settings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed * Time.deltaTime);
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.running)
        {
            settings.basicAnimationSettings.currentAnimationSubstateValue = Mathf.Lerp(settings.basicAnimationSettings.currentAnimationSubstateValue, 1, settings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed * Time.deltaTime);
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.walking)
        {
            settings.basicAnimationSettings.currentAnimationSubstateValue = Mathf.Lerp(settings.basicAnimationSettings.currentAnimationSubstateValue, 2, settings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed * Time.deltaTime);
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.crouching)
        {
            settings.basicAnimationSettings.currentAnimationSubstateValue = Mathf.Lerp(settings.basicAnimationSettings.currentAnimationSubstateValue, 3, settings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed * Time.deltaTime);
        }
        localReferences.animator.SetFloat("groundedSubstate", settings.basicAnimationSettings.currentAnimationSubstateValue);
    }

    //Sets the animator velocity
    public void SetAnimatorVelocity(float xValue, float yValue)
    {
        localReferences.animator.SetFloat("zVelocity", xValue);
        localReferences.animator.SetFloat("xVelocity", yValue);
    }

    //Sets and animation trigger
    public void SetAnimatorTrigger(string trigger)
    {
        localReferences.animator.SetTrigger(trigger);
    }

    //Resets and animator trigger
    public void ResetAnimatorTrigger(string trigger)
    {
        localReferences.animator.ResetTrigger(trigger);
    }
    //Sets an animator float parameter
    public void SetAnimatorFloat(string name, float value)
    {
        localReferences.animator.SetFloat(name, value);
    }

    //Checks if the railSystem variable should be set to null
    void CheckForRailSystemDetach()
    {
        if (settings.railSystemSettings.currentRailInteractionTime < 0)
        {
            settings.railSystemSettings.currentRailInteractionTime = Mathf.Clamp(settings.railSystemSettings.currentRailInteractionTime + Time.deltaTime, -settings.railSystemSettings.railInteractionTime, 0);
        }

        if (settings.railSystemSettings.willDetachFromRail)
        {
            //Resets common rail system variables
            settings.railSystemSettings.railSystem = null;
            settings.railSystemSettings.railPosition = 0;
            settings.railSystemSettings.willDetachFromRail = false;
        }
    }

    //Records rotation
    void RecordRotation()
    {
        settings.basicAnimationSettings.angularVelocity = transform.rotation.eulerAngles.y;
    }

    //Sets the player friction based on the movement state and input
    //This prevents slow sliding on hills while standing still
    void UpdateFriction()
    {
        if (settings.acv == null) return;
        if (settings.acv.freeVector == Vector3.zero &&
            settings.movementStateSettings.movementState != MovementState.sliding &&
            settings.movementStateSettings.movementState != MovementState.falling &&
            settings.movementStateSettings.movementState != MovementState.jumping &&
            settings.movementStateSettings.movementState != MovementState.recovery)
        {
            localReferences.physicsGyro.GetComponent<CapsuleCollider>().material.dynamicFriction = 1f;
        }
        else
        {
            localReferences.physicsGyro.GetComponent<CapsuleCollider>().material.dynamicFriction = 0f;
        }

    }


    //Checks if the player should move to a target or be controlled by player input
    void IdentifyMoveToTarget()
    {
        if (settings.moveToTargetFunctionality.moveToTarget == null) { settings.groundedSubstateModifiers.groundedMovementMethod = MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased; return; }
        if (Vector3.Distance(settings.directionalVectorModifiers.referencePosition + Vector3.down * settings.basicControls.playerHeight, settings.moveToTargetFunctionality.moveToTarget.position) <= settings.moveToTargetFunctionality.moveToTargetDistance)
        {
            settings.moveToTargetFunctionality.moveToTarget = null;
            settings.groundedSubstateModifiers.groundedMovementMethod = MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased;
            return;
        }
        settings.groundedSubstateModifiers.groundedMovementMethod = MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.targetBased;
    }

    //Moves and rotates the character representation
    void UpdateCharacterRep()
    {
        //Slowly adjusted the character representation movement speed to match the speed required by the current movement state
        settings.visualRepresentation.repSpeed.currentCharacterRepSpeed = Mathf.Lerp(settings.visualRepresentation.repSpeed.currentCharacterRepSpeed, settings.visualRepresentation.repSpeed.targetCharacterRepSpeed, settings.visualRepresentation.characterRepAdjustmentSpeed * Time.deltaTime);
        //Checks to see if the character should match the projected player's rotation
        if (settings.railSystemSettings.railSystem == null)
        {
            Quaternion newRotation = transform.rotation;
            newRotation.eulerAngles = new Vector3(0, newRotation.eulerAngles.y, 0);
            localReferences.characterRepresentation.rotation = newRotation;
        }
        else if(settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.lockRotation || settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.freeRotation)
        {
            Quaternion newRotation = transform.rotation;
            newRotation.eulerAngles = new Vector3(0, newRotation.eulerAngles.y, 0);
            localReferences.characterRepresentation.rotation = newRotation;
        }

        //Moves the character representation
        if (settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.recovery || settings.movementStateSettings.movementState == MovementState.sliding || settings.movementStateSettings.movementState == MovementState.swimming && settings.swimmingControls.swimmingFalseWalk)
        {
            localReferences.characterRepresentation.position = Vector3.SmoothDamp(localReferences.characterRepresentation.position, GroundVector(settings.directionalVectorModifiers.referencePosition) + new Vector3(0, settings.visualRepresentation.characterRepHeightOffset + settings.basicControls.playerHeight, 0), ref settings.basicControls.referenceVelocity, settings.visualRepresentation.repSpeed.currentCharacterRepSpeed);
        }
        else
        {
            localReferences.characterRepresentation.position = Vector3.SmoothDamp(localReferences.characterRepresentation.position, settings.directionalVectorModifiers.referencePosition + new Vector3(0, settings.visualRepresentation.characterRepHeightOffset, 0), ref settings.basicControls.referenceVelocity, settings.visualRepresentation.repSpeed.currentCharacterRepSpeed);
        }


    }

    //Sets the isGrounded bool
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 8)
        {
            settings.basicControls.isGrounded = true;
        }
        else
        {
            settings.basicControls.isGrounded = false;
        }
    }

    //Actually moves the projected player based on the directional vectors calculated based on the current movement state
    void MoveTowardsStepTransitionalValue()
    {
        if (settings.movementStateSettings.movementState == MovementState.ragdoll)
        {
            return;
        }

        //Assigned the calculated direction value based on state
        Vector3 direction;

        //Handles visually grounded states
        if (settings.movementStateSettings.movementState == MovementState.undefined || settings.movementStateSettings.movementState == MovementState.recovery || settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.sliding)
        {
            //Applies a small amount of constant downward force
            settings.rigidbodyForceModifiers.playerRigidbody.AddForce(new Vector3(0, settings.rigidbodyForceModifiers.constantYGroundedForce, 0));

            //Calculates the attempted climbing angle
            Vector3 usedPosition = settings.directionalVectorModifiers.externalTargetValue;
            float forwardDistance, heightDistance;
            GetClimbingDistances(usedPosition + new Vector3(0, settings.basicControls.playerHeight, 0), out forwardDistance, out heightDistance);
            float climbingAngle = GetClimbAngle(heightDistance, forwardDistance);

            //Player will not attempt to climb higher than the maximum climbing angle
            if (usedPosition.y > settings.directionalVectorModifiers.referencePosition.y && climbingAngle > (settings.slideControls.maximumClimbAngle))
            {
                return;
            }

            //Calculates the forces value sent to the rigidbody
            direction = (((usedPosition + new Vector3(0, settings.basicControls.playerHeight, 0)) - settings.directionalVectorModifiers.referencePosition) * settings.directionalVectorModifiers.currentTransitionalSpeed);

            //Averages y force values to prevent stuttering
            settings.directionalVectorModifiers.averagedYValue.Add(direction.y);
            if (settings.directionalVectorModifiers.averagedYValue.Count > settings.directionalVectorModifiers.averageYValueCount)
            {
                settings.directionalVectorModifiers.averagedYValue.RemoveAt(0);
            }
            direction.y = settings.directionalVectorModifiers.averagedYValue.Average();

            //Prevents force from be applied upward 
            if (direction.y > 0)
            {
                direction.y += -(direction.y * direction.y) * settings.rigidbodyForceModifiers.variableDistanceYForceMultiplier;
            }

            //Adds extra downward force if sliding
            if (settings.movementStateSettings.movementState == MovementState.sliding)
            {
                direction.y += settings.slideControls.slideConstantY;
            }

            //Applies the final force value to the projected players rigidbody
            settings.rigidbodyForceModifiers.playerRigidbody.AddForce(direction * settings.rigidbodyForceModifiers.forceMultiplier);
        }
        //Handles movement in states that are not visually grounded
        if (settings.movementStateSettings.movementState == MovementState.jumping || settings.movementStateSettings.movementState == MovementState.swimming || settings.movementStateSettings.movementState == MovementState.flying || settings.movementStateSettings.movementState == MovementState.falling || settings.movementStateSettings.movementState == MovementState.lunge)
        {
            //Applies the final force value to the projected players rigidbody
            direction = settings.directionalVectorModifiers.externalTargetValue - settings.directionalVectorModifiers.referencePosition;
            settings.rigidbodyForceModifiers.playerRigidbody.AddForce(direction * settings.rigidbodyForceModifiers.forceMultiplier);
        }
        //Handles movement in the onRails state
        if (settings.movementStateSettings.movementState == MovementState.onRails && settings.railSystemSettings.railSystem != null)
        {
            //Smoothly moves player towards rail system goal
            if (!settings.railSystemSettings.railSystem.primarySettings.absolutePositionLock)
            {
                direction = settings.directionalVectorModifiers.externalTargetValue - settings.directionalVectorModifiers.referencePosition;
                settings.rigidbodyForceModifiers.playerRigidbody.velocity = direction;
                return;
            }
            //Locks player at the rail system goal
            settings.rigidbodyForceModifiers.playerRigidbody.MovePosition(settings.directionalVectorModifiers.externalTargetValue);
        }
    }


    //Draws a line showing the direction of the externalTarget in front of the player
    void DebugDirectionalVectors()
    {
        if (settings.directionalVectorModifiers.externalTargetValue == null) return;
        if (debugModeEnabled) UnityEngine.Debug.DrawLine(settings.directionalVectorModifiers.referencePosition, settings.directionalVectorModifiers.externalTargetValue + new Vector3(0, settings.basicControls.playerHeight, 0), Color.red);
    }

    //Updates the projected player rotation based on control scheme, camera rotation, and other factors
    void UpdateRotation()
    {
        //Returns if the player has not received the acv yet
        if (settings.acv == null) return;

        if (settings.railSystemSettings.railSystem != null)
        {
            UpdateOnRailsRotation();
        }

        if (settings.basicControls.lockRotation != 0) return;

        if (settings.movementStateSettings.movementState == MovementState.blink)
        {
            if (settings.blinkControls.blinkSnapRotation)
            {
                transform.rotation = settings.blinkControls.idealStateRotation;
                return;
            }
        }


        //Checks if the player should be rotating based on input
        if (settings.groundedSubstateModifiers.groundedMovementMethod == MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased && !settings.acv.isUnlockedModern || settings.groundedSubstateModifiers.groundedMovementMethod == MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased && settings.acv.isModernMixed)
        {
            SetCameraRotationSpeed();
            RotateTowardsTargetRotation(settings.acv.cameraRigY.rotation);
            return;
        }
        //Checks if the player should be rotating based on input
        if (settings.groundedSubstateModifiers.groundedMovementMethod == MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.controlVectorBased && settings.acv.isUnlockedModern && !settings.acv.isModernMixed)
        {
            SetCameraRotationSpeed();
            Quaternion targetRot = Quaternion.LookRotation(settings.acv.flatVector * 10, Vector3.up);
            targetRot.eulerAngles = new Vector3(0, targetRot.eulerAngles.y, 0);
            RotateTowardsTargetRotation(targetRot);
            return;
        }

        //look at moveToTarget
        LookAtPosition(settings.moveToTargetFunctionality.moveToTarget.position);

    }

    //Rotates the player towards a target
    void LookAtPosition(Vector3 target)
    {
        settings.directionalVectorModifiers.currentRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed;
        Quaternion lookRot = Quaternion.LookRotation(target - settings.directionalVectorModifiers.referencePosition, Vector3.up);
        lookRot.eulerAngles = new Vector3(0, lookRot.eulerAngles.y, 0);
        RotateTowardsTargetRotation(lookRot);
    }

    //Sets the rotation speed based on the control scheme
    void SetCameraRotationSpeed()
    {
        if (!settings.acv.allowRotationDifference)
        {
            settings.basicControls.cameraRotationDifference = Quaternion.identity;
        }
        else if (!settings.acv.lockRotation)
        {
            UpdateRotationUnlocked();
            return;
        }
        if (settings.acv.lockRotation)
        {
            UpdateRotationLocked();
            return;
        }
        if (settings.acv.rotateWithMovement)
        {
            UpdateRotationRWM();
            return;
        }
    }

    //Rotates the player towards a target rotation
    void RotateTowardsTargetRotation(Quaternion rotationValue)
    {
        Vector3 additionalRotation = new Vector3(0, settings.acv.rotation * settings.directionalVectorModifiers.rotationalSpeedMultiplier, 0);
        Quaternion calculatedRotation = rotationValue * settings.basicControls.cameraRotationDifference;

        transform.rotation = Quaternion.Slerp(transform.rotation, calculatedRotation, settings.directionalVectorModifiers.currentRotationSpeed * Time.deltaTime);
        transform.Rotate(additionalRotation * Time.deltaTime);
    }
    //Sets the rotation speed for locked rotation
    void UpdateRotationLocked()
    {
        //Reduces the rotation speed while flying
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            settings.directionalVectorModifiers.currentRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed * settings.flyingControls.flyingMaxRotationSpeedMultiplier;
            return;
        }
        settings.directionalVectorModifiers.currentRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed;
    }
    //Sets the rotation speed for unlocked rotation
    void UpdateRotationUnlocked()
    {
        settings.directionalVectorModifiers.currentRotationSpeed = 0;
        settings.basicControls.cameraRotationDifference = transform.rotation * Quaternion.Inverse(settings.acv.cameraRigY.rotation);
    }
    //Sets the rotation speed to rotate with movement
    void UpdateRotationRWM()
    {
        //Adjusts the speed for flying
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            settings.directionalVectorModifiers.currentRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed * settings.flyingControls.flyingKeyRotationSpeedMultiplier * settings.acv.freeVector.magnitude * Time.deltaTime;
            return;
        }
        //Rotation speed is vertical input dependent  
        settings.directionalVectorModifiers.currentRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed * settings.acv.flatVector.magnitude * Time.deltaTime;
    }
    //Updates onRails rotation
    void UpdateOnRailsRotation()
    {
        //Rotates the character representation based on the rail system's next waypoint
        if (settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.rotateTowardsNextWaypoint)
        {
            //Set the rotation target to the target
            settings.directionalVectorModifiers.targetCharacterRepRotation = Quaternion.LookRotation(settings.railSystemSettings.railSystem.GetFloorWaypoint(1) - new Vector3(0, settings.basicControls.playerHeight - settings.railSystemSettings.railSystem.primarySettings.yValueOffset, 0) - localReferences.characterRepresentation.position, Vector3.up);

            //Rotate on y axis only
            if (settings.railSystemSettings.railSystem.primarySettings.rotateOnYAxisOnly)
                settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles = new Vector3(0, settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles.y, 0);

            //If the rail system is key controlled it sets the direction based on input. 
            //Time controlled rail direction is based on the value set by the rails caps
            if (settings.railSystemSettings.railSystem.primarySettings.keyControlled)
            {
                if (Input.GetAxis(settings.railSystemSettings.railSystem.primarySettings.keyAxis) > 0)
                {
                    settings.railSystemSettings.onRailsLastLookDir = 1* settings.acv.calculatedRailDirection;
                }
                if (Input.GetAxis(settings.railSystemSettings.railSystem.primarySettings.keyAxis) < 0)
                {
                    settings.railSystemSettings.onRailsLastLookDir = 0;
                }
                Vector3 lookRotTarget = settings.railSystemSettings.railSystem.GetFloorWaypoint(settings.railSystemSettings.onRailsLastLookDir);

                settings.directionalVectorModifiers.targetCharacterRepRotation = Quaternion.LookRotation(lookRotTarget - new Vector3(0, settings.basicControls.playerHeight - settings.railSystemSettings.railSystem.primarySettings.yValueOffset, 0) - localReferences.characterRepresentation.position, Vector3.up);
                if (settings.railSystemSettings.railSystem.primarySettings.rotateOnYAxisOnly)
                    settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles = new Vector3(0, settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles.y, 0);
            }
        }
        //Rotates the character representation based on the rail system's set target
        if (settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.rotateTowardsTarget)
        {
            //Set the rotation target to the target
            settings.directionalVectorModifiers.targetCharacterRepRotation = Quaternion.LookRotation(settings.railSystemSettings.railSystem.primarySettings.rotationTarget.position - new Vector3(0, settings.basicControls.playerHeight - settings.railSystemSettings.railSystem.primarySettings.yValueOffset, 0) - localReferences.characterRepresentation.position, Vector3.up);

            //Rotate on y axis only
            if (settings.railSystemSettings.railSystem.primarySettings.rotateOnYAxisOnly)
                settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles = new Vector3(0, settings.directionalVectorModifiers.targetCharacterRepRotation.eulerAngles.y, 0);
        }

        //Rotates the character representation based on the player default rotation
        if (settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.freeRotation)
        {
            //The rail system automatically locks rotation, a rail system lock's priority is set to 5 
            AttemptUnlockRotation(5);
        }

        if (settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.rotateTowardsNextWaypoint || settings.railSystemSettings.railSystem.primarySettings.rotationControl == RailSystemPrimarySettings.RotationControl.rotateTowardsTarget)
        {
            localReferences.characterRepresentation.rotation = Quaternion.Slerp(localReferences.characterRepresentation.rotation, settings.directionalVectorModifiers.targetCharacterRepRotation, settings.railSystemSettings.railSystem.primarySettings.playerRotationSpeed * Time.deltaTime);
            Quaternion newRot = localReferences.characterRepresentation.rotation;
            newRot.eulerAngles = new Vector3(0, localReferences.characterRepresentation.eulerAngles.y, 0);
            transform.rotation = newRot;
        }
    }

    //Identifies and sets the correct movement state based on player location and override factors
    void IdentifyMovementState()
    {
        if (settings.movementStateSettings.ejectPlayerFromDisabledStates && !CheckStateActivation(settings.movementStateSettings.movementState, settings.movementStateSettings.activeMovementStates))
        {
            ChangeState(MovementState.undefined);
        }

        //Prevents jump spamming
        settings.jumpingControls.isEvaluatingAttemptJump = Mathf.Clamp(settings.jumpingControls.isEvaluatingAttemptJump + Time.deltaTime, -1, 0);

        //Checks to see if player should be in an override state
        if (IdentifyOverrideState()) return;

        //Checks if player is grounded based on state specific raycast distance
        RaycastHit raycastHit;
        if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition, Vector3.down, out raycastHit, settings.basicControls.playerHeight + settings.raycastDistances.currentRaycastDistance, settings.layerMasks.groundLayer))
        {
            //Checks type of grounded state
            IdentifyGroundedState();
            return;
        }
        //Checks type of non-grounded state
        IdentifyNonGroundedState();
    }

    //Identifies the correct override state
    //returns true if the player is in an override state
    //States are tested in order of priority
    bool IdentifyOverrideState()
    {
        if (CheckGenericOverrideState(MovementState.ragdoll)) return true;
        if (CheckGenericOverrideState(MovementState.onRails)) return true;

        //Checks for water above player
        if (IdentifyswimmingState()) return true;

        //Flying is an override state only if automaticLand is set to false or if the player is above a disableLandLayer
        if (IdentifyFlyingState()) return true;

        if (CheckGenericOverrideState(MovementState.jumping)) return true;
        if (CheckGenericOverrideState(MovementState.blink)) return true;
        if (CheckGenericOverrideState(MovementState.lunge)) return true;
        if (CheckGenericOverrideState(MovementState.recovery)) return true;

        if (settings.movementStateSettings.requestedState == MovementState.falling && CheckStateActivation(MovementState.falling, settings.movementStateSettings.activeMovementStates))
        {
            ChangeState(MovementState.falling);
            settings.movementStateSettings.requestedState = MovementState.undefined;
            return true;
        }
        if (settings.movementStateSettings.requestedState == MovementState.grounded && CheckStateActivation(MovementState.grounded, settings.movementStateSettings.activeMovementStates))
        {
            ChangeState(MovementState.grounded);
            settings.movementStateSettings.requestedState = MovementState.undefined;
            return true;
        }
        return false;
    }
    //Preforms a generic override state check.
    //If the player is in an override state, then the player will more than likely just stay in that state
    bool CheckGenericOverrideState(MovementState mState)
    {
        if (!CheckStateActivation(mState, settings.movementStateSettings.activeMovementStates))
        {
            if (settings.movementStateSettings.requestedState == mState) settings.movementStateSettings.requestedState = MovementState.undefined;
            return false;
        }

        if (settings.movementStateSettings.movementState == mState)
        {
            return true;
        }
        else
        {
            if (settings.movementStateSettings.requestedState == mState)
            {
                ChangeState(mState);
                settings.movementStateSettings.requestedState = MovementState.undefined;
                return true;
            }
        }
        return false;
    }
    //Checks for the flying state
    bool IdentifyFlyingState()
    {
        if (!CheckStateActivation(MovementState.flying, settings.movementStateSettings.activeMovementStates)) return false;

        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            if (!settings.flyingControls.automaticLand) return true;
            RaycastHit raycastHit;
            if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition, Vector3.down, out raycastHit, 500, settings.layerMasks.disableLandLayer))
            {
                return true;
            }
        }
        else
        {
            if (settings.movementStateSettings.requestedState == MovementState.flying)
            {
                ChangeState(MovementState.flying);
                settings.movementStateSettings.requestedState = MovementState.undefined;
                return true;
            }
        }
        return false;
    }

    //Checks for water layer above player, and sets water level and swimming state if raycast is true
    bool IdentifyswimmingState()
    {
        if (!CheckStateActivation(MovementState.swimming, settings.movementStateSettings.activeMovementStates)) return false;

        RaycastHit raycastHit;
        if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition + new Vector3(0, settings.swimmingControls.swimmingRaycastOffset, 0), Vector3.up, out raycastHit, 1000, settings.layerMasks.waterLayer))
        {
            settings.swimmingControls.waterLevel = raycastHit.point.y;
            ChangeState(MovementState.swimming);
            return true;
        }
        return false;
    }
    void IdentifyGroundedState()
    {
        //Slide Physics Simulation output
        bool phyFallCheck;
        //Slide Physics Simulation output
        Vector3 referenceVector3;

        //Check if player should be sliding
        if (CheckStateActivation(MovementState.sliding, settings.movementStateSettings.activeMovementStates))
        {
            //If player is currently sliding, uses an extended test range
            if (settings.movementStateSettings.movementState == MovementState.sliding)
            {
                //Tests locations around player for steep ground angles
                if (SimulatePhysics(settings.directionalVectorModifiers.referencePosition, false, out referenceVector3, out phyFallCheck))
                {
                    //if simulation determined player should be falling not sliding
                    if (phyFallCheck)
                    {
                        if (settings.directionalVectorModifiers.currentRecovery < 0 && CheckStateActivation(MovementState.recovery, settings.movementStateSettings.activeMovementStates))
                        {
                            ChangeState(MovementState.recovery);
                            return;
                        }
                        if (CheckStateActivation(MovementState.grounded, settings.movementStateSettings.activeMovementStates))
                        {
                            ChangeState(MovementState.grounded);
                            return;
                        }
                    }
                    //if simulation determined player should be sliding
                    settings.slideControls.slideVector = referenceVector3;
                    ChangeState(MovementState.sliding);
                    return;
                }
            }
            else //If player is not currently sliding, uses a smaller test range
            {
                if (SimulatePhysics(settings.directionalVectorModifiers.referencePosition, true, out referenceVector3, out phyFallCheck))
                {
                    //if simulation determined player should be falling not sliding
                    if (phyFallCheck)
                    {
                        if (settings.directionalVectorModifiers.currentRecovery < 0 && CheckStateActivation(MovementState.recovery, settings.movementStateSettings.activeMovementStates))
                        {
                            ChangeState(MovementState.recovery);
                            return;
                        }
                        if (CheckStateActivation(MovementState.grounded, settings.movementStateSettings.activeMovementStates))
                        {
                            ChangeState(MovementState.grounded);
                            return;
                        }
                    }
                    //if simulation determined player should be sliding
                    settings.slideControls.slideVector = referenceVector3;
                    ChangeState(MovementState.sliding);
                    return;
                }
            }
        }

        //Checks if player should be in the recovery state. This state only overrides grounded movement.
        if (settings.directionalVectorModifiers.currentRecovery < 0 && CheckStateActivation(MovementState.recovery, settings.movementStateSettings.activeMovementStates))
        {
            ChangeState(MovementState.recovery);
            return;
        }

        //Sets the movement state to grounded if all other state checks have failed
        if (CheckStateActivation(MovementState.grounded, settings.movementStateSettings.activeMovementStates))
        ChangeState(MovementState.grounded);
    }

    //Identifies a non grounded state
    void IdentifyNonGroundedState()
    {
        //The flying state must be set, this is the flying state with automatic land 
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            return;
        }
        if (CheckStateActivation(MovementState.falling, settings.movementStateSettings.activeMovementStates))
        {
            //If the player isnt flying its falling:
            ChangeState(MovementState.falling);
        }
    }

    //Compares a movement state against the activation list
    bool CheckStateActivation(MovementState ms, EnabledStates es)
    {
        if (ms == MovementState.blink)
        {
            if (es.blinkMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.falling)
        {
            if (es.fallingMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.flying)
        {
            if (es.flyingMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.grounded)
        {
            if (es.groundedMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.jumping)
        {
            if (es.jumpingMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.onRails)
        {
            if (es.onRailsMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.ragdoll)
        {
            if (es.ragdollMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.recovery)
        {
            if (es.recoveryMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.lunge)
        {
            if (es.lungeMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.sliding)
        {
            if (es.slidingMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        if (ms == MovementState.swimming)
        {
            if (es.swimmingMovement == EnabledDisabled.disabled)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return true;
    }

    //Sets the movement state, called every frame
    public void ChangeState(MovementState newState)
    {
        if (newState == MovementState.grounded)
        {
            //Prevents player from glitching onto rail system
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed, settings.raycastDistances.groundRaycastDistance);

            //Calls the OnPlayerLandEvent
            if (settings.movementStateSettings.movementState == MovementState.falling)
            {
                mSEvents.OnPlayerLand.Invoke(settings.directionalVectorModifiers.fallTime);
                settings.directionalVectorModifiers.fallTime = 0;
            }
        }
        if (newState == MovementState.recovery)
        {
            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed, settings.raycastDistances.recoveryRaycastDistance);

            //Calls the OnPlayerLandEvent
            if (settings.movementStateSettings.movementState == MovementState.falling)
            {
                mSEvents.OnPlayerLand.Invoke(settings.directionalVectorModifiers.fallTime);
                settings.directionalVectorModifiers.fallTime = 0;
            }
        }
        if (newState == MovementState.sliding)
        {
            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.fallingCharacterRepSpeed, settings.raycastDistances.slidingRaycastDistance, settings.slideControls.slideRecoveryTime);

            //Calls the OnPlayerLandEvent
            if (settings.movementStateSettings.movementState == MovementState.falling)
            {
                mSEvents.OnPlayerLand.Invoke(settings.directionalVectorModifiers.fallTime);
                settings.directionalVectorModifiers.fallTime = 0;
            }
        }

        if (newState == MovementState.flying)
        {
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.flyingControls.flyingDrag, settings.visualRepresentation.repSpeed.flyingCharacterRepSpeed, settings.raycastDistances.flyingRaycastDistance);

            //This state interrupts MoveToTarget
            settings.moveToTargetFunctionality.moveToTarget = null;

            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Prevents the character flying animation from starting at a severe angle.
            if (newState != settings.movementStateSettings.movementState) 
            {
                SetAnimatorVelocity(0, 0);
            }


            //Resets fall time
            settings.directionalVectorModifiers.fallTime = 0;
        }

        if (newState == MovementState.jumping)
        {
            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.fallingCharacterRepSpeed, settings.raycastDistances.jumpingRaycastDistance, settings.jumpingControls.fallRecoveryTime);

            //Sets the player's velocity to zero before jumping
            if (settings.jumpingControls.zeroPreJumpVelocity)
            {
                settings.rigidbodyForceModifiers.playerRigidbody.velocity = new Vector3(0, 0, 0);
            }
            else
            {
                settings.rigidbodyForceModifiers.playerRigidbody.velocity = new Vector3(settings.rigidbodyForceModifiers.playerRigidbody.velocity.x * settings.jumpingControls.jumpHorizontalForceMultipler, 0, settings.rigidbodyForceModifiers.playerRigidbody.velocity.z * settings.jumpingControls.jumpHorizontalForceMultipler);
            }
        }

        if (newState == MovementState.swimming)
        {
            //Sets common state default values
            AdjustStateDefaults(settings.swimmingControls.swimmingDrag, settings.visualRepresentation.repSpeed.swimmingCharacterRepSpeed, settings.raycastDistances.swimmingRaycastDistance);

            //This state interrupts MoveToTarget
            settings.moveToTargetFunctionality.moveToTarget = null;

            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            settings.jumpingControls.jumpDirection = Vector3.zero;

            //Prevents the character swimming animation from starting at a severe angle.
            if (newState != settings.movementStateSettings.movementState)
            {
                SetAnimatorVelocity(0, 0);
            }

            //Resets fall time
            settings.directionalVectorModifiers.fallTime = 0;
        }

        if (newState == MovementState.falling)
        {
            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.fallingCharacterRepSpeed, settings.raycastDistances.fallingRaycastDistance, settings.jumpingControls.fallRecoveryTime);
        }

        if (newState == MovementState.onRails)
        {
            //This state interrupts MoveToTarget
            settings.moveToTargetFunctionality.moveToTarget = null;

            //Sets common state default values
            AdjustStateDefaults(settings.railSystemSettings.railSystem.primarySettings.onRailsDrag, settings.railSystemSettings.railSystem.primarySettings.characterRepSpeed, settings.raycastDistances.groundRaycastDistance, settings.railSystemSettings.railSystem.primarySettings.recovery);
            settings.jumpingControls.jumpDirection = Vector3.zero;

            //Resets fall time
            settings.directionalVectorModifiers.fallTime = 0;
        }
        if (newState == MovementState.ragdoll)
        {
            //Prevents player from glitching onto rail systems
            if (!settings.railSystemSettings.willDetachFromRail && settings.railSystemSettings.railSystem != null)
            {
                settings.railSystemSettings.willDetachFromRail = true;
            }
            //Sets common state default values
            settings.jumpingControls.jumpDirection = Vector3.zero;
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed, 0);

            foreach (Rigidbody rigidbody in localReferences.ragdoll)
            {
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, settings.mSRagdoll.initialRagdollYVelocity, rigidbody.velocity.z);
            }
        }
        if (newState == MovementState.blink)
        {
            settings.rigidbodyForceModifiers.playerRigidbody.velocity = new Vector3(0, 0, 0);
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.blinkControls.blinkCharacterRepSpeed, 0, settings.blinkControls.blinkRecoveryTime);
        }
        if (newState == MovementState.lunge)
        {
            AttemptLockRotation(3);
            AdjustStateDefaults(settings.rigidbodyForceModifiers.normalDrag, settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed, 0, settings.lungeControls.lungeAction.recoveryTime);
        }
        //Separates the ragdoll from the projected player
        if (settings.movementStateSettings.movementState == MovementState.ragdoll && newState != MovementState.ragdoll)
        {
            AttemptUnlockMovement(100);
            AttemptUnlockRotation(100);
        }

        //Calls the state change event
        if (newState != settings.movementStateSettings.movementState)
        {
            mSEvents.OnPlayerMovementStateChange.Invoke(newState);
        }

        //Sets the movement state
        settings.movementStateSettings.movementState = newState;
        if (debugModeEnabled) UnityEngine.Debug.Log(newState);

    }

    //Sets common state values
    void AdjustStateDefaults(float drag, float repSpeed, float raycastDistance)
    {
        settings.rigidbodyForceModifiers.playerRigidbody.drag = drag;
        settings.visualRepresentation.repSpeed.targetCharacterRepSpeed = repSpeed;
        settings.raycastDistances.currentRaycastDistance = raycastDistance;
    }

    //Sets common state values with a recovery time for when the player exits higher priority states
    void AdjustStateDefaults(float drag, float repSpeed, float raycastDistance, float recoveryTime)
    {
        settings.rigidbodyForceModifiers.playerRigidbody.drag = drag;
        settings.directionalVectorModifiers.currentRecovery = -recoveryTime;
        settings.visualRepresentation.repSpeed.targetCharacterRepSpeed = repSpeed;
        settings.raycastDistances.currentRaycastDistance = raycastDistance;
    }

    //Calculates the directional vectors based on the movement state
    void UpdateMovementByState()
    {
        //Checks if the acv has been received yet
        if (settings.acv == null) return;

        if (settings.movementStateSettings.movementState == MovementState.recovery)
        {
            UpdateRecovery();
        }
        else
        {
            AttemptUnlockMovement(2);
        }

        //locks player movement based on priority
        if (settings.basicControls.lockMovement != 0) return;

        if (settings.movementStateSettings.movementState == MovementState.grounded)
        {
            UpdateGroundedState();
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.sliding)
        {
            UpdateSlidingState();
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.onRails)
        {
            UpdateOnRailsState();
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.falling)
        {
            UpdateFallingState();
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            UpdateFlyingState();
            return;
        }
        if (settings.movementStateSettings.movementState == MovementState.swimming)
        {
            UpdateSwimmingState();
        }
        if (settings.movementStateSettings.movementState == MovementState.jumping)
        {
            UpdateJumpingState();
        }
        if (settings.movementStateSettings.movementState == MovementState.blink)
        {
            UpdateBlink();
        }
        if (settings.movementStateSettings.movementState == MovementState.ragdoll)
        {
            UpdateRagdoll();
        }
        if (settings.movementStateSettings.movementState == MovementState.lunge)
        {
            UpdateLunge();
        }
    }
    //Updates lunge state movement
    void UpdateLunge()
    {
        settings.lungeControls.currentLungeStep += Time.deltaTime;

        if (settings.lungeControls.lungeAction == null)
        {
            AttemptUnlockRotation(3);
            ChangeState(MovementState.undefined);
            return;
        }
        if (settings.lungeControls.currentLungeStep > settings.lungeControls.lungeAction.duration)
        {
            settings.lungeControls.currentLungeStep = 0;
            settings.lungeControls.lungeAction = null;
            AttemptUnlockRotation(3);
            ChangeState(MovementState.undefined);
            return;
        }

        float appliedIntensity = Mathf.Lerp(settings.lungeControls.lungeAction.intensity, 0, settings.lungeControls.currentLungeStep / settings.lungeControls.lungeAction.duration);
        settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition + Vector3.Normalize(settings.lungeControls.lungeAction.direction) * appliedIntensity - Vector3.up* settings.basicControls.playerHeight;
        UnityEngine.Debug.DrawRay(settings.directionalVectorModifiers.externalTargetValue, Vector3.up * 2, Color.red);
    }
    //Updates blink state movement
    void UpdateBlink()
    {
        settings.railSystemSettings.currentRailInteractionTime = 0;
        if (settings.blinkControls.blinkStateDelay != 0)
        {
            settings.blinkControls.blinkStateCurrentDelay += Time.deltaTime;
            if (settings.blinkControls.blinkStateCurrentDelay < settings.blinkControls.blinkStateDelay) return;
        }
        SetPlayerPosition((settings.blinkControls.calculatedBlinkTarget), false, false);
        if (Vector3.Distance(localReferences.characterRepresentation.position, settings.blinkControls.calculatedBlinkTarget) <= settings.blinkControls.blinkAchievementDistance)
        {
            settings.blinkControls.blinkStateDelay = 0;
            ChangeState(MovementState.undefined);
        }
    }

    //Keeps movement locked during the ragdoll state
    void UpdateRagdoll()
    {
        AttemptLockMovement(100);
        AttemptLockRotation(100);
    }

    //Updates the recovery state
    void UpdateRecovery()
    {
        //No movement occurs in this state
        AttemptLockMovement(2);
        settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition;

        settings.directionalVectorModifiers.currentRecovery += Time.deltaTime;
        if (settings.directionalVectorModifiers.currentRecovery >= 0)
        {
            settings.directionalVectorModifiers.currentRecovery = 0;
            
            //Marks the current state for requiring re-evaluation
            ChangeState(MovementState.undefined);
        }
    }

    //Updates the directional vector based on the grounded state
    void UpdateGroundedState()
    {
        //Returns if movement is locked
        if (settings.basicControls.lockMovement > 0) return;

        //Sets the base speed and slide check range
        settings.slideControls.currentSlideRange = settings.slideControls.minSlideRange;

        if (settings.rootMotionSettings.enableRootMotion)
        {
            settings.directionalVectorModifiers.currentTransitionalSpeed = settings.rootMotionSettings.rootMotionSpeed * settings.rootMotionSettings.rootMotionModifier;
        }
        else
        {
            settings.directionalVectorModifiers.currentTransitionalSpeed = settings.directionalVectorModifiers.groundedTransitionalSpeed;
        }

        SetCurrentSubStateConditions();

        //Checks if the projected player should move based on target based movement or player input
        if (settings.groundedSubstateModifiers.groundedMovementMethod == MultiStateSettings.GroundedSubstateModifiers.GroundedMovementMethod.targetBased)
        {
            UpdateTargetBasedMovement();
        }
        else
        {
            UpdateControlVectorBasedMovement();
        }
    }

    //Sets grounded speed multiplier based on the grounded substate
    void SetCurrentSubStateConditions()
    {

        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.sprinting)
        {
            settings.groundedSubstateModifiers.currentSubstateMultiplier = settings.groundedSubstateModifiers.sprintingSpeedMultiplier;
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.running)
        {
            settings.groundedSubstateModifiers.currentSubstateMultiplier = settings.groundedSubstateModifiers.runningSpeedMultiplier;
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.walking)
        {
            settings.groundedSubstateModifiers.currentSubstateMultiplier = settings.groundedSubstateModifiers.walkingSpeedMultiplier;
        }
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.crouching)
        {
            settings.groundedSubstateModifiers.currentSubstateMultiplier = settings.groundedSubstateModifiers.crouchingSpeedMultiplier;
        }
    }

    //Sets the directional vector of the player based on target based movement
    void UpdateTargetBasedMovement()
    {
        //Sets the directional vector to be forward of the player
        //Rotation is handled above in the rotation section
        Vector3 groundedVector = GroundVector(settings.directionalVectorModifiers.referencePosition + transform.forward * settings.groundedSubstateModifiers.currentSubstateMultiplier);
        settings.directionalVectorModifiers.externalTargetValue = groundedVector;
    }

    //Sets the directional vector based on the player input
    void UpdateControlVectorBasedMovement()
    {
        //Sets the directional vector based on the player input
        Vector3 groundedVector = GroundVector(settings.directionalVectorModifiers.referencePosition + settings.acv.flatVector * settings.groundedSubstateModifiers.currentSubstateMultiplier);
        settings.directionalVectorModifiers.externalTargetValue = groundedVector;
    }

    //Sets the directional vector based on the sliding state
    void UpdateSlidingState()
    {
        //Sets the directional vector based the results of the physics simulation that occurred during the state identification checks.
        //Increases the slide check range while sliding
        //The directional vector is not set on the first frame of the slide due to the original checks small range.
        if (settings.slideControls.currentSlideRange == settings.slideControls.maxSlideRange)
        {
            settings.directionalVectorModifiers.currentTransitionalSpeed = settings.directionalVectorModifiers.groundedTransitionalSpeed * settings.slideControls.slideSpeedMultiplier;
            settings.directionalVectorModifiers.externalTargetValue = settings.slideControls.slideVector;
        }

        settings.slideControls.currentSlideRange = settings.slideControls.maxSlideRange;
    }

    //Sets the directional vector based on the jumping state
    void UpdateJumpingState()
    {
        //Returns if movement is locked
        if (settings.basicControls.lockMovement > 0) return;

        //Advances the jump time
        settings.jumpingControls.currentJumpTime = settings.jumpingControls.currentJumpTime + Time.deltaTime;
        if (settings.jumpingControls.currentJumpTime >= -settings.jumpingControls.jumpDuration)
        {
            //Reduces jump force over the jump duration
            float force = Mathf.Lerp(settings.jumpingControls.jumpForce, 0, settings.jumpingControls.currentJumpTime / settings.jumpingControls.jumpDuration);
            float horizontalMultiplier = Mathf.Lerp(settings.jumpingControls.jumpHorizontalForceMultipler, 0, settings.jumpingControls.currentJumpTime / -settings.jumpingControls.jumpDuration);

            //Sets the directional vector based on the player default jump or an external direction
            if (settings.jumpingControls.jumpDirection == Vector3.zero)
            {
                settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition + new Vector3(settings.acv.flatVector.x * horizontalMultiplier*force, force, settings.acv.flatVector.z * horizontalMultiplier * force);
            }
            else
            {
                settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition + new Vector3(settings.jumpingControls.jumpDirection.x * horizontalMultiplier * force, settings.jumpingControls.jumpDirection.y * force, settings.jumpingControls.jumpDirection.z * horizontalMultiplier * force);
            }
        }
        if (settings.jumpingControls.currentJumpTime >= 0)
        {
            //Marks the set as requiring re-evaluation
            ChangeState(MovementState.undefined);
        }
    }

    //Sets the directional vector based on the falling state
    void UpdateFallingState()
    {
        if (settings.directionalVectorModifiers.fallTime == 0) settings.jumpingControls.jumpStartingVelocity = settings.rigidbodyForceModifiers.playerRigidbody.velocity;

        //Records the amount of time the player has been falling.
        settings.directionalVectorModifiers.fallTime += Time.deltaTime;

        //Slowly Reduces Velocity 
        float fallx = Mathf.Lerp(settings.jumpingControls.jumpStartingVelocity.x, 0, Mathf.Clamp(0.2f + settings.rigidbodyForceModifiers.fallVelocityPreservationFactor * (settings.directionalVectorModifiers.fallTime * settings.directionalVectorModifiers.fallTime), 0, 1));
        float fallY = Mathf.Lerp(settings.jumpingControls.jumpStartingVelocity.y, (settings.rigidbodyForceModifiers.gravity - settings.basicControls.playerHeight), 0.2f + Mathf.Clamp(settings.rigidbodyForceModifiers.fallVelocityYPreservationFactor *(settings.directionalVectorModifiers.fallTime* settings.directionalVectorModifiers.fallTime), 0, 1));
        float fallz = Mathf.Lerp(settings.jumpingControls.jumpStartingVelocity.z, 0, Mathf.Clamp(0.2f + settings.rigidbodyForceModifiers.fallVelocityPreservationFactor * (settings.directionalVectorModifiers.fallTime * settings.directionalVectorModifiers.fallTime), 0, 1));

        //Sets the directional vector below the player
        settings.directionalVectorModifiers.externalTargetValue = settings.directionalVectorModifiers.referencePosition + new Vector3(fallx, fallY, fallz);

        //Slowly adds horizontal force if the player is stuck.
        CheckForFallProtection();
    }

    //Slowly adds horizontal force if the player is stuck.
    void CheckForFallProtection()
    {
        //Checks if the player is moving and records time if it isnt moving
        if (settings.rigidbodyForceModifiers.playerRigidbody.velocity.magnitude < 1)
        {
            settings.fallProtection.currentFallProtectionRecordTime += Time.deltaTime;
        }
        else
        {
            settings.fallProtection.currentFallProtectionRecordTime = 0;
        }

        //Add random horizontal force in proportion in the amount of time not spent moving
        if (settings.fallProtection.currentFallProtectionRecordTime > settings.fallProtection.minimumFallTime)
        {
            float correctiveForce = Mathf.Clamp(settings.fallProtection.currentFallProtectionRecordTime * settings.fallProtection.fallProtectionForceMultiplier, 0, settings.fallProtection.fallProtectionMaximumForce);
            float randomX = UnityEngine.Random.Range(-1, 2);
            float randomZ = UnityEngine.Random.Range(-1, 2);
            settings.rigidbodyForceModifiers.playerRigidbody.AddForce(correctiveForce * randomX, 0, correctiveForce * randomZ);
        }
    }

    //Sets the directional vector based on the flying state
    void UpdateFlyingState()
    {
        //Returns if movement is locked
        if (settings.basicControls.lockMovement > 0) return;

        //Sets the initial direction
        Vector3 baseflyingVector = settings.acv.freeVector * settings.flyingControls.flyingBaseSpeedMultiplier;

        //Applies constant forward if required
        if (settings.flyingControls.flyingConstantForward)
        {
            if (!settings.acv.lockRotation)
            {
                baseflyingVector = (transform.forward * settings.flyingControls.flyingBaseSpeedMultiplier);
            }
            else if (settings.acv.isClassic)
            {
                baseflyingVector = (Vector3.Normalize(settings.acv.freeVector+transform.forward*2) * settings.flyingControls.flyingBaseSpeedMultiplier);
            }
            else
            {
                baseflyingVector = (settings.acv.cameraRigX.forward * settings.flyingControls.flyingBaseSpeedMultiplier);
            }
        }

        //Gets the world position from the directional vector so climbing angle can be calculated
        Vector3 calculatedWorldPosition = settings.directionalVectorModifiers.referencePosition + (baseflyingVector);

        //Calculates climbing angle
        float forwardDistance, heightDistance;
        GetClimbingDistances(calculatedWorldPosition, out forwardDistance, out heightDistance);
        float climbingAngle = GetClimbAngle(heightDistance, forwardDistance);

        //Sets the climbing angle to 90 if the player is not moving forwards or backwards
        if (forwardDistance == 0) climbingAngle = 90;

        //Multiplies the vector by the ascension multiplier if required
        if (settings.flyingControls.flyingAdditiveAscensionMultiplier != 0 && calculatedWorldPosition.y > settings.directionalVectorModifiers.referencePosition.y) //Asscension
        {
            baseflyingVector += baseflyingVector * ((climbingAngle / 90) * settings.flyingControls.flyingAdditiveAscensionMultiplier);
        }
        //Multiplies the vector by the dive multiplier if required
        if (settings.flyingControls.flyingAdditiveDiveMultiplier != 0 && calculatedWorldPosition.y < settings.directionalVectorModifiers.referencePosition.y) //Dive
        {
            baseflyingVector += baseflyingVector * ((climbingAngle / 90) * settings.flyingControls.flyingAdditiveDiveMultiplier);
        }

        //Adds rigidbodyForceModifiers.gravity if required
        Vector3 flyingGravityCalc = new Vector3(0, settings.flyingControls.flyingGravity, 0);

        //Sets the directional vector
        calculatedWorldPosition = settings.directionalVectorModifiers.referencePosition + (baseflyingVector) + flyingGravityCalc;
        settings.directionalVectorModifiers.externalTargetValue = calculatedWorldPosition;
    }

    //Sets the directional vector based on the swimming state
    void UpdateSwimmingState()
    {
        //Returns if movement is locked
        if (settings.basicControls.lockMovement > 0) return;

        if (!settings.swimmingControls.allowDive)
        {
            settings.acv.freeVector.y = 1;
        }

        Vector3 freeVector = settings.acv.freeVector;

        float waterDepth = GetHeightDistance(Vector3.up * settings.swimmingControls.waterLevel, settings.directionalVectorModifiers.referencePosition);
        if ((Vector3.up * settings.swimmingControls.waterLevel).y < settings.directionalVectorModifiers.referencePosition.y)
        {
            waterDepth *= -1;
        }

        if (freeVector.y > 0 && waterDepth <  - settings.basicControls.playerHeight/2 )
        {
            freeVector.y = 0;
            freeVector.Normalize();
        }


        //Sets the initial vector based on whether or not diving is allowed
        Vector3 calcPos = ((freeVector) * settings.swimmingControls.swimmingSpeedMultiplier) + settings.directionalVectorModifiers.referencePosition;

        //Adjusts the vector for surface break
        if (calcPos.y + settings.swimmingControls.swimmingRaycastOffset + settings.swimmingControls.surfaceBreakAdjustment > settings.swimmingControls.waterLevel) calcPos.y = settings.swimmingControls.waterLevel - settings.swimmingControls.swimmingRaycastOffset - settings.swimmingControls.surfaceBreakAdjustment;

        
        

        //Sets the directional vector
        settings.directionalVectorModifiers.externalTargetValue = calcPos;
    }

    //Sets the directional vector based on the OnRail state and modifies character representation rotation target 
    void UpdateOnRailsState()
    {
        if (!settings.railSystemSettings.railSystem) return;
        if (settings.railSystemSettings.railSystem.primarySettings.enabledCameraBasedDirection)
        {
            settings.railSystemSettings.railDirection = settings.acv.calculatedRailDirection;
        }
        //Sets the directional vector based on the rail system control type
        if (settings.railSystemSettings.railSystem.primarySettings.keyControlled)
        {
            settings.railSystemSettings.railPosition = Mathf.Clamp(settings.railSystemSettings.railPosition + Input.GetAxis(settings.railSystemSettings.railSystem.primarySettings.keyAxis) * settings.railSystemSettings.railSystem.primarySettings.speed * Time.deltaTime * settings.railSystemSettings.railDirection, 0, settings.railSystemSettings.railSystem.universalSettings.duration);
        }
        if (settings.railSystemSettings.railSystem.primarySettings.timeControlled)
        {
            settings.railSystemSettings.railPosition = Mathf.Clamp(settings.railSystemSettings.railPosition + Time.deltaTime * settings.railSystemSettings.railDirection * settings.railSystemSettings.railSystem.primarySettings.speed, 0, settings.railSystemSettings.railSystem.universalSettings.duration);
        }

        //Sets the directional vector
        settings.directionalVectorModifiers.externalTargetValue = settings.railSystemSettings.railSystem.GetRailLocation(settings.railSystemSettings.railPosition) + new Vector3(0, settings.railSystemSettings.railSystem.primarySettings.yValueOffset, 0);
    }

    //***************************************************************************************************************************************************************
    //public functions

    //Returns of the amount of time that the player has been in the falling state.
    public float GetPlayerFallTime()
    {
        return settings.directionalVectorModifiers.fallTime;
    }

    //Gets active camera dolly
    public Transform GetCameraDolly()
    {
        return settings.railSystemSettings.activeCameraDolly;
    }

    //Gets the value of the isGrounded variable
    public bool GetIsGrounded(bool actual)
    {
        if (actual) return settings.basicControls.isGrounded;
        if (settings.movementStateSettings.movementState == MovementState.grounded) return true;
        return false;
    }

    //Attempts to enter flying state from non grounded, non swimming state
    public void AttemptFlyFromFall()
    {
        //Checks to see if player will immediately land
        if (settings.flyingControls.automaticLand)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition, Vector3.down, out raycastHit, settings.raycastDistances.flyingRaycastDistance + settings.basicControls.playerHeight + 0.05f, settings.layerMasks.groundLayer))
            {
                return;
            }
        }

        //Enters flying state if currently falling or jumping
        if (settings.movementStateSettings.movementState == MovementState.falling || settings.movementStateSettings.movementState == MovementState.jumping)
        {
            settings.jumpingControls.isEvaluatingAttemptJump -= 0.3f;
            settings.movementStateSettings.requestedState = MovementState.flying;
            //ChangeState(MovementState.flying);
            settings.rigidbodyForceModifiers.playerRigidbody.velocity = Vector3.zero;
            //Prevents the player from immediately landing
            settings.rigidbodyForceModifiers.playerRigidbody.AddForce(new Vector3(0, 50, 0));
            return;
        }

        //Exits flying state if currently flying
        if (settings.movementStateSettings.movementState == MovementState.flying)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition, Vector3.down, out raycastHit, 500, settings.layerMasks.disableLandLayer))
            {
                return;
            }

            settings.jumpingControls.isEvaluatingAttemptJump -= 0.3f;
            settings.movementStateSettings.requestedState = MovementState.falling;
            return;
        }
    }

    //Sets the character controller's acv value
    //Return Transform is activeDollyValue 
    public Transform UpdateACV(AdvancedControlValue advancedControlValue, out List<KeyCode> keySet, out Transform[] railSystemBrackets)
    {
        settings.acv = advancedControlValue;
        if (settings.movementStateSettings.movementState == MovementState.onRails)
        {
            keySet = settings.railSystemSettings.railSystem.primarySettings.unlatchKeyset;
            if (settings.railSystemSettings.railSystem.primarySettings.enabledCameraBasedDirection)
            {
                settings.railSystemSettings.railSystem.GetEndWaypoints(out railSystemBrackets);
            }
            else
            {
                railSystemBrackets = new Transform[0];
            }
        }
        else
        {
            keySet = new List<KeyCode>();
            railSystemBrackets = new Transform[0];
        }
        return settings.railSystemSettings.activeCameraDolly;
    }
    //Sets animator trigger 'RailExit'
    public void ExitOnRailsAnimationLoop()
    {
        SetAnimatorTrigger("RailExit");
    }
    //Enters into the walk substate while attached to a rail system
    public void SetGroundedWalkSubstate(bool exitOnRailsLoop)
    {
        SetGroundedMovementSubstate(GroundedMovementSubState.walking);
        if (exitOnRailsLoop) ExitOnRailsAnimationLoop();
    }
    //Enters into the crouch substate while attached to a rail system
    public void SetGroundedCrouchSubstate(bool exitOnRailsLoop)
    {
        SetGroundedMovementSubstate(GroundedMovementSubState.crouching);
        if (exitOnRailsLoop) ExitOnRailsAnimationLoop();
    }
    //Enters into the run substate while attached to a rail system
    public void SetGroundedRunSubstate(bool exitOnRailsLoop)
    {
        SetGroundedMovementSubstate(GroundedMovementSubState.running);
        if (exitOnRailsLoop) ExitOnRailsAnimationLoop();
    }
    //Enters into the sprint substate while attached to a rail system
    public void SetGroundedSprintSubstate(bool exitOnRailsLoop)
    {
        SetGroundedMovementSubstate(GroundedMovementSubState.sprinting);
        if (exitOnRailsLoop) ExitOnRailsAnimationLoop();
    }

    //Sets grounded movement substate and adjusts player collider height if required
    public void SetGroundedMovementSubstate(GroundedMovementSubState subState)
    {
        if (settings.groundedSubstateModifiers.groundedMovementSubState == GroundedMovementSubState.crouching)
        {
            if (subState != GroundedMovementSubState.crouching)
            {
                float radius = localReferences.physicsGyro.GetComponent<CapsuleCollider>().radius;
                Vector3 position = settings.directionalVectorModifiers.referencePosition - Vector3.up * radius;
                Collider[] hitColliders = Physics.OverlapSphere(position, radius, settings.layerMasks.overheadLayer);

                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.left * radius, Color.yellow);
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.right * radius, Color.yellow);
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.up * radius, Color.yellow);
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.down * radius, Color.yellow);
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.forward * radius, Color.yellow);
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(position, position + Vector3.back * radius, Color.yellow);

                if (hitColliders.Length != 0)
                {
                    return;
                }
            }
        }

        settings.groundedSubstateModifiers.groundedMovementSubState = subState;
    }

    //Toggles between the default substate and another substate
    public void ToggleGroundedMovementSubstate(GroundedMovementSubState subState)
    {
        if (subState == GroundedMovementSubState.defaultSubstate)
            subState = settings.groundedSubstateModifiers.defaultGroundedMovementSubState;

        if (subState == GroundedMovementSubState.defaultSubstate)
        {
            UnityEngine.Debug.LogError("default substate must be set!");
            return;
        }

        if (settings.groundedSubstateModifiers.groundedMovementSubState == subState && subState != settings.groundedSubstateModifiers.defaultGroundedMovementSubState)
        {
            SetGroundedMovementSubstate(settings.groundedSubstateModifiers.defaultGroundedMovementSubState);
            return;
        }
        SetGroundedMovementSubstate(subState);
    }
    //Sets running as the default grounded substate
    public void SetRunningAsDefaultGroundedMovementSubstate(bool switchActiveState = true)
    {
        SetDefaultGroundedMovementSubstate(GroundedMovementSubState.running, switchActiveState);
    }
    //Sets walking as the default grounded substate
    public void SetWalkingAsDefaultGroundedMovementSubstate(bool switchActiveState = true)
    {
        SetDefaultGroundedMovementSubstate(GroundedMovementSubState.walking, switchActiveState);
    }
    //Sets crouching as the default grounded substate
    public void SetCrouchingAsDefaultGroundedMovementSubstate(bool switchActiveState = true)
    {
        SetDefaultGroundedMovementSubstate(GroundedMovementSubState.crouching, switchActiveState);
    }
    //Sets sprinting as the default grounded substate
    public void SetSprintingAsDefaultGroundedMovementSubstate(bool switchActiveState = true)
    {
        SetDefaultGroundedMovementSubstate(GroundedMovementSubState.sprinting, switchActiveState);
    }
    //Sets a substate as the default grounded substate
    public void SetDefaultGroundedMovementSubstate(GroundedMovementSubState subState, bool switchActiveState = true)
    {
        settings.groundedSubstateModifiers.defaultGroundedMovementSubState = subState;
        if (switchActiveState)
            SetGroundedMovementSubstate(settings.groundedSubstateModifiers.defaultGroundedMovementSubState);
    }

    //Forces the jump state regardless of any conditions including grounding
    public void ForceJump(Vector3 direction)
    {
        settings.jumpingControls.jumpDirection = direction;
        settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration - settings.jumpingControls.jumpDelay;
        settings.movementStateSettings.requestedState = MovementState.jumping;
    }
    //Forces the jump state regardless of any conditions including grounding, uses local position of a gameobject as the direction
    public void ForceJump(Transform direction)
    {
        settings.jumpingControls.jumpDirection = direction.localPosition;
        settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration - settings.jumpingControls.jumpDelay;
        settings.movementStateSettings.requestedState = MovementState.jumping;
    }

    //Attempts to jump without a preset direction
    public void AttemptJump()
    {
        //Prevents jump spamming
        if (settings.jumpingControls.isEvaluatingAttemptJump < 0) { return; }
        settings.jumpingControls.isEvaluatingAttemptJump -= 0.1f;

        //Check to see if player is trying to fly
        if (settings.flyingControls.enableFlyingFromFalling) AttemptFlyFromFall();

        //Checks to see if player is already jumping
        if (settings.movementStateSettings.movementState == MovementState.jumping) { return; }

        if (settings.moveToTargetFunctionality.moveToTarget != null) return;

        //Checks to see if player needs to recover
        if (settings.directionalVectorModifiers.currentRecovery < 0) { return; }

        RaycastHit raycastHit;
        if (Physics.Raycast(settings.directionalVectorModifiers.referencePosition-Vector3.up*(settings.basicControls.playerHeight/2), Vector3.up, out raycastHit,settings.basicControls.playerHeight, settings.layerMasks.overheadLayer))
        {
            return;
        }

        //Checks if ground is required
        if (settings.jumpingControls.requireGroundState)
        {
            //Checks for grounding
            if (settings.movementStateSettings.movementState == MovementState.grounded)
            {
                //Jumps
                settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration - settings.jumpingControls.jumpDelay;
                settings.movementStateSettings.requestedState = MovementState.jumping;
            }
        }
        else
        {
            //Jumps
            settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration;
            settings.movementStateSettings.requestedState = MovementState.jumping;
        }
    }

    //Attempts to jump with a preset direction
    public void AttemptJump(Vector3 direction)
    {
        //Prevents jump spamming
        if (settings.jumpingControls.isEvaluatingAttemptJump < 0) { return; }
        settings.jumpingControls.isEvaluatingAttemptJump -= 0.1f;

        //Check to see if player is trying to fly
        if (settings.flyingControls.enableFlyingFromFalling) AttemptFlyFromFall();

        //Checks to see if player is already jumping
        if (settings.movementStateSettings.movementState == MovementState.jumping) { return; }

        //Checks to see if player needs to recover
        if (settings.directionalVectorModifiers.currentRecovery < 0) { return; }

        //Checks if ground is required
        if (settings.jumpingControls.requireGroundState)
        {

            //Checks for grounding
            if (settings.movementStateSettings.movementState == MovementState.grounded)
            {
                //Sets jump direction
                settings.jumpingControls.jumpDirection = direction;

                //Jumps
                settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration - settings.jumpingControls.jumpDelay;
                settings.movementStateSettings.requestedState = MovementState.jumping;
            }
        }
        else
        {
            //Sets jump direction
            settings.jumpingControls.jumpDirection = direction;

            //Jumps
            settings.jumpingControls.currentJumpTime = -settings.jumpingControls.jumpDuration;
            settings.movementStateSettings.requestedState = MovementState.jumping;
        }
    }

    //Receives basic information from the rail system cap without a camera dolly
    public int EvaluateRail(MultiStateMovementRailSystem rSystem, float position, float direction, bool allowLatch, bool allowUnlatch, MultiStateSettings.IKSettings.FeetLocationMethod feetLocation, MultiStateSettings.IKSettings.HandLocationMethod handLocation, Transform ikTargetParentTransform, float rPMultiplier, float rPOffset)
    {
        if (settings.railSystemSettings.currentRailInteractionTime != 0) return 0;
        settings.railSystemSettings.currentRailInteractionTime = -settings.railSystemSettings.railInteractionTime;

        //Check if the player should be unlatching from a rail system
        if (settings.railSystemSettings.railSystem == rSystem && allowUnlatch)
        {
            //Set the ik location methods to their defaults
            settings.iKSettings.handLocationMethod = MultiStateSettings.IKSettings.HandLocationMethod.none;
            settings.iKSettings.feetLocationMethod = MultiStateSettings.IKSettings.FeetLocationMethod.projectDown;
            settings.iKSettings.ikTargetSet.Clear();

            //Detaches from the rail system
            DetachFromRail(MovementState.undefined);

            //Lets the rail cap know the player unlatched. -1 is unlatched, 1 is latched
            return -1;
        }

        //Check if the player should be latching to a rail system
        if (settings.railSystemSettings.railSystem != rSystem && allowLatch)
        {
            if (settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.jumping || settings.movementStateSettings.movementState == MovementState.falling)
            {
                //Set the ik location methods to the rail system requirements 
                settings.iKSettings.handLocationMethod = handLocation;
                settings.iKSettings.feetLocationMethod = feetLocation;
                EvaluateIkTargetSet(ikTargetParentTransform);

                //Sets the rail position
                settings.railSystemSettings.animationRailPositionMultiplier = rPMultiplier;
                settings.railSystemSettings.animationRailPositionOffset = rPOffset;

                //Attaches the player to the rail system
                AttachToRail(rSystem, position, direction);

                //Lets the rail cap know the player has latched. -1 is unlatched, 1 is latched
                return 1;
            }
        }

        //Lets the rail cap know nothing has happened. -1 is unlatched, 1 is latched
        return 0;
    }

    //Receives basic information from the rail system cap with a camera dolly
    public int EvaluateRail(MultiStateMovementRailSystem rSystem, float position, float direction, bool allowLatch, bool allowUnlatch, MultiStateSettings.IKSettings.FeetLocationMethod feetLocation, MultiStateSettings.IKSettings.HandLocationMethod handLocation, Transform ikTargetParentTransform, float rPMultiplier, float rPOffset, Transform cameraDolly)
    {
        if (settings.railSystemSettings.currentRailInteractionTime != 0) return 0;
        settings.railSystemSettings.currentRailInteractionTime = -settings.railSystemSettings.railInteractionTime;

        //Check if the player should be unlatching from a rail system
        if (settings.railSystemSettings.railSystem == rSystem && allowUnlatch)
        {
            //Set the ik location methods to their defaults
            settings.iKSettings.handLocationMethod = MultiStateSettings.IKSettings.HandLocationMethod.none;
            settings.iKSettings.feetLocationMethod = MultiStateSettings.IKSettings.FeetLocationMethod.projectDown;
            settings.iKSettings.ikTargetSet.Clear();

            //Detaches from the rail system
            DetachFromRail(MovementState.undefined);

            //Lets the rail cap know the player unlatched. -1 is unlatched, 1 is latched
            return -1;
        }

        //Check if the player should be latching to a rail system
        if (settings.railSystemSettings.railSystem != rSystem && allowLatch)
        {
            if (settings.movementStateSettings.movementState == MovementState.grounded || settings.movementStateSettings.movementState == MovementState.jumping || settings.movementStateSettings.movementState == MovementState.falling || settings.movementStateSettings.movementState == MovementState.falling)
            {
                //Set the ik location methods to the rail system requirements 
                settings.iKSettings.handLocationMethod = handLocation;
                settings.iKSettings.feetLocationMethod = feetLocation;
                EvaluateIkTargetSet(ikTargetParentTransform);

                //Sets the rail position
                settings.railSystemSettings.animationRailPositionMultiplier = rPMultiplier;
                settings.railSystemSettings.animationRailPositionOffset = rPOffset;

                //Attaches the player to the rail system
                AttachToRail(rSystem, position, direction, cameraDolly);

                //Lets the rail cap know the player has latched. -1 is unlatched, 1 is latched
                return 1;
            }
        }

        //Lets the rail cap know nothing has happened. -1 is unlatched, 1 is latched
        return 0;
    }
    //Forces a rail-system detachment
    public void ExitRailSystem(string key = "")
    {
        if (settings.movementStateSettings.movementState != MovementState.onRails) return;
        if (settings.railSystemSettings.currentRailInteractionTime != 0) return;
        settings.railSystemSettings.currentRailInteractionTime = -settings.railSystemSettings.railInteractionTime;

        //Set the ik location methods to their defaults
        settings.iKSettings.handLocationMethod = MultiStateSettings.IKSettings.HandLocationMethod.none;
        settings.iKSettings.feetLocationMethod = MultiStateSettings.IKSettings.FeetLocationMethod.projectDown;
        settings.iKSettings.ikTargetSet.Clear();

        //Detaches from the rail system
        DetachFromRail(MovementState.undefined, key);
    }

    //Detaches the player from a rail system
    public void DetachFromRail(string key = "")
    {
        UnityEngine.Debug.Log("detach: " + key);
        //Detaches the camera from the dolly
        settings.railSystemSettings.activeCameraDolly = null;

        //Attempts to unlock player rotation, a priority of 5 is used for rail systems
        AttemptUnlockRotation(5);

        //Changes the state
        ChangeState(MovementState.undefined);

        //Triggers the rail system unlatch event 
        settings.railSystemSettings.railSystem.UnlatchFromRail(key);

        settings.railSystemSettings.willDetachFromRail = true;
    }

    //Detaches the player from a rail system
    public void DetachFromRail(MovementState newState, string key = "")
    {

        //Detaches the camera from the dolly
        settings.railSystemSettings.activeCameraDolly = null;

        //Attempts to unlock player rotation, a priority of 5 is used for rail systems
        AttemptUnlockRotation(5);

        //Changes the state
        ChangeState(newState);

        //Triggers the rail system unlatch event 
        settings.railSystemSettings.railSystem.UnlatchFromRail(key);

        settings.railSystemSettings.willDetachFromRail = true;
    }

    //Attaches the player to a rail system 
    void AttachToRail(MultiStateMovementRailSystem rSystem, float position, float direction)
    {

        //Attempts to lock player rotation, a priority of 5 is used for rail systems
        AttemptLockRotation(5);

        //Sets common rail system variables
        settings.railSystemSettings.railSystem = rSystem;
        settings.railSystemSettings.railPosition = position;
        settings.railSystemSettings.railDirection = direction;
        settings.railSystemSettings.hasCalledOnRailsTrigger = false;

        ResetAnimatorTrigger("RailExit");

        //Changes the movement state
        settings.movementStateSettings.requestedState = MovementState.onRails;
    }

    //Attaches the player to a rail system and the camera to a dolly
    void AttachToRail(MultiStateMovementRailSystem rSystem, float position, float direction, Transform cameraDolly)
    {

        //attaches the camera to a dolly, will take affect when the mscc input script updates the acv
        settings.railSystemSettings.activeCameraDolly = cameraDolly;

        //Attempts to lock player rotation, a priority of 5 is used for rail systems
        AttemptLockRotation(5);

        //Sets common rail system variables
        settings.railSystemSettings.railSystem = rSystem;
        settings.railSystemSettings.railPosition = position;
        settings.railSystemSettings.railDirection = direction;
        settings.railSystemSettings.hasCalledOnRailsTrigger = false;

        ResetAnimatorTrigger("RailExit");

        //Changes the movement state
        settings.movementStateSettings.requestedState = MovementState.onRails;
    }

    //Attempts to lock player rotation
    //A priority of 5 is used for rail systems
    public void AttemptLockRotation(int priority)
    {
        if (priority > settings.basicControls.lockRotation)
        {
            settings.basicControls.lockRotation = priority;
        }
    }

    //Attempts to unlock player rotation
    //A priority of 5 is used for rail systems
    public void AttemptUnlockRotation(int priority)
    {
        if (priority >= settings.basicControls.lockRotation)
        {
            settings.basicControls.lockRotation = 0;
        }
    }

    //Attempts to lock player rotation
    public void AttemptLockMovement(int priority)
    {
        if (priority > settings.basicControls.lockMovement)
        {
            settings.basicControls.lockMovement = priority;
        }
    }

    //Attempts to unlock player rotation
    public void AttemptUnlockMovement(int priority)
    {
        if (priority >= settings.basicControls.lockMovement)
        {
            settings.basicControls.lockMovement = 0;
        }
    }

    //Returns the percentage of the current character rep speed vs the grounded character rep speed
    public float GetCurrentCharacterRepAdjSpeedPercent()
    {
        return settings.visualRepresentation.repSpeed.currentCharacterRepSpeed / settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed;
    }

    //Sets the player position to a specific location
    public void SetPlayerPosition(Vector3 position, bool snapCharacterRep = true, bool resetState = true)
    {
        settings.rigidbodyForceModifiers.playerRigidbody.position = (position);

        if (snapCharacterRep)
            localReferences.characterRepresentation.position = position;

        if (resetState)
            ChangeState(MovementState.undefined);
    }

    //Requests the ragdoll state
    public void EnableRagdoll()
    {
        settings.movementStateSettings.requestedState = MovementState.ragdoll;
    }

    //Exits the ragdoll state
    public void DisableRagdoll()
    {
        ChangeState(MovementState.undefined);
    }
    //Cleanly changes the movement state
    public void RequestState(MovementState movementState)
    {
        settings.movementStateSettings.requestedState = movementState;
    }

    //Lunge
    public void RequestLunge(LungeAction lA)
    {
        if (settings.lungeControls.currentLungeStep != 0) return;
        if (settings.directionalVectorModifiers.currentRecovery != 0) return;
        if (!CheckStateActivation(settings.movementStateSettings.movementState, lA.canTransitionFrom)) return;
        if (lA.direction == Vector3.zero) return;

        settings.lungeControls.lungeAction = lA;
        settings.lungeControls.currentLungeStep = 0;
        settings.basicAnimationSettings.commonAnimationTrigger = lA.animationTrigger;
        settings.movementStateSettings.requestedState = MovementState.lunge;
    }

    //Blink
    public enum BlinkTravelMethod
    {
        characterBased,
        cameraBased,
        worldBased,
        globalObjectBased,
        relativeObjectBased
    }
    //Requests the blink state
    public void RequestBlink(float rBlinkCharacterRepSpeed, Vector3 blinkDirection, BlinkTravelMethod blinkTravelMethod, bool rBlinkProjectForward, bool groundVector, string animationName = "", bool snapRotation = false, float stateDelay = 0, float newRecoveryTime = 0f, float newAchievementDistance = 0.4f, float newMaxHeightChange = 2f, float minDistanceChange = 0.5f, float animationHeightOffset = 0)
    {
        
        if (settings.movementStateSettings.movementState == MovementState.blink) return;
        if (settings.movementStateSettings.movementState == MovementState.recovery) return;

        settings.blinkControls.blinkAchievementDistance = newAchievementDistance;
        settings.blinkControls.blinkMaxHeightChange = newMaxHeightChange;
        settings.blinkControls.blinkRecoveryTime = newRecoveryTime;
        settings.blinkControls.blinkAnimationHeightOffset = animationHeightOffset;
        settings.blinkControls.blinkMinDistanceChange = minDistanceChange;


        settings.blinkControls.blinkStateCurrentDelay = 0;
        settings.blinkControls.blinkStateDelay = stateDelay;

        Vector3 travelDirection = GetBlinkDirectionByMethod(blinkDirection, blinkTravelMethod);
        if (StartBlink(rBlinkCharacterRepSpeed, travelDirection, rBlinkProjectForward, groundVector, snapRotation))
        {
            UnityEngine.Debug.Log("Blink Failed");
            return;
        }

        SetBlinkAnimationDefaults(animationName);
        SetStateRotation(settings.blinkControls.calculatedBlinkTarget);

        SetBlinkAnimationDefaults(settings.blinkControls.calculatedBlinkTarget);
        settings.movementStateSettings.requestedState = MovementState.blink;
    }
    //Requests the blink state
    public void RequestBlink(float rBlinkCharacterRepSpeed, Vector3 blinkDirection, BlinkTravelMethod blinkTravelMethod, bool rBlinkProjectForward, Transform blinkObject, bool groundVector, string animationName= "", bool snapRotation = false, float stateDelay = 0, float newRecoveryTime = 0f, float newAchievementDistance = 0.4f, float newMaxHeightChange = 2f, float minDistanceChange = 0.5f, float animationHeightOffset = 0)
    {
        if (settings.movementStateSettings.movementState == MovementState.blink) return;
        if (settings.movementStateSettings.movementState == MovementState.recovery) return;

        settings.blinkControls.blinkAchievementDistance = newAchievementDistance;
        settings.blinkControls.blinkMaxHeightChange = newMaxHeightChange;
        settings.blinkControls.blinkRecoveryTime = newRecoveryTime;
        settings.blinkControls.blinkAnimationHeightOffset = animationHeightOffset;
        settings.blinkControls.blinkMinDistanceChange = minDistanceChange;

        settings.blinkControls.blinkStateCurrentDelay = 0;
        settings.blinkControls.blinkStateDelay = stateDelay;

        Vector3 travelDirection = GetBlinkDirectionByMethod(blinkDirection, blinkTravelMethod, blinkObject);
        if (StartBlink(rBlinkCharacterRepSpeed, travelDirection, rBlinkProjectForward, groundVector, snapRotation))
        {
            UnityEngine.Debug.Log("Blink Failed");
            return;
        }

        SetBlinkAnimationDefaults(animationName);
        SetStateRotation(settings.blinkControls.calculatedBlinkTarget);

        SetBlinkAnimationDefaults(settings.blinkControls.calculatedBlinkTarget);

        settings.movementStateSettings.requestedState = MovementState.blink;
    }
    //Sets the blink animation information
    void SetBlinkAnimationDefaults(string animationName)
    {
            if (animationName != "")
            settings.basicAnimationSettings.commonAnimationTrigger = animationName;
        settings.basicAnimationSettings.blinkAnimationHeight = localReferences.characterRepresentation.position;
        settings.basicAnimationSettings.blinkAnimationHeight.x = 0;
        settings.basicAnimationSettings.blinkAnimationHeight.z = 0;
            return;
    }
    //Sets the blink animation information
    void SetBlinkAnimationDefaults(Vector3 cBT)
    {
        Vector3 position1 = localReferences.characterRepresentation.position;
        position1.x = 0;
        position1.z = 0;
        cBT.x = 0;
        cBT.z = 0;
        float distance = Vector3.Distance(position1, cBT);
        if (position1.y > cBT.y)
            distance *= -1;
        SetAnimatorFloat("BlinkTotalHeightChange", distance);
    }
    //Sets the blink rotation
    void SetStateRotation(Vector3 worldPosition)
    {
        Quaternion lookRot = Quaternion.LookRotation(worldPosition - settings.directionalVectorModifiers.referencePosition, Vector3.up);
        lookRot.eulerAngles = new Vector3(0, lookRot.eulerAngles.y, 0);
        settings.blinkControls.idealStateRotation = lookRot;
    }
    //Tests for the blink
    bool StartBlink(float rBlinkCharacterRepSpeed, Vector3 travelDirection, bool rBlinkProjectForward, bool groundVector, bool snapRotation)
    {
        settings.blinkControls.blinkSnapRotation = snapRotation;
        settings.blinkControls.blinkCharacterRepSpeed = 1/(rBlinkCharacterRepSpeed);
        settings.blinkControls.calculatedBlinkTarget = CalculateBlinkLocation(travelDirection, rBlinkProjectForward, out bool blinkFailed, groundVector);
        CapsuleCollider capsuleCollider = localReferences.physicsGyro.GetComponent<CapsuleCollider>();
        Vector3 pointOne = Vector3.up * (capsuleCollider.radius -settings.basicControls.playerHeight);
        Vector3 pointTwo = Vector3.up * (-capsuleCollider.radius);
        if (!rBlinkProjectForward)
        {
            Collider[] hitColliders = Physics.OverlapCapsule(settings.blinkControls.calculatedBlinkTarget +pointOne + Vector3.up*(settings.basicControls.playerHeight+0.1f), settings.blinkControls.calculatedBlinkTarget +pointTwo + Vector3.up * settings.basicControls.playerHeight, capsuleCollider.radius-0.15f, settings.layerMasks.blinkCollisionLayer);
            UnityEngine.Debug.DrawLine(settings.blinkControls.calculatedBlinkTarget + pointOne + Vector3.up * settings.basicControls.playerHeight, settings.blinkControls.calculatedBlinkTarget + pointTwo + Vector3.up * settings.basicControls.playerHeight, Color.magenta, 10f);
            if (hitColliders.Length != 0)
            {
                UnityEngine.Debug.Log(hitColliders[0].gameObject);
                blinkFailed = true;
                UnityEngine.Debug.Log("failed");
            }
        }
        else
        {
            UnityEngine.Debug.DrawRay(settings.directionalVectorModifiers.referencePosition + pointOne, settings.blinkControls.calculatedBlinkTarget - (settings.directionalVectorModifiers.referencePosition + Vector3.up * -settings.basicControls.playerHeight), Color.magenta, 10f);

            UnityEngine.Debug.DrawLine(settings.directionalVectorModifiers.referencePosition + pointOne, settings.directionalVectorModifiers.referencePosition + pointTwo, Color.magenta, 10f);
            RaycastHit raycastHit;
            if (Physics.CapsuleCast(settings.directionalVectorModifiers.referencePosition + pointOne, settings.directionalVectorModifiers.referencePosition + pointTwo, capsuleCollider.radius-0.15f, settings.blinkControls.calculatedBlinkTarget - (settings.directionalVectorModifiers.referencePosition+Vector3.up*-settings.basicControls.playerHeight), out raycastHit, Vector3.Distance(settings.directionalVectorModifiers.referencePosition, settings.blinkControls.calculatedBlinkTarget), settings.layerMasks.blinkCollisionLayer))
            {
                UnityEngine.Debug.DrawLine(settings.directionalVectorModifiers.referencePosition + pointOne, raycastHit.point, Color.red, 10f);
                if (groundVector)
                {
                    settings.blinkControls.calculatedBlinkTarget = GroundVector(raycastHit.point + (Vector3.ClampMagnitude(settings.blinkControls.calculatedBlinkTarget - settings.directionalVectorModifiers.referencePosition, 1) * -(capsuleCollider.radius + 0.1f)));
                }
                else
                {
                    settings.blinkControls.calculatedBlinkTarget = raycastHit.point + (Vector3.ClampMagnitude(settings.blinkControls.calculatedBlinkTarget - settings.directionalVectorModifiers.referencePosition, 1) * -(capsuleCollider.radius + 0.1f));
                }
            }
        }
        if (GetHeightDistance(settings.directionalVectorModifiers.referencePosition - Vector3.up * settings.basicControls.playerHeight, settings.blinkControls.calculatedBlinkTarget) > settings.blinkControls.blinkMaxHeightChange)
        {
            blinkFailed = true;
            UnityEngine.Debug.Log(settings.blinkControls.calculatedBlinkTarget);
            UnityEngine.Debug.Log(GetHeightDistance(settings.directionalVectorModifiers.referencePosition - Vector3.up * settings.basicControls.playerHeight, settings.blinkControls.calculatedBlinkTarget));
        }
        if (Vector3.Distance(settings.directionalVectorModifiers.referencePosition + Vector3.up*-settings.basicControls.playerHeight, settings.blinkControls.calculatedBlinkTarget) < settings.blinkControls.blinkMinDistanceChange)
        {
            blinkFailed = true;
            UnityEngine.Debug.Log("failed");
        }

        if (!blinkFailed)
        {
            UnityEngine.Debug.DrawLine(settings.blinkControls.calculatedBlinkTarget, settings.blinkControls.calculatedBlinkTarget + Vector3.up * -settings.basicControls.playerHeight, Color.green);
        }
        else
        {
            settings.blinkControls.calculatedBlinkTarget = settings.directionalVectorModifiers.referencePosition;
            UnityEngine.Debug.DrawLine(settings.blinkControls.calculatedBlinkTarget, settings.blinkControls.calculatedBlinkTarget + Vector3.up * -settings.basicControls.playerHeight, Color.red);
        }

        return blinkFailed;
    }
    //Gets the blink destination by method
    Vector3 GetBlinkDirectionByMethod(Vector3 blinkDirection, BlinkTravelMethod blinkTravelMethod, Transform blinkObject)
    {
        if (blinkTravelMethod == BlinkTravelMethod.characterBased)
        {
            return transform.TransformDirection(blinkDirection);
        }
        if (blinkTravelMethod == BlinkTravelMethod.cameraBased)
        {
            return settings.acv.cameraRigY.TransformDirection(blinkDirection);
        }
        if (blinkTravelMethod == BlinkTravelMethod.worldBased)
        {
            return blinkDirection;
        }
        if (blinkTravelMethod == BlinkTravelMethod.globalObjectBased)
        {
            return blinkObject.TransformDirection(blinkDirection);
        }
        if (blinkTravelMethod == BlinkTravelMethod.relativeObjectBased)
        {
            Vector3 positionOne = blinkObject.InverseTransformPoint(settings.directionalVectorModifiers.referencePosition);
            if (positionOne.z < 0)
            {
                return blinkObject.TransformDirection(blinkDirection);
            }
            else
            {
                return blinkObject.TransformDirection(-blinkDirection);
            }
        }
        return Vector3.zero;
    }
    Vector3 GetBlinkDirectionByMethod(Vector3 blinkDirection, BlinkTravelMethod blinkTravelMethod)
    {
        if (blinkTravelMethod == BlinkTravelMethod.characterBased)
        {
            return transform.TransformDirection(blinkDirection);
        }
        if (blinkTravelMethod == BlinkTravelMethod.cameraBased)
        {
            return settings.acv.cameraRigY.TransformDirection(blinkDirection);
        }
        if (blinkTravelMethod == BlinkTravelMethod.worldBased)
        {
            return blinkDirection;
        }
        return Vector3.zero;
    }
    //Gets the blink destination by method
    Vector3 CalculateBlinkLocation(Vector3 rBlinkTravel, bool rBlinkProjectForward, out bool hasFailed, bool groundVector)
    {
        Vector3 targetLocation = Vector3.zero;
        if (groundVector)
        {
            targetLocation = GroundVector(settings.directionalVectorModifiers.referencePosition + rBlinkTravel);
        }
        else
        {
            targetLocation = (settings.directionalVectorModifiers.referencePosition + rBlinkTravel + Vector3.up*-settings.basicControls.playerHeight);
        }
        hasFailed = false;
        UnityEngine.Debug.DrawRay(targetLocation, Vector3.up * 2, Color.yellow, 10);

        return targetLocation;
    }
    //Sets the transitional speed
    public void SetTransitionalSpeeds(float speed, bool groundedSpeed = true, bool SwimmingSpeed = true, bool flyingSpeed = true)
    {
        settings.directionalVectorModifiers.groundedTransitionalSpeed = speed;
        if (SwimmingSpeed)
            settings.swimmingControls.swimmingSpeedMultiplier = speed;
        if(flyingSpeed)
            settings.flyingControls.flyingBaseSpeedMultiplier = speed;
    }

    //**********************************************************************************************************************************************
    //Utilities
    //This section contains math functions

    //Returns a percentage of a number between two values
    float GetPercentBetweenValues(float lowerValue, float upperValue, float value)
    {
        return (value - lowerValue) / (upperValue - lowerValue);
    }

    //Set vector's y value to the ground level.
    Vector3 GroundVector(Vector3 rayPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(rayPos, Vector3.down, out hit, 2000, settings.layerMasks.groundLayer))
        {
            return new Vector3(rayPos.x, hit.point.y, rayPos.z);

        }
        return rayPos;
    }

    //Rotates a vector3 around another vector3 by a specific angle
    private Vector3 RotateAroundPivot(Vector3 originalPosition, Vector3 pivot, Vector3 rotationAngle)
    {
        Vector3 direction = originalPosition - pivot;
        direction = Quaternion.Euler(rotationAngle) * direction;
        Vector3 newPosition = direction + pivot;
        return newPosition;
    }

    //Returns a given vector3 with the y value set to 0 
    private Vector3 GetFlatVector3(Vector3 vector3)
    {
        vector3.y = 0;
        return vector3;
    }

    //Returns the distance between two vector3s using only the x and z positions
    private float GetFlatVector3Distance(Vector3 positionOne, Vector3 positionTwo)
    {
        return Vector3.Distance(GetFlatVector3(positionOne), GetFlatVector3(positionTwo));
    }

    //Returns the distance between two vector3s using only the y positions
    public float GetHeightDistance(Vector3 positionOne, Vector3 positionTwo)
    {
        return GetLinearDistance(positionOne.y, positionTwo.y);
    }

    //Returns the difference between two vector3s using only the y positions
    public float GetHeightDifference(Vector3 positionOne, Vector3 positionTwo)
    {
        return positionOne.y - positionTwo.y;
    }

    //Finds the distance between two floats
    private float GetLinearDistance(float valueOne, float valueTwo)
    {
        return Mathf.Abs(valueOne - valueTwo);
    }

    //Calculates the information required for a climbing angle calculation
    private void GetClimbingDistances(Vector3 newLocation, out float forwardDistance, out float heightDistance)
    {
        forwardDistance = GetFlatVector3Distance(settings.directionalVectorModifiers.referencePosition, newLocation);
        heightDistance = GetHeightDistance(settings.directionalVectorModifiers.referencePosition + new Vector3(0, -settings.basicControls.playerHeight, 0), newLocation);
    }

    //Returns an angle based on the opposite and adjacent triangle lengths given
    public float GetClimbAngle(float height, float forwardDistance)
    {
        if (forwardDistance <= 0.001) return 0;
        if (height <= 0.001) return 0;
        return Mathf.Rad2Deg * Mathf.Atan2(height, forwardDistance);
    }

    //Checks around the player for steep angles
    //returns true if the player should be sliding
    public bool SimulatePhysics(Vector3 location, bool useEntryAngle, out Vector3 simulationOutput, out bool isFalling)
    {
        //Grounds the starting location
        GroundVector(location);
        if (debugModeEnabled) UnityEngine.Debug.DrawRay(location, Vector3.up, Color.blue);

        //Grounds vectors around the player at a specific range from the player
        //Amounts of checks preformed is specified by the slidePhysicsCheck variable
        //Angle between checks is 360/slideControls.slidePhysicsChecks
        List<Vector3> testVectorAngles = new List<Vector3>();
        for (int i = 0; i < settings.slideControls.slidePhysicsChecks; i++)
        {
            //Rotates and tests a vector
            Vector3 newTestVector;
            newTestVector = RotateAroundPivot((location + Vector3.forward * settings.slideControls.currentSlideRange) + Vector3.up * settings.basicControls.playerHeight, location, new Vector3(0, (360 / settings.slideControls.slidePhysicsChecks) * i, 0));
            newTestVector = GroundVector(newTestVector);
            if (debugModeEnabled) UnityEngine.Debug.DrawRay(newTestVector, Vector3.up, Color.green, Time.deltaTime);

            //If the raycast didn't find ground 
            if (newTestVector == Vector3.zero) { continue; }

            //If tested value is higher than the specified value, the tested value is discarded.
            if (newTestVector.y > location.y) continue;

            //Checks if the tested vector is below water. This prevents the player from sliding into water.
            RaycastHit raycastHit;
            if (!Physics.Raycast(newTestVector, Vector3.up, out raycastHit, settings.basicControls.playerHeight, settings.layerMasks.waterLayer))
            {
                if (debugModeEnabled) UnityEngine.Debug.DrawLine(new Vector3(location.x, location.y + settings.slideControls.slideHeightOffset - settings.basicControls.playerHeight, location.z), new Vector3(newTestVector.x, newTestVector.y + settings.slideControls.slideHeightOffset, newTestVector.z), Color.yellow, Time.deltaTime);
                //Checks if the player can move to the tested location without obstructions 
                if (!Physics.Linecast(new Vector3(location.x, location.y + settings.slideControls.slideHeightOffset - settings.basicControls.playerHeight, location.z), new Vector3(newTestVector.x, newTestVector.y + settings.slideControls.slideHeightOffset, newTestVector.z), settings.layerMasks.groundLayer))
                {
                    testVectorAngles.Add(newTestVector);
                }
            }
            else
            {
                //The simulation is stops if water is detected
                simulationOutput = location - new Vector3(0, settings.basicControls.playerHeight, 0);
                isFalling = false;
                return false;
            }
        }

        //Finds the sharpest angle of the tested vectors
        if (testVectorAngles.Count > 0)
        {
            //Finds the tested vector with the lowest y value
            float LowestY = testVectorAngles[0].y;
            int LowestYIndex = 0;
            for (int i = 1; i < testVectorAngles.Count; i++)
            {
                if (testVectorAngles[i].y < LowestY)
                {
                    LowestY = testVectorAngles[i].y;
                    LowestYIndex = i;
                }
            }

            //Finds the climbing angle of the tested vector with the lowest y value
            float forwardDistance, heightDistance;
            GetClimbingDistances(testVectorAngles[LowestYIndex], out forwardDistance, out heightDistance);
            float climbingAngle = GetClimbAngle(heightDistance, forwardDistance);

            //Determines if the climb angle is steep enough for the player to be falling instead of sliding
            if (useEntryAngle && climbingAngle > settings.slideControls.minimumFallAngle)
            {
                simulationOutput = location - new Vector3(0, settings.basicControls.playerHeight, 0);
                isFalling = true;
                return true;
            }

            // Determines if the climb angle is steep enough for the player to start sliding
            if (useEntryAngle && climbingAngle > settings.slideControls.slideEntryAngle)
            {
                simulationOutput = testVectorAngles[LowestYIndex];
                isFalling = false;
                return true;
            }

            // Determines if the climb angle is steep enough for the player to stop sliding
            else if (!useEntryAngle && climbingAngle > settings.slideControls.slideExitAngle)
            {
                simulationOutput = testVectorAngles[LowestYIndex];
                isFalling = false;
                return true;
            }
        }

        //The simulation determined the player should not be falling or sliding
        simulationOutput = location - new Vector3(0, settings.basicControls.playerHeight, 0);
        isFalling = false;
        return false;
    }

    //*****************************************************************************************************************************************************
    //IK

    //Called from AnimationEventHandler on the character GameObject
    public void IKPass()
    {
        //Returns if IK is disabled
        if (!settings.iKSettings.enableIK) return;
        //Sets IK Weights based on the animation curve
        SetIKWeights();

        Vector3 adjustedBodyPosition = new Vector3();

        //Projects the Feet IK target forward and down if required by the feetLocationMethod
        if (settings.iKSettings.feetLocationMethod == MultiStateSettings.IKSettings.FeetLocationMethod.projectForwardAndDown)
        {
            Vector3 forwardLeftFootPosition, forwardRightFootPosition;
            ProjectForward(localReferences.leftFoot.position, localReferences.rightFoot.position, out forwardLeftFootPosition, out forwardRightFootPosition, settings.iKSettings.footLength);
            Vector3 downLeftFootPosition, downRightFootPosition, outBodyPosition;
            Quaternion leftFootRot, rightFootRot;
            ProjectFeetDown(forwardLeftFootPosition, forwardRightFootPosition, out downLeftFootPosition, out downRightFootPosition, out outBodyPosition, out leftFootRot, out rightFootRot);
            SetFootTarget(downLeftFootPosition, downRightFootPosition, leftFootRot, rightFootRot);
            adjustedBodyPosition += outBodyPosition;

        }
        //Projects the Feet IK target down if required by the feetLocationMethod
        if (settings.iKSettings.feetLocationMethod == MultiStateSettings.IKSettings.FeetLocationMethod.projectDown)
        {
            Vector3 downLeftFootPosition, downRightFootPosition, outBodyPosition;
            Quaternion leftFootRot, rightFootRot;
            ProjectFeetDown(localReferences.leftFoot.position, localReferences.rightFoot.position, out downLeftFootPosition, out downRightFootPosition, out outBodyPosition, out leftFootRot, out rightFootRot);
            SetFootTarget(downLeftFootPosition, downRightFootPosition, leftFootRot, rightFootRot);
            adjustedBodyPosition += outBodyPosition;
        }
        //Sets Feet IK target to a transform if required by the feetLocationMethod
        if (settings.iKSettings.feetLocationMethod == MultiStateSettings.IKSettings.FeetLocationMethod.lockToTarget)
        {
            Transform leftClosestTarget = GetClosestIKTarget(localReferences.leftFoot.position);
            Transform rightClosestTarget = GetClosestIKTarget(localReferences.rightFoot.position);
            SetFootTarget(leftClosestTarget.position, rightClosestTarget.position, leftClosestTarget.rotation, rightClosestTarget.rotation);
        }
        //Sets Hand IK target to a transform if required by the handLocationMethod
        if (settings.iKSettings.handLocationMethod == MultiStateSettings.IKSettings.HandLocationMethod.lockToTarget)
        {
            Transform leftClosestTarget = GetClosestIKTarget(localReferences.leftHand.position);
            Transform rightClosestTarget = GetClosestIKTarget(localReferences.rightHand.position);
            SetHandTarget(leftClosestTarget.position, rightClosestTarget.position, leftClosestTarget.rotation, rightClosestTarget.rotation);
        }
        //Projects the Hand IK target forward if required by the handLocationMethod
        if (settings.iKSettings.handLocationMethod == MultiStateSettings.IKSettings.HandLocationMethod.projectForward)
        {
            Vector3 leftHandPosition, rightHandPosition;
            ProjectForward(localReferences.leftHand.position, localReferences.rightHand.position, out leftHandPosition, out rightHandPosition, settings.iKSettings.handThickness);
            Quaternion leftHandRot, rightHandRot;
            leftHandPosition = CheckBackProject(leftHandPosition, true, out leftHandRot);
            rightHandPosition = CheckBackProject(rightHandPosition, false, out rightHandRot);
            SetHandTarget(leftHandPosition, rightHandPosition, leftHandRot, rightHandRot);
        }
        //Moves the entire body down based on the location of the feet ik targets
        SetBodyPosition(adjustedBodyPosition);
    }

    //Checks if the hands are clipping through a collider
    Vector3 CheckBackProject(Vector3 hand, bool isLeft, out Quaternion rotation)
    {
        if (isLeft)
        {
            rotation = localReferences.leftHand.rotation;
        }
        else
        {
            rotation = localReferences.rightHand.rotation;
        }

        RaycastHit raycastHit;
        if (debugModeEnabled) UnityEngine.Debug.DrawRay(hand + localReferences.characterRepresentation.forward * -(settings.iKSettings.handThickness + 0.1f), localReferences.characterRepresentation.forward, Color.green);
        if (Physics.Raycast(hand + localReferences.characterRepresentation.forward * -(settings.iKSettings.handThickness + 0.1f), localReferences.characterRepresentation.forward, out raycastHit, 1, settings.layerMasks.ikLayer))
        {
            if (localReferences.characterRepresentation.InverseTransformPoint(raycastHit.point).z < localReferences.characterRepresentation.InverseTransformPoint(hand).z)
                if (isLeft)
                {
                    localReferences.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                    localReferences.animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                }
                else
                {
                    localReferences.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                    localReferences.animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                }
            rotation = Quaternion.LookRotation(localReferences.characterRepresentation.up, raycastHit.normal);
            return raycastHit.point + localReferences.characterRepresentation.forward * -settings.iKSettings.handThickness;
        }
        return hand;
    }

    //Returns the position of a target in a group of targets
    Transform GetClosestIKTarget(Vector3 position)
    {
        List<float> ikTargetDistances = new List<float>();
        foreach (Transform trans in settings.iKSettings.ikTargetSet)
        {
            ikTargetDistances.Add(Vector3.Distance(position, trans.position));
        }
        float lowestDistance = ikTargetDistances.ToArray().Min();
        return settings.iKSettings.ikTargetSet[ikTargetDistances.IndexOf(lowestDistance)];
    }
    //Sets the Foot IK Method
    public void SetFootIKMethod(MultiStateSettings.IKSettings.FeetLocationMethod locationMethod)
    {
        settings.iKSettings.feetLocationMethod = locationMethod;
    }

    //Sets the hand ik method
    public void SetHandIKMethod(MultiStateSettings.IKSettings.HandLocationMethod locationMethod)
    {
        settings.iKSettings.handLocationMethod = locationMethod;
    }
    //Generates an ik target list from the parents transform
    public void EvaluateIkTargetSet(Transform parentTransform)
    {
        settings.iKSettings.ikTargetSet.Clear();
        if (!parentTransform) return;
        foreach (Transform trans in parentTransform.GetComponentsInChildren<Transform>())
        {
            settings.iKSettings.ikTargetSet.Add(trans);
        }
    }
    //Sets the ik weights based on the animator curves
    void SetIKWeights()
    {
        localReferences.animator.SetIKPositionWeight(AvatarIKGoal.RightHand, localReferences.animator.GetFloat("IKRightHandWeight"));
        if (settings.iKSettings.enableHandIkRotation) localReferences.animator.SetIKRotationWeight(AvatarIKGoal.RightHand, localReferences.animator.GetFloat("IKRightHandWeight"));

        localReferences.animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, localReferences.animator.GetFloat("IKLeftHandWeight"));
        if (settings.iKSettings.enableHandIkRotation) localReferences.animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, localReferences.animator.GetFloat("IKLeftHandWeight"));

        localReferences.animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, localReferences.animator.GetFloat("IKLeftFootWeight"));
        if (settings.iKSettings.enableFeetIkRotation) localReferences.animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, localReferences.animator.GetFloat("IKLeftFootWeight"));


        localReferences.animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, localReferences.animator.GetFloat("IKRightFootWeight"));
        if (settings.iKSettings.enableFeetIkRotation) localReferences.animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, localReferences.animator.GetFloat("IKRightFootWeight"));
    }
    //Projects two vector3 positions forward
    void ProjectForward(Vector3 leftInPostion, Vector3 rightInPosition, out Vector3 leftOutPosition, out Vector3 rightOutPosition, float offset)
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(leftInPostion + localReferences.characterRepresentation.forward * -1, localReferences.characterRepresentation.forward, out raycastHit, settings.iKSettings.forwardProjectionDistance + 1, settings.layerMasks.ikLayer))
        {
            leftOutPosition = raycastHit.point + localReferences.characterRepresentation.forward * -offset;
        }
        else
        {
            leftOutPosition = leftInPostion;
        }

        if (Physics.Raycast(rightInPosition + localReferences.characterRepresentation.forward * -1, localReferences.characterRepresentation.forward, out raycastHit, settings.iKSettings.forwardProjectionDistance + 1, settings.layerMasks.ikLayer))
        {
            rightOutPosition = raycastHit.point + localReferences.characterRepresentation.forward * -offset;
        }
        else
        {
            rightOutPosition = rightInPosition;
        }
    }
    //Projects the feet ik targets downward
    void ProjectFeetDown(Vector3 givenLeftFootPosition, Vector3 givenRightFootPosition, out Vector3 outLeftFootPosition, out Vector3 outRightFootPosition, out Vector3 outBodyPosition, out Quaternion leftFootRot, out Quaternion rightFootRot)
    {
        leftFootRot = localReferences.leftFoot.rotation;
        rightFootRot = localReferences.rightFoot.rotation;

        RaycastHit raycastHit;
        Vector3 leftFootPos = Vector3.zero;
        float leftFootRaylength = 0;
        Vector3 rightFootPos = Vector3.zero;
        float rightFootRaylength = 0;
        if (Physics.Raycast(givenLeftFootPosition + new Vector3(0, settings.basicControls.playerHeight, 0), Vector3.down, out raycastHit, settings.basicControls.playerHeight + settings.iKSettings.footHeight + settings.iKSettings.bodyDropFactor, settings.layerMasks.ikLayer))
        {
            leftFootPos = raycastHit.point + new Vector3(0, settings.iKSettings.footHeight, 0);
            leftFootRaylength = GetHeightDistance(givenLeftFootPosition, raycastHit.point) * localReferences.animator.GetFloat("IKLeftFootWeight");
            leftFootRot = Quaternion.LookRotation(localReferences.characterRepresentation.forward, raycastHit.normal);
        }

        if (Physics.Raycast(givenRightFootPosition + new Vector3(0, settings.basicControls.playerHeight, 0), Vector3.down, out raycastHit, settings.basicControls.playerHeight + settings.iKSettings.footHeight + settings.iKSettings.bodyDropFactor, settings.layerMasks.ikLayer))
        {
            rightFootPos = raycastHit.point + new Vector3(0, settings.iKSettings.footHeight, 0);
            rightFootRaylength = GetHeightDistance(givenRightFootPosition, raycastHit.point) * localReferences.animator.GetFloat("IKRightFootWeight");
            rightFootRot = Quaternion.LookRotation(localReferences.characterRepresentation.forward, raycastHit.normal);
        }

        if (leftFootRaylength > settings.iKSettings.bodyDropLimit)
        {
            leftFootRaylength = 0;
            leftFootPos = Vector3.zero;
        }
        if (rightFootRaylength > settings.iKSettings.bodyDropLimit)
        {
            rightFootRaylength = 0;
            rightFootPos = Vector3.zero;
        }

        //Sets the body drop
        float avWeight = localReferences.animator.GetIKPositionWeight(AvatarIKGoal.LeftFoot) * localReferences.animator.GetIKPositionWeight(AvatarIKGoal.RightFoot);
        outBodyPosition = (Vector3.down * Mathf.Max(leftFootRaylength, rightFootRaylength) * settings.iKSettings.bodyOffsetMultiplier) + Vector3.up * (settings.iKSettings.footHeight + settings.iKSettings.bodyOffset *avWeight);
        if (leftFootPos != Vector3.zero) { outLeftFootPosition = leftFootPos; } else { outLeftFootPosition = givenLeftFootPosition; }
        if (rightFootPos != Vector3.zero) { outRightFootPosition = rightFootPos; } else { outRightFootPosition = givenRightFootPosition; }

    }
    //Sets the body drop
    void SetBodyPosition(Vector3 newBodyPosition)
    {
        newBodyPosition += localReferences.characterRepresentation.forward * localReferences.animator.GetFloat("IKBodyForward");
        localReferences.animator.bodyPosition = localReferences.animator.bodyPosition + newBodyPosition;
    }
    //Sets the hand ik target to a transform's position
    void SetHandTarget(Vector3 leftTarget, Vector3 rightTarget, Quaternion leftRotation, Quaternion rightRotation)
    {
        localReferences.animator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget);
        localReferences.animator.SetIKPosition(AvatarIKGoal.RightHand, rightTarget);

        if (settings.iKSettings.enableHandIkRotation) localReferences.animator.SetIKRotation(AvatarIKGoal.LeftHand, leftRotation);
        if (settings.iKSettings.enableHandIkRotation) localReferences.animator.SetIKRotation(AvatarIKGoal.RightHand, rightRotation);
    }
    //Sets the foot ik target to a transform's position
    void SetFootTarget(Vector3 leftTarget, Vector3 rightTarget, Quaternion leftRotation, Quaternion rightRotation)
    {
        localReferences.animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftTarget);
        localReferences.animator.SetIKPosition(AvatarIKGoal.RightFoot, rightTarget);

        if (settings.iKSettings.enableFeetIkRotation) localReferences.animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftRotation);
        if (settings.iKSettings.enableFeetIkRotation) localReferences.animator.SetIKRotation(AvatarIKGoal.RightFoot, rightRotation);
    }

    //Draws the blink gizmo
    private void OnDrawGizmos()
    {
        if (!localReferences.physicsGyro) return;

        CapsuleCollider capsuleCollider = localReferences.physicsGyro.GetComponent<CapsuleCollider>();

        Gizmos.DrawWireSphere(settings.blinkControls.calculatedBlinkTarget + (Vector3.up*(capsuleCollider.radius)),capsuleCollider.radius);
        Gizmos.DrawWireSphere(settings.blinkControls.calculatedBlinkTarget + (Vector3.up * (-capsuleCollider.radius + settings.basicControls.playerHeight)), capsuleCollider.radius);
    }
}

//Events

//This event is called when the player transitions from the falling state to the grounded state.
[System.Serializable]
public class PlayerLandEvent : UnityEvent<float> { }
//This event is called when the player transitions into a different movement state.
[System.Serializable]
public class PlayerStateChangeEvent : UnityEvent<MovementState> { }

//The movement information for the lunge movement state
[Serializable]
public class LungeAction
{
    [Tooltip("The direction of the lunge action. Note: advanced direction usage may nullify this setting.")]
    public Vector3 simpleDirection = Vector3.zero;
    [HideInInspector]
    public Vector3 direction = Vector3.zero;
    [Tooltip("Intensity of the lunge action.")]
    public float intensity = 0;
    [Tooltip("Duration of the lunge action.")]
    public float duration = 1;
    [Tooltip("Recovery time in seconds of the lunge action.")]
    public float recoveryTime = 0.5f;
    [Tooltip("Animation trigger of the lunge action.")]
    public string animationTrigger = "";
    [Tooltip("States the lunge action can activated from.")]
    public EnabledStates canTransitionFrom;
}

//Public Movement Enums

//An enum containing all of the possible movement states:
[System.Serializable]
public enum MovementState
{
    undefined,
    recovery,
    grounded,
    sliding,
    blink,
    lunge,
    jumping,
    falling,
    flying,
    swimming,
    onRails,
    ragdoll
}

[Serializable]
public enum EnabledDisabled
{
    enabled,
    disabled
}

//Defines the enabled states
[Serializable]
public class EnabledStates
{
    public EnabledDisabled groundedMovement = EnabledDisabled.enabled;
    public EnabledDisabled recoveryMovement = EnabledDisabled.enabled;
    public EnabledDisabled slidingMovement = EnabledDisabled.enabled;
    public EnabledDisabled blinkMovement = EnabledDisabled.enabled;
    public EnabledDisabled lungeMovement = EnabledDisabled.enabled;
    public EnabledDisabled jumpingMovement = EnabledDisabled.enabled;
    public EnabledDisabled fallingMovement = EnabledDisabled.enabled;
    public EnabledDisabled flyingMovement = EnabledDisabled.enabled;
    public EnabledDisabled swimmingMovement = EnabledDisabled.enabled;
    public EnabledDisabled onRailsMovement = EnabledDisabled.enabled;
    public EnabledDisabled ragdollMovement = EnabledDisabled.enabled;
}

//An enum containing all of the possible grounded movement sub states:
[System.Serializable]
public enum GroundedMovementSubState
{
    running,
    walking,
    crouching,
    sprinting,
    defaultSubstate
}
