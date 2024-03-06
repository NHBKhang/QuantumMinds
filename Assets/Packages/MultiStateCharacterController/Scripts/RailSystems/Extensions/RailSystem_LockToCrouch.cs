using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailSystem_LockToCrouch : MonoBehaviour, IRailSystemExtension
{
    [Tooltip("Allow the switch key to change the grounded substate?")]
    public bool allowKeyBasedSwitching = true;
    [Tooltip("Key used for grounded substate switching.")]
    public KeyCode switchKey = KeyCode.Mouse1;

    private bool active = false;
    private MultistateCharacterController character;

    public void OnKeyUnlatch(MultistateCharacterController characterController)
    {
        
    }

    //Checks for key-based switching
    private void Update()
    {
        if (!allowKeyBasedSwitching) return;
        if (!active) return;
        if (Input.GetKey(switchKey))
        {
            character.SetRunningAsDefaultGroundedMovementSubstate();
        }
        else
        {
            character.SetCrouchingAsDefaultGroundedMovementSubstate();
        }
    }

    //Sets the grounded substate to crouching
    public void OnLatch(MultistateCharacterController characterController)
    {
        characterController.settings.acv.playerInput.SetKeyBasedSwitching(false);
        characterController.SetCrouchingAsDefaultGroundedMovementSubstate();

        character = characterController;
        active = true;
    }

    //Sets the grounded substate to running
    public void OnUnlatch(MultistateCharacterController characterController)
    {
        characterController.settings.acv.playerInput.SetKeyBasedSwitching(true);
        characterController.SetRunningAsDefaultGroundedMovementSubstate();

        active = false;
    }
}
