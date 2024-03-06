using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MultiStateSettings
{
    [Header("Movement State Activations:")]
    public MovementStateSettings movementStateSettings = new MovementStateSettings();


    [Serializable]
    public class MovementStateSettings
    {
        [Tooltip("What movement states can the player enter? Note: Adjusting these values may cause unwanted effects! Please be aware that there may be better methods to disable certain movement states!")]
        public EnabledStates activeMovementStates = new EnabledStates();
        //The current movement state of the player.
        [HideInInspector]
        public MovementState movementState = MovementState.grounded;
        [Tooltip("The movement state the player should attempt to enter. Note: this does not guarantee the player will stay in the requested state for any length of time.")]
        public MovementState requestedState = MovementState.undefined;
        [Tooltip("Should the player be ejected from disabled states?")]
        public bool ejectPlayerFromDisabledStates = true;
    }

    //Layer masks: 
    [Header("Layer Masks (Required!): ")]
    public LayerMasks layerMasks = new LayerMasks();
    [Serializable]
    public class LayerMasks
    {
        [Tooltip("The layer mask that defines walkable surfaces. This mask should not represent non-walkable obstacles.")]
        public LayerMask groundLayer;
        [Tooltip("The layer mask that defines surfaces that prevent the player from standing under")]
        public LayerMask overheadLayer;
        [Tooltip("The layer mask that defines water surfaces")]
        public LayerMask waterLayer;
        [Tooltip("The layer mask that defines ik eligible surfaces")]
        public LayerMask ikLayer;
        [Tooltip("The layer mask that defines surfaces that the player cannot land on")]
        public LayerMask disableLandLayer;
        [Tooltip("This layer represents objects that will cause the player to fail the blink on a calculated collision.")]
        public LayerMask blinkCollisionLayer;
    }

    //This is the primary input value to which this script functions, provided by the MultistatePlayerInput Script.
    [HideInInspector]
    public AdvancedControlValue acv;

    [Header("Visual Representation:")]
    public VisualRepresentation visualRepresentation = new VisualRepresentation();


    [Serializable]
    public class VisualRepresentation
    {
        [Tooltip("The speed at which the currentCharacterRepSpeed adjusts towards the targetCharacterRepSpeed.")]
        public float characterRepAdjustmentSpeed = 6;
        [Tooltip("A Y value offset for the character mesh.")]
        public float characterRepHeightOffset = -2.08f;

        public RepSpeed repSpeed = new RepSpeed();

        [Serializable]
        public class RepSpeed
        {
            //Character Rep Speeds

            [HideInInspector]
            //The current speed at which the character mesh gameobject follows the projected player gameobject.
            public float currentCharacterRepSpeed = 0.1f;
            //The ideal speed at which the character mesh gameobject follows the projected player gameobject based on the movement state:
            [HideInInspector]
            public float targetCharacterRepSpeed = 0.15f;
            [Range(0.01f, 1), Header("Character Representation Movement Speeds:"), Tooltip("The speed at which the character mesh gameobject follows the projected player gameobject while in the grounded state.")]
            public float groundedCharacterRepSpeed = 0.15f;
            [Range(0.01f, 1), Tooltip("The speed at which the character mesh gameobject follows the projected player gameobject while in the falling state.")]
            public float fallingCharacterRepSpeed = 0.02f;
            [Range(0.01f, 1), Tooltip("The speed at which the character mesh gameobject follows the projected player gameobject while in the flying state.")]
            public float flyingCharacterRepSpeed = 0.02f;
            [Range(0.01f, 1), Tooltip("The speed at which the character mesh gameobject follows the projected player gameobject while in the swimming state.")]
            public float swimmingCharacterRepSpeed = 0.15f;
        }

    }

    [Header("Ground Check Raycast Distances By State:")]
    public RaycastDistances raycastDistances = new RaycastDistances();
    [Serializable]
    public class RaycastDistances
    {
        //Raycast distances
        //The raycast distance at which the player currently checks for the grounded state:
        [HideInInspector]
        public float currentRaycastDistance = 0.6f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the grounded state.")]
        public float groundRaycastDistance = 0.6f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the falling state.")]
        public float fallingRaycastDistance = 0.1f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the recovery state.")]
        public float recoveryRaycastDistance = 0.5f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the sliding state.")]
        public float slidingRaycastDistance = 0.6f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the jumping state.")]
        public float jumpingRaycastDistance = 0f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the flying state.")]
        public float flyingRaycastDistance = 0.3f;
        [Tooltip("The raycast distance at which the player checks for the grounded state while in the swimming state.")]
        public float swimmingRaycastDistance = 1f;
    }

    [Header("Basic Controls:")]
    public BasicControls basicControls = new BasicControls();

    [Serializable]
    public class BasicControls
    {
        [Tooltip("The width of the player.")]
        public float playerWidth = 0.5f;
        [Tooltip("The height of the player.")]
        public float playerHeight = 2;

        //The original collider height
        [HideInInspector]
        public float colliderHeight;
        //The original collider position
        [HideInInspector]
        public Vector3 colliderPosition;

        //Other variables:
        [HideInInspector]
        //The reference velocity for the character representation's smoothDamp function:
        public Vector3 referenceVelocity = Vector3.zero;
        [HideInInspector]
        //The camera's rotation difference between its current rotation and its ideal rotation, normally directly behind the player: 
        public Quaternion cameraRotationDifference = Quaternion.identity;
        [HideInInspector]
        //Is the player actually grounded, this is different from being in the grounded state:
        public bool isGrounded = false;
        [HideInInspector]
        //Is the players location locked, 0 is unlocked and every number higher than 0 is the priority:
        public int lockMovement = 0;
        [HideInInspector]
        //Is the players rotation locked, 0 is unlocked and every number higher than 0 is the priority:
        public int lockRotation = 0;
    }

    [Header("Directional Vector Modifiers:")]
    public DirectionalVectorModifiers directionalVectorModifiers = new DirectionalVectorModifiers();

    [Serializable]
    public class DirectionalVectorModifiers
    {
        [Tooltip("The primary grounded movement speed.")]
        public float groundedTransitionalSpeed = 2f;
        [HideInInspector]
        //A list containing the last several requested final directional Y values, used to average Y movement:
        public List<float> averagedYValue = new List<float>();
        [Tooltip("Over how many frames the Y value directional movement should be averaged. Functions as a studder control when player is trying to climb steep angles.")]
        public int averageYValueCount = 30;
        [Tooltip("The maximum speed at which the player can rotate")]
        public float maximumRotationSpeed = 400;
        [HideInInspector]
        //The calculated rotation speed based indirectly on the control scheme:
        public float currentRotationSpeed = 0;
        [HideInInspector]
        //The current primary directional vector multiplier for almost every movement state based on the movement state.
        public float currentTransitionalSpeed = 0.2f;
        [Tooltip("A speed multiplier, primarily used for modern unlocked rotation.")]
        public float rotationalSpeedMultiplier = 80;
        [HideInInspector]
        //The current amount of time in seconds the Recovery State will last for.
        public float currentRecovery = 0;
        [HideInInspector]
        //The amount of time the player has been in the falling state:
        public float fallTime = 0;
        [HideInInspector]
        //The target rotation of the Character Representation, used for Rail Systems:
        public Quaternion targetCharacterRepRotation = Quaternion.identity;
        [HideInInspector]
        //The calculated position the player tries to move to:
        public Vector3 externalTargetValue = Vector3.zero;
        [HideInInspector]
        public Vector3 referencePosition = Vector3.zero;
    }


    [Header("Physics Gyro:")]
    public PhysicsGyroSettings physicsGyroSettings = new PhysicsGyroSettings();
    [Serializable]
    public class PhysicsGyroSettings
    {
        [Tooltip("Should the player's collider rotate to simulate the characters animations?")]
        public bool enablePhysicsGyro = true;
        [Tooltip("Collider radius multiplier while the physics gyro is active.")]
        public float physicsGyroRadiusMultiplier = 1.5f;
        [Header("Flying Controls:"), Tooltip("The maximum animation angle reached by the physics gyro. Physics Gyro must be enabled. (Flying State)")]
        public float flyingPhysicsGyroMaximumAnimationAngle = 90;
        [Range(0, 1), Tooltip("Physics gyro reaction is a blend of the character rep velocity and input direction determined by this value. Physics Gyro must be enabled. (Flying State)")]
        public float flyingPhysicsGyroInputFactor = 0.632f;
        [Tooltip("Defines what mixed value, between character rep velocity and input direction, will equate to a 90 degree rotation in the physics gyro. Physics Gyro must be enabled. (Flying State)")]
        public float flyingPhysicsGyroMagnitudeFactor = 8;
        [Tooltip("Dampening value for the physics gyro. Physics Gyro must be enabled. (Flying State)")]
        public float flyingPhysicsGyroTransitionSpeed = 5;

        [Header("Swimming Controls:"), Tooltip("The maximum animation angle reached by the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroMaximumAnimationAngle = 90;
        [Range(0, 1), Tooltip("Physics gyro reaction is a blend of the character rep velocity and input direction determined by this value. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroInputFactor = 0.632f;
        [Tooltip("Defines what mixed value, between character rep velocity and input direction, will equate to a 90 degree rotation in the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroMagnitudeFactor = 6;
        [Tooltip("Dampening value for the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroTransitionSpeed = 2;
        [HideInInspector]
        public float normalColliderRadius = 0.5f;
    }

    [Header("Grounded Movement Substate Modifiers:")]
    public GroundedSubstateModifiers groundedSubstateModifiers = new GroundedSubstateModifiers();

    [Serializable]
    public class GroundedSubstateModifiers
    {
        [Tooltip("The default grounded substate")]
        public GroundedMovementSubState defaultGroundedMovementSubState = GroundedMovementSubState.running;
        [HideInInspector]
        public GroundedMovementSubState groundedMovementSubState = GroundedMovementSubState.running;
        [HideInInspector]
        //The value the player speed is multiplied by based on the grounded substate
        public float currentSubstateMultiplier = 1;
        [Range(1f, 2f), Tooltip("Speed Multiplier while in the running substate, do not use this value in place of the directionalVectorModifiers.groundedTransitionalSpeed variable.")]
        public float runningSpeedMultiplier = 1;
        [Range(0.2f, 1f), Tooltip("Speed Multiplier while in the walking substate")]
        public float walkingSpeedMultiplier = 0.35f;
        [Range(0.2f, 1f), Tooltip("Speed Multiplier while in the crouching substate")]
        public float crouchingSpeedMultiplier = 0.7f;
        [Range(1f, 4f), Tooltip("Speed Multiplier while in the sprinting substate")]
        public float sprintingSpeedMultiplier = 1.5f;
        [Tooltip("The value the player collider is shortened by")]
        public float crouchingColliderDifference = 0.4f;

        [Serializable]
        //A simple enum that contains the various movement opportunities available while in the grounded state:
        public enum GroundedMovementMethod
        {
            controlVectorBased, //based on key input
            targetBased, //simple follow target
        }

        [HideInInspector]
        //The current value for the players groundedSubstateModifiers.groundedMovementMethod:
        public GroundedMovementMethod groundedMovementMethod = GroundedMovementMethod.controlVectorBased;

    }

    [Header("Rigibody Force Modifiers:")]
    public RigidbodyForceModifiers rigidbodyForceModifiers = new RigidbodyForceModifiers();

    [Serializable]
    public class RigidbodyForceModifiers
    {
        [Tooltip("The primary force multiplier used for almost every movement state.")]
        public float forceMultiplier = 10;
        [Tooltip("A constant downward force applied in the sliding, recovery, and grounded states in order to keep the player on the ground.")]
        public float constantYGroundedForce = -10;
        [Tooltip("A variable downward force multiplier applied in the sliding and grounded states in order to keep the player from bouncing up steep inclines.")]
        public float variableDistanceYForceMultiplier = 0.2f;
        [Tooltip("The rigidbody drag in the grounded, recovery, and other states that don't require a change in drag.")]
        public float normalDrag = 5;
        [Tooltip("The force applied in the falling state.")]
        public float gravity = -7.5f;
        [Tooltip("Factor that effects how horizontal force is preserved while falling")]
        public float fallVelocityPreservationFactor = 0.6f;
        [Tooltip("Factor that effects how vertical force is preserved while falling")]
        public float fallVelocityYPreservationFactor = 10f;
        //The player's rigidbody
        [HideInInspector]
        public Rigidbody playerRigidbody;
    }

    //Fall Movement Protection

    //If the player is moving a velocity less than 1 for x amount of seconds while in the falling state, 
    //it is assumed that the player is stuck, and directionally random horizontal force is applied that 
    //is directly proportional to the amount of time that the player has been assumed to be stuck.

    //The amount of time spent at less than 1 velocity while in the falling state required to engage fall movement protection
    [HideInInspector]
    public FallProtection fallProtection = new FallProtection();


    [Serializable]
    public class FallProtection
    {
        public float minimumFallTime = 1;
        //The actual time spent at less than 1 velocity while in the falling state
        [HideInInspector]
        public float currentFallProtectionRecordTime = 0;
        //Force is time in seconds spent stuck multiplied by this multiplier
        public float fallProtectionForceMultiplier = 20;
        //The maximum amount of force fall movement protection can apply in a single frame
        public float fallProtectionMaximumForce = 500;
    }

    [HideInInspector]
    public MoveToTargetFunctionality moveToTargetFunctionality = new MoveToTargetFunctionality();

    [Serializable]
    public class MoveToTargetFunctionality
    {
        //Move To Target
        [HideInInspector]
        //The follow target for targetBased movement:
        public Transform moveToTarget;
        [HideInInspector]
        //The minimum distance from the player at which the MoveToTarget will be accomplished:
        public float moveToTargetDistance = 0.1f;
    }

    [Header("Slide Controls:")]
    public SlideControls slideControls = new SlideControls();
    [Serializable]
    public class SlideControls
    {
        [Tooltip("Extra Y force applied while sliding in order to keep the player on the ground.")]
        public float slideConstantY = -10;
        [Range(5, 360), Tooltip("The amount of directional tests the player preforms to determine a slide direction. This has a large effect on performance.")]
        public int slidePhysicsChecks = 30;
        [Range(5, 90), Tooltip("The minimum angle that defines a fall instead of a slide")]
        public float minimumFallAngle = 75;
        [Range(5, 90), Tooltip("The maximum angle the player will try to climb.")]
        public float maximumClimbAngle = 60;
        [Range(5, 90), Tooltip("The minimum angle at which the player will start to slide")]
        public float slideEntryAngle = 60;
        [Range(5, 90), Tooltip("The maximum angle at which the player will stop sliding.")]
        public float slideExitAngle = 35f;
        [Range(5, 90), Tooltip("The linecast Y value distance offset used to prevent the player from clipping off sharp terrain angles.")]
        public float slideHeightOffset = 0.1f;
        [HideInInspector]
        //The current range at which the player calculates the nearby terrain angles:
        public float currentSlideRange = 0.1f;
        [Tooltip("The normal range at which the player calculates the nearby terrain angles.")]
        public float minSlideRange = 0.2f;
        [Tooltip("The range at which the player calculates the nearby terrain angles while sliding.")]
        public float maxSlideRange = 0.8f;
        [Tooltip("A speed multiplier for sliding.")]
        public float slideSpeedMultiplier = 1.5f;
        [Tooltip("The amount of time in seconds in the recovery state after sliding.")]
        public float slideRecoveryTime = 0.1f;
        //The calculated slide direction:
        [HideInInspector]
        public Vector3 slideVector;
    }

    [Header("Jumping Controls:")]
    public JumpingControls jumpingControls = new JumpingControls();
    [Serializable]
    public class JumpingControls
    {
        [Tooltip("Can the player jump while falling?")]
        public bool requireGroundState = true;
        [Tooltip("The amount of time in seconds the Recovery State lasts after falling.")]
        public float fallRecoveryTime = 0.2f;
        [Tooltip("The force multiplier of the jump.")]
        public float jumpForce = 10;
        [Tooltip("How long the jump force is applied for.")]
        public float jumpDuration = 0.1f;
        [Tooltip("The amount of time before the jump force is applied, for use with animations with a build-up")]
        public float jumpDelay = 0.25f;
        [HideInInspector]
        //The current of amount of time spent in the jump state. It will shift to the falling state once this reaches 0:
        public float currentJumpTime = 0;
        [Tooltip("Stops the player completely before jumping.")]
        public bool zeroPreJumpVelocity = true;
        [Tooltip("Force multiplier for x, z movement in the jump state")]
        public float jumpHorizontalForceMultipler = 0.68f;
        //Input delay for jumping and flying transitions, prevents spamming the jump key:
        [HideInInspector]
        public float isEvaluatingAttemptJump = 0;
        //Directional vector used for non-key based jumps such as jump plates:
        [HideInInspector]
        public Vector3 jumpDirection = Vector3.zero;
        //The starting velocity of the jump.
        [HideInInspector]
        public Vector3 jumpStartingVelocity = Vector3.zero;
    }

    [Header("Flying Controls:")]
    public FlyingControls flyingControls = new FlyingControls();

    [Serializable]
    public class FlyingControls
    {
        [Tooltip("Changes the state to the grounded state when the player is near the ground.")]
        public bool automaticLand = true;
        [Tooltip("The Amount of drag while flying.")]
        public float flyingDrag = 0.5f;
        [Tooltip("The base flying speed multiplier")]
        public float flyingBaseSpeedMultiplier = 1;
        [Tooltip("flying speed multiplier when moving downwards.")]
        public float flyingAdditiveDiveMultiplier = 0.1f;
        [Tooltip("flying speed multiplier when moving upwards.")]
        public float flyingAdditiveAscensionMultiplier = -0.1f;
        [Tooltip("Is the player always moving forward while flying?")]
        public bool flyingConstantForward = false;
        [Tooltip("Rotation speed multiplier for mouse control.")]
        public float flyingMaxRotationSpeedMultiplier = 0.2f;
        [Tooltip("Rotation speed multiplier for key control.")]
        public float flyingKeyRotationSpeedMultiplier = 0.2f;
        [Tooltip("Constant downwards force applied while flying.")]
        public float flyingGravity = 0;
        [Tooltip("Can the player enter the flying state from falling state?")]
        public bool enableFlyingFromFalling = true;
    }

    [Header("Swimming Controls:")]
    public SwimmingControls swimmingControls = new SwimmingControls();


    [Serializable]
    public class SwimmingControls
    {
        [Tooltip("The Amount of drag while swimming.")]
        public float swimmingDrag = 5;
        [Tooltip("The swimming base speed multiplier.")]
        public float swimmingSpeedMultiplier = 1f;
        [Tooltip("Y position offset for the raycast that checks for water, serves as adjustment for the required portion of the player in the water before swimming.")]
        public float swimmingRaycastOffset = -1.2f;
        [Tooltip("Adjustment for maximum player height against the water plane.")]
        public float surfaceBreakAdjustment = 0.3f;
        //The return distance of the water checking raycast:
        [HideInInspector]
        public float waterLevel = 0;
        [Tooltip("Can the player dive or only swim on the surface?")]
        public bool allowDive = true;
        [Tooltip("The maximum animation angle reached by the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroMaximumAnimationAngle = 90;
        [Range(0, 1), Tooltip("Physics gyro reaction is a blend of the character rep velocity and input direction determined by this value. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroInputFactor = 0.632f;
        [Tooltip("Defines what mixed value, between character rep velocity and input direction, will equate to a 90 degree rotation in the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroMagnitudeFactor = 6;
        [Tooltip("Dampening value for the physics gyro. Physics Gyro must be enabled. (Swimming State)")]
        public float swimmingPhysicsGyroTransitionSpeed = 2;
        [HideInInspector]
        public bool swimmingFalseWalk = false;
    }

    [HideInInspector]
    public BlinkControls blinkControls = new BlinkControls();

    [Serializable]
    public class BlinkControls
    {
        //Blink Variables
        public float blinkMinDistanceChange = 0.4f;
        public float blinkRecoveryTime = 0.3f;
        public float blinkAchievementDistance = 0.1f;
        public float blinkMaxHeightChange = 0.4f;
        public float blinkAnimationHeightOffset = 0;
        public bool blinkSnapRotation = false;
        public float blinkCharacterRepSpeed = 0.2f;
        public Vector3 calculatedBlinkTarget;
        public Quaternion idealStateRotation;
        public float blinkStateDelay = 0;
        public float blinkStateCurrentDelay = 0;
        public float currentCooldown = 0;
    }

    [HideInInspector]
    public LungeControls lungeControls = new LungeControls();

    [Serializable]
    public class LungeControls
    {
        //Lunge Variables
        public LungeAction lungeAction;
        public float currentLungeStep = 0;
    }

    //Rail System Variables:
    [HideInInspector]
    public RailSystemSettings railSystemSettings = new RailSystemSettings();

    [Serializable]
    public class RailSystemSettings
    {
        //The current rail system being evaluated by the player:
        [HideInInspector]
        public MultiStateMovementRailSystem railSystem;
        //The current position on the rail system:
        public float railPosition = 0;
        //The current direction, -1 being backward, 1 being forward, for the rail system.
        public float railDirection = 0;
        //Should the player detach from a rail system cleanly
        public bool willDetachFromRail = false;
        [HideInInspector]
        public float currentRailInteractionTime = 0;
        public float railInteractionTime = 0.5f;
        //Multiplier for animator railPosition parameter:
        public float animationRailPositionMultiplier = 1;
        //Offset for animator railPosition parameter:
        public float animationRailPositionOffset = 0;
        //Has the rail system called the animation trigger yet:
        public bool hasCalledOnRailsTrigger = false;
        //The last look direction while attached to a railSystem:
        public int onRailsLastLookDir = 1;
        //The transform of the active camera dolly:
        public Transform activeCameraDolly;
    }


    //Ragdoll
    [Header("Ragdoll Controls:")]
    public MSRagdoll mSRagdoll = new MSRagdoll();

    [Serializable]
    public class MSRagdoll
    {
        [Tooltip("The set y velocity when the ragdoll is first triggered")]
        public float initialRagdollYVelocity = 1;
    }

    [Header("Animation Settings:")]
    public BasicAnimationSettings basicAnimationSettings = new BasicAnimationSettings();

    [Serializable]
    public class BasicAnimationSettings
    {


        //Defines how the gyro transform rotates to visualize up and down motion, while using free vectors, and how the x and z animator variables are calculated:
        public enum GyroCalculationMethod
        {
            //Based on the reference velocity variable, this method can be unstable during certain maneuvers or in certain directions:   
            characterRepVelocity,
            //Based on the user input, this method is very stable:
            freeVectorInput,
            //Based on the external target location, this method can be unstable during certain maneuvers or in certain directions:  
            externalTargetDirectional,
            //Based on the difference in location between the character representation and the projected player, this method is very stable:
            characterRepPlayerOffset
        }
        [Tooltip("Defines how the gyro transform rotates to visualize up and down direction while using free vectors, and how the x and z animator variables are calculated. \n \n" +
            "   CharacterRepVelocity: Based on the reference velocity variable. This method can be unstable during certain maneuvers. \n \n" +
            "   FreeVectorInput: Based on the user input, this method is very stable. \n \n" +
            "   ExternalTargetDirectional: Based on the external target location, this method can be unstable during certain maneuvers. \n \n" +
            "   CharacterRepPlayerOffset: Based of the difference in location between the character representation and the projected player. This method is very stable.")]
        public GyroCalculationMethod gyroCalculationMethod = GyroCalculationMethod.freeVectorInput;
        [Range(0, 5)]
        [Tooltip("Multiplier for the animator's speed parameter: speedAdjustment.")]
        public float animationSpeedMultiplier = 1;
        [Tooltip("The maximum speed an animation can be while in the grounded state.")]
        public float groundedMaximumAnimationSpeed = 100;
        [Tooltip("The amount of single value tolerance before multiplying the animations speed.")]
        public float animationBaseTransition = 0.9f;
        [Tooltip("The speed at which the animator adjusts to a new grounded substate")]
        public float groundedAnimationSubstateTransitionSpeed = 8;
        //Defines the grounded substate for the animator
        [HideInInspector]
        public float currentAnimationSubstateValue = 0;
        [HideInInspector]
        public string commonAnimationTrigger = "";
        [HideInInspector]
        public List<float> angularVelocities = new List<float>();
        [HideInInspector]
        public Vector3 blinkAnimationHeight = new Vector3();
        [HideInInspector]
        //recorded rotation of the player
        public float angularVelocity;
        [HideInInspector]
        //animation velocity
        public Vector3 animationVelocity = Vector3.zero;
    }

    [Header("Swimming Animation Settings:")]
    public SwimmingAnimationSettings swimmingAnimationSettings = new SwimmingAnimationSettings();

    [Serializable]
    public class SwimmingAnimationSettings
    {
        [Tooltip("The maximum speed an animation can be while in the flying state.")]
        public float swimmingMaximumAnimationSpeed = 5;
        [Tooltip("Rotation speed of the gyro while swimming.")]
        public float swimmingGyroTransitionSpeed = 2;
        [Tooltip("Speed of the animator x, z values while swimming.")]
        public float swimmingAnimationTransitionSpeed = 2;
        [Tooltip("The minimum depth in water required for the gyro to be active.")]
        public float swimmingMinimumGyroDepth = 1.4f;
        [Tooltip("The maximum depth in water required before the gyro weakens in effect.")]
        public float swimmingGyroFadeDistance = 0.2f;
        [Tooltip("How weak the gyro will become when approaching the surface, before it is disabled entirely.")]
        public float swimmingGyroFadeMultiplier = 1000;
        [Tooltip("Gyro intensity control while swimming up.")]
        public float swimmingFreeVectorUpperVertMagLimit = 0.6f;
        [Tooltip("Gyro intensity control while swimming down.")]
        public float swimmingFreeVectorLowerVertMagLimit = 0.5f;
    }

    [Header("Flying Animation Settings:")]
    public FlyingAnimationSettings flyingAnimationSettings = new FlyingAnimationSettings();
    [Serializable]
    public class FlyingAnimationSettings
    {
        [Tooltip("The maximum speed an animation can be while in the swimming state.")]
        public float flyingMaximumAnimationSpeed = 2;
        [Tooltip("Rotation speed of the gyro while flying.")]
        public float flyingGyroTransitionSpeed = 1f;
        [Tooltip("Speed of the animator x, z values while flying.")]
        public float flyingAnimationTransitionSpeed = 1f;
        [Tooltip("Gyro intensity control while flying up.)")]
        public float flyingFreeVectorUpperVertMagLimit = 0.9f;
        [Tooltip("Gyro intensity control while flying down.")]
        public float flyingFreeVectorLowerVertMagLimit = 0.5f;
    }

    [Header("Root Motion (Beta!)")]
    public RootMotionSettings rootMotionSettings = new RootMotionSettings();

    [Serializable]
    public class RootMotionSettings
    {
        [Tooltip("Enable Root Motion for the grounded state? Included animations are NOT root motion compatable! Disable the animator's grounded blend tree's speed multiplier! You may want to adjust the grounded character rep speed. Beta: This feature has not been fully documented and tested, please report any problems to support@imitationstudios.com.")]
        public bool enableRootMotion = false;
        [HideInInspector]
        public float rootMotionSpeed = 0;
        [Tooltip("BETA! This value directly influcences character speed. This value overrides grounded transitional speed!")]
        public float rootMotionModifier = 0.6f;
        [Tooltip("BETA! This value influcences the speed of direction changes.")]
        public float rootMotionAnimationAdjustmentSpeed = 3;
        [Tooltip("BETA! Check for obstructions before adjusting the animator?")]
        public bool rootMotionCheckInputDirection = true;
        [Tooltip("BETA! Obstruction raycast distance.")]
        public float rootMotionCastDistance = 1;
        [Tooltip("BETA! Obstruction raycast height offset.")]
        public float rootMotionCastHeightOffset = 0.25f;
        [Tooltip("BETA! Obstructions layer mask.")]
        public LayerMask rootMotionLayerMask;
    }

    [Header("IK Controls:")]
    public IKSettings iKSettings = new IKSettings();

    [Serializable]
    public class IKSettings
    {
        [Tooltip("Is IK enabled?")]
        public bool enableIK = true;
        [Tooltip("Is feet IK rotation enabled? Note: the feet will always face forward when enabled.")]
        public bool enableFeetIkRotation = true;
        [Tooltip("Is hand IK rotation enabled?")]
        public bool enableHandIkRotation = true;
        [Tooltip("The foot's height.")]
        public float footHeight = 0.08f;
        [Tooltip("The foot's Length.")]
        public float footLength = 02f;
        [Tooltip("The thickness of the hands.")]
        public float handThickness = 0.037f;
        [Tooltip("The body's y value offset. This is similar in function to characterRepHeightOffset, but only affects height while applying IK.")]
        public float bodyOffset = 0f;
        [Tooltip("The total distance the body can be lowered towards the feet.")]
        public float bodyDropFactor = 1f;
        [Tooltip("The maximum distance the body will drop.")]
        public float bodyDropLimit = 0.2f;
        [Tooltip("Multiplier for the distance the feet pull down the body.")]
        public float bodyOffsetMultiplier = 1f;
        [Tooltip("The forward ik raycast length.")]
        public float forwardProjectionDistance = 1f;
        //A list of available ik targets:
        [HideInInspector]
        public List<Transform> ikTargetSet = new List<Transform>();

        //Determines how feet ik is handled:
        public enum FeetLocationMethod
        {
            //None:
            none,
            //Feet will try to reach the floor:
            projectDown,
            //Feet will be projected forward then to the floor, useful for climbing purposes:
            projectForwardAndDown,
            //Feet will be pinned to the closet target in the ikTargetSet:
            lockToTarget
        }
        [HideInInspector]
        public FeetLocationMethod feetLocationMethod = FeetLocationMethod.projectDown;

        //Determines how hand ik is handled:
        public enum HandLocationMethod
        {
            //None:
            none,
            //Hands will be projected forward:
            projectForward,
            //Hands will be pinned to the closet target in the ikTargetSet:
            lockToTarget
        }
        [HideInInspector]
        public HandLocationMethod handLocationMethod = HandLocationMethod.none;
    }
}
