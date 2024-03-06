using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleJumpPlate : MonoBehaviour
{
    [Tooltip("Should this jump plate interrupt rail systems?")]
    public bool interruptRailSystem = true;
    [Tooltip("The preset direction of the jump")]
    public Vector3 jumpDirection = new Vector3();
    [Tooltip("Is the jump plate disabled after its activation?")]
    public bool disableOnTrigger = false;

    //Causes the player to jump
    private void OnTriggerEnter(Collider other)
    {
        MultistateCharacterController player = other.transform.GetComponent<MultistateCharacterController>();
        if (player != null)
        {
            if (interruptRailSystem) { player.DetachFromRail(MovementState.jumping); player.ForceJump(jumpDirection); }
            else
            {
                player.AttemptJump();
            }
            
            if (disableOnTrigger) gameObject.SetActive(false);
        }
    }
}
