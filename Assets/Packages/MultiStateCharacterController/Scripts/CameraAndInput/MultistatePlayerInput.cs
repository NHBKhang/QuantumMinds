using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MultistatePlayerInput : MonoBehaviour
{

    //Determines how camera and player rotation is handled
    public enum ControlScheme
    {
        classic,
        semiModern,
        modernUnlocked,
        modernLocked
    }
    [Header("Control Scheme:"), Tooltip("The control scheme determines how camera and player rotation is handled. \n \n " +
    "Classic: This resembles typical WASD, MMO style controls. \n \n" +
    "Semi-Modern: This is an extension of the classic control. See tooltip for allowClassicSemiModernSwitching below for more information. \n \n" +
    "Modern-Unlocked: The player will rotate towards the camera's direction when moving. This can either resemble modern third-person RPG's or classic third-person platformers. See isModernUnlockedTypeA Tooltip for more information. \n \n +" +
    "Modern Locked: The player will always face towards the camera regardless of movement.")]
    public ControlScheme controlScheme = ControlScheme.classic;
    [Tooltip("When the control scheme is set to classic and this value is true, left clicking will rotate the camera around the player, right clicking will align the player with the camera, and holding the left and right mouse buttons will move the player forward in the camera's direction.")]
    public bool allowClassicSemiModernSwitching = true;
    [Tooltip("When the control scheme is set to modern-unlocked and this value is true, the player's movement will resemble a classic third-person platformer. When this value is false, the player's movement will resemble a more modern RPG.")]
    public bool isModernUnlockedTypeA = true;
    [Tooltip("The MultistateCharacterController script found on the projected player gameobject.")]
    public MultistateCharacterController characterController;

    [Header("Required Transforms:"), Tooltip("The transform of the character rep")]
    public Transform characterTransform;
    [Tooltip("The transform of the gameobject that handles camera rotation on the X axis")]
    public Transform cameraXAxisGimbal;
    [Tooltip("The transform of the gameobject that holds the actual camera gameobject's ideal position")]
    public Transform cameraHolderTransform;
    [Tooltip("The transform of the actual camera gameobject, the parent of the main camera gameobject")]
    public Transform actualCameraTransform;

    [Header("Basic Settings:"), Tooltip("The speed at which the camera moves towards the player")]
    public float cameraMovementSpeed = 0.1f;
    [Tooltip("The effect that changes the player's character rep adjustment speed will have on the camera")]
    public float characterRepAdjustmentMultiplier = 1;
    private float currentCameraMovementSpeed = 0;
    //The speed at which the camera rig adjusts between the base movement speed and the adjusted movement speed
    private float cameraAdjustmentSpeed = 0.1f;
    [Tooltip("The speed at which key input rotates the camera")]
    public float keySensitivity = 40;
    [Tooltip("The speed at which mouse input rotates the camera")]
    public float mouseSensitivity = 1;
    [Range(0, 90), Tooltip("The maximum x value camera rotation")]
    public float xRotationMax = 70;
    [Range(0, 90), Tooltip("The minimum x value camera rotation")]
    public float xRotationMin = 70;
    [Tooltip("The camera's look rotation offset")]
    public Vector3 cameraPlayerTargetOffset = new Vector3(0, 1.75f, 0);
    [Tooltip("The amount the camera moves back on the z axis while moving backwards.")]
    public float reverseCameraOffsetMultiplier = 1;

    [Header("Camera Collision:"), Tooltip("The CameraCollision script located on the Main Camera gameobject")]
    public CameraCollision cameraCollision;

    [Header("Zoom:"), Tooltip("The camera's default zoom")]
    public float zoom = 0.5f;
    [Tooltip("The rate at which scrolling affects camera zoom")]
    public float scrollMultiplier = 0.5f;
    [Tooltip("The camera's minimum zoom")]
    public float cameraMinimumDistance = 0.1f;

    //Primary movement driving value sent to the MultiStateCharacterController
    private AdvancedControlValue acv = new AdvancedControlValue();
    //SmoothDamp reference
    private Vector3 referenceVelocity;

    //Transform of attached dolly
    [HideInInspector]
    public Transform attachedDolly;
    [Header("Rail System Settings:"), Tooltip("The camera's rotation speed when attached to a rail system dolly")]
    public float cameraDollyRotationSpeed = 5;
    [Tooltip("The camera's movement speed when attached to a rail system dolly")]
    public float cameraDollyMovementSpeed = 1;
    [Tooltip("The speed at which the actual camera gameobject moves towards its calculated position. This is primarily used to smooth transitions in and out of rail systems.")]
    public float actualCameraTransitionSpeed = 30;
    [Tooltip("The speed at which the current actual camera transition speed adjusts towards the actual camera transition speed")]
    public float actualCameraAdjustmentSpeed = 0.5f;
    //The current speed at which the actual camera gameobject moves towards its calculated position
    [HideInInspector]
    public float currentCameraTransitionSpeed = 50;
    [HideInInspector]
    public List<KeyCode> unlatchKeyset = new List<KeyCode>();
    private Transform[] railSystemBrackets = new Transform[0];

    [Header("Grounded Substates:"), Tooltip("Is the user required to hold down the key to stay in the grounded substate?")]
    public bool requireSubstateKeyHold = true;
    public bool enableKeyBasedSwitching = true;
    [Tooltip("Key used to enable the crouch grounded substate")]
    public KeyCode crouchToggle = KeyCode.LeftControl;
    [Tooltip("Key used to enable the sprint grounded substate")]
    public KeyCode sprintToggle = KeyCode.LeftShift;
    [Tooltip("Key used to enable the walk grounded substate")]
    public KeyCode walkToggle = KeyCode.Tab;

    [Header("Remote Rail System Sampling:"), Tooltip("Check for rail-system remote access?")]
    public bool enableRemoteRailSystemSampling = true;
    [Tooltip("Obstructions and triggers involved in remote access.")]
    public LayerMask remoteSamplingLayerMask;
    [Tooltip("Remote access query delay")]
    public float remoteSamplingRequestDelay = 0.35f;
    private float currentRemoteSamplingDelay = 0;

    //Lunge
    [Serializable]
    public enum LungeAdvancedDirection
    {
        relativeStatic,
        flatVector,
        freeVector
    }

    [Serializable]
    public class LungeKeybind
    {
        [Tooltip("Name of the lunge action")]
        public string name;
        [Tooltip("(Optional) key activation")]
        public KeyCode key;
        [Tooltip("Is the lunge action enabled?")]
        public bool enabled;
        [Tooltip("How should the lunge action's direction be calculated?")]
        public LungeAdvancedDirection lungeAdvancedDirection;
        [Tooltip("Cooldown of the lunge action.")]
        public float cooldown;
        [HideInInspector]
        public float currentCooldown;
        public LungeAction lungeAction;
    }

    [Header("Lunge Keybinds:")]
    public List<LungeKeybind> lungeKeybinds = new List<LungeKeybind>();



    //Sets the default variables 
    private void Start()
    {
        currentCameraTransitionSpeed = actualCameraTransitionSpeed;
        currentCameraMovementSpeed = cameraMovementSpeed;
        acv.cameraRigY = transform;
        acv.cameraRigX = cameraXAxisGimbal;
    }
    // Update is called once per frame
    //FixedUpdate handles camera movement while Update handles input
    void FixedUpdate()
    {
        //Moves camera and sends ACV if not attached to a rail system dolly
        if (attachedDolly == null)
        {
            //Updates Camera rotation and sets ACV input based on the scheme
            UpdateCameraRotationScheme();

            CalculateRailSystemDirection();
            //Sends the updated ACV to the MultistateCharacterController
            SendACVToController();
        }
        else
        {
            //Rotates the camera to look at the player
            LookAtPlayer();
        }
        //Updates the Camera rig's position
        UpdateCameraRigPosition(characterTransform.position);
        //Move the actual camera gameobject to either the dolly's position or the camera rig's placeholder gameobject's position
        CalculateActualCameraTransform();
    }
    //FixedUpdate handles camera movement while Update handles input
    private void Update()
    {
        if (attachedDolly == null)
        {
            //Determines if the movement scheme needs to be switched and sets mouse lock
            EvaluateMovementScheme();
            //Updates camera zoom
            UpdateCameraZoom();
        }

        //Gets Vertical Input
        SetVerticalInput();
        //Gets Horizontal Input
        SetHorizontalInput();
        //Gets SidestepInput
        SetSidestepInput();
        //Checks if the user is trying to jump
        CheckJump();
        //Checks if the grounded substate needs to be changed
        UpdateSubstates();
        //Checks for rail-system unlatch keys
        CheckRailPrematureExit();
        //Monitors for remote access 
        SampleRailSystems();

        //Checks for lunge actions
        CheckLungeKeybinds();
    }

    //Monitors for remote access 
    void SampleRailSystems()
    {
        if (!enableRemoteRailSystemSampling) return;

        if (currentRemoteSamplingDelay > 0)
        {
            currentRemoteSamplingDelay = Mathf.Clamp(currentRemoteSamplingDelay - Time.deltaTime, 0, remoteSamplingRequestDelay);
        }
        if (currentRemoteSamplingDelay != 0) return;
        if (characterController.settings.movementStateSettings.movementState != MovementState.grounded) return;
        RaycastHit raycastHit;
        if(Physics.Raycast(actualCameraTransform.position, actualCameraTransform.forward, out raycastHit, 100f, remoteSamplingLayerMask))
        {
            MultiStateMovementRailCap railCap = raycastHit.transform.gameObject.GetComponent<MultiStateMovementRailCap>();
            if (!railCap) return;
            if (!railCap.remoteAccess.allowRemoteAccess) return;
            if (Input.GetKey(railCap.interactionKey))
            {
                Vector3 pos1 = characterTransform.position;
                Vector3 pos2 = raycastHit.point;
                pos1.y = 0;
                pos2.y = 0;
                float distance = Vector3.Distance(pos1, pos2);
                if (distance < railCap.remoteAccess.remoteAccessMinDistance) return;
                if (distance > railCap.remoteAccess.remoteAccessMaxDistance) return;
                AttemptRemoteRailSystemAccess(railCap, raycastHit.point, distance);
            }
        }
    }

    //Attempts to access a rail-system remotely 
    void AttemptRemoteRailSystemAccess(MultiStateMovementRailCap railcap, Vector3 accessPoint, float totalDistance)
    {
        currentRemoteSamplingDelay = remoteSamplingRequestDelay;
       characterController.RequestBlink(railcap.remoteAccess.speedFactor/ totalDistance, accessPoint - characterController.transform.position, MultistateCharacterController.BlinkTravelMethod.worldBased, true, true, railcap.remoteAccess.animationTrigger, true, 0, 0, 0.4f, 0.3f, 3f);
    }

    //Calculates rail-system direction
    void CalculateRailSystemDirection()
    {
        if (railSystemBrackets.Length == 0) return;
        if (controlScheme == ControlScheme.classic)
        {
            acv.calculatedRailDirection = 1;
            return;
        }

        float angleOne = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(transform.position - railSystemBrackets[0].position, Vector3.up));
        float angleTwo = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(transform.position - railSystemBrackets[1].position, Vector3.up));
        if (angleOne >= angleTwo)
            acv.calculatedRailDirection = -1;
        if (angleOne < angleTwo)
            acv.calculatedRailDirection = 1;
    }

    //Checks for lunge related key input
    void CheckLungeKeybinds()
    {
        for (int i= 0; i<  lungeKeybinds.Count; i++)
        {
            if (lungeKeybinds[i].currentCooldown != 0)
            {
                lungeKeybinds[i].currentCooldown = Mathf.Clamp(lungeKeybinds[i].currentCooldown+ Time.deltaTime, - lungeKeybinds[i].cooldown, 0);
                continue;
            }

            if (!lungeKeybinds[i].enabled) continue;

            if (Input.GetKey(lungeKeybinds[i].key))
            {
                RequestLunge(lungeKeybinds[i]);
            }
        }
    }

    //Calculates lunge direction and sends the request to the controller
    void RequestLunge(LungeKeybind lungeKeybind)
    {
        if (!lungeKeybind.enabled) return;
        lungeKeybind.currentCooldown = -lungeKeybind.cooldown;
        if (lungeKeybind.lungeAdvancedDirection == LungeAdvancedDirection.flatVector)
        {
            lungeKeybind.lungeAction.direction = acv.flatVector;
        }
        if (lungeKeybind.lungeAdvancedDirection == LungeAdvancedDirection.freeVector)
        {
            lungeKeybind.lungeAction.direction = acv.freeVector;
        }
        if (lungeKeybind.lungeAdvancedDirection == LungeAdvancedDirection.relativeStatic)
        {
            lungeKeybind.lungeAction.direction = transform.TransformDirection(lungeKeybind.lungeAction.simpleDirection);
        }
        characterController.RequestLunge(lungeKeybind.lungeAction);
    }

    //Checks for rail-system unlatch keys
    void CheckRailPrematureExit()
    {
        if (unlatchKeyset.Count < 1) return;
        foreach(KeyCode kC in unlatchKeyset)
        {
            if (Input.GetKey(kC))
            {
                characterController.ExitRailSystem(kC.ToString());
            }
        }
    }

    //Handles camera zoom
    void UpdateCameraZoom()
    {
        zoom = Mathf.Clamp(zoom + Input.GetAxis("Mouse ScrollWheel") * scrollMultiplier, 0, 1 - cameraMinimumDistance);
    }

    //Rotates the camera to look at the player
    void LookAtPlayer()
    {
        Vector3 pos1 = new Vector3(characterTransform.position.x, characterTransform.position.y, characterTransform.position.z);
        Vector3 pos2 = new Vector3(actualCameraTransform.position.x, actualCameraTransform.position.y, actualCameraTransform.position.z);
        Vector3 revOffset = characterTransform.InverseTransformDirection(acv.flatVector);
        Quaternion targetRotation = Quaternion.LookRotation(pos1 + characterTransform.TransformDirection(cameraPlayerTargetOffset) - pos2, Vector3.up);
        actualCameraTransform.rotation = Quaternion.Slerp(actualCameraTransform.rotation, targetRotation, cameraDollyRotationSpeed * Time.smoothDeltaTime);
    }

    //Checks for grounded substate keys and requests that the MultistateCharacterController switches substates if required
    void UpdateSubstates()
    {
        if (!enableKeyBasedSwitching) return;

        //If key is not required to be held
        if (Input.GetKeyDown(crouchToggle))
        {
            characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.crouching);
        }
        if (Input.GetKeyDown(sprintToggle))
        {
            characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.sprinting);
        }
        if (Input.GetKeyDown(walkToggle))
        {
            characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.walking);
        }
        //If key is required to be held
        if (requireSubstateKeyHold)
        {
            if (Input.GetKeyUp(crouchToggle))
            {
                characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.defaultSubstate);
            }
            if (Input.GetKeyUp(sprintToggle))
            {
                characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.defaultSubstate);
            }
            if (Input.GetKeyUp(walkToggle))
            {
                characterController.ToggleGroundedMovementSubstate(GroundedMovementSubState.defaultSubstate);
            }
        }
    }

    //Moves the Actual Camera Gameobject
    void CalculateActualCameraTransform()
    {
        //Gets the rail system dolly if there is one
        attachedDolly = characterController.GetCameraDolly();
        //Moves the actual camera gameobject to the camera placeholder gameobject's position and rotation and changes the actual camera gameobject's parent
        if (attachedDolly == null)
        {
            currentCameraTransitionSpeed = Mathf.Lerp(currentCameraTransitionSpeed, actualCameraTransitionSpeed, actualCameraAdjustmentSpeed * Time.deltaTime);
            actualCameraTransform.parent = cameraXAxisGimbal;

            Vector3 revOffset = characterTransform.InverseTransformDirection(acv.flatVector);
            revOffset.x = 0;
            if (revOffset.z < 0)
            {
                revOffset.z *= reverseCameraOffsetMultiplier;
            }
            else
            {
                revOffset.z = 0;
            }
            revOffset = characterTransform.TransformDirection(revOffset);

            Vector3 calcVector = Vector3.Lerp(cameraHolderTransform.position, characterTransform.position + cameraPlayerTargetOffset + revOffset, Mathf.Clamp(zoom, 0, 1 - cameraMinimumDistance));

            actualCameraTransform.position = Vector3.Lerp(actualCameraTransform.position, calcVector, currentCameraTransitionSpeed * Time.smoothDeltaTime);
            actualCameraTransform.rotation = Quaternion.Slerp(actualCameraTransform.rotation, cameraHolderTransform.rotation, cameraDollyRotationSpeed * Time.smoothDeltaTime);
        }
        else //Moves the actual camera gameobject to the dolly's position, rotates the camera rig to match the rail system's output rotation, and changes the actual camera gameobject's parent
        {
            currentCameraTransitionSpeed = cameraDollyMovementSpeed;
            actualCameraTransform.parent = null;
            SetMouseLock(false);

            Vector3 targetPosition = attachedDolly.position;
            actualCameraTransform.position = Vector3.Slerp(actualCameraTransform.position, targetPosition, cameraDollyMovementSpeed * Time.smoothDeltaTime);

            Quaternion yActCamRot = actualCameraTransform.rotation;
            yActCamRot.eulerAngles = new Vector3(0, yActCamRot.eulerAngles.y, 0);
            transform.rotation = yActCamRot;
        }
    }

    //Checks if the user is trying to jump
    void CheckJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            characterController.AttemptJump();
        }
    }
    //Records the vertical input
    void SetVerticalInput()
    {
        acv.verticalInput = Input.GetAxis("Vertical");
    }
    //Records the horizontal input
    void SetHorizontalInput()
    {
        acv.horizontalInput = Input.GetAxis("Horizontal");
    }
    //Records the sidestep input
    void SetSidestepInput()
    {
        acv.sideStepInput = Input.GetAxis("Sidestep");
    }
    //Sends the ACV to the MSCC, toggles camera collision based on the rail system, and sets a new zoom value based on the rail system
    void SendACVToController()
    {
        acv.playerInput = this;

        //Limits directional vector magnitude
        acv.flatVector = Vector3.ClampMagnitude(acv.flatVector, 1);
        acv.freeVector = Vector3.ClampMagnitude(acv.freeVector, 1);

        //If the camera is not attached to a dolly, send the acv, and checks for a dolly
        if (!attachedDolly)
        {
            //Sends 
            attachedDolly = characterController.UpdateACV(acv, out unlatchKeyset, out railSystemBrackets);
            if (attachedDolly) // do
            {
                //Disables camera collision based on the rail system
                SetCameraCollision(!attachedDolly.GetComponent<OnRailDolly>().railSystem.secondarySettings.disableCameraCollision);

                //Sets the camera's output zoom after the rail system
                float newZoom = attachedDolly.GetComponent<OnRailDolly>().railSystem.secondarySettings.setCameraZoom;
                if (newZoom != -1)
                {
                    SetZoom(newZoom);
                }
            }
            else
            {
                //Attempts to enable camera collision
                SetCameraCollision(true);
            }
        }
        else //Updates the ACV
        {
            attachedDolly = characterController.UpdateACV(acv, out unlatchKeyset, out railSystemBrackets);
        }
    }

    //Checks if the movement scheme needs to be changed and sets mouse lock
    private void EvaluateMovementScheme()
    {
        //Returns if the movement scheme does not need to be changed
        if (!allowClassicSemiModernSwitching) return;
        //Sets Mouse lock
        if (controlScheme == ControlScheme.modernLocked) { SetMouseLock(true); return; }
        if (controlScheme == ControlScheme.modernUnlocked) { SetMouseLock(true); return; }

        //Switches between classic and semiModern movement schemes if allowed, based on mouse input
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            SetControlScheme(ControlScheme.semiModern);
            return;
        }
        SetControlScheme(ControlScheme.classic);
    }

    //Sets a new control scheme and locks or unlocks the mouse as needed
    private void SetControlScheme(ControlScheme newControlScheme)
    {
        if (newControlScheme == ControlScheme.classic)
        {
            SetMouseLock(false);
        }
        else
        {
            SetMouseLock(true);
        }
        controlScheme = newControlScheme;
    }

    //Updates the entire camera rig's position
    private void UpdateCameraRigPosition(Vector3 target)
    {
        //Calculates the camera rig's speed based on the MSCC's character rep adjustment speed
        float adjPercent = characterController.GetCurrentCharacterRepAdjSpeedPercent();
        float calculatedSpeed = (cameraMovementSpeed + ((1 / adjPercent) * characterRepAdjustmentMultiplier) - characterRepAdjustmentMultiplier);
        currentCameraMovementSpeed = Mathf.Lerp(currentCameraMovementSpeed, calculatedSpeed, cameraAdjustmentSpeed);
        //Moves the camera rig's position
        transform.position = Vector3.SmoothDamp(transform.position, target, ref referenceVelocity, currentCameraMovementSpeed * Time.smoothDeltaTime);
    }

    //Updates Camera rotation and sets ACV input based on the scheme
    private void UpdateCameraRotationScheme()
    {
        if (controlScheme == ControlScheme.classic)
        {
            UpdateClassic();
            return;
        }
        if (controlScheme == ControlScheme.semiModern)
        {
            UpdateSemiModern();
            return;
        }
        if (controlScheme == ControlScheme.modernUnlocked)
        {
            UpdateModernUnlocked(!isModernUnlockedTypeA);
            return;
        }
        if (controlScheme == ControlScheme.modernLocked)
        {
            UpdateModernLocked();
            return;
        }
    }

    //Sets ACV values and rotates the camera based on the classic movement scheme
    void UpdateClassic()
    {
        acv.isClassic = true;
        acv.isUnlockedModern = false;
        acv.isModernMixed = false;
        transform.Rotate(new Vector3(0, acv.horizontalInput * keySensitivity * Time.smoothDeltaTime, 0));

        acv.lockRotation = true;
        acv.rotateWithMovement = false;
        acv.allowRotationDifference = true;
        acv.flatVector = characterTransform.TransformDirection(new Vector3(acv.sideStepInput, 0, acv.verticalInput));
        acv.freeVector = characterTransform.TransformDirection(new Vector3(acv.sideStepInput, Input.GetAxis("UpDown"), acv.verticalInput));
    }

    //Sets ACV values and rotates the camera based on the semi-modern movement scheme
    void UpdateSemiModern()
    {
        UpdateMouseCameraMovement();
        acv.isClassic = false;
        acv.isUnlockedModern = false;
        acv.isModernMixed = false;
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            acv.lockRotation = false;
            acv.rotateWithMovement = false;
            acv.allowRotationDifference = true;
            acv.rotation = acv.horizontalInput * keySensitivity * Time.smoothDeltaTime;

            acv.flatVector = characterTransform.TransformDirection(new Vector3(acv.sideStepInput, 0, acv.verticalInput));
            acv.freeVector = characterTransform.TransformDirection(new Vector3(acv.sideStepInput, Input.GetAxis("UpDown"), acv.verticalInput));
        }
        if (Input.GetMouseButton(1))
        {

            acv.lockRotation = true;
            acv.rotateWithMovement = false;
            acv.allowRotationDifference = false;
            if (Input.GetMouseButton(0))
            {
                acv.flatVector = transform.TransformDirection(new Vector3(acv.horizontalInput, 0, 1));
                acv.freeVector = cameraXAxisGimbal.TransformDirection(new Vector3(acv.horizontalInput + acv.sideStepInput, Input.GetAxis("UpDown"), 1));
            }
            else
            {
                acv.flatVector = transform.TransformDirection(new Vector3(acv.horizontalInput, 0, acv.verticalInput));
                acv.freeVector = cameraXAxisGimbal.TransformDirection(new Vector3(acv.horizontalInput + acv.sideStepInput, Input.GetAxis("UpDown"), acv.verticalInput));
            }
        }
    }

    //Sets ACV values and rotates the camera based on the modern-unlocked movement scheme
    void UpdateModernUnlocked(bool setIdentifier)
    {
        UpdateMouseCameraMovement();
        acv.isClassic = false;
        acv.isUnlockedModern = true;
        acv.isModernMixed = setIdentifier;
        acv.lockRotation = false;
        acv.rotateWithMovement = true;
        acv.allowRotationDifference = false;
        acv.flatVector = transform.TransformDirection(new Vector3(acv.horizontalInput, 0, acv.verticalInput));
        acv.freeVector = cameraXAxisGimbal.TransformDirection(new Vector3(acv.horizontalInput, Input.GetAxis("UpDown"), acv.verticalInput));
    }

    //Sets ACV values and rotates the camera based on the modern locked movement scheme
    void UpdateModernLocked()
    {
        UpdateMouseCameraMovement();
        acv.isClassic = false;
        acv.isUnlockedModern = false;
        acv.isModernMixed = false;
        acv.lockRotation = true;
        acv.rotateWithMovement = false;
        acv.allowRotationDifference = false;
        acv.flatVector = transform.TransformDirection(new Vector3(acv.horizontalInput, 0, acv.verticalInput));
        acv.freeVector = cameraXAxisGimbal.TransformDirection(new Vector3(acv.horizontalInput, Input.GetAxis("UpDown"), acv.verticalInput));
    }

    //Rotates and limits the camera based on mouse movement input
    void UpdateMouseCameraMovement()
    {
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0));

        Quaternion targetCamRotation = cameraXAxisGimbal.localRotation;
        targetCamRotation.eulerAngles = new Vector3(targetCamRotation.eulerAngles.x - Input.GetAxis("Mouse Y") * mouseSensitivity, 0, 0);
        float angle = Quaternion.Angle(targetCamRotation, Quaternion.identity);

        if (targetCamRotation.eulerAngles.x < 180 && angle > xRotationMax)
        {
            Quaternion cappedRot = Quaternion.identity;
            cappedRot.eulerAngles = new Vector3(xRotationMax, 0, 0);
            cameraXAxisGimbal.localRotation = cappedRot;
            return;
        }
        if (targetCamRotation.eulerAngles.x > 180 && angle > xRotationMin)
        {
            Quaternion cappedRot = Quaternion.identity;
            cappedRot.eulerAngles = new Vector3(360 - xRotationMin, 0, 0);
            cameraXAxisGimbal.localRotation = cappedRot;
            return;
        }

        cameraXAxisGimbal.localRotation = targetCamRotation;
    }
    //Calls a lunge action
    public void PreformLungeAction(int index)
    {
        if (index >= lungeKeybinds.Count || index < 0)
        {
            Debug.LogWarning("Lunge index is out of range!");
            return;
        }
        RequestLunge(lungeKeybinds[index]);
    }
    //Calls a lunge action
    public void PreformLungeAction(string testedName)
    {
        int index = -1;

        for (int i = 0; i < lungeKeybinds.Count; i++)
        {
            if (lungeKeybinds[i].name == testedName)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            Debug.LogWarning("Lunge action was not found!");
            return;
        }

        RequestLunge(lungeKeybinds[index]);
    }
    //Enables and Disables lunge actions
    public void ToggleLungeAction(int index, bool isEnabled)
    {
        if (index >= lungeKeybinds.Count || index < 0)
        {
            Debug.LogWarning("Lunge index is out of range!");
            return;
        }

        lungeKeybinds[index].enabled = isEnabled;
    }
    //Enables and Disables lunge actions
    public void ToggleLungeAction(string testedName, bool isEnabled)
    {
        int index = -1;

        for (int i = 0; i < lungeKeybinds.Count; i++)
        {
            if (lungeKeybinds[i].name == testedName)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            Debug.LogWarning("Lunge action was not found!");
            return;
        }

        lungeKeybinds[index].enabled = isEnabled;
    }

    //Set mouse screen lock
    public void SetMouseLock(bool isLocked)
    {
        if (isLocked && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            return;
        }
        else if (!isLocked && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    //Enables and Disables grounded substate key based switching
    public void SetKeyBasedSwitching (bool isEnabled)
    {
        enableKeyBasedSwitching = isEnabled;
    }

    //Sets zoom to a specific value
    void SetZoom(float Value)
    {
        zoom = Value;
    }

    //Sets Camera Collision
    void SetCameraCollision(bool isEnabled)
    {
        if (!cameraCollision) return;
        cameraCollision.isEnabled = isEnabled;
    }
}
