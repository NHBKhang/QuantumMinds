using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Food Object", menuName = "Inventory/Items/Food")]
public class FoodObject : ItemObject
{
    public float restoreHealthValue;
    public bool isRestored;
    private void Awake()
    {
        itemType = ItemType.Food; 
    }

    public override bool IsRestored
    {
        get { return isRestored; }
    }
    public override float RestoreHealthValue
    {
        get { return restoreHealthValue; }
    }
}
