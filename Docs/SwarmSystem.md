# Swarm AI System

The Swarm AI system is designed for coordinated group behavior in a multiplayer environment (Mirror). It handles target selection, movement coordination (surrounding), specialized attacks, and networked animation synchronization.

## Core Components

| Component | Responsibility |
|-----------|----------------|
| `EnemyAggro` | Selects targets from `PlayerMovement` components. Handles swarm-wide target sharing and spatial positioning (surround slots). |
| `EnemyBrain` | The base server-authoritative state machine (Idle, Chasing, Attacking, Repositioning). Controls locomotion and state transitions. |
| `TiktikBrain` | Specialized brain for flying enemies. Handles hover-patrol, swoop diving, and vertical orbiting behavior. |
| `EnemyAttackController` | Manages the selection and execution of modular `EnemyAttack` scripts. |
| `EnemyAnimationController` | Syncs walking speed across the network by calculating displacement (works for both the Server Agent and Client visuals). |

## Movement & Coordination

### 1. Spatial Surrounding (`EnemyAggro.GetSurroundPosition`)
Enemies don't just run directly at the player. They calculate a "slot" on a circle around the target.
- **Angle Alignment**: Slots are distributed based on the number of enemies currently targeting the same player.
- **Jitter**: Adds slight randomness to prevent robotic, perfect circles.
- **Collision Safety**: Uses `NavMesh.SamplePosition` to ensure the target slot isn't inside a wall.

### 2. Manual Facing Logic
To prevent the "gliding" look where enemies look where they walk instead of at their target:
- `NavMeshAgent.updateRotation = false` is enforced.
- `FaceTarget()` manually rotates the enemy towards the target body while ignoring vertical tilt (kept upright).

## Combat & Attacks

### Modular Attack System
All attacks inherit from `EnemyAttack` and follow a "Trigger-Execution-Notify" flow:
1. `OnExecute()`: Server triggers the `NetworkAnimator` trigger.
2. **Animation Playback**: Syncs to all clients.
3. **Animation Event**: The FBX clip fires a function (e.g., `DealDamageEvent` or `FireProjectileEvent`) on the server.
4. **Server Validation**: The server performs the overlap check/projectile spawn.

### Specialized Logic
- **Multi-Hit Guard**: `ChargePunchAttack` uses a `HashSet<GameObject>` to ensure a single dash can only damage a specific player once per attack.
- **Target Child Indexing**: `EnemyAggro` can be configured via `targetChildName` to have enemies look at a specific body part (e.g., "Model") instead of the feet/root.

## Network Synchronization

- **Animations**: Handles locomotion speed via `EnemyAnimationController`. Triggers attacks via `NetworkAnimator`.
- **Position/Rotation**: Uses Mirror's `NetworkTransform`.
- **Attack States**: Syncs start/end times via `SyncVar` where necessary to ensure UI health bars and visual effects align.

## How to Add a New Swarm Enemy (Checklist)

Follow these steps to create a new enemy using this system:

1.  **Prefab Setup**:
    - Add `Network Identity` and `Network Transform` (with *Sync Rotation* checked).
    - Add `NavMesh Agent` (ensure `updateRotation = false`).
    - Add `EnemyAggro`: Set `Target Child Name` to "Model" or your model object's name.
    - Add `EnemyBrain` (Ground) or `TiktikBrain` (Flyer).
    - Add `EnemyAnimationController`: Assign your `Animator` and the root transform.

2.  **Animation Setup**:
    - Create a new **Animator Controller**.
    - Implement a `Speed` float parameter for the locomotion blend tree.
    - Set up **Triggers** for attacks (e.g., "AttackTrigger").
    - Add `Network Animator` to the prefab and link the Animator.

3.  **Combat Integration**:
    - Add `EnemyAttackController`.
    - Create/Add a script inheriting from `EnemyAttack` (e.g., `ProjectileAttack` or a custom melee script).
    - Add the attack script to the `Attack Controller` list.

---

## How to Add a New Attack (Pattern)

This project uses the **Server-Side Animation Event Pattern** for 100% sync reliability:

1.  **The Script**: Inherit from `EnemyAttack`.
    - In `OnExecute()`, just call `networkAnimator.SetTrigger("YourAttack")`.
    - Create a public `void HandleAttackEvent()` method marked with `[ServerCallback]`.
2.  **The FBX Event**:
    - Select the attack clip in the FBX Importer -> Animation tab.
    - Add an **Animation Event** at the impact frame.
    - Set Function name to `HandleAttackEvent`.
3.  **The Logic**: Inside `HandleAttackEvent()`, perform your `Physics.OverlapSphere` or `Instantiate(projectile)`. This ensures damage only happens when the animation visually hits.

---

## Technical Gotchas (AI Mental Model)

- **Root Snap**: `EnemyAggro` ignores tags and searches for `PlayerMovement`. If an enemy looks at the camera, ensure your Camera object doesn't have a `PlayerMovement` script or that the Hierarchy is `Player (Root) -> Model`.
- **Client Speed**: Clients calculate animation speed via `displacement / Time.deltaTime`. If an enemy slides, check the `EnemyAnimationController` smoothing (`Mathf.Lerp`) value.
- **Server Truth**: All damage is applied by the server. If a client sees an effect but no health changes, ensure the Animation Event is properly calling a server-bound method.

