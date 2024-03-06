using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable, CreateAssetMenu(fileName = "MSCCProfile", menuName = "MSCC/MSCC Profile", order = 1)]
public class MSCCProfile : ScriptableObject
{
    public MultiStateSettings settings = new MultiStateSettings();

    public MultiStateSettings CreateInstanceOfProfile(MultiStateSettings preservedSettings)
    {
        MultiStateSettings newSettings = preservedSettings;
        newSettings.basicAnimationSettings.animationBaseTransition = settings.basicAnimationSettings.animationBaseTransition;
        newSettings.basicAnimationSettings.animationSpeedMultiplier = settings.basicAnimationSettings.animationSpeedMultiplier;
        newSettings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed = settings.basicAnimationSettings.groundedAnimationSubstateTransitionSpeed;
        newSettings.basicAnimationSettings.groundedMaximumAnimationSpeed = settings.basicAnimationSettings.groundedMaximumAnimationSpeed;
        newSettings.basicAnimationSettings.gyroCalculationMethod = settings.basicAnimationSettings.gyroCalculationMethod;

        newSettings.basicControls.playerWidth = settings.basicControls.playerWidth;
        newSettings.basicControls.playerHeight = settings.basicControls.playerHeight;

        newSettings.directionalVectorModifiers.averageYValueCount = settings.directionalVectorModifiers.averageYValueCount;
        newSettings.directionalVectorModifiers.groundedTransitionalSpeed = settings.directionalVectorModifiers.groundedTransitionalSpeed;
        newSettings.directionalVectorModifiers.maximumRotationSpeed = settings.directionalVectorModifiers.maximumRotationSpeed;
        newSettings.directionalVectorModifiers.rotationalSpeedMultiplier = settings.directionalVectorModifiers.rotationalSpeedMultiplier;

        newSettings.fallProtection.fallProtectionForceMultiplier = settings.fallProtection.fallProtectionForceMultiplier;
        newSettings.fallProtection.fallProtectionMaximumForce = settings.fallProtection.fallProtectionMaximumForce;
        newSettings.fallProtection.minimumFallTime = settings.fallProtection.minimumFallTime;

        newSettings.flyingAnimationSettings.flyingAnimationTransitionSpeed = settings.flyingAnimationSettings.flyingAnimationTransitionSpeed;
        newSettings.flyingAnimationSettings.flyingFreeVectorLowerVertMagLimit = settings.flyingAnimationSettings.flyingFreeVectorLowerVertMagLimit;
        newSettings.flyingAnimationSettings.flyingFreeVectorUpperVertMagLimit = settings.flyingAnimationSettings.flyingFreeVectorUpperVertMagLimit;
        newSettings.flyingAnimationSettings.flyingGyroTransitionSpeed = settings.flyingAnimationSettings.flyingGyroTransitionSpeed;
        newSettings.flyingAnimationSettings.flyingMaximumAnimationSpeed = settings.flyingAnimationSettings.flyingMaximumAnimationSpeed;

        newSettings.flyingControls.automaticLand = settings.flyingControls.automaticLand;
        newSettings.flyingControls.enableFlyingFromFalling = settings.flyingControls.enableFlyingFromFalling;
        newSettings.flyingControls.flyingAdditiveAscensionMultiplier = settings.flyingControls.flyingAdditiveAscensionMultiplier;
        newSettings.flyingControls.flyingBaseSpeedMultiplier = settings.flyingControls.flyingBaseSpeedMultiplier;
        newSettings.flyingControls.flyingConstantForward = settings.flyingControls.flyingConstantForward;
        newSettings.flyingControls.flyingAdditiveDiveMultiplier = settings.flyingControls.flyingAdditiveDiveMultiplier;
        newSettings.flyingControls.flyingDrag = settings.flyingControls.flyingDrag;
        newSettings.flyingControls.flyingGravity = settings.flyingControls.flyingGravity;
        newSettings.flyingControls.flyingKeyRotationSpeedMultiplier = settings.flyingControls.flyingKeyRotationSpeedMultiplier;
        newSettings.flyingControls.flyingMaxRotationSpeedMultiplier = settings.flyingControls.flyingMaxRotationSpeedMultiplier;

        newSettings.groundedSubstateModifiers.crouchingColliderDifference = settings.groundedSubstateModifiers.crouchingColliderDifference;
        newSettings.groundedSubstateModifiers.crouchingSpeedMultiplier = settings.groundedSubstateModifiers.crouchingSpeedMultiplier;
        newSettings.groundedSubstateModifiers.defaultGroundedMovementSubState = settings.groundedSubstateModifiers.defaultGroundedMovementSubState;
        newSettings.groundedSubstateModifiers.runningSpeedMultiplier = settings.groundedSubstateModifiers.runningSpeedMultiplier;
        newSettings.groundedSubstateModifiers.sprintingSpeedMultiplier = settings.groundedSubstateModifiers.sprintingSpeedMultiplier;
        newSettings.groundedSubstateModifiers.walkingSpeedMultiplier = settings.groundedSubstateModifiers.walkingSpeedMultiplier;

        newSettings.iKSettings.bodyDropFactor = settings.iKSettings.bodyDropFactor;
        newSettings.iKSettings.bodyOffset = settings.iKSettings.bodyOffset;
        newSettings.iKSettings.bodyOffsetMultiplier = settings.iKSettings.bodyOffsetMultiplier;
        newSettings.iKSettings.enableFeetIkRotation = settings.iKSettings.enableFeetIkRotation;
        newSettings.iKSettings.enableHandIkRotation = settings.iKSettings.enableHandIkRotation;
        newSettings.iKSettings.enableIK = settings.iKSettings.enableIK;
        newSettings.iKSettings.footHeight = settings.iKSettings.footHeight;
        newSettings.iKSettings.footLength = settings.iKSettings.footLength;
        newSettings.iKSettings.forwardProjectionDistance = settings.iKSettings.forwardProjectionDistance;
        newSettings.iKSettings.handThickness = settings.iKSettings.handThickness;
        newSettings.iKSettings.bodyDropLimit = settings.iKSettings.bodyDropLimit;

        newSettings.jumpingControls.fallRecoveryTime = settings.jumpingControls.fallRecoveryTime;
        newSettings.jumpingControls.jumpDelay = settings.jumpingControls.jumpDelay;
        newSettings.jumpingControls.jumpDuration = settings.jumpingControls.jumpDuration;
        newSettings.jumpingControls.jumpForce = settings.jumpingControls.jumpForce;
        newSettings.jumpingControls.jumpHorizontalForceMultipler = settings.jumpingControls.jumpHorizontalForceMultipler;
        newSettings.jumpingControls.requireGroundState = settings.jumpingControls.requireGroundState;
        newSettings.jumpingControls.zeroPreJumpVelocity = settings.jumpingControls.zeroPreJumpVelocity;

        newSettings.layerMasks.blinkCollisionLayer = settings.layerMasks.blinkCollisionLayer;
        newSettings.layerMasks.disableLandLayer = settings.layerMasks.disableLandLayer;
        newSettings.layerMasks.groundLayer = settings.layerMasks.groundLayer;
        newSettings.layerMasks.ikLayer = settings.layerMasks.ikLayer;
        newSettings.layerMasks.overheadLayer = settings.layerMasks.overheadLayer;
        newSettings.layerMasks.waterLayer = settings.layerMasks.waterLayer;

        newSettings.movementStateSettings.activeMovementStates = settings.movementStateSettings.activeMovementStates;
        newSettings.movementStateSettings.ejectPlayerFromDisabledStates = settings.movementStateSettings.ejectPlayerFromDisabledStates;
        newSettings.movementStateSettings.requestedState = settings.movementStateSettings.requestedState;

        newSettings.mSRagdoll.initialRagdollYVelocity = settings.mSRagdoll.initialRagdollYVelocity;

        newSettings.physicsGyroSettings.enablePhysicsGyro = settings.physicsGyroSettings.enablePhysicsGyro;
        newSettings.physicsGyroSettings.flyingPhysicsGyroInputFactor = settings.physicsGyroSettings.flyingPhysicsGyroInputFactor;
        newSettings.physicsGyroSettings.flyingPhysicsGyroMagnitudeFactor = settings.physicsGyroSettings.flyingPhysicsGyroMagnitudeFactor;
        newSettings.physicsGyroSettings.flyingPhysicsGyroMaximumAnimationAngle = settings.physicsGyroSettings.flyingPhysicsGyroMaximumAnimationAngle;
        newSettings.physicsGyroSettings.flyingPhysicsGyroTransitionSpeed = settings.physicsGyroSettings.flyingPhysicsGyroTransitionSpeed;
        newSettings.physicsGyroSettings.physicsGyroRadiusMultiplier = settings.physicsGyroSettings.physicsGyroRadiusMultiplier;
        newSettings.physicsGyroSettings.swimmingPhysicsGyroInputFactor = settings.physicsGyroSettings.swimmingPhysicsGyroInputFactor;
        newSettings.physicsGyroSettings.swimmingPhysicsGyroMagnitudeFactor = settings.physicsGyroSettings.swimmingPhysicsGyroMagnitudeFactor;
        newSettings.physicsGyroSettings.swimmingPhysicsGyroMaximumAnimationAngle = settings.physicsGyroSettings.swimmingPhysicsGyroMaximumAnimationAngle;
        newSettings.physicsGyroSettings.swimmingPhysicsGyroTransitionSpeed = settings.physicsGyroSettings.swimmingPhysicsGyroTransitionSpeed;

        newSettings.raycastDistances.fallingRaycastDistance = settings.raycastDistances.fallingRaycastDistance;
        newSettings.raycastDistances.flyingRaycastDistance = settings.raycastDistances.flyingRaycastDistance;
        newSettings.raycastDistances.groundRaycastDistance = settings.raycastDistances.groundRaycastDistance;
        newSettings.raycastDistances.jumpingRaycastDistance = settings.raycastDistances.jumpingRaycastDistance;
        newSettings.raycastDistances.recoveryRaycastDistance = settings.raycastDistances.recoveryRaycastDistance;
        newSettings.raycastDistances.slidingRaycastDistance = settings.raycastDistances.slidingRaycastDistance;
        newSettings.raycastDistances.swimmingRaycastDistance = settings.raycastDistances.swimmingRaycastDistance;

        newSettings.rigidbodyForceModifiers.constantYGroundedForce = settings.rigidbodyForceModifiers.constantYGroundedForce;
        newSettings.rigidbodyForceModifiers.fallVelocityPreservationFactor = settings.rigidbodyForceModifiers.fallVelocityPreservationFactor;
        newSettings.rigidbodyForceModifiers.fallVelocityYPreservationFactor = settings.rigidbodyForceModifiers.fallVelocityYPreservationFactor;
        newSettings.rigidbodyForceModifiers.forceMultiplier = settings.rigidbodyForceModifiers.forceMultiplier;
        newSettings.rigidbodyForceModifiers.gravity = settings.rigidbodyForceModifiers.gravity;
        newSettings.rigidbodyForceModifiers.normalDrag = settings.rigidbodyForceModifiers.normalDrag;
        newSettings.rigidbodyForceModifiers.variableDistanceYForceMultiplier = settings.rigidbodyForceModifiers.variableDistanceYForceMultiplier;

        newSettings.rootMotionSettings.enableRootMotion = settings.rootMotionSettings.enableRootMotion;
        newSettings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed = settings.rootMotionSettings.rootMotionAnimationAdjustmentSpeed;
        newSettings.rootMotionSettings.rootMotionCastDistance = settings.rootMotionSettings.rootMotionCastDistance;
        newSettings.rootMotionSettings.rootMotionCastHeightOffset = settings.rootMotionSettings.rootMotionCastHeightOffset;
        newSettings.rootMotionSettings.rootMotionCheckInputDirection = settings.rootMotionSettings.rootMotionCheckInputDirection;
        newSettings.rootMotionSettings.rootMotionLayerMask = settings.rootMotionSettings.rootMotionLayerMask;
        newSettings.rootMotionSettings.rootMotionModifier = settings.rootMotionSettings.rootMotionModifier;
        newSettings.rootMotionSettings.rootMotionSpeed = settings.rootMotionSettings.rootMotionSpeed;

        newSettings.slideControls.maximumClimbAngle = settings.slideControls.maximumClimbAngle;
        newSettings.slideControls.maxSlideRange = settings.slideControls.maxSlideRange;
        newSettings.slideControls.minimumFallAngle = settings.slideControls.minimumFallAngle;
        newSettings.slideControls.minSlideRange = settings.slideControls.minSlideRange;
        newSettings.slideControls.slideConstantY = settings.slideControls.slideConstantY;
        newSettings.slideControls.slideEntryAngle = settings.slideControls.slideEntryAngle;
        newSettings.slideControls.slideExitAngle = settings.slideControls.slideExitAngle;
        newSettings.slideControls.slideHeightOffset = settings.slideControls.slideHeightOffset;
        newSettings.slideControls.slidePhysicsChecks = settings.slideControls.slidePhysicsChecks;
        newSettings.slideControls.slideRecoveryTime = settings.slideControls.slideRecoveryTime;
        newSettings.slideControls.slideSpeedMultiplier = settings.slideControls.slideSpeedMultiplier;

        newSettings.swimmingAnimationSettings.swimmingAnimationTransitionSpeed = settings.swimmingAnimationSettings.swimmingAnimationTransitionSpeed;
        newSettings.swimmingAnimationSettings.swimmingFreeVectorLowerVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorLowerVertMagLimit;
        newSettings.swimmingAnimationSettings.swimmingFreeVectorUpperVertMagLimit = settings.swimmingAnimationSettings.swimmingFreeVectorUpperVertMagLimit;
        newSettings.swimmingAnimationSettings.swimmingGyroFadeDistance = settings.swimmingAnimationSettings.swimmingGyroFadeDistance;
        newSettings.swimmingAnimationSettings.swimmingGyroFadeMultiplier = settings.swimmingAnimationSettings.swimmingGyroFadeMultiplier;
        newSettings.swimmingAnimationSettings.swimmingGyroTransitionSpeed = settings.swimmingAnimationSettings.swimmingGyroTransitionSpeed;
        newSettings.swimmingAnimationSettings.swimmingMaximumAnimationSpeed = settings.swimmingAnimationSettings.swimmingMaximumAnimationSpeed;
        newSettings.swimmingAnimationSettings.swimmingMinimumGyroDepth = settings.swimmingAnimationSettings.swimmingMinimumGyroDepth;

        newSettings.swimmingControls.allowDive = settings.swimmingControls.allowDive;
        newSettings.swimmingControls.surfaceBreakAdjustment = settings.swimmingControls.surfaceBreakAdjustment;
        newSettings.swimmingControls.swimmingDrag = settings.swimmingControls.swimmingDrag;
        newSettings.swimmingControls.swimmingPhysicsGyroInputFactor = settings.swimmingControls.swimmingPhysicsGyroInputFactor;
        newSettings.swimmingControls.swimmingPhysicsGyroMagnitudeFactor = settings.swimmingControls.swimmingPhysicsGyroMagnitudeFactor;
        newSettings.swimmingControls.swimmingPhysicsGyroMaximumAnimationAngle = settings.swimmingControls.swimmingPhysicsGyroMaximumAnimationAngle;
        newSettings.swimmingControls.swimmingPhysicsGyroTransitionSpeed = settings.swimmingControls.swimmingPhysicsGyroTransitionSpeed;
        newSettings.swimmingControls.swimmingRaycastOffset = settings.swimmingControls.swimmingRaycastOffset;
        newSettings.swimmingControls.swimmingSpeedMultiplier = settings.swimmingControls.swimmingSpeedMultiplier;

        newSettings.visualRepresentation.characterRepAdjustmentSpeed = settings.visualRepresentation.characterRepAdjustmentSpeed;
        newSettings.visualRepresentation.characterRepHeightOffset = settings.visualRepresentation.characterRepHeightOffset;

        newSettings.visualRepresentation.repSpeed.fallingCharacterRepSpeed = settings.visualRepresentation.repSpeed.fallingCharacterRepSpeed;
        newSettings.visualRepresentation.repSpeed.flyingCharacterRepSpeed = settings.visualRepresentation.repSpeed.flyingCharacterRepSpeed;
        newSettings.visualRepresentation.repSpeed.groundedCharacterRepSpeed = settings.visualRepresentation.repSpeed.groundedCharacterRepSpeed;
        newSettings.visualRepresentation.repSpeed.swimmingCharacterRepSpeed = settings.visualRepresentation.repSpeed.swimmingCharacterRepSpeed;

        return newSettings;
    }
}
