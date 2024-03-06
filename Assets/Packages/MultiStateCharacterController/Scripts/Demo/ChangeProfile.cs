using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeProfile : MonoBehaviour
{
    //New Profile
    public MSCCProfile profile;
    //Character Controller
    public MultistateCharacterController characterController;
    //Key press that changes the profile
    public KeyCode keyCode;
    public bool _enabled = true;

    // Update is called once per frame
    void Update()
    {
        if (_enabled && Input.GetKey(keyCode))
        {
            _enabled = false;
            //Changes the profile to the new profile
            characterController.AttemptProfileChange(profile);
        }
    }
}
