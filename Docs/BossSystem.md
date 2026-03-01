# Boss System (Mirror, Server-Authoritative) — Setup + Architecture

## Status in this repo (Feb 2026)

This document describes the Boss System architecture for *Crispin at Basilio*.

As of now, the **Boss core framework is implemented** under:
- `Assets/Scripts/Boss/Core/`

Boss-specific content (attacks + movement + phase tuning) is still up to you to author per boss.

If something in code differs from this doc later, treat that as a bug in either the docs or the implementation and reconcile it.

---

## What the Boss System is

The Boss System is a **modular, server-authoritative, multiplayer-ready** combat architecture for **arena encounters** built in **Unity 6** using **Mirror** networking.

Bosses are **large, multi-phase combat entities** with:
- Phase-driven difficulty escalation
- Pattern-recognition focused attack cycles
- Animation-timed (telegraphed) execution
- Clean modular expansion for future bosses

**Non-goals**:
- Bosses are not swarm enemies.
- Bosses do not “freestyle” with chaotic behavior; they run structured cycles.

---

## Core principles (non-negotiables)

- **Server authority**: the server decides attacks, phases, damage, projectiles, AI, and death.
- **Animation is timing**: animations telegraph and define *when* attack logic fires; the server decides *what* happens.
- **No Mirror edits**: Mirror internal code must never be modified.
- **No gameplay logic under Mirror**: all gameplay scripts live outside `Assets/Scripts/Network/Mirror/`.
- **Boss content is modular**: bosses swap attacks/movement/config without changing core.

---

## Boss prefab layout (required components)

Each boss prefab contains (minimum):

- **Networking**
  - `NetworkIdentity`
  - `NetworkTransform` (or an approved alternative) for position sync
- **Core boss scripts (reused across all bosses)**
  - `BossController` (`NetworkBehaviour`)
  - `BossHealth` (`NetworkBehaviour`)
  - `BossPhaseManager`
  - `BossAttackManager`
  - `BossAnimationRelay`
- **Boss-specific scripts (varies by boss)**
  - A movement script derived from `BossMovementBase` (unique per boss: e.g. `TikbalangMovement`)
  - Attack scripts (unique per boss, each derives from `BaseAttack`)
- **Unity components**
  - `Animator`
  - Colliders (hitbox + hurtbox as needed)
  - `AudioSource`
  - VFX spawn point transforms (empty child objects are fine)

**Rule**: core scripts are reused across bosses; only **movement**, **attacks**, and **phase configuration** change per boss.

---

## High-level fight loop

1. Fight starts (server)
2. Boss enters **Idle**
3. `BossAttackManager` selects an attack from the **current phase**
4. Server triggers the animator (`SetTrigger(...)`)
5. Animation plays on all clients
6. Animation event fires (on each client + host)
7. `BossAnimationRelay` forwards the event to the server-side boss logic
8. The current attack executes server-only logic
9. Cooldown begins
10. Repeat until a phase threshold is reached
11. Phase transition occurs
12. Final phase ends with death

---

## Boss state system

`BossController` is the source of truth for boss state.

### States
- **Idle**: allowed to select attacks, allowed to move.
- **Attacking**: exactly one attack active; movement may be locked depending on the attack.
- **Transitioning**: no attacks; play transition animation; swap phase content; apply phase changes.
- **Dead**: no movement; no attacks; collider disable + rewards.

### State rules (enforced server-side)
- Only **one attack** can be active at a time.
- No attack starts during **Transitioning**.
- No movement during **Dead**.
- All state changes occur **server-side**.
- **Failsafe**: If stuck in **Attacking** for too long (e.g., missed animation event), it automatically forces a reset to **Idle** after a 10-second timeout.

---

## Health system (server authoritative)

`BossHealth` responsibilities (`IDamageable`):
- Store `maxHealth`
- SyncVar `currentHealth`
- Provide `TakeDamage(float, Transform)` via `IDamageable` interface, which routes to `Server_TakeDamage(...)`
- Notify phase thresholds (based on percentage)
- Trigger death when HP reaches 0

### Health flow

`Server_TakeDamage` → reduce HP → check threshold(s) → notify `BossPhaseManager` → SyncVar replication to clients.

**Important separation**:
- `BossHealth` **never** changes phases directly.
- It only **notifies** `BossPhaseManager` when thresholds are crossed.

---

## Phase system (multi-phase behavior)

`BossPhaseManager` controls multi-phase behavior.

Each phase contains:
- **Health threshold percentage** (entry condition)
- **Allowed attacks list**
- **Transition animation trigger**
- Optional **movement modifier**
- Optional **special behavior flag** (boss-specific meaning)

### Phase transition flow

Threshold reached → `BossController.State = Transitioning` → play transition animation → swap attack list → apply movement modifier → return to `Idle`.

**Phases are defined per boss** (config/data), not hard-coded inside the controller.

---

## Attack system

`BossAttackManager` responsibilities:
- Attack selection
- Cooldown enforcement
- Prevent overlapping attacks
- Own the “current attack” concept
- Call attack execution (server-side)

### Attack selection (MVP)
- Randomly select from the current phase’s allowed attacks.

Later extensions (when needed):
- Weighted random
- Pattern playlists / cycles
- Cooldown groups (e.g. “big attacks” share a group cooldown)
- “Counter” attacks based on player position/behavior (still server-side)

---

## Attack architecture (`BaseAttack`)

All attacks inherit from `BaseAttack`.

Each attack defines:
- `attackName`
- `cooldown`
- `maxRange` (prevents attacks from executing if player is too far)
- `animationTriggerName`
- `requiresMovementLock`

Each attack implements server-only methods:
- `Server_Execute()`
- `Server_OnAnimationEvent(eventName)`
- `Server_Stop()`

Examples (boss content):
- `EyeLaserAttack`
- `BoulderThrowAttack`
- `StormCallAttack`
- `SummonMinionsAttack`

**Rule**: attack logic runs on the server only.

---

## Animation-driven attack execution (the critical pipeline)

All attacks are driven by animation events so timing is consistent and telegraphed.

### Intended flow
1. Server selects attack
2. Server triggers `BossController.Server_PlayTrigger(...)`
3. Animation plays on all clients
4. Animation event fires at a keyframe
5. `BossAnimationRelay` receives the event
6. Relay forwards the event to server-side code
7. Current attack handles `Server_OnAnimationEvent(eventName)` and executes logic on server

### Why the relay exists
Unity animation events fire where the animation runs (clients too). Without a relay/guard, you risk:
- client-side double execution
- desync / cheating surfaces
- host-mode running twice if not careful

**Rule**: animation events are allowed to *signal timing*, but only the server is allowed to *apply gameplay results*.

---

## Movement system (boss-specific)

Each boss has a unique movement script (examples by intent):
- `TikbalangMovement`: fast repositioning / pressure
- `BungisngisMovement`: heavy melee space control
- `KapreMovement`: zoning and space-denial positioning

Movement responsibilities:
- Arena positioning
- Dashes / teleports
- Phase-based movement changes (via modifiers from `BossPhaseManager`)

**Hard rule**: Movement never contains attack logic.

**Hard rule**: Movement never contains attack logic.

Networking & Sync:
- Movement extends `BossMovementBase`, which holds a `SyncVar` for `syncedMovementSpeed` to ensure locomotion animations play properly on clients.
- Movement logic runs server-side using **physics-safe** methods (e.g., `Rigidbody.MovePosition` and `Rigidbody.MoveRotation`) on a `Kinematic` rigidbody.
- Standard `NetworkTransform` replicates position/rotation to clients with `Interpolation` enabled for smooth movement without fighting the animator's root motion (which should be disabled).

---

## Multiplayer authority rules (Mirror)

### Server must own
- Attack selection
- Damage application
- Phase switching
- Projectile and minion spawning
- AI logic / target selection
- Death resolution + rewards

### Clients may do
- Render boss animations and VFX
- Display UI based on SyncVars
- Play audio/VFX triggered by server messages

### Clients must never do
- Decide boss behavior
- Apply damage
- Trigger phase transitions

---

## Boss death flow

HP reaches 0 → State = Dead → stop movement → stop attacks → play death animation → trigger VFX → disable colliders → trigger rewards.

All death logic runs server-side.

---

## How to set up a boss (step-by-step)

This is the practical checklist you follow in Unity to create a boss prefab that fits the architecture.

### 1) Create a new boss prefab
- Create a new prefab under something like `Assets/Prefabs/Bosses/<BossName>/`.
- Ensure the root has an `Animator` and appropriate colliders.

### 2) Add Mirror networking components
- Add `NetworkIdentity`.
- Add `NetworkTransform` (or the chosen network movement sync component).

### 3) Add core boss scripts
Attach the core scripts:
- `BossController`
- `BossHealth`
- `BossPhaseManager`
- `BossAttackManager`
- `BossAnimationRelay`

`BossController` will auto-wire references if left empty, but it’s best to assign them explicitly.

### 4) Add boss-specific movement
- Add your boss movement component derived from `BossMovementBase` (e.g. `TikbalangMovement : BossMovementBase`).
- Ensure it supports being enabled/disabled (for Dead) and applies phase modifiers via `Server_ApplyPhaseModifier(...)`.

### 5) Create boss attacks
For each attack:
- Create a new script deriving from `BaseAttack`.
- Define the `animationTriggerName` (must match Animator trigger parameter).
- Implement `Server_Execute`, `Server_OnAnimationEvent`, and `Server_Stop`.

### 6) Configure phases
Define phases for this boss:
- Thresholds (e.g. 70%, 40%, 0%)
- Allowed attacks per phase
- Transition trigger names
- Movement modifiers / special flags

### 7) Configure Animator
In the boss Animator Controller:
- Add trigger parameters for each attack (names match `animationTriggerName`).
- Add trigger parameters for phase transitions (names match phase transition trigger).
- Ensure transitions do not interrupt each other in a way that can cause overlapping triggers.

### 8) Add animation events
In each attack animation clip:
- Add an animation event at the telegraphed “hit frame”.
- The event should call `BossAnimationRelay.AnimationEvent(string eventName)` with an event name (e.g. `AttackHit`, `SpawnProjectile`, `LaserStart`, `LaserEnd`).
- At the end of the attack animation, add an event that calls `BossAnimationRelay.AttackAnimationComplete()` to release the boss back to `Idle`.

### 9) Multiplayer test checklist (minimum)
- Host + 1 client: verify attacks only apply damage once.
- Dedicated server + 2 clients (if you run one): verify state/phase/health are consistent.
- Verify that disabling the boss (Dead) stops both movement and attacks.

---

## Recommended project folder layout (for implementation)

This keeps boss gameplay separate from Mirror and keeps modular boss content clean:

- `Assets/Scripts/Boss/Core/`
  Core reusable scripts: controller, health, phase manager, attack manager, animation relay, base types.
- `Assets/Scripts/Boss/Attacks/`
  Shared attack utilities (optional) and base attack.
- `Assets/Scripts/Boss/Bosses/<BossName>/`
  Boss-specific movement, attacks, and phase config.

If you already have conventions you prefer, keep the key rule: **do not place gameplay boss scripts under** `Assets/Scripts/Network/Mirror/`.

---

## Common pitfalls (avoid these)

- **Client executes attack logic**: animation events fire on clients; guard so only server applies gameplay results.
- **Overlapping attacks**: ensure `BossAttackManager` enforces “one current attack”.
- **Phase logic hardcoded in controller**: phases must be data/config driven and owned by `BossPhaseManager`.
- **Movement script contains attack logic**: keep responsibilities separated.
- **NetworkTransform fighting server movement**: ensure only server writes transforms; clients should be observers.

---

## Expansion rules (adding bosses safely)

To add a new boss:
1. Create new prefab
2. Add core boss scripts
3. Create unique movement script
4. Create attack scripts
5. Configure phases

You must NOT modify core scripts (`BossController`, `BossHealth`, `BossAttackManager`). If you feel you need to, it means the architecture needs better extension points (composition/config), not core edits.

