# Crispin at Basilio — Development Roadmap

## DONE (Stable / Working)

- Mirror multiplayer integration
- CustomNetworkManager
- Lobby system
- Scene transitions (Lobby → Overworld → Gameplay)
- Player spawning with character persistence across scenes
- PlayerMovement
- ThirdPersonCamera
- MeleeCombat
- RangedAttack
- Projectile system
- IDamageable interface
- Enemy base AI
- EnemySync
- EnemySpawner
- Basic Swarm AI structure
    - EnemyBrain
    - EnemyAggro
    - EnemyAttack
    - EnemyAttackController
    - PunchAttack
- Enemy health system
- Enemy health UI
- **Overworld Hub System**
    - Cuphead-inspired explorable overworld
    - Level nodes with cooperative countdown (both players required)
    - Server-authoritative level progression and unlock system
    - Shop system with server-validated purchases
    - Character persistence across all scenes (Lobby → Overworld → Gameplay)
    - Currency system (PlayerCurrency component)
    - Countdown UI with billboarding
    - Shop UI with cursor unlock/lock management

---

## IN PROGRESS

- Swarm Enemy AI tuning
    - Aggro balancing
    - Attack timing behavior
    - Reposition logic after attack
- Preventing enemy pushing player issues
- Improving attack cooldown consistency
- Refining server-authoritative movement interactions

---

## NEXT (Immediate Focus)

- Improve swarm reposition behavior after attack
- Tune stopping distance & attack range logic
- Improve aggro switching logic
- Add visual debug tools for AI state

---

## LATER / BACKLOG

- Dodge / roll mechanic with stamina cost and damage invincibility
- Running stamina drain system
- **Boss System (Implementation Track)**
    - Phase 0 — Documentation & conventions
        - Boss System docs (done): `Docs/BossSystem.md`
        - Decide folder layout for boss scripts (outside `Assets/Scripts/Network/Mirror/`)
        - Define naming conventions for animator triggers + animation events
    - Phase 1 — Core framework (server-authoritative)
        - Implement core components:
            - `BossController` (states: Idle/Attacking/Transitioning/Dead)
            - `BossHealth` (SyncVar health, server-only `TakeDamage`, threshold detection)
            - `BossPhaseManager` (phase config, transition flow, swaps attacks)
            - `BossAttackManager` (selection + cooldowns + “one attack at a time”)
            - `BossAnimationRelay` (animation event → server-only execution)
            - `BaseAttack` (shared attack contract)
        - Define boss phase config format (serialized list vs ScriptableObject)
        - Ensure phases are **not** hardcoded inside `BossController`
    - Phase 2 — Vertical slice boss (1 boss proves the system)
        - Create 1 test boss prefab in an arena scene
        - Implement 1 movement script (boss-specific)
        - Implement 2 attacks (animation-event driven)
        - Implement phase transitions (at least 2 phases)
        - Add basic telegraph VFX spawn points + audio hooks
    - Phase 3 — Multiplayer hardening
        - Verify server-only execution (no double-hit in host mode)
        - Dedicated server + 2 clients test pass (health, phases, death sync)
        - Validate projectile/minion spawns are server-owned and replicated correctly
        - Add guardrails for invalid client inputs / event spoofing
    - Phase 4 — Debug & iteration tools
        - Visual debug for boss state/phase/attack cooldown
        - Logging toggles for boss decisions (server-only)
        - Gizmos for hit areas / targeting / arena bounds
- Overworld Hub enhancements (visual polish, more shop items, optional levels)
- Player class specialization
- Animation polish
- Sound manager system
- Performance optimization pass
- Advanced enemy types
- UI polish
- Match win / lose conditions
- Replayability features

