using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq; // Idagdag ito para sa filtering ng list

[RequireComponent(typeof(Collider))]
public class LevelNode : NetworkBehaviour
{
    [Header("Level Config")]
    public string levelId;
    public string sceneName;
    public float requiredHoldTime = 3f;

    [SyncVar(hook = nameof(OnUnlockedChanged))]
    bool isUnlocked;

    [SyncVar(hook = nameof(OnCountdownChanged))]
    float countdownRemaining;

    readonly HashSet<NetworkIdentity> playersInside = new HashSet<NetworkIdentity>();
    Coroutine countdownRoutine;

    LevelProgressionManager progression;
    OverworldManager overworldManager;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        progression = LevelProgressionManager.Instance;
        overworldManager = OverworldManager.Instance;

        if (progression != null)
        {
            progression.RegisterNode(this);
            isUnlocked = progression.IsLevelUnlocked(levelId);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (progression != null)
        {
            progression.UnregisterNode(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null || identity.connectionToClient == null)
            return;

        // Siguraduhin na ang pumapasok ay buhay na player
        var stats = identity.GetComponent<PlayerStatsManager>();
        if (stats != null && !stats.IsDead)
        {
            playersInside.Add(identity);
            TryStartCountdown();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isServer)
            return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null)
            return;

        playersInside.Remove(identity);

        if (!HasAllRequiredPlayers())
        {
            StopCountdown();
        }
    }

    // --- ITO ANG BINAGO ---
    bool HasAllRequiredPlayers()
    {
        if (!isUnlocked) return false;

        // 1. Kunin lahat ng connected players na BUHAY (hindi isDead)
        var alivePlayers = FindObjectsByType<PlayerStatsManager>(FindObjectsSortMode.None)
                            .Where(p => !p.IsDead)
                            .ToList();

        int aliveCount = alivePlayers.Count;

        // Walang buhay na player? Wag mag-start.
        if (aliveCount <= 0) return false;

        // 2. Linisin ang playersInside (tanggalin ang mga nadisconnect o biglang namatay habang nasa loob)
        playersInside.RemoveWhere(identity => {
            if (identity == null) return true;
            var stats = identity.GetComponent<PlayerStatsManager>();
            return stats == null || stats.IsDead;
        });

        // 3. I-check kung lahat ng buhay na players ay nasa loob na ng zone
        return playersInside.Count >= aliveCount;
    }

    void TryStartCountdown()
    {
        if (countdownRoutine != null)
            return;
        if (!HasAllRequiredPlayers())
            return;

        countdownRoutine = StartCoroutine(ServerCountdown());
    }

    void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        countdownRemaining = 0f;
    }

    IEnumerator ServerCountdown()
    {
        countdownRemaining = requiredHoldTime;

        while (countdownRemaining > 0f)
        {
            yield return new WaitForSeconds(0.1f);

            // Re-check kung andun pa rin lahat ng buhay na players
            if (!HasAllRequiredPlayers())
            {
                StopCountdown();
                yield break;
            }

            countdownRemaining -= 0.1f;
        }

        countdownRoutine = null;

        if (HasAllRequiredPlayers())
        {
            if (overworldManager != null)
            {
                overworldManager.ServerEnterLevel(levelId, sceneName);
            }
            else if (CustomNetworkManager.Instance != null)
            {
                CustomNetworkManager.Instance.ServerChangeScene(sceneName);
            }
        }
    }

    [Server]
    public void ServerSetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        if (!isUnlocked)
        {
            StopCountdown();
        }
    }

    void OnUnlockedChanged(bool oldValue, bool newValue)
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = newValue;
    }

    void OnCountdownChanged(float oldValue, float newValue)
    {
        // UI implementation here
    }

    public bool IsUnlocked() => isUnlocked;
    public float GetCountdownRemaining() => countdownRemaining;
    public float GetRequiredHoldTime() => requiredHoldTime;
}