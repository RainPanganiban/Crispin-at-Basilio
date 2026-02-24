using UnityEngine;
using Mirror;
using System;

/// <summary>
/// Tracks per-player currency (coins). Server-authoritative.
/// Each player has their own independent coin count; coins are NOT shared.
/// </summary>
public class PlayerCurrencyManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnCoinsChanged))]
    private int coins = 0;

    /// <summary>Fired on the local client whenever coin count changes.</summary>
    public event Action<int> OnCoinsUpdated;

    // =========================================================
    // Server Methods
    // =========================================================

    /// <summary>
    /// Called by the server (e.g. from CoinPickup) to award coins to this player.
    /// </summary>
    [Server]
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
    }

    // =========================================================
    // Client → Server Request
    // =========================================================

    /// <summary>
    /// Client requests the server to award a coin pickup.
    /// The coinNetIdentity is validated on the server before awarding.
    /// </summary>
    [Command]
    public void CmdRequestPickup(NetworkIdentity coinIdentity)
    {
        if (coinIdentity == null) return;

        CoinPickup coin = coinIdentity.GetComponent<CoinPickup>();
        if (coin == null || coin.IsCollected) return;

        coin.ServerCollect(this);
    }

    // =========================================================
    // SyncVar Hook
    // =========================================================

    /// <summary>Hook called on ALL clients when coins SyncVar changes.</summary>
    void OnCoinsChanged(int oldValue, int newValue)
    {
        // Only fire the UI event for the local player who owns this object
        if (!isLocalPlayer) return;
        OnCoinsUpdated?.Invoke(newValue);
    }

    // =========================================================
    // Accessors
    // =========================================================

    public int GetCoins() => coins;
}
