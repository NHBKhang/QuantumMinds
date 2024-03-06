using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementStateActivator : MonoBehaviour
{
    [Tooltip("Particle systems that are toggled based on player flight")]
    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    [Tooltip("Gameobject that is toggled based on player flight")]
    public GameObject onScreen;
    [Tooltip("Gameobject that is toggled based on player flight")]
    public GameObject offScreen;

    //Toggles the particle system's and gameobjects player on the player's movement state. This is called by an MSCC event by default
    public void ToggleActivation(MovementState movementState)
    {
        if (movementState == MovementState.flying)
        {
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                    particleSystem.Play();
            }
            onScreen.SetActive(true);
            offScreen.SetActive(false);
        }
        else
        {
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                    particleSystem.Stop();
            }
            onScreen.SetActive(false);
            offScreen.SetActive(true);
        }
    }
}
