using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAudio : Audio
{
    [Header("Preferences")]
    public Transform player;

    [Header("Stats")]
    public float baseStepSpeed = 0.65f;
    public float crouchStepSpped = 1.6f;
    public AudioSource footstepsAudioSource = default;
    public AudioClip[] metalFootsteps;
    
    private float footstepTimer = 0;
    private float GetCurrentOffset => FPSController.isCrouching ? crouchStepSpped : baseStepSpeed;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FootstepsSound()
    {
        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(player.position, Vector3.down, out RaycastHit hit, 1))
            {
                switch (hit.collider.tag)
                {
                    case "Footsteps/Metal":
                        footstepsAudioSource.PlayOneShot(metalFootsteps[Random.Range(0, metalFootsteps.Length - 1)]);
                        break;
                    default:
                        footstepsAudioSource.PlayOneShot(metalFootsteps[Random.Range(0, metalFootsteps.Length - 1)]);
                        break;
                }
            }

            footstepTimer = GetCurrentOffset;
        }
    }
}
