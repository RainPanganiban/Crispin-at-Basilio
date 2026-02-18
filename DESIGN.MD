# Crispin at Basilio — Game Design Document (Short AI Context)

## What is the Game

Crispin at Basilio is a multiplayer 3D action combat game built in Unity using Mirror networking.

Players connect through a lobby, enter a gameplay scene, and fight AI-controlled enemies in a combat arena.

The game focuses on:
- Responsive third-person combat
- Server-authoritative multiplayer
- Swarm-style enemy behavior
- Expandable boss systems

Mirror handles all networking and authority.

---

## Core Game Loop

1. Player joins lobby
2. Match starts (server loads gameplay scene)
3. Players spawn
4. Enemies spawn
5. Players fight enemies
6. Repeat combat loop until win/lose condition
7. Return to lobby or next phase

Primary loop during gameplay:
Move → Aim → Attack → Dodge → Take Damage → Repeat

---

## Game Mechanics

### Movement
- Third-person character controller
- Movement input using Unity Input System
- Rotation aligned with camera
- Jumping
- Running (planned stamina integration)
- Dodge / roll mechanic (planned)

### Camera
- ThirdPersonCamera script
- Mouse-controlled rotation
- Follows local player only

### Multiplayer Rules
- Server authoritative movement validation
- Server authoritative damage
- Mirror SyncVars for state syncing
- CustomNetworkManager controls scene transitions

---

## Combat System

Server authoritative.

Flow:
1. Client sends input
2. Server validates
3. Server applies damage
4. Server updates health
5. SyncVar replicates health state

Combat types:
- MeleeCombat
- RangedAttack (projectile-based)
- Projectile implements IDamageable interaction

All damageable entities implement IDamageable interface.

No combat logic inside Mirror source files.

---

## Enemy Mechanics

Base:
- EnemyAI handles navigation & base behavior
- EnemySync handles network synchronization
- EnemySpawner handles spawning

Swarm AI:
- EnemyBrain = high-level decision logic
- EnemyAggro = target selection logic
- EnemyAttackController = manages attack timing
- PunchAttack = specific attack implementation

Enemies:
- Use NavMesh movement
- Are server-controlled
- Attack closest or highest aggro player
- Apply damage through server validation

---

## Boss Mechanics (Planned / Expandable)

Boss system design:
- Multi-phase behavior
- Health-based phase transitions
- Distinct attack patterns
- Server authoritative abilities

Bosses will:
- Override base EnemyBrain logic
- Use specialized attack controllers
- Potentially use area-based attacks and telegraphs

---

## Architectural Rules

- Mirror internal code must never be modified.
- All gameplay logic must live outside Assets/Scripts/Network/Mirror/.
- Server decides damage and state changes.
- Clients only request actions.
- AI logic runs on server only.
- Health and critical state values use SyncVar.

