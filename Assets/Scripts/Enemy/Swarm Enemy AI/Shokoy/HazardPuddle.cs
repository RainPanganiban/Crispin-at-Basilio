using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class HazardPuddle : NetworkBehaviour
{
    [Header("Hazard Settings")]
    [SyncVar] public float damagePerTick = 2f;
    [SyncVar] public float tickRate = 0.5f;
    [SyncVar] public float lifetime = 4f;
    public LayerMask targetLayer;

    private Dictionary<IDamageable, float> nextTickTimes = new Dictionary<IDamageable, float>();

    public override void OnStartServer()
    {
        StartCoroutine(LifetimeRoutine());
    }

    [ServerCallback]
    private void OnTriggerStay(Collider other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (!nextTickTimes.ContainsKey(damageable) || Time.time >= nextTickTimes[damageable])
            {
                damageable.TakeDamage(damagePerTick, transform);
                nextTickTimes[damageable] = Time.time + tickRate;
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
         IDamageable damageable = other.GetComponent<IDamageable>();
         if (damageable != null && nextTickTimes.ContainsKey(damageable))
         {
             nextTickTimes.Remove(damageable);
         }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        NetworkServer.Destroy(gameObject);
    }
}
