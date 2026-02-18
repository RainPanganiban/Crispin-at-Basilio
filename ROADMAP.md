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
- Syncing Stamina drain with client and the host

---

## NEXT (Immediate Focus)

- Improve swarm reposition behavior after attack
- Add range attack type for swarm enemies
- Tune stopping distance & attack range logic
- Improve aggro switching logic
- Add visual debug tools for AI state

---

## LATER / BACKLOG

- Dodge / roll mechanic with stamina cost and damage invincibility
- Running stamina drain system
- Boss system implementation
- Multi-phase boss AI
- Overworld Hub enhancements (visual polish, more shop items, optional levels)
- Player class specialization
- Animation polish
- Sound manager system
- Performance optimization pass
- Advanced enemy types
- UI polish
- Match win / lose conditions
- Replayability features

