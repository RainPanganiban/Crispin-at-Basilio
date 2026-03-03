using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a single entry within a wave: which enemy prefab to spawn and how many.
/// </summary>
[System.Serializable]
public class WaveEntry
{
    [Tooltip("Enemy prefab (must have NetworkIdentity + EnemyHealth).")]
    public GameObject enemyPrefab;

    [Tooltip("How many of this enemy type to spawn in this wave.")]
    public int count = 1;
}

/// <summary>
/// Defines one wave: its enemy composition and the delay before it starts.
/// </summary>
[System.Serializable]
public class WaveDefinition
{
    [Tooltip("Enemy types and counts for this wave.")]
    public List<WaveEntry> enemies = new List<WaveEntry>();

    [Tooltip("Seconds to wait before this wave's enemies spawn (inter-wave countdown).")]
    public float delayBeforeWave = 3f;
}

/// <summary>
/// ScriptableObject asset that holds all wave definitions for an arena level.
/// Create via: Right-click → Create → Arena → Wave Data
/// </summary>
[CreateAssetMenu(fileName = "NewArenaWaveData", menuName = "Arena/Wave Data")]
public class ArenaWaveData : ScriptableObject
{
    [Tooltip("List of waves. Minimum 3 recommended for a proper arena run.")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    public int WaveCount => waves.Count;
}
