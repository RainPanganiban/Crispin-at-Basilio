using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class LaserDamageHandler : NetworkBehaviour
{
    private float damage;
    private float range;
    private float width;
    private float interval;
    private LayerMask mask;
    private Collider ownerCol;
    private NetworkIdentity ownerId;

    [SerializeField] private LineRenderer lineRenderer;
    private Dictionary<Transform, float> lastHitTimes = new Dictionary<Transform, float>();

    // This is the function the Boss script will call
    public void Initialize(float dmg, float dist, float life, float w, float inter, Collider owner, NetworkIdentity id, LayerMask m)
    {
        damage = dmg;
        range = dist;
        width = w;
        interval = inter;
        ownerCol = owner;
        ownerId = id;
        mask = m;

        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        if (isServer) Destroy(gameObject, life);
    }

    void Update()
    {
        UpdateVisuals();
        if (isServer) ProcessDamage();
    }

    private void UpdateVisuals()
    {
        if (lineRenderer == null) return;
        lineRenderer.SetPosition(0, transform.position);

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range))
            lineRenderer.SetPosition(1, hit.point);
        else
            lineRenderer.SetPosition(1, transform.position + (transform.forward * range));
    }

    [Server]
    private void ProcessDamage()
    {
        // SphereCast acts like a thick cylinder to match the laser's width
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, width, transform.forward, range, mask);

        foreach (var hit in hits)
        {
            if (hit.collider == ownerCol) continue;

            if (!lastHitTimes.ContainsKey(hit.transform) || Time.time >= lastHitTimes[hit.transform] + interval)
            {
                // DAMAGE LOGIC HERE
                Debug.Log($"Laser hit {hit.transform.name} for {damage} damage!");
                lastHitTimes[hit.transform] = Time.time;

                // If your player has a health script, call it here:
                // hit.transform.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            }
        }
    }
}