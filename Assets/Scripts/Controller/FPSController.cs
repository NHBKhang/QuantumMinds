using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public KeyBind keyBind;
    public Collider[] colliders;
    
    [Header("Player")]
    public GameObject player;
    public Audio movementAudio;
    public Animation playerAnimation;
    
    [Header("Stats")]
    public float walkSpeed = 1.8f;
    public float crouchSpeed = 1.2f;

    private float horizontalInput, verticalInput;
    public static bool isCrouching = false;

    private AgnesAnimation agnesAnimation;
    private MovementAudio playerMovementAudio;
    private bool isMoving => horizontalInput != 0 || verticalInput != 0;
    private float moveSpeed => isCrouching ? crouchSpeed : walkSpeed;

    private void OnValidate()
    {
        if (crouchSpeed >= walkSpeed)
        {
            crouchSpeed = walkSpeed - .1f;
        }
    }

    private void Awake()
    {
        agnesAnimation = (AgnesAnimation)playerAnimation;
        playerMovementAudio = (MovementAudio)movementAudio;
    }

    // Update is called once per frame
    void Update()
    {
        if (MyUI.ModernMenu.UIPauseMenuManager.isPaused)
        {
            return;
        }

        PlayerInput();
        MovePlayer();
        PlayerAnimation();
        PlayerAudio();


        colliders[0].enabled = !isCrouching;
        colliders[1].enabled = isCrouching;
       
    }

    private void PlayerInput()
    {
        //Movment
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(keyBind.crouchKey))
        {
            isCrouching = !isCrouching;
        }
    }

    private void MovePlayer()
    {
        Vector3 moveDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

        {
            rb.velocity = moveDir.normalized * moveSpeed;
        }
    }

    private void PlayerAnimation()
    {
        agnesAnimation.SetBlendTree(horizontalInput, verticalInput);
        agnesAnimation.SwitchState(isCrouching);
    }

    private void PlayerAudio()
    {
        if (isMoving)
        {
            playerMovementAudio.FootstepsSound();
        }
    }
}
