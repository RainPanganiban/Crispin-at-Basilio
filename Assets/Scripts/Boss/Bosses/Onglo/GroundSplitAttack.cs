using UnityEngine;
using Mirror;

public class GroundSplitAttack : BaseAttack
{
    public const string Event_Impact = "GroundSplitImpact";

    [Header("Spawn")]
    public Transform fissureOrigin;
    public OngloFissure fissurePrefab;

    [Header("Fissure travel")]
    public float fissureSpeed = 8f;
    public float fissureMaxDistance = 12f;

    [Header("Eruption")]
    public float eruptDelay = 0.8f;
    public float eruptRadius = 2.5f;
    public float damage = 35f;
    public LayerMask playerLayer;

    [Header("Feedback")]
    public float impactShakeIntensity = 3f;

    // --- DAGDAG NA SOUND SETTINGS ---
    [Header("Audio")]
    public AudioClip splitImpactClip;
    private EnemySoundManager enemySound;

    public override void Initialize(BossController bossController)
    {
        base.Initialize(bossController);
        // Kunin ang reference para sa sound manager
        enemySound = bossController != null ? bossController.GetComponent<EnemySoundManager>() : null;
    }
    // --------------------------------

    public override void Server_Execute()
    {
        // Telegraph is animation-driven.
    }

    public override void Server_OnAnimationEvent(string eventName)
    {
        if (eventName != Event_Impact)
            return;

        Server_SpawnFissure();
        Rpc_OnImpact();

        // DAGDAG: Patunugin ang split sound sa lahat ng clients
        Rpc_PlaySplitSound();
    }

    [ClientRpc]
    void Rpc_OnImpact()
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.5f, impactShakeIntensity * 0.05f);
        }
    }

    // --- DAGDAG NA RPC PARA SA TUNOG ---
    [ClientRpc]
    void Rpc_PlaySplitSound()
    {
        if (enemySound != null && splitImpactClip != null)
        {
            enemySound.PlaySpecificAttack(splitImpactClip);
        }
    }
    // ----------------------------------

    [Server]
    void Server_SpawnFissure()
    {
        if (fissurePrefab == null)
            return;

        Transform origin = fissureOrigin != null ? fissureOrigin : transform;
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector3.forward;

        OngloFissure fissure = Instantiate(fissurePrefab, origin.position, Quaternion.LookRotation(dir));
        fissure.Server_Initialize(
            owner: boss != null ? boss.netIdentity : null,
            direction: dir,
            speed: fissureSpeed,
            maxDistance: fissureMaxDistance,
            eruptDelay: eruptDelay,
            eruptRadius: eruptRadius,
            damage: damage,
            playerLayer: playerLayer
        );

        NetworkServer.Spawn(fissure.gameObject);
    }

    public override void Server_Stop()
    {
        // Nothing persistent to stop in MVP.
    }
}