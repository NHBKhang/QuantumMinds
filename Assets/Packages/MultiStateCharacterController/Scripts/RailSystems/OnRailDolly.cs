using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class OnRailDolly : MonoBehaviour
{
    //Is the dolly active
    [HideInInspector]
    public bool isActive;
    [Header("Base Function Settings:"), Tooltip("The rail system that this dolly will travel along. This cannot be the original rail system.")]
    public MultiStateMovementRailSystem railSystem;
    [Tooltip("Positional offset along the rail system from the evaluated player's rail system position")]
    public float offset = 0;
    [Tooltip("Positional multiplier along the rail system from the evaluated player's rail system position")]
    public float multiplier = 1;
    [Tooltip("The speed at which the dolly moves to the evaluated position. This is NOT the speed that the dolly moves along the rail system independently of the player's rail system position.")]
    public float speed = 1;
    [Header("Rotation Settings:"), Tooltip("Should the dolly rotate towards the next waypoint?")]
    public bool rotateTowardsNextWaypoint = true;
    [Tooltip("The speed at which the dolly rotates")]
    public float rotationSpeedMultiplier = 1;
    [Tooltip("Should the dolly change directions based on the direction the player is traveling along the rail system?")]
    public bool allowReverseRotation = true;
    [Tooltip("Should the dolly only rotate on the Y axis?")]
    public bool limitRotationToY = false;
    //The last evaluated position
    private float lastPosition = -1;
    //The last evaluated direction
    private int lastDir = 1;
    //The evaluated position in world space
    private Vector3 targetLocation;
    //Look rotation
    Quaternion lookRot;

    //Sets the default variables
    private void Start()
    {
        targetLocation = transform.position;
        lookRot = transform.rotation;
    }

    //Moves the dolly towards the target location and deactivates it when it reaches the position
    private void FixedUpdate()
    {
        if (isActive)
        {
            transform.position = Vector3.Lerp(transform.position, targetLocation, speed * Time.deltaTime);
            EvaluateRotation();
        }
        if (transform.position == targetLocation && transform.rotation == lookRot)
        {
            isActive = false;
        }
    }
    //Resets the dolly's position
    public void ResetPosition(float position)
    {
        transform.position = railSystem.GetRailLocation((position * multiplier) + offset);
        targetLocation = railSystem.GetRailLocation((position * multiplier) + offset);
    }
    //Sets the target position based on the player's rail system position
    public void EvaluatePosition(float position)
    {
        isActive = true;
        targetLocation = railSystem.GetRailLocation((position * multiplier) + offset);
        
            if (lastPosition > position)
            {
                lastDir = 0;
            }
            if (lastPosition < position)
            {
                lastDir = 1;
            }
        
        lastPosition = position;
    }
    //Sets the target rotation and limits it if required
    void EvaluateRotation()
    {
        if (rotateTowardsNextWaypoint)
        {
            if (allowReverseRotation)
            {
                lookRot = Quaternion.LookRotation(railSystem.GetFloorWaypoint(lastDir) - transform.position, Vector3.up);
            }
            else
            {
                lookRot = Quaternion.LookRotation(railSystem.GetFloorWaypoint(1) - transform.position, Vector3.up);
            }
        }


        if (limitRotationToY)
        {
            lookRot.eulerAngles = new Vector3(0,lookRot.eulerAngles.y,0);
        }


        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeedMultiplier * Time.deltaTime);
    }
}
