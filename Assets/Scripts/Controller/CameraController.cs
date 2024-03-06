using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Preferences")]
    public Transform virtualCamera;
    public Transform player;
    public Transform playerObj;
    public Transform orientation;

    [Header("Stats")]
    public float mouseSensitivityX;
    public float mouseSensitivityY;
    
    private float mouseX;
    private float mouseY;
    private float xRotation, yRotation;
    private Transform mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main.transform;
    }

    void Start()
    {
        MouseLocked();
    }

    // Update is called once per frame
    void Update()
    {
        mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseSensitivityX;
        mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseSensitivityY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        mainCamera.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        playerObj.rotation = orientation.rotation;


        //Head moving with camera
        //target.localPosition = defaultTargetPos + new Vector3(0,
        //    Mathf.Tan(mainCamera.rotation.x) * (defaultTargetPos - mainCamera.position.normalized).x, 0);
    }
    public static void MouseUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void MouseLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
