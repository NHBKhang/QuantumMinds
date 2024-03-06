using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBind : MonoBehaviour
{
    [Header("Movement Keys")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode proneKey = KeyCode.Z;

    [Header("Other Keys")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode inventoryKey = KeyCode.I;

    public static KeyBind keys = default;

    private void Awake()
    {
        keys = this;
    }
}
