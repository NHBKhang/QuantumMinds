using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgnesAnimation : Animation
{
    private int moveX, moveZ;
    private int isCrouching;
    private new void Awake()
    {
        base.Awake();
        moveX = Animator.StringToHash("MoveX");
        moveZ = Animator.StringToHash("MoveZ");
        isCrouching = Animator.StringToHash("isCrouching");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetBlendTree(float moveX, float moveZ)
    {
        animator.SetFloat(this.moveX, moveX);
        animator.SetFloat(this.moveZ, moveZ);
    }
    public void SwitchState(bool isCrouching)
    {
        animator.SetBool(this.isCrouching, isCrouching);
    }
}
