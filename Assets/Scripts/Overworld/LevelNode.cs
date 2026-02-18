using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

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

        playersInside.Add(identity);
        TryStartCountdown();
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

    bool HasAllRequiredPlayers()
    {
        // Require all connected players to be inside this node.
        int connectedPlayers = NetworkServer.connections.Count;
        return isUnlocked && connectedPlayers > 0 && playersInside.Count >= connectedPlayers;
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

    // Called server-side by LevelProgressionManager when progression changes.
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
        // Client-side visuals for locked vs unlocked state go here.
        // Example: enable/disable highlight, collider, icon, etc.
        Collider col = GetComponent<Collider>();
        col.enabled = newValue;
    }

    void OnCountdownChanged(float oldValue, float newValue)
    {
        // Client-side UI update based on countdownRemaining.
        // Hook this up to a world-space progress bar or HUD element.
    }

    // Public getters for UI components to read SyncVar values
    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    public float GetCountdownRemaining()
    {
        return countdownRemaining;
    }

    public float GetRequiredHoldTime()
    {
        return requiredHoldTime;
    }
}

