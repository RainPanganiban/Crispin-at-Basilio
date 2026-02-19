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
- **Swarm Enemy AI Improvements**
    - Fixed attack completion callback wiring (`EnemyAttack` → `EnemyBrain.OnAttackFinished()`)
    - Improved reposition behavior after attack
        - NavMesh-validated back-off positions (prevents backing through walls)
        - Perpendicular jitter to prevent swarm clustering
        - Timeout handling to prevent stuck repositioning
    - Tuned stopping distance & attack range logic
        - Auto-sync `NavMeshAgent.stoppingDistance` with attack range
        - `GetSurroundPosition()` now respects attack range bounds
        - Fixed surround radius vs attack range mismatch
    - Improved aggro switching logic
        - Better target validation (handles destroyed/null targets)
        - Reduced chaos threshold (15% vs 25%)
        - Chaos switches only when targets are similarly distant
        - Reduced switch distance threshold (1.5m vs 2m)
        - Reduced lock time (1.5s vs 2s)

---

## IN PROGRESS

- Swarm Enemy AI tuning (continuing)
    - Attack timing behavior refinement
    - Testing and validating recent improvements
- Improving attack cooldown consistency
- Refining server-authoritative movement interactions

---

## NEXT (Immediate Focus)

- **Test & validate swarm AI improvements**
    - Playtest swarm behavior with multiple enemies
    - Verify reposition timing feels natural
    - Confirm aggro switching isn't too chaotic or too sticky
    - Tune exposed parameters based on feel (chaos chance, lock times, distances)
- **Attack cooldown consistency improvements**
    - Ensure cooldowns sync properly across network
    - Fix any timing discrepancies between client/server
    - Add visual feedback for cooldown state (if applicable)
- **Consider adding more swarm attack variety**
    - Review existing attacks (PunchAttack, ChargePunchAttack, ProjectileAttack)
    - Add combo attacks or area attacks
    - Varied attack ranges/patterns for swarm diversity

---

## SUGGESTED NEXT STEPS (After Testing)

Based on completed swarm AI improvements, consider these priorities:

1. **If swarm AI feels good after testing** → Move to Boss System Phase 1
   - Swarm enemies are now polished enough to serve as baseline
   - Boss system can reuse patterns (health, aggro, attacks)
   - Good time to start boss framework while swarm AI is fresh

2. **If swarm AI needs more polish** → Continue iteration
   - Add attack variety (combo attacks, area effects)
   - Implement enemy type variations (fast/agile vs tanky/slow)
   - Add enemy formations/group tactics

3. **Player-facing features** → Dodge/roll mechanic
   - Complements improved swarm AI (more dynamic combat)
   - Players need defensive options against coordinated swarms
   - Stamina system ties into combat flow

4. **Quality of life** → Visual debug tools for swarm AI
   - Gizmos for aggro radius, attack range, surround positions
   - On-screen state indicators (chasing/attacking/repositioning)
   - Helps with tuning and debugging

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

