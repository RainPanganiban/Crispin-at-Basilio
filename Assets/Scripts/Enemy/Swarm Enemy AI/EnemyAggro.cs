using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class EnemyAggro : NetworkBehaviour
{
    public enum AggroType
    {
        ClosestOnly,
        DamagePriority,
        SwarmShared
    }

    [Header("Aggro Type")]
    [SerializeField] private AggroType aggroType = AggroType.DamagePriority;

    [Header("Surround Settings")]
    [SerializeField] private float surroundRadius = 3f;
    [SerializeField] private float surroundJitter = 0.5f;

    [Header("Distance Settings")]
    [SerializeField] private float aggroRadius = 15f;
    [SerializeField] private float aggroSwitchDistance = 1.5f;  // How much closer new target must be to switch
    [SerializeField] private float similarDistanceThreshold = 0.2f; // Only chaos-switch when distances within this ratio

    [Header("Timing")]
    [SerializeField] private float aggroUpdateInterval = 0.6f;
    [SerializeField] private float minimumLockTime = 1.5f;

    [Header("Chaos Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float chaosChance = 0.15f; // Chance to switch when targets are similarly distant

    [Header("Swarm Settings")]
    [SerializeField] private float swarmShareRadius = 8f;

    private Transform currentTarget;
    private float lastSwitchTime;

    private static List<EnemyAggro> allEnemies = new List<EnemyAggro>();

    public Transform GetCurrentTarget() => currentTarget;

    public override void OnStartServer()
    {
        allEnemies.Add(this);
        StartCoroutine(AggroRoutine());
    }

    public override void OnStopServer()
    {
        allEnemies.Remove(this);
    }

    [Server]
    private IEnumerator AggroRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.5f)); // Desync swarm timing

        while (true)
        {
            ChooseTarget();
            yield return new WaitForSeconds(aggroUpdateInterval);
        }
    }

    // ===============================
    // CORE TARGET SELECTION
    // ===============================

    [Server]
    private void ChooseTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            currentTarget = null;
            return;
        }

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance > aggroRadius)
                continue;

            float score = distance;

            // Chaos: randomly bias score only when comparing similar distances
            if (Random.value < chaosChance)
                score *= Random.Range(0.9f, 1.15f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = player.transform;
            }
        }

        // Clear target if current is invalid (destroyed, left radius) or no valid targets
        if (currentTarget != null && (currentTarget.gameObject == null || !currentTarget.CompareTag("Player")))
            currentTarget = null;

        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.position) > aggroRadius)
            currentTarget = null;

        if (bestTarget == null)
            return;

        TrySwitchTarget(bestTarget);
    }

    // ===============================
    // SWITCH LOGIC
    // ===============================

    [Server]
    private void TrySwitchTarget(Transform newTarget)
    {
        if (currentTarget == null)
        {
            SetTarget(newTarget);
            return;
        }

        if (newTarget == currentTarget)
            return;

        if (Time.time < lastSwitchTime + minimumLockTime)
            return;

        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
        float newDistance = Vector3.Distance(transform.position, newTarget.position);

        bool significantlyCloser = newDistance + aggroSwitchDistance < currentDistance;

        // Chaos: only switch when distances are similar (avoid erratic long-range switches)
        bool distancesSimilar = Mathf.Abs(currentDistance - newDistance) / (currentDistance + 0.01f) < similarDistanceThreshold;
        bool chaosSwitch = distancesSimilar && Random.value < chaosChance;

        if (significantlyCloser || chaosSwitch)
        {
            SetTarget(newTarget);
        }
    }

    [Server]
    private void SetTarget(Transform target)
    {
        currentTarget = target;
        lastSwitchTime = Time.time;

        if (aggroType == AggroType.SwarmShared)
        {
            ShareTargetWithSwarm(target);
        }
    }

    // ===============================
    // DAMAGE PULL (Option 1)
    // ===============================

    [Server]
    public void ForceTarget(Transform attacker)
    {
        Debug.Log($"{name} forced to target {attacker.name}");
        
        if (aggroType == AggroType.ClosestOnly)
            return;

        SetTarget(attacker);
    }

    // ===============================
    // SWARM SHARE (Option 2)
    // ===============================

    [Server]
    private void ShareTargetWithSwarm(Transform target)
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy == this)
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= swarmShareRadius)
            {
                if (Random.value > 0.3f) // Not 100% adoption → keeps chaos
                {
                    enemy.ReceiveSwarmTarget(target);
                }
            }
        }
    }

    [Server]
    public void ReceiveSwarmTarget(Transform target)
    {
        if (currentTarget == null || Random.value > 0.5f)
        {
            SetTarget(target);
        }
    }

    /// <summary>
    /// Get a surround position around the current target. Use maxRadius to ensure
    /// slots are within attack range (e.g. pass EnemyBrain.attackRange).
    /// </summary>
    public Vector3 GetSurroundPosition(float maxRadius = -1f)
    {
        if (currentTarget == null)
            return transform.position;

        float radius = surroundRadius;
        if (maxRadius > 0f)
            radius = Mathf.Min(radius, maxRadius);

        // Get only enemies attacking same target
        List<EnemyAggro> sameTarget = new List<EnemyAggro>();

        foreach (var enemy in allEnemies)
        {
            if (enemy.currentTarget == currentTarget)
                sameTarget.Add(enemy);
        }

        int index = sameTarget.IndexOf(this);
        int count = sameTarget.Count;

        if (count == 0)
            return currentTarget.position + (transform.position - currentTarget.position).normalized * Mathf.Min(radius, 1f);

        float angleStep = 360f / count;
        float angle = angleStep * index;

        angle += Random.Range(-15f, 15f);

        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians),
            0,
            Mathf.Sin(radians)
        ) * radius;

        offset += Random.insideUnitSphere * surroundJitter;
        offset.y = 0;

        return currentTarget.position + offset;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, swarmShareRadius);
    }
}
