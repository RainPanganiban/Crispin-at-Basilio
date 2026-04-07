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

    [Header("Targeting Preferences")]
    public string targetChildName = "Model";

    [Header("Surround Settings")]
    [SerializeField] private float surroundRadius = 3f;
    [SerializeField] private float surroundJitter = 0.5f;

    [Header("Distance Settings")]
    [SerializeField] private float aggroRadius = 15f;
    [SerializeField] private float aggroSwitchDistance = 1.5f;
    [SerializeField] private float similarDistanceThreshold = 0.2f;

    [Header("Timing")]
    [SerializeField] private float aggroUpdateInterval = 0.6f;
    [SerializeField] private float minimumLockTime = 1.5f;

    [Header("Chaos Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float chaosChance = 0.15f;

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

    // DAGDAG: Ito ang sasagot sa SendMessage mula sa PlayerStatsManager
    [Server]
    public void OnTargetDead(GameObject deadPlayer)
    {
        // Kung ang target ngayon ay ang player na namatay, i-null agad
        if (currentTarget != null && (currentTarget.gameObject == deadPlayer || currentTarget.IsChildOf(deadPlayer.transform)))
        {
            currentTarget = null;
        }
    }

    [Server]
    private IEnumerator AggroRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));

        while (true)
        {
            ChooseTarget();
            yield return new WaitForSeconds(aggroUpdateInterval);
        }
    }

    [Server]
    private void ChooseTarget()
    {
        PlayerMovement[] players = GameObject.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            currentTarget = null;
            return;
        }

        Transform bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (PlayerMovement player in players)
        {
            if (player == null) continue;

            // FIX: I-check kung patay na ang player
            PlayerStatsManager stats = player.GetComponent<PlayerStatsManager>();
            if (stats != null && stats.IsDead) continue; // Skip kapag patay na

            // FIX: I-check din ang Tag (para sigurado)
            if (!player.CompareTag("Player")) continue;

            Transform playerRoot = player.transform;
            float distance = Vector3.Distance(transform.position, playerRoot.position);

            if (distance > aggroRadius)
                continue;

            float score = distance;

            if (Random.value < chaosChance)
                score *= Random.Range(0.9f, 1.15f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = playerRoot;
            }
        }

        // VALIDATION: Siguraduhin na ang currentTarget ay hindi naging invalid habang tumatakbo
        if (currentTarget != null)
        {
            PlayerStatsManager currentStats = currentTarget.GetComponentInParent<PlayerStatsManager>();
            if (currentStats != null && currentStats.IsDead)
            {
                currentTarget = null;
            }
        }

        if (bestTarget == null)
        {
            // Kung wala nang makitang buhay, i-clear ang target
            currentTarget = null;
            return;
        }

        TrySwitchTarget(bestTarget);
    }

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
        Transform actualTarget = target;

        if (!string.IsNullOrEmpty(targetChildName))
        {
            Transform modelTransform = target.Find(targetChildName);
            if (modelTransform != null)
            {
                actualTarget = modelTransform;
            }
        }

        currentTarget = actualTarget;
        lastSwitchTime = Time.time;

        if (aggroType == AggroType.SwarmShared)
        {
            ShareTargetWithSwarm(actualTarget);
        }
    }

    [Server]
    public void ForceTarget(Transform attacker)
    {
        if (aggroType == AggroType.ClosestOnly)
            return;

        // Huwag payagan ang force target kung patay na ang attacker
        PlayerStatsManager stats = attacker.GetComponentInParent<PlayerStatsManager>();
        if (stats != null && stats.IsDead) return;

        SetTarget(attacker);
    }

    [Server]
    private void ShareTargetWithSwarm(Transform target)
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy == this) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= swarmShareRadius)
            {
                if (Random.value > 0.3f)
                {
                    enemy.ReceiveSwarmTarget(target);
                }
            }
        }
    }

    [Server]
    public void ReceiveSwarmTarget(Transform target)
    {
        // Check kung buhay pa bago tanggapin ang swarm target
        PlayerStatsManager stats = target.GetComponentInParent<PlayerStatsManager>();
        if (stats != null && stats.IsDead) return;

        if (currentTarget == null || Random.value > 0.5f)
        {
            SetTarget(target);
        }
    }

    public Vector3 GetSurroundPosition(float maxRadius = -1f)
    {
        if (currentTarget == null)
            return transform.position;

        float radius = surroundRadius;
        if (maxRadius > 0f)
            radius = Mathf.Min(radius, maxRadius);

        List<EnemyAggro> sameTarget = new List<EnemyAggro>();
        foreach (var enemy in allEnemies)
        {
            if (enemy.currentTarget == currentTarget)
                sameTarget.Add(enemy);
        }

        int index = sameTarget.IndexOf(this);
        int count = sameTarget.Count;

        if (count == 0 || index == -1)
            return currentTarget.position + (transform.position - currentTarget.position).normalized * Mathf.Min(radius, 1f);

        float angleStep = 360f / count;
        float angle = angleStep * index;
        angle += Random.Range(-15f, 15f);
        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)) * radius;
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