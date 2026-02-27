using UnityEngine;
using Mirror;

public abstract class BossMovementBase : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnMovementSpeedChanged))]
    protected float syncedMovementSpeed;

    protected Animator animator;
    protected static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public abstract void Server_SetMovementEnabled(bool enabled);
    public abstract void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase);

    protected virtual void OnMovementSpeedChanged(float oldSpeed, float newSpeed)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(SpeedHash, newSpeed);
    }
}

