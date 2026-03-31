using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class EnemyAttackController : NetworkBehaviour
{
    private EnemyBrain brain;
    private EnemySoundManager soundManager;
    private List<EnemyAttack> attacks = new List<EnemyAttack>();

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        soundManager = GetComponent<EnemySoundManager>();
        attacks.AddRange(GetComponents<EnemyAttack>());
    }

    public bool CanAttack()
    {
        foreach (var attack in attacks)
        {
            if (attack.CanUse())
                return true;
        }

        return false;
    }

    [Server]
    public void ExecuteRandomAttack()
    {
        List<EnemyAttack> available = attacks.FindAll(a => a.CanUse());

        if (available.Count == 0)
            return;

        int index = Random.Range(0, available.Count);
        EnemyAttack selected = available[index];
        
        // Find original index for network syncing
        int originalIndex = attacks.IndexOf(selected);

        // Execute the attack
        selected.Execute();
        
        // Play attack sound for all clients
        RpcNotifyAttack(originalIndex);

        // No callback to brain needed; the brain handles strafing automatically
        // You can optionally set a cooldown inside EnemyAttack itself
    }

    [ClientRpc]
    void RpcNotifyAttack(int attackIndex)
    {
        if (soundManager == null)
            soundManager = GetComponent<EnemySoundManager>();
            
        if (soundManager != null)
        {
            // Only play if the specific attack has a sound assigned.
            if (attackIndex >= 0 && attackIndex < attacks.Count && attacks[attackIndex].attackSound != null)
            {
                soundManager.PlaySpecificAttack(attacks[attackIndex].attackSound);
            }
        }
    }
}
