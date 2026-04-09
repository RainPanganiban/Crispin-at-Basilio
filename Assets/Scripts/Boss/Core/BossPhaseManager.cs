using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class BossPhaseManager : NetworkBehaviour
{
    [Serializable]
    public class BossPhase
    {
        [Range(0f, 1f)]
        public float enterAtHealthPercent = 1f;

        [Header("Combat")]
        public List<BaseAttack> allowedAttacks = new List<BaseAttack>();

        [Header("Transition")]
        public string transitionTriggerName = "";

        // --- DAGDAG NA SOUND PARA SA PHASE ---
        [Tooltip("Tunog na maririnig kapag pumasok sa phase na ito.")]
        public AudioClip transitionSFX;
        // --------------------------------------

        [Header("Optional modifiers")]
        public float movementSpeedMultiplier = 1f;
        public bool specialBehaviorFlag = false;
    }

    [Header("Phases (highest -> lowest threshold recommended)")]
    public List<BossPhase> phases = new List<BossPhase>();

    [SyncVar]
    private int currentPhaseIndex = 0;

    public event Action<int> OnPhaseChanged;

    private BossAttackManager attackManager;
    private BossMovementBase movement;
    private BossController controller;

    // REFERENCE PARA SA SOUND
    private EnemySoundManager enemySound;

    public override void OnStartServer()
    {
        attackManager = GetComponent<BossAttackManager>();
        movement = GetComponent<BossMovementBase>();
        controller = GetComponent<BossController>();

        // DAGDAG: Kunin ang EnemySoundManager
        enemySound = GetComponent<EnemySoundManager>();

        currentPhaseIndex = 0;
        Server_ApplyCurrentPhase();
    }

    public int GetCurrentPhaseIndex() => currentPhaseIndex;

    public BossPhase GetCurrentPhase()
    {
        if (phases == null || phases.Count == 0)
            return null;
        if (currentPhaseIndex < 0 || currentPhaseIndex >= phases.Count)
            return null;
        return phases[currentPhaseIndex];
    }

    [Server]
    public void Server_OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (controller != null && controller.State == BossState.Dead)
            return;

        if (phases == null || phases.Count == 0)
            return;

        float pct = maxHealth <= 0f ? 0f : (currentHealth / maxHealth);

        int newIndex = currentPhaseIndex;
        for (int i = 0; i < phases.Count; i++)
        {
            if (pct <= phases[i].enterAtHealthPercent)
                newIndex = i;
        }

        if (newIndex != currentPhaseIndex)
            Server_TransitionToPhase(newIndex);
    }

    [Server]
    void Server_TransitionToPhase(int newIndex)
    {
        if (controller == null)
            return;

        controller.Server_BeginPhaseTransition();

        currentPhaseIndex = Mathf.Clamp(newIndex, 0, phases.Count - 1);

        BossPhase phase = GetCurrentPhase();

        if (phase != null)
        {
            // 1. Play Animation Trigger
            if (!string.IsNullOrWhiteSpace(phase.transitionTriggerName))
                controller.Server_PlayTrigger(phase.transitionTriggerName);

            // 2. DAGDAG: Patunugin ang transition SFX sa lahat ng clients
            if (phase.transitionSFX != null)
                Rpc_PlayPhaseSFX(newIndex);
        }

        Server_ApplyCurrentPhase();

        OnPhaseChanged?.Invoke(currentPhaseIndex);

        controller.Server_EndPhaseTransition();
    }

    // --- DAGDAG NA RPC PARA SA PHASE SOUND ---
    [ClientRpc]
    void Rpc_PlayPhaseSFX(int phaseIndex)
    {
        // Siguraduhin na valid ang index at may sound manager
        if (enemySound != null && phaseIndex >= 0 && phaseIndex < phases.Count)
        {
            AudioClip clip = phases[phaseIndex].transitionSFX;
            if (clip != null)
            {
                // Gagamitin natin ang PlaySpecificAttack para sa transition sound
                enemySound.PlaySpecificAttack(clip);
            }
        }
    }
    // -----------------------------------------

    [Server]
    void Server_ApplyCurrentPhase()
    {
        BossPhase phase = GetCurrentPhase();
        if (phase == null)
            return;

        if (attackManager != null)
            attackManager.Server_SetAllowedAttacks(phase.allowedAttacks);

        if (movement != null)
            movement.Server_ApplyPhaseModifier(phase);
    }
}