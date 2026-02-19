using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BossAttackManager : NetworkBehaviour
{
    [Header("Attack cadence")]
    public float globalCooldownBetweenAttacks = 0.5f;

    private readonly Dictionary<BaseAttack, float> nextReadyTimeByAttack = new Dictionary<BaseAttack, float>();
    private readonly List<BaseAttack> allowedAttacks = new List<BaseAttack>();

    private BaseAttack currentAttack;
    private float nextGlobalReadyTime;

    private BossController controller;

    void Awake()
    {
        controller = GetComponent<BossController>();
    }

    public override void OnStartServer()
    {
        foreach (var attack in GetComponents<BaseAttack>())
        {
            if (attack == null) continue;
            attack.Initialize(controller);
            nextReadyTimeByAttack[attack] = 0f;
        }

        nextGlobalReadyTime = 0f;
    }

    [Server]
    public void Server_SetAllowedAttacks(List<BaseAttack> attacks)
    {
        allowedAttacks.Clear();
        if (attacks == null) return;
        allowedAttacks.AddRange(attacks);
    }

    [Server]
    public bool Server_HasCurrentAttack()
    {
        return currentAttack != null;
    }

    [Server]
    public void Server_StopCurrentAttack()
    {
        if (currentAttack == null)
            return;

        currentAttack.Server_Stop();
        currentAttack = null;
    }

    [Server]
    public void Server_TrySelectAndStartAttack()
    {
        if (controller == null)
            return;

        if (controller.State != BossState.Idle)
            return;

        if (currentAttack != null)
            return;

        if (Time.time < nextGlobalReadyTime)
            return;

        BaseAttack selected = Server_SelectAttack_MvpRandom();
        if (selected == null)
            return;

        currentAttack = selected;
        nextGlobalReadyTime = Time.time + globalCooldownBetweenAttacks;

        if (nextReadyTimeByAttack.TryGetValue(selected, out float t) && Time.time < t)
        {
            currentAttack = null;
            return;
        }

        nextReadyTimeByAttack[selected] = Time.time + Mathf.Max(0f, selected.cooldown);

        controller.Server_BeginAttack(selected);
    }

    [Server]
    BaseAttack Server_SelectAttack_MvpRandom()
    {
        if (allowedAttacks.Count == 0)
            return null;

        List<BaseAttack> available = new List<BaseAttack>();
        foreach (var a in allowedAttacks)
        {
            if (a == null) continue;
            if (!a.Server_CanExecute()) continue;

            if (nextReadyTimeByAttack.TryGetValue(a, out float t) && Time.time < t)
                continue;

            available.Add(a);
        }

        if (available.Count == 0)
            return null;

        int index = Random.Range(0, available.Count);
        return available[index];
    }

    [Server]
    public void Server_OnAnimationEvent(string eventName)
    {
        if (currentAttack == null)
            return;

        currentAttack.Server_OnAnimationEvent(eventName);
    }

    [Server]
    public void Server_OnAttackAnimationComplete()
    {
        currentAttack = null;
    }
}

