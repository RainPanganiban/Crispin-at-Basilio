using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class EnemyAttackController : NetworkBehaviour
{
    private EnemyBrain brain;
    private List<EnemyAttack> attacks = new List<EnemyAttack>();

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
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

        selected.Execute();

        Invoke(nameof(FinishAttack), selected.duration);
    }

    [Server]
    void FinishAttack()
    {
        brain.OnAttackFinished();
    }
}
