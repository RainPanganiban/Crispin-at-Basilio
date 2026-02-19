using UnityEngine;
using Mirror;

public abstract class BossMovementBase : NetworkBehaviour
{
    public abstract void Server_SetMovementEnabled(bool enabled);

    public abstract void Server_ApplyPhaseModifier(BossPhaseManager.BossPhase phase);
}

