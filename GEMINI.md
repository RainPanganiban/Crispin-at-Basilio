# Project: Crispin at Basilio

## Project Overview

Crispin at Basilio is a multiplayer 3D action combat game built in Unity (Version 6000.0.62f1) using Mirror networking. The game features a `Lobby → Overworld Hub → Gameplay` scene flow with character persistence, including a Cuphead-inspired overworld with level nodes, cooperative countdowns, and a shop system. Players fight AI-controlled enemies in a combat arena, focusing on responsive third-person combat and server-authoritative multiplayer. The project includes a robust Swarm Enemy AI system with coordinated attacks and repositioning, as well as a modular, server-authoritative Boss System for arena encounters.

Current development is focused on refining Swarm Enemy AI tuning, improving attack cooldown consistency, and optimizing server-authoritative movement interactions.

## Technologies

- **Game Engine:** Unity (Version 6000.0.62f1)
- **Networking:** Mirror Networking
- **Scripting Language:** C#
- **Input System:** Unity Input System (using `Assets/Scripts/PlayerControls.inputactions`)
- **Rendering:** Universal Render Pipeline (URP)

Key Unity packages and modules in use include: `com.unity.ai.navigation`, `com.unity.netcode.gameobjects`, `com.unity.render-pipelines.universal`, and `com.unity.visualscripting`.

## Building and Running

This project is a Unity game and should be opened and managed with the Unity Editor.

1.  **Open the Project:**
    *   Ensure you have Unity Editor version `6000.0.62f1` installed.
    *   Open the Unity Hub and add this project by navigating to the project root directory (`C:\Coding Projects\Games\Crispin-at-Basilio`).
    *   Open the project in the Unity Editor. Unity may take some time to import assets and compile scripts on the first open.

2.  **Running the Game in Editor:**
    *   Once the project is open, navigate to the `Assets/Scenes` folder.
    *   Open the `Lobby.unity` scene.
    *   Press the Play button in the Unity Editor to run the game.

3.  **Building the Game:**
    *   To build a standalone executable, go to `File > Build Settings...`.
    *   Select your target platform (e.g., PC, Mac & Linux Standalone).
    *   Add the necessary scenes to the "Scenes In Build" list (at minimum, Lobby, Mirror Networking (Overworld Hub), and gameplay scenes like Level 1, Level 2, Level 3).
    *   Click "Build" or "Build And Run".

## Development Conventions

The following architectural rules and conventions are critical for development:

*   **Mirror Internal Code:** The internal code of the Mirror networking library must **never** be modified.
*   **Gameplay Logic Location:** All gameplay logic must reside outside of the `Assets/Scripts/Network/Mirror/` directory.
*   **Server Authority:** The server is authoritative for all critical game states, including damage calculation, health updates, phase switching, projectile/minion spawning, AI logic, and death resolution. Clients only send requests for actions and display UI based on `SyncVars`.
*   **AI Logic:** All Artificial Intelligence (AI) logic runs exclusively on the server.
*   **State Synchronization:** Health and other critical state values should use Mirror's `SyncVar` for network synchronization.
*   **IDamageable Interface:** All damageable entities implement the `IDamageable` interface.
*   **Boss System:** The Boss System is modular, server-authoritative, and animation-driven. Bosses feature multi-phase behavior, health-based phase transitions, distinct attack patterns, and server-authoritative abilities. Core boss scripts (`BossController`, `BossHealth`, `BossPhaseManager`, `BossAttackManager`, `BossAnimationRelay`) are reused, while movement, attacks, and phase configurations are boss-specific. Boss phases should **not** be hardcoded inside `BossController`. Animation events are used to signal timing, but only the server applies gameplay results.
*   **Player Animation System:** A shared base controller (`CharacterAnimationController`) handles locomotion and uses `NetworkAnimator`. Character-specific controllers (`BasilioAnimation`, `CrispinAnimation`) inherit from this base. Player scripts trigger actions via `NetworkAnimator.SetTrigger(...)` with client authority for player-owned characters.

## Known Issues

*   **Crispin's Wind-up Attack:** The wind-up attack for the Crispin character is currently not playing/functioning correctly. Refer to `Docs/playeranimation.md` for detailed troubleshooting steps.

## Documentation

For more in-depth information on specific systems, please refer to the following documentation files:

*   **Project Context:** `Docs/project-context.md` (High-level overview, structure, and roadmap)
*   **Boss System Architecture:** `Docs/BossSystem.md` (Detailed architecture and setup rules for the Boss System)
*   **Onglo Boss Design & Setup:** `Docs/Onglo.md` (Specific design, behavior, and Unity setup for the Onglo boss example)
*   **Player Animation Documentation:** `Docs/playeranimation.md` (Details player animation setup, code, and troubleshooting for Basilio and Crispin)
