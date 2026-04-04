using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single shop item row.
/// Instantiated dynamically by OverworldShopClient.
/// </summary>
public class ShopItemButton : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button buyButton;

    // --- ETO LANG ANG DINAGDAG NA VARIABLE ---
    [SerializeField] private Image itemIconImage;

    private ShopItemData itemData;
    private System.Action<string> onPurchaseCallback;

    public void Setup(ShopItemData data, int currentLevel, int playerCoins, System.Action<string> onPurchase)
    {
        itemData = data;
        onPurchaseCallback = onPurchase;

        if (nameText != null)
            nameText.text = data.displayName;

        if (costText != null)
            costText.text = $"{data.cost} coins";

        // --- ETO LANG ANG DINAGDAG NA LOGIC PARA SA ICON ---
        if (itemIconImage != null && data.itemIcon != null)
        {
            itemIconImage.sprite = data.itemIcon;
        }

        // Show level for upgrades
        if (levelText != null)
        {
            if (data.type == ShopItemType.Upgrade && data.maxLevel > 0)
            {
                levelText.gameObject.SetActive(true); // Siguraduhin nating active kung upgrade
                levelText.text = $"Lv. {currentLevel}/{data.maxLevel}";
            }
            else
            {
                levelText.gameObject.SetActive(false);
            }
        }

        // Enable/disable based on affordability and max level
        bool canAfford = playerCoins >= data.cost;
        bool atMaxLevel = data.type == ShopItemType.Upgrade && data.maxLevel > 0 && currentLevel >= data.maxLevel;

        if (buyButton != null)
        {
            buyButton.interactable = canAfford && !atMaxLevel;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onPurchaseCallback?.Invoke(itemData.id));
        }
    }

    public void UpdateState(int currentLevel, int playerCoins)
    {
        if (itemData == null) return;

        bool canAfford = playerCoins >= itemData.cost;
        bool atMaxLevel = itemData.type == ShopItemType.Upgrade && itemData.maxLevel > 0 && currentLevel >= itemData.maxLevel;

        if (buyButton != null)
            buyButton.interactable = canAfford && !atMaxLevel;

        if (levelText != null && itemData.type == ShopItemType.Upgrade && itemData.maxLevel > 0)
            levelText.text = $"Lv. {currentLevel}/{itemData.maxLevel}";
    }
}