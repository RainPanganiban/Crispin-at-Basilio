using UnityEngine;
using Mirror; // required because PlayerStatsManager uses NetworkBehaviour

public class phase1_tidalBite : StateMachineBehaviour
{
    public float damage = 30f;       // damage amount
    public float attackRange = 3f;   // range to check player
    private bool hasDamaged;
    private Transform boss;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        boss = animator.transform;
        hasDamaged = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime >= 0.5f && !hasDamaged)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerStatsManager stats = player.GetComponent<PlayerStatsManager>();
                if (stats != null && stats.isServer) // Only call on server
                {
                    stats.TakeDamage(damage, boss); // Pass boss as attacker
                    Debug.Log("Player took " + damage + " damage from boss!");
                }
            }

            hasDamaged = true;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasDamaged = false;
    }
}