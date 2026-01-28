using UnityEngine;
using UnityEngine.InputSystem;

public interface ICombatHandler
{
    void OnLightAttack(InputAction.CallbackContext context);
}
