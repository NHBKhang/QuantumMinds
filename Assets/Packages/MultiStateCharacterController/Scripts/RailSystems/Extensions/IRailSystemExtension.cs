using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRailSystemExtension
{
     void OnLatch(MultistateCharacterController characterController);
     void OnUnlatch(MultistateCharacterController characterController);
     void OnKeyUnlatch(MultistateCharacterController characterController);
}
