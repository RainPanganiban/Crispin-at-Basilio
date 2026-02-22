using UnityEngine;
using Mirror;
using System.Collections;

public abstract class EnemyAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    public float cooldown = 2f;   // Time between uses
    public float duration = 1f;   // How long the attack "takes"

    protected float lastUsedTime = -Mathf.Infinity;

    // Check if the attack can currently be used
    public bool CanUse()
    {
        return Time.time >= lastUsedTime + cooldown;
    }

    [Server]
    public void Execute()
    {
        lastUsedTime = Time.time;

        // Perform the actual attack
        OnExecute();

        // Start duration countdown to notify brain when finished
        StartCoroutine(AttackRoutine());
    }

    // Handles attack timing
    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(duration);

        EnemyBrain brain = GetComponent<EnemyBrain>();
        if (brain != null)
            brain.OnAttackFinished();
    }

    // Implement your specific attack logic in subclasses
    protected abstract void OnExecute();
}