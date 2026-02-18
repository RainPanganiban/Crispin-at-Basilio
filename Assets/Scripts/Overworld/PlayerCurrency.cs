using Mirror;
using UnityEngine;

/// <summary>
/// Syncs player currency (coins) to clients via SyncVar.
/// Attach this to player prefabs.
/// </summary>
public class PlayerCurrency : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnCoinsChanged))]
    private int coins;

    public int Coins => coins;

    public static event System.Action<int> OnLocalPlayerCoinsChanged;

    void Start()
    {
        if (isServer)
        {
            // Initialize coins from session data
            if (connectionToClient != null)
            {
                CustomNetworkManager manager = CustomNetworkManager.Instance;
                if (manager != null && manager.TryGetSessionData(connectionToClient, out CustomNetworkManager.PlayerSessionData data))
                {
                    coins = data.coins;
                }
            }
        }
    }

    [Server]
    public void SetCoins(int amount)
    {
        coins = amount;
    }

    [Server]
    public void AddCoins(int amount)
    {
        coins += amount;
    }

    [Server]
    public bool TrySpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;
            return true;
        }
        return false;
    }

    void OnCoinsChanged(int oldValue, int newValue)
    {
        if (isLocalPlayer)
        {
            OnLocalPlayerCoinsChanged?.Invoke(newValue);
        }
    }
}
