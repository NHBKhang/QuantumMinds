using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MSBarricade : MonoBehaviour
{
    [Header("Basic Settings:"), Tooltip("a.	The key used to initiate the vault movement. The rail-systems should have the same key marked as an unlatch key. ")]
    public KeyCode interactionKey = KeyCode.R;
    [Tooltip("The travel distance of the vault. b.	When Edit Mode is enabled, this parameter will move the rail-systems out and in accordingly.")]
    public float barricadeWidth = 1.6f;
    [Tooltip("The animation trigger of the vault action.")]
    public string animationTrigger = "barricade";
    [Range(-1,1),Tooltip("This value is sent to the animator to simulate a height change by blending three vault animations.")]
    public float animationHeightOffset = 0.35f;
    [Tooltip("The overall movement speed of the vault. Warning: setting this parameter to extreme values will cause snapping.")]
    public float transitionSpeed = 2;
    [Tooltip("A movement delay that occurs at the start of the vault. This is used for animation build-ups.")]
    public float movementDelay = 0.1f;
    [Tooltip("Should a ground level check be preformed on the other side of the barricade?")]
    public bool respectGround = true;
    [Tooltip("The maximum height distance the player can move without failing the blink.")]
    public float maxHeightChange = 2;
    [Tooltip("The distance between the character rep and projected player in which the player will exit the blink state. ")]
    public float achievementDistance = 0.4f;
    [Tooltip("The amount of time in seconds spent in the recovery state after the vault.")]
    public float recoveryTime = 0;
    [Tooltip("The time in seconds it takes for the barricade to reset after the vault. This is used to control rapid vaulting."),Range(0,20)]
    public float resetTime = 1.3f;
    private float currentResetTime = 0;
    [Tooltip("If any of these keys are pressed along with the interaction key, the vault will not occur. ")]
    public List<KeyCode> keyBlackList = new List<KeyCode>();

    [Header("References:"),Tooltip("Reference to the rail-systems of the barricade. Disabled rail-systems must be removed from this list.")]
    public List<GameObject> railSystems = new List<GameObject>();
    [Tooltip("Reference to the rail-system caps of the barricade. Disabled rail-systems must be removed from this list.")]
    public List<MultiStateMovementRailCap> railSystemCaps = new List<MultiStateMovementRailCap>();
    [Tooltip("Reference to the character controller. This will allow the player to vault the barricade immediately after unlatching from the rail-system, without the need for two individual keystrokes. ")]
    public MultistateCharacterController characterController;

    [Header("Enable Edit Mode:"),Tooltip("When this is enabled, the barricade width parameter will move the rail-systems out or in accordingly. ")]
    public bool EditMode = true;

    private bool called = false;

    //Reduces cooldown
    private void Update()
    {
        if (currentResetTime > 0)
        {
            currentResetTime -= Time.deltaTime;
        }
        else if (called)
        {
            called = false;
            SetRailSystemActivation(true);
        }
    }
    //Vaults remotely 
    public void RemoteActivate(string keyCheck = "")
    {
        if (characterController == null) return;
        if (keyCheck != "" && interactionKey.ToString() != keyCheck) return;

        if (currentResetTime > 0)
        {
            return;
        }
        if (characterController.settings.movementStateSettings.movementState == MovementState.grounded || characterController.settings.movementStateSettings.movementState == MovementState.undefined)
        {
            called = true;
            SetRailSystemActivation(false);
            characterController.RequestBlink(transitionSpeed, Vector3.forward * barricadeWidth, MultistateCharacterController.BlinkTravelMethod.relativeObjectBased, false, transform, respectGround, animationTrigger, true, movementDelay, recoveryTime, achievementDistance, maxHeightChange, 0.5f, animationHeightOffset);
            currentResetTime = resetTime;
        }
    }
    //Vaults within the trigger bounds
    private void OnTriggerStay(Collider other)
    {
        if (currentResetTime > 0)
        {
            return;
        }

        if (!characterController) characterController = other.GetComponent<MultistateCharacterController>();
        if (!characterController) return;

        if (characterController.settings.railSystemSettings.currentRailInteractionTime < 0) return;
        foreach (MultiStateMovementRailCap railCap in railSystemCaps)
        {
            if (railCap.remoteAccess.called) return;
        }

        foreach (KeyCode key in keyBlackList)
        {
            if (Input.GetKey(key))
            {
                return;
            }
        }

        if (Input.GetKey(interactionKey))
        {
            if (characterController.settings.movementStateSettings.movementState == MovementState.grounded || characterController.settings.movementStateSettings.movementState == MovementState.undefined)
            {
                called = true;
                SetRailSystemActivation(false);
                characterController.RequestBlink(transitionSpeed, Vector3.forward * barricadeWidth, MultistateCharacterController.BlinkTravelMethod.relativeObjectBased, false, transform, respectGround, animationTrigger, true, movementDelay, recoveryTime, achievementDistance, maxHeightChange, 0.5f, animationHeightOffset);
                currentResetTime = resetTime;
            }
        }
    }

    //Enables and disables the rail-systems
    void SetRailSystemActivation(bool isEnabled)
    {
        foreach(GameObject go in railSystems)
        {
            go.SetActive(isEnabled);
        }
    }


    //Draws lines representing the width
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!EditMode) return;
        Debug.DrawLine(transform.position + transform.forward * (barricadeWidth/2), transform.position + transform.forward * (barricadeWidth/2) + transform.up * 3, Color.yellow);
        Debug.DrawLine(transform.position + transform.forward * -(barricadeWidth / 2), transform.position + transform.forward * -(barricadeWidth / 2) + transform.up * 3, Color.yellow);

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, (barricadeWidth-0.6f));
        if (railSystems.Count > 0)
        {
            railSystems[0].transform.localPosition = new Vector3(railSystems[0].transform.localPosition.x, railSystems[0].transform.localPosition.y, -0.66f - (0.5f * (barricadeWidth -1.6f)));
        }
        if (railSystems.Count > 1)
        {
            railSystems[1].transform.localPosition = new Vector3(railSystems[1].transform.localPosition.x, railSystems[1].transform.localPosition.y, 0.66f + (0.5f * (barricadeWidth - 1.6f)));
        }
    }
#endif
}
