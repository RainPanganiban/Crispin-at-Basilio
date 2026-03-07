# Crispin at Basilio

## Overview

**Crispin at Basilio** is a multiplayer 3D action combat game built in **Unity 6 (6000.0.62f1)** using **Mirror networking**.

**Scene Flow:** Lobby → Overworld Hub → Gameplay (with player persistence)

**Core Pillars:**

* Server-authoritative multiplayer
* Responsive third-person combat
* Swarm-based enemy AI
* Modular boss system

---

## AI-FRIENDLY PROJECT SUMMARY

If you are an AI (or developer) working in this repo, here is the **quick mental model**:

* **Server owns truth** (damage, AI, health, phases, spawning)
* **Clients send input only**
* **All gameplay code is outside Mirror/**
* **AI runs server-side only**
* **Everything damageable implements `IDamageable`**

**Main systems:**

* Player (movement, combat, animation)
* Enemy (base AI + swarm logic)
* Overworld (progression, shop, nodes)
* Boss System (modular, phase-based)
* Level Progression (defeat → overworld, persistent health/coins)

---

## CORE GAME LOOP

1. Join lobby
2. Load gameplay
3. Spawn players & enemies
4. Combat loop
5. Win/lose
6. Return / progress

**Moment loop:** Move → Aim → Attack → Dodge → Repeat

---

## SYSTEM OVERVIEW (AI-OPTIMIZED)

### Player System

Handles all player behavior.

* Movement, camera, input
* Melee + ranged combat
* Animation via `NetworkAnimator`
* Stats and currency syncing

**Key idea:** Client-controlled input → server validates → state syncs

---

### Combat System

Server-authoritative pipeline.

Flow:

1. Client input
2. Server validation
3. Damage applied
4. Health updated
5. SyncVar replication

Supports:

* MeleeCombat
* RangedAttack
* Projectile system

---

### Enemy System

#### Base AI

* Navigation + behavior (`EnemyAI`)
* Server-controlled

#### Swarm AI

Coordinated group behavior.

Core logic:

* Target selection (aggro)
* Attack timing
* Repositioning

**Important behaviors:**

* Prevent clustering
* Smart repositioning
* Balanced aggro switching

---

### Boss System

Modular and reusable.

Core components:

* `BossController`
* `BossHealth`
* `BossPhaseManager`
* `BossAttackManager`
* `BossAnimationRelay`
* `BossUIController`

**Rules:**

* No logic in animation events (server only applies results)
* Phases are data-driven
* Bosses extend, not rewrite core
* Bosses must implement `IDamageable` (via `BossHealth`) to receive player attacks

---

### Overworld System

* Level nodes with coop countdown
* Server-authoritative progression
* Shop with validated purchases
* Currency synced via SyncVar

### Level Progression & Persistence

* **Boss Defeat Flow:** Handled by `LevelCompleteManager`. Triggers victory UI, delay, and return to overworld.
* **Health Persistence:** Damage taken in levels persists in the overworld. Restorable via shop items.
* **Currency Persistence:** Coins earned in gameplay scenes are saved to `PlayerSessionData` and restored in the overworld.
* **Level Unlocks:** Completing a boss level queues an unlock in `CustomNetworkManager`, which filters back to `LevelProgressionManager` on overworld load.

### Arena System (Optional Levels)

Wave-based combat arenas for optional currency farming before boss fights.

**Flow:** Overworld → Arena Scene → Clear Waves → Auto-Return to Overworld

* **ArenaWaveData** – ScriptableObject defining enemy composition per wave (Create → Arena → Wave Data)
* **ArenaManager** – Server-authoritative wave controller (spawns enemies, tracks deaths, advances waves)
* **ArenaCompleteManager** – Snapshots player data and returns to overworld after all waves cleared
* **ArenaUI** – Client-side HUD (wave banner, inter-wave countdown, completion screen)
* **EnemyDeathTracker** – Lightweight runtime component for tracking enemy deaths without modifying existing AI

**Rules:**

* Arenas do NOT unlock progression — only boss levels do
* Arenas are repeatable (can re-enter anytime)
* Coins earned from enemy drops persist via `PlayerSessionData`
* Health damage taken persists (same as boss levels)
* Minimum 3 waves recommended per arena

---

## WORKSPACE INDEX (MERGED & CLEAN)

### Root Structure

| Path                         | Purpose         |
| ---------------------------- | --------------- |
| `Assets/`                    | Game content    |
| `Packages/`                  | Dependencies    |
| `ProjectSettings/`           | Config          |
| `Library/`, `Temp/`, `Logs/` | Generated       |
| `.vscode/`                   | Editor settings |

---

### Assets Layout

| Folder                  | Contents              |
| ----------------------- | --------------------- |
| `Assets/Scripts/`       | Gameplay code         |
| `Assets/Scenes/`        | Scenes                |
| `Assets/Prefabs/`       | Prefabs               |
| `Assets/Shaders/`       | Shader files          |
| `Assets/UI Elements/`   | UI                    |
| `Assets/Player Models/` | Player assets         |
| `Assets/Enemy Models/`  | Enemy assets          |

---

### Scenes

| Scene         | Path                                    |
| ------------- | --------------------------------------- |
| Main Menu     | `Assets/Scenes/Main Menu.unity`         |
| Lobby         | `Assets/Scenes/Lobby.unity`             |
| Overworld Hub | `Assets/Scenes/Mirror Networking.unity` |

---

## GAME SCRIPTS (ESSENTIAL MAP)

Only scripts outside `Network/Mirror/` are gameplay logic.

### Player

* Movement, camera, combat, stats

### Player UI

* UI, identity, nametags

### Enemy

* Base AI, sync, spawning

### Swarm AI

* Brain, aggro, attacks, coordination

### Lobby / Menu

* Network manager, lobby UI, transitions

### Overworld

* Progression, nodes, shop, currency

### Level Progression

* `LevelCompleteManager`, `LevelCompleteUI` (victory flow)

### System

* Interfaces (`IDamageable`, `ICombatHandler`), projectiles

---

## NETWORK STACK (MIRROR)

Located in:
`Assets/Scripts/Network/Mirror/`

Contains:

* Core networking
* Transforms, animators
* Transports
* Tools and examples

**Rule:** Never modify Mirror source

---

## INPUT SYSTEM

* `Assets/Scripts/PlayerControls.inputactions`

---

## OUTLINE SHADER

Universal URP outline shader using the inverted hull (clip-space extrusion) method.

* **File:** `Assets/Shaders/OutlineShader.shader`
* **Shader path:** `Custom/Outline`

| Property          | Type          | Default | Description                                    |
| ----------------- | ------------- | ------- | ---------------------------------------------- |
| Outline Color     | Color         | Black   | Color of the outline                           |
| Outline Width     | Range (0–10)  | 2       | Thickness in screen pixels                     |
| Render Distance   | Float         | 20      | Max distance (world units) outline is visible  |

**Usage:** Create a material with the `Custom/Outline` shader, then add it as a second material element on any MeshRenderer/SkinnedMeshRenderer.

---

## DEVELOPMENT RULES (CRITICAL)

* Server is authoritative
* Clients only request actions
* AI runs only on server
* Use SyncVar for important state
* No gameplay logic inside Mirror folder

---

## DEVELOPMENT STATUS

### Completed

* Multiplayer system
* Lobby + transitions
* Player combat (Responsive Input Buffer & Melee Polish)
* Enemy + swarm AI foundation & tuning
* Overworld system
* Boss system core
* Level progression system (Boss defeat → Overworld)
* Persistent health & currency
* Playtest swarm AI
* Optional arena-style wave system (Currency farming)
* **Melee Combat Polish (Basilio):**
  - True Input Buffer system (eliminates dropped clicks)
  - Snappy animation-cancelable combos
  - Programmatic Hit-Stop (weighted impact feel)
  - Attack Stepping (auto-forward thrust on swing)
  - Non-animation Hit Stagger (visual flash & knockback)
  - **Dynamic Aiming:** Automatic re-orientation towards camera crosshair at attack start and between combo hits.
* **Ranged Combat Polish (Crispin):**
  - **Physical Camera ADS:** Physical distance-based camera zoom for reliable aiming.
  - **Aim Sensitivity Scaling:** Automatically reduces mouse sensitivity while charging/aiming.
  - **Crosshair Accuracy:** Physics-based projectile targeting (projectiles fire exactly at the crosshair hit point).
  - **Slow-Walk Mechanic:** Strategic speed reduction (70%) while aiming to improve animation quality and control.
  - **Directional Wind-up:** 1D Blend Tree integration for left/right strafing while charging.
* **General Combat Polish:**
  - **Friendly Fire Prevention:** Attacks (both ranged and melee) no longer damage teammates.

### In Progress

* Cooldown consistency
* Movement validation

### Next

* Improve attack syncing
* Add attack variety

### Future

* Dodge/roll system
* Boss content
* Player classes
* Sound system

---

## KNOWN ISSUES

None currently reported.

---

## MODULAR DOCS STRUCTURE

Use this README as entry point.

### Core Docs

* `Docs/project-context.md` — full architecture + roadmap
* `Docs/BossSystem.md` — boss framework
* `Docs/SwarmSystem.md` — swarm AI documentation
* `Docs/Onglo.md` — example boss
* `Docs/playeranimation.md` — animation system

### Design

* `DESIGN.MD` — gameplay + rules

---

## NOTES

This README is optimized for:

* Fast onboarding
* AI code assistants (Cursor, GPT, Gemini)
* Clear system boundaries

Update this file when systems or architecture change.
