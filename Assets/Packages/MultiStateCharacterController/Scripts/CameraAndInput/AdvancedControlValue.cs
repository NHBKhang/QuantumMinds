using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedControlValue
{
    public MultistatePlayerInput playerInput;
    //This is the requested movement direction excluding Y transform movement
    public Vector3 flatVector = Vector3.zero;
    //This is the requested movement direction including Y transform movement
    public Vector3 freeVector = Vector3.zero;
    //Is the rotation locked to the camera direction?
    public bool lockRotation = false;
    //Is the players rotational speed scaled to the players transitional speed?
    public bool rotateWithMovement = false;
    //Can the camera's original rotation be offset without rotating the player?
    public bool allowRotationDifference = false;
    //Requested rotation value
    public float rotation;
    //The part of the camera rig that handles Y value rotation
    public Transform cameraRigY;
    //The part of the camera rig that handles X value rotation
    public Transform cameraRigX;
    //Vertical input used for animation correction
    public float verticalInput = 0;
    //Horizontal input used for animation correction
    public float horizontalInput = 0;
    //SideStep input used for animation correction
    public float sideStepInput = 0;
    //Is the MultiStatePlayerInput's control scheme unlocked modern?
    public bool isUnlockedModern = false;
    //Is the MultiStatePlayerInput's control scheme classic?
    public bool isClassic = false;
    //Is the MultiStatePlayerInput's control scheme semi modern?
    public bool isModernMixed = false;

    public int calculatedRailDirection = 0;
}