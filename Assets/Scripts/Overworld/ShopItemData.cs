using UnityEngine;

public enum ShopItemType
{
    Upgrade,
    Consumable
}

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;   // Hahayaan nating 'displayName' para hindi mag-pula ang Visual Studio mo

    [Header("Visuals")]
    public Sprite itemIcon;      // ETO LANG ANG DINAGDAG NATIN. Dito mo ilalagay yung unique image.

    [Header("Cost")]
    public int cost = 10;        // Hahayaan nating 'cost'

    [Header("Type")]
    public ShopItemType type = ShopItemType.Upgrade;

    [Header("Upgrade Settings")]
    public string statToUpgrade;
    public float upgradeAmount = 10f;
    public int maxLevel = 1;

    [Header("Consumable Settings")]
    public bool isRevive = false;
    public bool isHealthRefill = false;
}