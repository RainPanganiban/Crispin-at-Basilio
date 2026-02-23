# Crispin at Basilio — Project Context

**Engine:** Unity 6  
**Networking:** Mirror  
**Type:** Cooperative multiplayer action game

This document centralizes the high-level context, structure, and roadmap for the project. It replaces the previous `INDEX.md` and `ROADMAP.md` in the project root.

---

## High-Level Overview

- Multiplayer action game built in Unity with Mirror networking
- Lobby → Overworld Hub → Gameplay scene flow with character persistence
- Cuphead-inspired overworld with level nodes, countdown gates, and a shop
- Swarm enemy AI system with coordinated attacks and repositioning
- Server-authoritative Boss System framework for arena bosses

---

## Key Project Docs

- **Project context (this file):** `project-context.md`
- **Boss System architecture:** `Docs/BossSystem.md`
- **Onglo boss design & setup:** `Docs/Onglo.md`
- **General repository overview:** `README.md`

Use this file as the main entry point; follow links to deeper docs when working on specific systems.

---

## Root Structure

| Path | Purpose |
|------|--------|
| `Assets/` | Game content: scripts, prefabs, scenes, models, UI |
| `Packages/` | Unity packages & dependencies |
| `ProjectSettings/` | Unity project config (PlayerSettings, etc.) |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/` | Unity-generated (typically not versioned) |
| `.vscode/` | Editor settings |
| `*.sln`, `*.csproj` | Solution and C# projects (Unity + Mirror assemblies) |

---

## Assets Layout

| Folder | Contents |
|--------|----------|
| `Assets/Prefabs/` | Prefabs (e.g. `Player`) |
| `Assets/Resources/` | TextMesh Pro and other runtime resources |
| `Assets/Scenes/` | Unity scenes |
| `Assets/Scripts/` | All C# scripts (game + Mirror) |
| `Assets/Enemy Models/`, `Assets/Player Models/` | Models and materials |
| `Assets/UI Elements/` | UI assets |
| `Assets/Settings/` | Project settings assets |
| `Assets/ScriptTemplates/` | Script templates |

---

## Scenes

| Scene | Path |
|-------|------|
| Main Menu | `Assets/Scenes/Main Menu.unity` |
| Lobby | `Assets/Scenes/Lobby.unity` |
| Mirror Networking (Overworld Hub) | `Assets/Scenes/Mirror Networking.unity` |

Subfolder: `Assets/Scenes/Mirror Networking/` (e.g. NavMesh assets).

---

## Core Gameplay Scripts

Scripts under `Assets/Scripts/` that are **not** under `Network/Mirror/` are project-specific gameplay logic.

### Player — `Assets/Scripts/Player/`

| Script | Role |
|--------|------|
| `PlayerMovement.cs` | Movement and input |
| `CharacterAnimationController` (base) | Shared animation driver (locomotion/NetworkAnimator) |
| `BasilioAnimation.cs` | Basilio animation (inherits base) |
| `CrispinAnimation.cs` | Crispin animation (inherits base) |
| `PlayerStats.cs` | Player stats data |
| `PlayerStatsManager.cs` | Manages stats (e.g. sync/UI) |
| `ThirdPersonCamera.cs` | Third-person camera |
| `PlayerAim.cs` | Aiming logic |
| `MeleeCombat.cs` | Melee attacks |
| `RangedAttack.cs` | Ranged attacks |

### Player UI — `Assets/Scripts/Player UI/`

| Script | Role |
|--------|------|
| `PlayerUI.cs` | Main player UI |
| `PlayerIdentity.cs` | Player identity (e.g. name/ID) |
| `NameTag.cs` | Name tag display |

### Enemy — `Assets/Scripts/Enemy/`

| Script | Role |
|--------|------|
| `EnemyAI.cs` | Base enemy AI |
| `EnemySync.cs` | Network sync for enemies |
| `EnemySpawner.cs` | Spawns enemies |

### Swarm Enemy AI — `Assets/Scripts/Enemy/Swarm Enemy AI/`

| Script | Role |
|--------|------|
| `EnemyBrain.cs` | High-level swarm AI behavior |
| `EnemyAggro.cs` | Aggro / target selection |
| `EnemyAttack.cs` | Attack behavior |
| `EnemyAttackController.cs` | Coordinates attacks |
| `PunchAttack.cs` | Basic melee punch attack |
| `ChargePunchAttack.cs` | Charge-in variant of the punch attack |
| `ProjectileAttack.cs` | Ranged projectile attack behavior |
| `EnemyProjectile.cs` | Projectile logic and hit resolution |
| `TiktikBrain.cs` | Specialized brain for Tiktik enemy variant |
| `EnemyHealth.cs` | Enemy health |
| `EnemyHealthUI.cs` | Enemy health bar / UI |

### Main Menu / Lobby — `Assets/Scripts/Main Menu/`

| Script | Role |
|--------|------|
| `CustomNetworkManager.cs` | Custom Mirror NetworkManager with character persistence across scenes |
| `LobbyUIManager.cs` | Lobby UI |
| `LobbyAutoAddPlayers.cs` | Auto-add players in lobby |
| `LobbyPlayer.cs` | Lobby player representation |
| `MenuUIManager.cs` | Main menu UI |
| `NetworkDebugClient.cs` | Debug networking client |

### Overworld Hub — `Assets/Scripts/Overworld/`

| Script | Role |
|--------|------|
| `OverworldManager.cs` | Manages overworld scene and level transitions |
| `LevelProgressionManager.cs` | Server-authoritative level unlock/completion tracking |
| `LevelNode.cs` | Physical level node trigger with cooperative countdown system |
| `LevelNodeUI.cs` | Client-side UI for countdown and unlock visuals |
| `ShopManager.cs` | Server-authoritative shop zone and purchase validation |
| `OverworldShopUI.cs` | Shop UI controller (open/close with cursor management) |
| `OverworldShopClient.cs` | Client-side shop interactions and currency display |
| `PlayerCurrency.cs` | Syncs player currency (coins) via SyncVar |

### System / Shared — `Assets/Scripts/System/`

| Script | Role |
|--------|------|
| `IDamageable.cs` | Interface for damageable objects |
| `ICombatHandler.cs` | Interface for combat |
| `Projectile.cs` | Projectile behavior |

### Boss System — `Assets/Scripts/Boss/`

- Core framework lives under `Assets/Scripts/Boss/Core/`.
- Boss-specific content (movement, attacks, tuning) lives under `Assets/Scripts/Boss/Bosses/`.
- Onglo boss example content lives under `Assets/Scripts/Boss/Bosses/Onglo/` and is documented in `Docs/Onglo.md`.

The Boss System is a modular, server-authoritative arena boss framework. Core components include:

- `BossController`
- `BossHealth`
- `BossPhaseManager`
- `BossAttackManager`
- `BossAnimationRelay`

See `Docs/BossSystem.md` for full architecture details and setup rules.

### Onglo Boss — `Assets/Scripts/Boss/Bosses/Onglo/`

| Script | Role |
|--------|------|
| `OngloMovement.cs` | Controls Onglo’s arena movement and facing |
| `BoulderBarrageAttack.cs` | Multi-projectile boulder barrage attack |
| `EarthshakerStompAttack.cs` | Close-range stomp that sends shockwaves |
| `GroundSplitAttack.cs` | Creates fissures along the arena ground |
| `OngloBoulder.cs` | Boulder projectile behavior and collisions |
| `OngloFissure.cs` | Fissure hazard spawned by ground-split attacks |
| `OngloShockwaveRing.cs` | Expanding shockwave ring visual and damage logic |

---

## Network Stack (Mirror)

Third-party/networking code lives under **`Assets/Scripts/Network/Mirror/`**:

- **Core** — Mirror core (Batching, LagCompensation, Prediction, SnapshotInterpolation, Threading, Tools)
- **Components** — NetworkTransform, NetworkAnimator, NetworkRigidbody, Discovery, InterestManagement, LagCompensation, Profiling, Lobby/Room managers
- **Transports** — Telepathy, SimpleWeb, Threaded
- **Authenticators** — Basic, Device, Timeout
- **Editor** — Weaver, Icon
- **Examples** — AdditiveLevels, Basic, Chat, Discovery, etc.
- **Hosting**, **Plugins**, **Presets**, **CompilerSymbols**

Game networking entry points:

- `CustomNetworkManager.cs`
- `NetworkDebugClient.cs`
- Mirror’s `NetworkManager` and related components attached to scenes and prefabs

---

## Player Animation System

- Architecture:
  - Shared base controller `CharacterAnimationController` updates locomotion parameters and uses `NetworkAnimator` when present.
  - Character-specific controllers (`BasilioAnimation`, `CrispinAnimation`) inherit from the base.
  - Networking via Mirror `NetworkAnimator` with client authority for player-owned characters.
- Animator parameters:
  - Floats: `Speed` (0–1, normalized), `Direction` (optional; currently 0)
  - Bools: `IsGrounded`
  - Triggers: `Jump`, `Roll`, `Attack` (expandable per character)
- Setup (per player prefab):
  - Ensure an `Animator` component exists and is assigned a controller.
  - Add `NetworkAnimator` (clientAuthority = true), point its `animator` to the same `Animator`.
  - Ensure the prefab has `CharacterAnimationController` derivative attached (Basilio/Crispin).
  - Player scripts now call `GetComponent<CharacterAnimationController>()` for roll/jump/attack triggers.

Animator state guidance:
- Base Layer: `Locomotion` (blend by `Speed`), `Jump`, `Roll`, all return to locomotion via exit time.
- Optional upper-body layer for attacks if needed later.

Multiplayer safety:
- Locomotion params are set on the owning client and synced by `NetworkAnimator`.
- Discrete actions use triggers through `NetworkAnimator.SetTrigger(...)` when available.

## Input

- `Assets/Scripts/PlayerControls.inputactions` — Input Actions asset for player input (movement, combat, etc.).

- `Assets/Scripts/PlayerControls.inputactions` — Input Actions asset for player input (movement, combat, etc.).

---

## Script Count (Approx.)

- Total `.cs` under `Assets/`: ~602  
- Game-specific (non-Mirror): ~33 scripts in Player, Player UI, Enemy, Main Menu, Overworld, System

---

## Current Feature Status

### DONE (Stable / Working)

- Mirror multiplayer integration
- `CustomNetworkManager`
- Lobby system
- Scene transitions (Lobby → Overworld → Gameplay)
- Player spawning with character persistence across scenes
- `PlayerMovement`
- `ThirdPersonCamera`
- `MeleeCombat`
- `RangedAttack`
- Projectile system
- `IDamageable` interface
- Enemy base AI
- `EnemySync`
- `EnemySpawner`
- Basic Swarm AI structure:
  - `EnemyBrain`
  - `EnemyAggro`
  - `EnemyAttack`
  - `EnemyAttackController`
  - `PunchAttack`
- Enemy health system
- Enemy health UI

**Overworld Hub System**

- Cuphead-inspired explorable overworld
- Level nodes with cooperative countdown (both players required)
- Server-authoritative level progression and unlock system
- Shop system with server-validated purchases
- Character persistence across all scenes (Lobby → Overworld → Gameplay)
- Currency system (`PlayerCurrency` component)
- Countdown UI with billboarding
- Shop UI with cursor unlock/lock management

**Swarm Enemy AI Improvements**

- Fixed attack completion callback wiring (`EnemyAttack` → `EnemyBrain.OnAttackFinished()`)
- Improved reposition behavior after attack:
  - NavMesh-validated back-off positions (prevents backing through walls)
  - Perpendicular jitter to prevent swarm clustering
  - Timeout handling to prevent stuck repositioning
- Tuned stopping distance and attack range logic:
  - Auto-sync `NavMeshAgent.stoppingDistance` with attack range
  - `GetSurroundPosition()` respects attack range bounds
  - Fixed surround radius vs attack range mismatch
- Improved aggro switching logic:
  - Better target validation (handles destroyed/null targets)
  - Reduced chaos threshold (15% vs 25%)
  - Chaos switches only when targets are similarly distant
  - Reduced switch distance threshold (1.5m vs 2m)
  - Reduced lock time (1.5s vs 2s)

**Boss System Core**

- Server-authoritative boss framework implemented under `Assets/Scripts/Boss/Core/`:
  - `BossController` (state management)
  - `BossHealth` (SyncVar health and damage pipeline)
  - `BossPhaseManager` (phase thresholds and phase config)
  - `BossAttackManager` (attack selection and cooldowns)
  - `BossAnimationRelay` (animation-event relay to server logic)
- Architecture and setup rules documented in `Docs/BossSystem.md`.

---

### IN PROGRESS

- Swarm Enemy AI tuning (continuing):
  - Attack timing behavior refinement
  - Testing and validating recent improvements
- Improving attack cooldown consistency
- Refining server-authoritative movement interactions

---

### NEXT (Immediate Focus)

- Test and validate swarm AI improvements:
  - Playtest swarm behavior with multiple enemies
  - Verify reposition timing feels natural
  - Confirm aggro switching is neither too chaotic nor too sticky
  - Tune exposed parameters based on feel (chaos chance, lock times, distances)
- Attack cooldown consistency improvements:
  - Ensure cooldowns sync properly across the network
  - Fix any timing discrepancies between client and server
  - Add visual feedback for cooldown state (if applicable)
- Consider adding more swarm attack variety:
  - Review existing attacks (e.g. `PunchAttack`, `ChargePunchAttack`, `ProjectileAttack`)
  - Add combo attacks or area attacks
  - Introduce varied attack ranges and patterns for swarm diversity

---

### SUGGESTED NEXT STEPS (After Testing)

Based on the completed swarm AI improvements and the implemented Boss System core, consider these priorities:

1. **If swarm AI feels good after testing** → Move to Boss System content (Phase 2+)
   - Swarm enemies are polished enough to serve as a gameplay baseline.
   - Boss framework is ready; focus shifts to boss-specific content.
   - Start with one vertical-slice boss to prove out the system.
2. **If swarm AI needs more polish** → Continue iteration:
   - Add attack variety (combo attacks, area effects)
   - Implement enemy type variations (fast/agile vs tanky/slow)
   - Add enemy formations and group tactics
3. **Player-facing features** → Dodge/roll mechanic:
   - Complements improved swarm AI (more dynamic combat)
   - Gives players defensive options against coordinated swarms
   - Stamina system ties into combat flow
4. **Quality of life** → Visual debug tools for swarm AI:
   - Gizmos for aggro radius, attack range, and surround positions
   - On-screen state indicators (chasing/attacking/repositioning)
   - Helps with tuning and debugging

---

### LATER / BACKLOG

- Dodge / roll mechanic with stamina cost and damage invincibility
- Player Animation Syncing


**Boss System Content Track**

- Phase 2 — Vertical slice boss (1 boss proves the system):
  - Create 1 test boss prefab in an arena scene
  - Implement 1 movement script (boss-specific)
  - Implement at least 2 attacks (animation-event driven)
  - Implement phase transitions (at least 2 phases)
  - Add basic telegraph VFX spawn points and audio hooks
- Phase 3 — Multiplayer hardening:
  - Verify server-only execution (no double-hit in host mode)
  - Dedicated server + 2 clients test pass (health, phases, death sync)
  - Validate projectile and minion spawns are server-owned and replicated correctly
  - Add guardrails for invalid client inputs and event spoofing
- Phase 4 — Debug and iteration tools:
  - Visual debug for boss state, phase, and attack cooldown
  - Logging toggles for boss decisions (server-only)
  - Gizmos for hit areas, targeting, and arena bounds

**Other Backlog Items**

- Overworld Hub enhancements (visual polish, more shop items, optional levels)
- Player class specialization
- Animation polish
- Sound manager system
- Performance optimization pass
- Advanced enemy types
- UI polish
- Match win / lose conditions
- Replayability features

---

## Notes

- Treat this file as the single source of truth for high-level project context.
- Update it when major systems are added, refactored, or removed.

