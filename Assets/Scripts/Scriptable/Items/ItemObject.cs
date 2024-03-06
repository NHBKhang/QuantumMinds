using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Food,
    Default
}
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
public abstract class ItemObject : ScriptableObject
{
    public string id;
    public new string name;
    public GameObject prefab;
    public Sprite icon;
    public ItemType itemType;
    public Rarity rarity;
    [TextArea(5, 20)] public string description;
    public int maxStack = 999;


    //Food stats
    public virtual bool IsRestored { get; }
    public virtual float RestoreHealthValue { get; }
}
