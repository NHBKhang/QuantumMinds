
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MultiStateMovementRailCap : MonoBehaviour
{
    [Tooltip("The rail system that this rail system cap is connected to")]
    public MultiStateMovementRailSystem railSystem;
    [Tooltip("Optional interaction key")]
    public KeyCode interactionKey;
    [Tooltip("Will the player attach to the rail system when interacting with this cap?")]
    public bool allowLatch = true;
    [Tooltip("Will the player detach to the rail system when interacting with this cap?")]
    public bool allowUnlatch = true;

    [Serializable]
    public enum LatchType
    {
        setPosition,
        calculatedPosition
    }

    [Serializable]
    public struct LatchPosition
    {
        [Tooltip("Defines where the player will latch to the rail system.        Set Position - The player will latch to the value of the position parameter.        Calculated position - The player will calculate a latch position within a window based on two reference points.")]
        public LatchType latchType;

        [Tooltip("Case 1: What location along the rail system the player will attach to. Case 2: defines the starting window position of a calculated latch.")]
        public float position;
        [Tooltip("Defines the ending window position of a calculated latch.")]
        public float endPosition;
        [Tooltip("The first comparison transform used in calculated latches.")]
        public Transform markerOne;
        [Tooltip("The second comparison transform used in calculated latches.")]
        public Transform markerTwo;
    }

    public LatchPosition latchPosition = new LatchPosition(); 

    [Tooltip("In what direction along the rail system will the player move? 1 is in the positive direction. -1 is in the negative direction.")]
    public float direction = 1;

    [Serializable]
    public class RemoteAccess
    {
        [Tooltip("Can the player blink to the latch position?")]
        public bool allowRemoteAccess = false;
        [Tooltip("The minimum distance required for remote access.")]
        public float remoteAccessMinDistance = 10;
        [Tooltip("The maixmum distance required for remote access.")]
        public float remoteAccessMaxDistance = 10;
        [Tooltip("The animation trigger for the remote access.")]
        public string animationTrigger = "";
        [Tooltip("The remote access speed as a product of distance.")]
        public float speedFactor = 1;
        [HideInInspector]
        public float timeout = 0;
        [HideInInspector]
        public bool called = false;
    }
    [Header("Remote Access:")]
    public RemoteAccess remoteAccess;

    //Either latches to or unlatches from the rail system
    void Interact(Collider other)
    {
        if (railSystem == null)
        {
            Debug.LogError("Assign the Rail System to the Caps!");
            return;
        }
        MultistateCharacterController characterController = other.GetComponent<MultistateCharacterController>();
        if (characterController != null)
        {
            if (characterController.settings.railSystemSettings.currentRailInteractionTime != 0) return;

            if (remoteAccess.allowRemoteAccess)
            {
                if (remoteAccess.called) return;
                if (characterController.settings.railSystemSettings.railSystem == railSystem) return;


                remoteAccess.timeout = 0;
                remoteAccess.called = true;
                StartCoroutine(InteractRemotely(other, characterController));
                return;
            }

            if (characterController.settings.railSystemSettings.railSystem == null || characterController.settings.railSystemSettings.railSystem == railSystem)
            {
                InteractLocal(other, characterController);
            }
            
        }
    }

    IEnumerator InteractRemotely(Collider other, MultistateCharacterController characterController)
    {

        while (characterController.settings.movementStateSettings.movementState != MovementState.grounded)
        {
            remoteAccess.timeout += Time.deltaTime;
            if (remoteAccess.timeout > 5)
            {
                break;
            }
            yield return 0;
        } 

        if (remoteAccess.timeout < 5)
        {
                InteractLocal(other, characterController);
        }

        remoteAccess.called = false;
    }

    void InteractLocal(Collider other, MultistateCharacterController characterController)
    {
        int wasLatched = 0;
        float evaLatchposition = latchPosition.position;

        if (latchPosition.latchType == LatchType.setPosition)
        {
            railSystem.LatchToRail(evaLatchposition, characterController);
        }
        if (latchPosition.latchType == LatchType.calculatedPosition)
        {
            evaLatchposition = CalculateLatchPosition(other.transform.position);
            railSystem.LatchToRail(evaLatchposition, characterController);
        }

        if (railSystem.primarySettings.attachCameraToDolly)
        {
            wasLatched = characterController.EvaluateRail(railSystem, evaLatchposition, direction, allowLatch, allowUnlatch, railSystem.primarySettings.feetLocationMethod, railSystem.primarySettings.handLocationMethod, railSystem.primarySettings.ikTargetParent, railSystem.primarySettings.railAnimationPositionMultiplier, railSystem.primarySettings.railAnimationPositionOffset, railSystem.universalSettings.onRailDollies[railSystem.primarySettings.cameraDollyIndex].transform);
        }
        else
        {
            wasLatched = characterController.EvaluateRail(railSystem, evaLatchposition, direction, allowLatch, allowUnlatch, railSystem.primarySettings.feetLocationMethod, railSystem.primarySettings.handLocationMethod, railSystem.primarySettings.ikTargetParent, railSystem.primarySettings.railAnimationPositionMultiplier, railSystem.primarySettings.railAnimationPositionOffset);
        }
    }

    float CalculateLatchPosition (Vector3 playerPosition)
    {
        float distanceOne = Vector3.Distance(playerPosition, latchPosition.markerOne.position);
        float distanceTwo = Vector3.Distance(playerPosition, latchPosition.markerTwo.position);
        float totalDistance = distanceOne + distanceTwo;
        float percentageDistance = distanceOne / totalDistance;
        float returnValue = (((latchPosition.endPosition - latchPosition.position) * percentageDistance) + latchPosition.position);
        return returnValue;
    }

    //Interacts with the rail system on trigger enter if an interaction key has not been set
    private void OnTriggerEnter(Collider other)
    {
        if (interactionKey != KeyCode.None) return;
        Interact(other);
    }

    //Interacts with the rail system when the player is within the trigger and the interaction key has been pressed
    private void OnTriggerStay(Collider other)
    {
        if (interactionKey == KeyCode.None) return;
        if (Input.GetKey(interactionKey))
            Interact(other);
    }
 }
