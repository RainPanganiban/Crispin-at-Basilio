# Crispin-at-Basilio — Workspace Index

**Project:** Crispin at Basilio (Unity)  
**Type:** Multiplayer game using Mirror networking  
**Product name:** Crispin at Basilio (from `ProjectSettings`)

---

## Root structure

| Path | Purpose |
|------|--------|
| `Assets/` | Game content: scripts, prefabs, scenes, models, UI |
| `Packages/` | Unity packages & dependencies |
| `ProjectSettings/` | Unity project config (PlayerSettings, etc.) |
| `Library/`, `Temp/`, `Logs/`, `UserSettings/` | Unity-generated (typically not versioned) |
| `.vscode/` | Editor settings |
| `*.sln`, `*.csproj` | Solution and C# projects (Unity + Mirror assemblies) |

---

## Assets layout

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

## Game scripts (project-specific)

Scripts under `Assets/Scripts/` that are **not** under `Network/Mirror/` are game logic.

### Player — `Assets/Scripts/Player/`

| Script | Role |
|--------|------|
| `PlayerMovement.cs` | Movement and input |
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
| `PunchAttack.cs` | Punch attack implementation |
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

### System / shared — `Assets/Scripts/System/`

| Script | Role |
|--------|------|
| `IDamageable.cs` | Interface for damageable objects |
| `ICombatHandler.cs` | Interface for combat |
| `Projectile.cs` | Projectile behavior |

---

## Network stack (Mirror)

Third-party/networking code lives under **`Assets/Scripts/Network/Mirror/`**:

- **Core** — Mirror core (Batching, LagCompensation, Prediction, SnapshotInterpolation, Threading, Tools)
- **Components** — NetworkTransform, NetworkAnimator, NetworkRigidbody, Discovery, InterestManagement, LagCompensation, Profiling, Lobby/Room managers
- **Transports** — Telepathy, SimpleWeb, Threaded
- **Authenticators** — Basic, Device, Timeout
- **Editor** — Weaver, Icon
- **Examples** — AdditiveLevels, Basic, Chat, Discovery, etc.
- **Hosting**, **Plugins**, **Presets**, **CompilerSymbols**

Game networking entry points: `CustomNetworkManager.cs`, `NetworkDebugClient.cs`, and Mirror’s `NetworkManager`/component APIs.

---

## Input

- **`Assets/Scripts/PlayerControls.inputactions`** — Input Actions asset for player input (movement, combat, etc.).

---

## Script count (approx.)

- **Total .cs under Assets:** ~602
- **Game-specific (non-Mirror):** ~33 scripts in Player, Player UI, Enemy, Main Menu, Overworld, System

---

## Git status (at index time)

- Branch: `SwarmEnemyAI-System` (ahead of `origin/SwarmEnemyAI-System` by 5)
- Modified: `.vscode/settings.json`, `PlayerUI.cs`, `PlayerStatsManager.cs`, `Packages/manifest.json`, `Packages/packages-lock.json`

---

*Generated as a workspace index for navigation and onboarding. Update as the project grows.*
