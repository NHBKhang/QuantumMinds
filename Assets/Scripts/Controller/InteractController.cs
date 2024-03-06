using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractController : MonoBehaviour
{
    [Header("References")]
    //public GameObject interactionUI;
    public CameraController cameraScript;
    public LayerMask interactLayer;

    [Header("Stats")]
    public float range;

    private TextMeshProUGUI textMesh;
    private string defaultText;
    private Outline outline;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (outline != null)
        {
            outline.enabled = false;
            outline = null;
        }

        Ray ray = new Ray(cameraScript.virtualCamera.position, cameraScript.virtualCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit rayHit, range, interactLayer))
        {
            outline = rayHit.collider.GetComponent<Outline>();
            outline.enabled = true;
        }
    }
}
