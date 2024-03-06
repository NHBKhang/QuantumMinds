using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "Default Object", menuName = "Inventory/Items/Default")]
public class DefaultObject : ItemObject
{
   
    private void Awake()
    {
        itemType = ItemType.Default;       
    }

}
