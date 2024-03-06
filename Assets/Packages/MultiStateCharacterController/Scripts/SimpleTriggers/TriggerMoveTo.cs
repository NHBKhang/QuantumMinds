using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerMoveTo : MonoBehaviour
{
    [Tooltip("Target that the player will move towards")]
    public Transform target;
    [Tooltip("The distance that defines when the player will stop moving towards the target")]
    public float exitDistance = 0.2f;
    [Tooltip("Is this trigger disabled after its activation?")]
    public bool disableOnTrigger = false;

    //Moves the player towards a target
    private void OnTriggerEnter(Collider other)
    {
        MultistateCharacterController player = other.transform.GetComponent<MultistateCharacterController>();
        if (player != null)
        {
            player.settings.moveToTargetFunctionality.moveToTarget = target;
            player.settings.moveToTargetFunctionality.moveToTargetDistance = exitDistance;
            if (disableOnTrigger) gameObject.SetActive(false);
        }
    }
}
