using UnityEngine;

public enum ShopItemType
{
    Upgrade,
    Consumable
}

/// <summary>
/// ScriptableObject defining a purchasable shop item.
/// Create new items via Assets → Create → Shop → Shop Item.
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;            // Unique key, e.g. "MaxHealth_1"
    public string displayName;   // Shown in UI, e.g. "Health +20"

    [Header("Cost")]
    public int cost = 10;

    [Header("Type")]
    public ShopItemType type = ShopItemType.Upgrade;

    [Header("Upgrade Settings (only for Upgrade type)")]
    [Tooltip("Which stat to upgrade: MaxHealth, MaxStamina, AttackDamage")]
    public string statToUpgrade;
    public float upgradeAmount = 10f;
    [Tooltip("Max times this upgrade can be purchased (0 = unlimited).")]
    public int maxLevel = 1;

    [Header("Consumable Settings (only for Consumable type)")]
    [Tooltip("For revive items, set this to true.")]
    public bool isRevive = false;
    [Tooltip("For health refill items, set this to true.")]
    public bool isHealthRefill = false;
}
