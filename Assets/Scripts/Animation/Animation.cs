using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Animation : MonoBehaviour
{
    protected Animator animator;

    protected void Awake()
    {
        animator = GetComponent<Animator>();
    }
}
