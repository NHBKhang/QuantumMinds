using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Tooltip("The character representation's transform")]
    public Transform characterRep;
    [Tooltip("Added player location offset used for camera focus")]
    public Vector3 playerOffset = new Vector3(0,1.75f,0);
    //Calculated world value of the player plus the player offset
    private Vector3 adjustedPlayerOffset;
    //Is camera collision enabled?
    [HideInInspector]
    public bool isEnabled = true;

    [Tooltip("The speed at which the camera zooms to avoid clipping")]
    public float movementSpeed = 10;
    [Tooltip("Layer mask that defines what the camera will collide with")]
    public LayerMask cameraClipLayer;
    //Calculated raycast points around the camera
    private Vector3[] floatingBorders = new Vector3[5];
    //Calculated raycast points around the player
    private Vector3[] playerFloatingBorders = new Vector3[5];
    [Range(0, 4), Tooltip("Distance, in the horizontal direction, separating the spherecast targets")]
    public float borderWidth = 1.87f;
    [Range(0, 4), Tooltip("Distance, in the vertical direction, separating the spherecast targets")]
    public float borderHeight = 1;
    [Range(0, 4), Tooltip("Distance, in the forward direction, separating the spherecast targets")]
    public float borderDistance = 1.69f;
    [Range(0, 1), Tooltip("Spherecast radius")]
    public float borderRadius = 0.202f;
    [Range(0, 1), Tooltip("The minimum distance the camera can be from the player")]
    public float minimumDistance = 1f;
    [Range(0, 3), Tooltip("Width and height border multiplier used for calculating the player's spherecast location.")]
    public float playerMultiplier = 0;
    [Range(0, 1), Tooltip("Forward distance border multiplier used for calculating the player's spherecast location.")]
    public float distanceMultiplier = 0;

    [Range(-1, 1), Tooltip("The camera's forward position offset from the calculated camera position")]
    public float cameraOffset = 0.1f;
    [Tooltip("Draws Gizmos that are useful for initial camera setup")]
    public bool drawGizmos = true;
    //The distance between the player and the camera's ideal position
    private float fullDistance;
    //Calculated camera position
    private Vector3 targetPos;

    // Update is called once per frame
    void FixedUpdate()
    {
        //Checks if the camera is enabled
        if (isEnabled)
        {
            //Finds the world location of the player plus the offset
            adjustedPlayerOffset = characterRep.TransformDirection(playerOffset);
            //Checks for clipping and sets the camera's target position
            CheckForClipping();
            //Moves the camera to the target position
            transform.position = Vector3.Lerp(transform.position, targetPos, movementSpeed * Time.deltaTime);
        }
        else
        {
            //Sets the camera to the ideal position. This is lerped to prevent snapping when enabling and disabling camera collision.
            transform.position = Vector3.Lerp(transform.position, transform.parent.position, movementSpeed * Time.deltaTime);
        }
    }

    //Checks if the camera is clipping and sets the target position
    void CheckForClipping()
    {
        //Calculates the full distance
        fullDistance = Vector3.Distance(characterRep.position + adjustedPlayerOffset, transform.parent.position);
        //Calculates the player's raycast points
        GetBorders(transform, characterRep.position + adjustedPlayerOffset, borderDistance* distanceMultiplier, playerMultiplier,  playerFloatingBorders);
        //Calculates the camera's raycast points
        GetBorders(transform, transform.parent.position, borderDistance, 1 , floatingBorders);
        //Tests for clipping and outputs the collision with the smallest distance
        if (RaycastCheckBorders(out float smallestDistance))
        {
            //Calculates the required camera movement and sets the target position
            float minDistance = minimumDistance / fullDistance;
            float position = Mathf.Clamp(smallestDistance / fullDistance, minDistance, 1);
            Vector3 forwardPosition = Vector3.Lerp(characterRep.position+adjustedPlayerOffset, transform.parent.position, position);
            targetPos = forwardPosition;
        }
        else
        {
            //Sets the target position to the ideal position if no collisions where detected
            targetPos = transform.parent.position;
        }

    }

    //Raycasts between each border, outputs the smallest collision distance, and returns true if a collision was detected
    bool RaycastCheckBorders(out float smallestDistance)
    {
        //Raycasts between each border and records the collision distance
        List<float> distances = new List<float>();
        for (int i = 0; i < floatingBorders.Length; i++)
        {
            float distance;
            if (RaycastCheckBorder(floatingBorders[i], playerFloatingBorders[i], out distance))
            {
                if (distance != -1000)
                {
                    distances.Add(distance);
                }
            }
        }
        //Outputs the smallest distance if a collision was detected
        if (distances.Count > 0)
        {
            smallestDistance = distances.ToArray().Min();
            return true;
        }
        smallestDistance = 0;
        return false;
    }

    //Spherecasts between the player's border and the camera's border and outputs the distance
    bool RaycastCheckBorder(Vector3 corner, Vector3 playerCorner, out float distance)
    {

        RaycastHit raycastHit;
        float castDistance = Vector3.Distance((characterRep.position + adjustedPlayerOffset), corner);
        if (Physics.SphereCast(playerCorner, borderRadius, corner - (characterRep.position + adjustedPlayerOffset), out raycastHit, castDistance, cameraClipLayer))
        {
            distance = Vector3.Distance(characterRep.position + adjustedPlayerOffset, raycastHit.point);
            return true;
        }
        distance = -1000;
        return false;
    }

    //Calculates borders
    void GetBorders(Transform perspective, Vector3 position, float distance, float multiplier, Vector3[] array)
    {
        //perspective is the transform representing the direction
        //position is the base position for the calculations
        //array is either the player's borders or the camera's borders

        Vector3 cornerBase = position + (perspective.forward * distance);

        //Top right
        Vector3 cornerOne = cornerBase + perspective.up * (borderHeight) * multiplier;
        cornerOne += perspective.right * borderWidth * multiplier;
        array[0] = cornerOne;

        //Top left
        Vector3 cornerTwo = cornerBase + perspective.up * (borderHeight) * multiplier;
        cornerTwo += perspective.right * -borderWidth * multiplier;
        array[1] = cornerTwo;

        //Bottom left
        Vector3 cornerThree = cornerBase + perspective.up * -(borderHeight * multiplier);
        cornerThree += perspective.right * -borderWidth * multiplier;
        array[2] = cornerThree;

        //Bottom right
        Vector3 cornerFour = cornerBase + perspective.up * -(borderHeight * multiplier);
        cornerFour += perspective.right * borderWidth * multiplier;
        array[3] = cornerFour;

        //Center
        array[4] = position;
    }

    //Draws Gizmos that are useful for initial camera setup and debugging
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;



        Vector3 cornerBase = targetPos + (transform.forward * (borderDistance+borderRadius));

        Vector3 cornerOne = cornerBase + transform.up * (borderHeight);
        cornerOne += transform.right * (borderWidth);
        Gizmos.DrawSphere(cornerOne, borderRadius);
        Gizmos.DrawSphere(floatingBorders[0], borderRadius);
        Gizmos.DrawWireSphere(playerFloatingBorders[0], borderRadius);

        Vector3 cornerTwo = cornerBase + transform.up * (borderHeight);
        cornerTwo += transform.right * -(borderWidth);
        Gizmos.DrawSphere(cornerTwo, borderRadius);
        Gizmos.DrawSphere(floatingBorders[1], borderRadius);
        Gizmos.DrawWireSphere(playerFloatingBorders[1], borderRadius);

        Vector3 cornerThree = cornerBase + transform.up * -(borderHeight);
        cornerThree += transform.right * -(borderWidth);
        Gizmos.DrawSphere(cornerThree, borderRadius);
        Gizmos.DrawSphere(floatingBorders[2], borderRadius);
        Gizmos.DrawWireSphere(playerFloatingBorders[2], borderRadius);

        Vector3 cornerFour = cornerBase + transform.up * -(borderHeight);
        cornerFour += transform.right * (borderWidth);
        Gizmos.DrawSphere(cornerFour, borderRadius);
        Gizmos.DrawSphere(floatingBorders[3], borderRadius);
        Gizmos.DrawWireSphere(playerFloatingBorders[3], borderRadius);

        Gizmos.DrawSphere(targetPos, borderRadius);

        Gizmos.DrawWireSphere(characterRep.position + adjustedPlayerOffset, borderRadius);

        Gizmos.DrawLine(cornerOne, cornerTwo);
        Gizmos.DrawLine(cornerTwo, cornerThree);
        Gizmos.DrawLine(cornerThree, cornerFour);
        Gizmos.DrawLine(cornerFour, cornerOne);

        Gizmos.DrawLine(cornerOne, playerFloatingBorders[0]);
        Gizmos.DrawLine(cornerTwo, playerFloatingBorders[1]);
        Gizmos.DrawLine(cornerThree, playerFloatingBorders[2]);
        Gizmos.DrawLine(cornerFour, playerFloatingBorders[3]);
        Gizmos.DrawLine(targetPos, playerFloatingBorders[4]);
    }
#endif
}
