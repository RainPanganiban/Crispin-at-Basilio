using UnityEngine;
using Mirror;
using System.Collections;

public class EarthshakerStompAttack : BaseAttack
{
    public const string Event_Impact = "StompImpact";

    [Header("Spawn")]
    public Transform ringOrigin;
    public OngloShockwaveRing ringPrefab;
    public LayerMask playerLayer;

    [Header("Damage")]
    public float ringDamage = 25f;

    [Header("Ring shape")]
    public float expandSpeed = 12f;
    public float maxRadius = 8f;

    [Header("Phase scaling")]
    public int phase1Rings = 1;
    public int phase2Rings = 1;
    public int phase3Rings = 2; // double shockwaves
    public float ringInterval = 0.18f;

    private BossPhaseManager phaseManager;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        phaseManager = bossController != null ? bossController.GetComponent<BossPhaseManager>() : null;
    }

    public override void Server_Execute()
    {
        // Telegraph + timing is owned by the animation.
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName != Event_Impact)
            return;

        int rings = Server_GetRingCountForCurrentPhase();
        if (rings <= 0)
            return;

        StartCoroutine(Server_SpawnRings(rings));
    }

    int Server_GetRingCountForCurrentPhase()
    {
        int idx = phaseManager != null ? phaseManager.GetCurrentPhaseIndex() : 0;
        return idx switch
        {
            0 => phase1Rings,
            1 => phase2Rings,
            _ => phase3Rings,
        };
    }

    IEnumerator Server_SpawnRings(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Server_SpawnSingleRing();
            if (i < count - 1)
                yield return new WaitForSeconds(ringInterval);
        }
    }

    [Server]
    void Server_SpawnSingleRing()
    {
        if (ringPrefab == null)
            return;

        Transform origin = ringOrigin != null ? ringOrigin : transform;
        OngloShockwaveRing ring = Instantiate(ringPrefab, origin.position, Quaternion.identity);
        ring.Server_Initialize(
            owner: boss != null ? boss.netIdentity : null,
            damage: ringDamage,
            expandSpeed: expandSpeed,
            maxRadius: maxRadius,
            playerLayer: playerLayer
        );

        NetworkServer.Spawn(ring.gameObject);
    }

    public override void Server_Stop()
    {
        StopAllCoroutines();
    }
}

