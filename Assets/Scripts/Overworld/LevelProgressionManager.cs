using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LevelProgressionManager : NetworkBehaviour
{
    public static LevelProgressionManager Instance { get; private set; }

    [System.Serializable]
    public class LevelDefinition
    {
        public string levelId;
        public string sceneName;
        public bool startUnlocked;
        public bool isMainPath;
        public bool isArena;
        public List<string> unlocksOnComplete = new List<string>();
    }

    [Header("Level Configuration")]
    public List<LevelDefinition> levels = new List<LevelDefinition>();

    public class SyncDictionaryStringBool : SyncDictionary<string, bool> { }

    // Tracks which levels are unlocked and which have been completed.
    public SyncDictionaryStringBool unlockedLevels = new SyncDictionaryStringBool();
    public SyncDictionaryStringBool completedLevels = new SyncDictionaryStringBool();

    // Nodes registered per level id so we can push unlock state to them.
    private readonly Dictionary<string, List<LevelNode>> nodesByLevelId =
        new Dictionary<string, List<LevelNode>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeProgression();
        ApplyPendingCompletions();
    }

    [Server]
    void InitializeProgression()
    {
        // Only initialize once per session.
        if (unlockedLevels.Count > 0)
            return;

        foreach (LevelDefinition def in levels)
        {
            unlockedLevels[def.levelId] = def.startUnlocked;
            completedLevels[def.levelId] = false;
        }

        ReevaluateAllNodes();
    }

    /// <summary>
    /// Applies level completions that were queued in CustomNetworkManager
    /// while the overworld scene (and this manager) didn't exist.
    /// </summary>
    [Server]
    void ApplyPendingCompletions()
    {
        CustomNetworkManager manager = CustomNetworkManager.Instance;
        if (manager == null) return;

        HashSet<string> pending = manager.ConsumePendingCompletedLevels();
        foreach (string levelId in pending)
        {
            Debug.Log($"[LevelProgressionManager] Applying pending completion for '{levelId}'.");
            MarkLevelCompleted(levelId);
        }
    }

    [Server]
    public bool IsLevelUnlocked(string levelId)
    {
        return unlockedLevels.TryGetValue(levelId, out bool isUnlocked) && isUnlocked;
    }

    [Server]
    public bool IsLevelCompleted(string levelId)
    {
        return completedLevels.TryGetValue(levelId, out bool isCompleted) && isCompleted;
    }

    [Server]
    public void MarkLevelCompleted(string levelId)
    {
        if (!completedLevels.ContainsKey(levelId))
        {
            completedLevels[levelId] = true;
        }
        else
        {
            completedLevels[levelId] = true;
        }

        ComputeNewUnlocks(levelId);
        ReevaluateAllNodes();
    }

    [Server]
    void ComputeNewUnlocks(string justCompletedLevelId)
    {
        LevelDefinition def = levels.Find(l => l.levelId == justCompletedLevelId);
        if (def == null)
            return;

        foreach (string id in def.unlocksOnComplete)
        {
            unlockedLevels[id] = true;
        }
    }

    [Server]
    public void RegisterNode(LevelNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.levelId))
            return;

        if (!nodesByLevelId.TryGetValue(node.levelId, out List<LevelNode> list))
        {
            list = new List<LevelNode>();
            nodesByLevelId[node.levelId] = list;
        }

        if (!list.Contains(node))
        {
            list.Add(node);
        }

        node.ServerSetUnlocked(IsLevelUnlocked(node.levelId));
    }

    [Server]
    public void UnregisterNode(LevelNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.levelId))
            return;

        if (nodesByLevelId.TryGetValue(node.levelId, out List<LevelNode> list))
        {
            list.Remove(node);
        }
    }

    [Server]
    void ReevaluateAllNodes()
    {
        foreach (KeyValuePair<string, List<LevelNode>> kvp in nodesByLevelId)
        {
            bool unlocked = IsLevelUnlocked(kvp.Key);

            foreach (LevelNode node in kvp.Value)
            {
                if (node != null)
                {
                    node.ServerSetUnlocked(unlocked);
                }
            }
        }
    }
}

