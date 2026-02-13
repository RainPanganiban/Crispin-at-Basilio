using UnityEngine;
using Mirror;

public class EnemyAggro : NetworkBehaviour
{
    [Header("Aggro Settings")]
    public float aggroSwitchDistance = 1f; // Minimum difference to switch target

    private Transform currentTarget;

    public Transform GetCurrentTarget()
    {
        if (currentTarget == null)
        {
            ChooseTarget();
        }
        return currentTarget;
    }

    [Server]
    public void ChooseTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            currentTarget = null;
            return;
        }

        Transform closest = players[0].transform;
        float minDistance = Vector3.Distance(transform.position, closest.position);

        for (int i = 1; i < players.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, players[i].transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = players[i].transform;
            }
        }

        // Only switch if new player is closer by aggroSwitchDistance
        if (currentTarget != null)
        {
            float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
            if (minDistance + aggroSwitchDistance < currentDistance)
            {
                currentTarget = closest;
            }
        }
        else
        {
            currentTarget = closest;
        }
    }
}