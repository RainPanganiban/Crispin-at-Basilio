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
    [SerializeField] private float aggroSwitchDistance = 2f;

    [Header("Timing")]
    [SerializeField] private float aggroUpdateInterval = 0.6f;
    [SerializeField] private float minimumLockTime = 2f;

    [Header("Chaos Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float chaosChance = 0.25f; // Chance to behave unpredictably

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
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance > aggroRadius)
                continue;

            float score = distance;

            // Chaos: randomly bias score
            if (Random.value < chaosChance)
                score *= Random.Range(0.8f, 1.3f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = player.transform;
            }
        }

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

        if (Time.time < lastSwitchTime + minimumLockTime)
            return;

        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
        float newDistance = Vector3.Distance(transform.position, newTarget.position);

        bool significantlyCloser = newDistance + aggroSwitchDistance < currentDistance;

        if (significantlyCloser || Random.value < chaosChance)
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

    public Vector3 GetSurroundPosition()
    {
        if (currentTarget == null)
            return transform.position;

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
            return currentTarget.position;

        float angleStep = 360f / count;
        float angle = angleStep * index;

        angle += Random.Range(-15f, 15f);

        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(radians),
            0,
            Mathf.Sin(radians)
        ) * surroundRadius;

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
