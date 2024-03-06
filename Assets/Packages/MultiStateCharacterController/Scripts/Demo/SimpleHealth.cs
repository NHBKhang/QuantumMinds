
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleHealth : MonoBehaviour
{
    [Tooltip("MultistateCharacterController script located on the projected player")]
    public MultistateCharacterController characterController;
    [Range(1,100), Tooltip("Health is capped at 100")]
    public float health = 100;
    [Tooltip("Fall damage is equal to the amount of seconds multiplied by this multiplier.")]
    public float fallDamageMultiplier = 10;

    //Calculates fall damage by the fall time
    public void TakeFallDamage(float fallTime)
    {
        if (fallTime < 0.6f) return;
        float damage = Mathf.Round(fallTime * fallDamageMultiplier);
        Debug.Log("Player took " + damage + " damage by falling for " +fallTime + "seconds!");
        TakeDamage(damage);
    }

    //Reduces health and enables the character ragdoll when its health is equal to 0
    void TakeDamage(float amount)
    {
        health = Mathf.Clamp(health-amount, 0, 100);

        if (health == 0)
        {
            characterController.EnableRagdoll();
        }
    }
}
