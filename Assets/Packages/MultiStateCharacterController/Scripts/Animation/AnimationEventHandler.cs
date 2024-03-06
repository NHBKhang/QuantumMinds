
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class AnimationEventHandler : MonoBehaviour
    {
        [Tooltip("MultistateCharacterController script located on the project player gameobject")]
        public MultistateCharacterController characterController;

        private void Start()
        {
            //Fixes ragdoll glitch
            GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).transform.position = GetComponent<Animator>().transform.position;
        }

        //Prevents exceptions from occurring due to common animation events
        public void FootR()
        {

        }

        //Prevents exceptions from occurring due to common animation events
        public void FootL()
        {

        }

        //Prevents exceptions from occurring due to common animation events
        public void Hit()
        {

        }

        //Prevents exceptions from occurring due to common animation events
        public void Land()
        {

        }

    void OnAnimatorMove()
    {
        if (characterController)
        {
            characterController.UpdateRootMotionAnimatorVelocity(GetComponent<Animator>().deltaPosition.magnitude / Time.deltaTime);
        }
    }

    //Passes IK functionality to the MSCC
    private void OnAnimatorIK(int layerIndex)
        {
            if(characterController)
            {
                characterController.IKPass();
            }
        }
    }
