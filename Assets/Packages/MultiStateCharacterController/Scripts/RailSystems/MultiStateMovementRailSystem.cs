
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Universal settings apply to both primary and secondary rail systems
[Serializable]
public class RailSystemUniversalSettings
{
    [Header("Base Settings:"), Tooltip("This value defines the end of the rail system")]
    public float duration = 10;
    [Tooltip("Should the evaluated position along a rail system be determined by the rail system's total distance or its location among the waypoints?")]
    public bool calculateWaypointsByUniformDistance = true;
    [Tooltip("The first waypoint in the rail system. The other waypoints should be parented to this waypoint.")]
    public Transform StartingWaypoint;
    [HideInInspector]
    public List<Transform> waypoints = new List<Transform>();
    [Tooltip("Should the position along the rail system be rounded?")]
    public bool roundPosition = false;
    [Tooltip("What decimal place should the system be rounded to?")]
    public int roundTo = 1;
    [HideInInspector] //The last evaluated waypoint
    public int currentWaypoint = 0;

    [Header("Dolly Settings:"), Tooltip("These are the rail system dollies connected to this rail system.")]
    public List<OnRailDolly> onRailDollies = new List<OnRailDolly>();

    [Header("Events:"), Tooltip("Positional events occur based on the evaluated position, and they will only reset when the player disconnects and re-connects to the system.")]
    public List<RailSystemPositionalEvent> railSystemPositionalEvents;

    //MSCCRSUnit************************************************************************************************************************************
    [HideInInspector]
    public int definingType = 0;
}

//Primary settings only affect the player or will matter only when the player interacts with the system
[Serializable]
public class RailSystemPrimarySettings
{
    [Header("Position Settings:"), Tooltip("The speed of which the player traverses the rail system")]
    public float speed = 1;
    [Tooltip("Should the player move towards the evaluated position or be set at the evaluated position?")]
    public bool absolutePositionLock = false;

    [Header("Camera Settings:"), Tooltip("Should the camera be attached to a dolly or retain its normal functionality?")]
    public bool attachCameraToDolly = false;
    [Tooltip("The index of the camera dolly set in the onRailDollies list")]
    public int cameraDollyIndex = 0;

    //These settings are evaluated by the character controller and have no effect on dollies:
    public enum RotationControl
    {
        rotateTowardsNextWaypoint,
        rotateTowardsTarget,
        lockRotation,
        freeRotation
    }

    [Header("Player Interaction Settings:"), Tooltip("The speed that the character representation moves towards the projected player when interacting with this rail system.")]
    public float characterRepSpeed = 0.05f;
    [Tooltip("The Y value positional offset for the player when interacting with this system. There is no other offset applied, so it is likely that the default bind point will be near to the character's head.")]
    public float yValueOffset = 0;
    [Tooltip("The drag of the projected player when interacting with this rail system.")]
    public float onRailsDrag = 5;
    [Tooltip("The amount of time the player spends in the recovery state after this rail system.")]
    public float recovery = 0.1f;
    [Tooltip("Will key input advance the player along this rail system?")]
    public bool keyControlled = true;
    [Tooltip("What key axis input will move the player along the rail system?")]
    public string keyAxis = "Vertical";
    [Tooltip("Will the player advance along the rail system as time passes?")]
    public bool timeControlled = false;
    [Tooltip("Camera direction will affect rail-system direction. (This is not supported when using the classic scheme)")]
    public bool enabledCameraBasedDirection = false;
    [Tooltip("How should the player handle rotation? \n \n " +
        "Rotate towards next waypoint: the player will rotate towards the next waypoint in both directions. \n \n " +
        "Rotate towards target: the player will rotate towards a transform. This can be a fixed point, a rail system dolly, or point with unique movement functionality. \n \n " +
        "Lock Rotation: the player's rotation will be locked.")]
    public RotationControl rotationControl = RotationControl.freeRotation;
    [Tooltip("The transform of the rotation target if one is in use")]
    public Transform rotationTarget;
    [Tooltip("Should the player only rotate on the y axis or on all axes?")]
    public bool rotateOnYAxisOnly = true;
    [Tooltip("The speed that the player rotates towards the target rotation")]
    public float playerRotationSpeed = 30;
    [Tooltip("The animator trigger that is called when the player first interacts with the system")]
    public string animationTrigger = "onRailsTrigger";
    [Tooltip("A value that the rail position is multiplied by to determine the evaluated position. This affects the connected dollies as well.")]
    public float railAnimationPositionMultiplier = 1;
    [Tooltip("A value that the rail position is added by to determine the evaluated position. This affects the connected dollies as well.")]
    public float railAnimationPositionOffset = 0;
    [HideInInspector] //The original speed of the rail system, this is used so that the speed of the system can adjusted at any point in the system without affecting ablilty to reset
    public float originalSpeed = 0;
    [Tooltip("Key input that will unlatch the player.")]
    public List<KeyCode> unlatchKeyset = new List<KeyCode>();

    [Header("Events:"), Tooltip("An event that is called when the player first interacts with the rail system")]
    public UnityEvent OnRailSystemLatch;
    [Tooltip("An event that is called when the player stops interacting with the rail system")]
    public UnityEvent OnRailSystemUnlatch;

    [Tooltip("Should key unlatches also trigger normal unlatch events?")]
    public bool keyBasedNormalUnlatchEvent = true;

    [System.Serializable]
    public class OnRailSystemKeyUnlatch : UnityEvent<string> { };

    [Tooltip("An event that is called when the player stops interacting with the rail system due to a key press")]
    public OnRailSystemKeyUnlatch onRailSystemKeyUnlatch;

    [Header("IK Settings:"), Tooltip("How player feet IK is handled when the player is interacting with the rail system. \n \n" +
        "None: feet ik is disabled. \n \n " +
        "Project Down: The feet will move down towards the floor. This is the typical IK behavior. \n \n" +
        "Project Forward and Down: The feet are moved forward to an object then moved down towards the floor. This is useful for climbing type purposes. \n \n" +
        "Lock To Target: the feet will try to move towards the closest set IK target. This is not limited by target distance.")]
    public MultiStateSettings.IKSettings.FeetLocationMethod feetLocationMethod = MultiStateSettings.IKSettings.FeetLocationMethod.projectDown;
    [Tooltip("How player hand IK is handled when the player is interacting with the rail system. \n \n" +
        "None: hand ik is disabled. \n \n " +
        "Project Forward: The hands are moved forward to an object. \n \n" +
        "Lock To Target: the hands will try to move towards the closest set IK target. This is not limited by target distance.")]
    public MultiStateSettings.IKSettings.HandLocationMethod handLocationMethod;
    [Tooltip("The parent transform that contains the ik target set")]
    public Transform ikTargetParent;
}


//Positional events are triggered based on the position evaluated by the rail system
[Serializable]
public class RailSystemPositionalEvent
{
    [Tooltip("The position along the rail system that defines the event")]
    public float position = 0;

    public enum CallSettings
    {
        triggerBeforePosition,
        triggerAfterPosition,
        triggerAtPosition
    }

    [Tooltip("Defines when the event is called in relation to the event position variable")]
    public CallSettings callSetting = CallSettings.triggerAtPosition;
    [Tooltip("Positive and negative range from the event position variable. If this value is set to 0 it is likely that trigger at position events will not be called.")]
    public float triggerAtPositionTolerance = 0.5f;
    [Tooltip("Defines what happens when the position requirements are met")]
    public UnityEvent positionalEvent;

    [HideInInspector] //Has the event been called? This resets when the player unlatches from the system
    public bool hasCalled = false;
}

//Secondary systems are systems that dollies and not the player interact with. 
[Serializable]
public class RailSystemSecondarySettings
{
    [Header("Camera Settings:"), Tooltip("Disables camera collision while the camera is attached to a dolly")]
    public bool disableCameraCollision = false;
    [Tooltip("Sets the camera zoom to smooth transitions out of the rail system. This value does not affect the camera while it is attached to the rail system. If you want the camera to be closer to the player during the rail system evaluation, move the camera's dolly's rail system towards the player.")]
    public float setCameraZoom = -1;
}

public class MultiStateMovementRailSystem : MonoBehaviour
{
    [Header("Universal Settings:"), Tooltip("These settings apply to all rail systems.")]
    public RailSystemUniversalSettings universalSettings;
    [Header("Primary Rail System Settings:"), Tooltip("These settings only apply to the rail systems that the player directly interacts with")]
    public RailSystemPrimarySettings primarySettings;
    [Header("Secondary Rail System Settings:"), Tooltip("These settings only apply to rail systems that the player does not directly interact with")]
    public RailSystemSecondarySettings secondarySettings;

    private IRailSystemExtension[] railSystemExtensions;
    private MultistateCharacterController characterControllerRef;

    //Calls the OnRailSystemUnlatch event
    public void UnlatchFromRail(string key = "")
    {
        if (key != "")
        {
            primarySettings.onRailSystemKeyUnlatch.Invoke(key);
            if (characterControllerRef)
            {
                foreach (IRailSystemExtension systemExtension in railSystemExtensions)
                {
                    systemExtension.OnKeyUnlatch(characterControllerRef);
                }
            }
            if (!primarySettings.keyBasedNormalUnlatchEvent) return;
        }
        primarySettings.OnRailSystemUnlatch.Invoke();
        if (characterControllerRef)
        {
            foreach (IRailSystemExtension systemExtension in railSystemExtensions)
            {
                systemExtension.OnUnlatch(characterControllerRef);
            }
        }
    }

    //Changes the rail system speed, this can be done while evaluating the rail system
    public void SetSpeed(float speed)
    {
        primarySettings.speed = speed;
    }

    //Returns the set camera dolly if one is defined
    public Transform GetCameraDolly()
    {
        if (primarySettings.cameraDollyIndex > 0 && universalSettings.onRailDollies.Count > 0 && primarySettings.cameraDollyIndex < universalSettings.onRailDollies.Count)
        {
            return universalSettings.onRailDollies[primarySettings.cameraDollyIndex].transform;
        }
        return null;
    }

    //Returns a world space location based on a rail system position, updates dollies based on that position, and calls positional events if required 
    public Vector3 GetRailLocation(float position)
    {
        position = Mathf.Clamp(position, 0, universalSettings.duration);
        if (universalSettings.roundPosition) position = Mathf.Round(position* universalSettings.roundTo)/ universalSettings.roundTo;
        UpdateDollies(position);
        EvaluatePositionalEvents(position);
        if (!universalSettings.calculateWaypointsByUniformDistance)
        {
            return Get01RailLocation(position);
        }
        return GetUniformRailLocation(position);
    }

    //Resets positional events and speed
    void ResetSystem()
    {
        primarySettings.speed = primarySettings.originalSpeed;
        foreach (RailSystemPositionalEvent pe in universalSettings.railSystemPositionalEvents)
        {
            pe.hasCalled = false;
        }
    }

    //Checks if a positional event needs to be called
    void EvaluatePositionalEvents(float position)
    {
        foreach (RailSystemPositionalEvent pe in universalSettings.railSystemPositionalEvents)
        {
            //Continue if the event has already been called
            if (pe.hasCalled) continue;

            //Trigger after position
            if (pe.callSetting == RailSystemPositionalEvent.CallSettings.triggerAfterPosition)
            {
                if (position >= pe.position)
                {
                    pe.hasCalled = true;
                    pe.positionalEvent.Invoke();
                }
                continue;
            }

            //Trigger before position
            if (pe.callSetting == RailSystemPositionalEvent.CallSettings.triggerBeforePosition)
            {
                if (position <= pe.position)
                {
                    pe.hasCalled = true;
                    pe.positionalEvent.Invoke();
                }
                continue;
            }

            //Trigger at position
            if (pe.callSetting == RailSystemPositionalEvent.CallSettings.triggerAtPosition)
            {
                if (position >= pe.position-pe.triggerAtPositionTolerance && position <= pe.position+ pe.triggerAtPositionTolerance)
                {
                    pe.hasCalled = true;
                    pe.positionalEvent.Invoke();
                }
                continue;
            }
        }
    }
    
    //Resets the rail system and calls the OnRailSystemLatch event
    public void LatchToRail(float position, MultistateCharacterController characterController)
    {
        characterControllerRef = characterController;
        ResetSystem();
        foreach (OnRailDolly dolly in universalSettings.onRailDollies)
        {
            if (dolly.railSystem != this)
            {
                dolly.ResetPosition(position);
            }
            else
            {
                //This error prevents the application or the unity editor from crashing 
                //Please see MSCC documentation for more information
                Debug.LogError("Rail System Circular Dependancy! A dolly's rail system cannot be the original system!");
            }
        }
        primarySettings.OnRailSystemLatch.Invoke();
        if (characterControllerRef)
        {
            foreach (IRailSystemExtension systemExtension in railSystemExtensions)
            {
                systemExtension.OnLatch(characterControllerRef);
            }
        }
    }

    //Each dolly will evaluate the position relative to their rail system based on the position evaluated by this system
    void UpdateDollies(float position)
    {
        foreach (OnRailDolly dolly in universalSettings.onRailDollies)
        {
            if (dolly.railSystem != this)
            {
                dolly.EvaluatePosition(position);
            }
            else
            {
                //This error prevents the application or the unity editor from crashing 
                //Please see MSCC documentation for more information
                Debug.LogError("Rail System Circular Dependancy! A dolly's rail system cannot be the original system!");
            }
        }
    }

    //Rounds down the position to the closest waypoint
    public Vector3 GetFloorWaypoint(int offset)
    {
        if (universalSettings.currentWaypoint + offset < 0)
            return universalSettings.waypoints[universalSettings.currentWaypoint +offset+1].position;
        if (universalSettings.currentWaypoint + offset > universalSettings.waypoints.Count)
            return universalSettings.waypoints[universalSettings.currentWaypoint + offset-1].position;

        return universalSettings.waypoints[universalSettings.currentWaypoint + offset].position;
    }

    public void GetEndWaypoints(out Transform[] endWaypoints)
    {
        endWaypoints = new Transform[2];
        endWaypoints[0] = universalSettings.StartingWaypoint;
        endWaypoints[1] = universalSettings.waypoints[universalSettings.waypoints.Count-1];
    }

    //Returns a world space location based on rail system position, this method does not take distance between waypoints into account
    Vector3 Get01RailLocation(float position)
    {
        //Finds the closest waypoint
        float fullPercent = position / universalSettings.duration;
        float closestPointIndex = Mathf.Clamp(Mathf.Floor((universalSettings.waypoints.Count-1) * fullPercent),0, universalSettings.waypoints.Count - 1);
      
        if (universalSettings.waypoints.Count - 1 == closestPointIndex)
        {
            return universalSettings.waypoints[(int)closestPointIndex].position;
        }
        //Get the position between waypoints if required
        if (closestPointIndex != universalSettings.waypoints.Count - 1)
        {
            universalSettings.currentWaypoint = (int)closestPointIndex;
            float closestPointIndexPercent = ((closestPointIndex) / (universalSettings.waypoints.Count - 1));
            float nextClosestPointIndexPercent = ((closestPointIndex + 1) / (universalSettings.waypoints.Count - 1));
            float adjustedPercent = (position-(closestPointIndexPercent* universalSettings.duration))/((nextClosestPointIndexPercent* universalSettings.duration)- (closestPointIndexPercent * universalSettings.duration));
            Debug.Log(adjustedPercent + " : " + closestPointIndexPercent + " : " + nextClosestPointIndexPercent);
            return GetLocationBetweenLocations(universalSettings.waypoints[(int)closestPointIndex].position, universalSettings.waypoints[(int)closestPointIndex + 1].position, 1-adjustedPercent);
        }
        return universalSettings.waypoints[(int)closestPointIndex].position;

    }

    //Returns a world space location based on rail system position, this method does take distance between waypoints into account
    Vector3 GetUniformRailLocation(float position)
    {
        //Calculates distances
        float distanceDuration = 0;
        List<float> waypointDistances = new List<float>();
        waypointDistances.Add(0);
        for (int i = 1; i < universalSettings.waypoints.Count; i++)
        {
            distanceDuration += Vector3.Distance(universalSettings.waypoints[i-1].position, universalSettings.waypoints[i].position);
            waypointDistances.Add(distanceDuration);
        }
        float distancePosition = (position / universalSettings.duration) * distanceDuration;
        //Returns a world space position based on the distance between waypoints
        for (int i = 0; i < universalSettings.waypoints.Count - 1; i++)
        {
            if (distancePosition >= waypointDistances[i] && distancePosition <= waypointDistances[i+1])
            {
                universalSettings.currentWaypoint = i;
                float adjustedPercent = (distancePosition - waypointDistances[i]) / (waypointDistances[i+1] - waypointDistances[i]);
                return GetLocationBetweenLocations(universalSettings.waypoints[i].position, universalSettings.waypoints[i+1].position, 1-adjustedPercent);
            }
        }
        return Vector3.zero;
    }

    //Returns a vector3 between two defined vector3s based on the percentage value between them, for example the midpoint would be a percentage value of 50 percent
    Vector3 GetLocationBetweenLocations(Vector3 pos1, Vector3 pos2, float percent)
    {
        float x = ((pos1.x - pos2.x) * percent) + pos2.x;
        float y = ((pos1.y - pos2.y) * percent) + pos2.y;
        float z = ((pos1.z - pos2.z) * percent) + pos2.z;
        return new Vector3(x,y,z);
    }

    //Records the original system speed to be used when the system is reset, and adds the children of the starting waypoint to the waypoints list
    private void Start()
    {
        railSystemExtensions = GetComponents<IRailSystemExtension>();

        primarySettings.originalSpeed = primarySettings.speed;
        if (universalSettings.StartingWaypoint != null)
        {
            universalSettings.waypoints.Clear();
            Transform[] newWaypoints = universalSettings.StartingWaypoint.GetComponentsInChildren<Transform>();
            foreach(Transform trans in newWaypoints)
            {
                universalSettings.waypoints.Add(trans);
            }
        }
    }

#if UNITY_EDITOR

    //Draws lines between the waypoints to help in visualizing the rail system in the editor, and generates the waypoint list
    private void OnDrawGizmos()
    {
        if (universalSettings == null) return;

        //Generates the waypoint list
        if (universalSettings.StartingWaypoint)
        {
            universalSettings.waypoints.Clear();
            Transform[] newWaypoints = universalSettings.StartingWaypoint.GetComponentsInChildren<Transform>();
            foreach (Transform trans in newWaypoints)
            {
                universalSettings.waypoints.Add(trans);
            }
        }

        //Throws a helpful warning if the rail system is set to be both key controlled and time controlled
        if (primarySettings.keyControlled && primarySettings.timeControlled) Debug.LogWarning("Rail System has multiple control types!");

        //Draws line between the waypoints if the parent's value has been set
        if (universalSettings.definingType == 0)
        {
            DrawPathLine(Color.yellow);
        }
        if (universalSettings.definingType == 1)
        {
            DrawPathLine(Color.grey);
        }
        if (universalSettings.definingType == 2)
        {
            DrawPathLine(Color.blue);
        }
    }
#endif

    void DrawPathLine(Color color)
    {
        //Draws line between the waypoints if the parent's value has been set
        if (universalSettings.waypoints.Count <= 1) return;

        for (int i = 0; i < universalSettings.waypoints.Count - 1; i++)
        {
            if (universalSettings.waypoints[i] == null) return;
            if (universalSettings.waypoints[i + 1] == null) return;
            Gizmos.color = color;
            Gizmos.DrawLine(universalSettings.waypoints[i].position, universalSettings.waypoints[i + 1].position);
        }
    }
}
