# Onglo — Boss Documentation (Example Boss)

## Fantasy + fight identity

**Onglo** is a territorial arena boss that **anchors space** and **punishes greed**.

- **Doesn’t dodge**
- **Doesn’t retreat**
- **Doesn’t constantly reposition**
- **Prefers center control**
- **Heavy, slow presence** with big punish windows

Core feel:
- Slow pacing, clear telegraphs → then escalating pressure in later phases
- Heavy cooldowns, devastating hits
- Rare/controlled overlap (the core boss system enforces “one attack at a time”)

---

## Behavior spec (server authoritative)

### Baseline movement
- **Walk heavily (slow)**
- **Minimal repositioning**
- **Center control**: if pushed away from the arena center, Onglo slowly walks back to it
- **Minor tremor per step** (optional, low damage + low radius)

Implementation notes (current repo):
- Movement is implemented by `OngloMovement : BossMovementBase`.
  It uses `arenaCenter` + `preferredRadius` to anchor space and does simple server-side movement (no dodging/retreat logic).

### Phase 3 “Primal Rage” bursts
In Phase 3 only, Onglo gains **short enraged movement bursts** toward the closest player to increase pressure without becoming a reposition-heavy boss.

Controlled by:
- `specialBehaviorFlag = true` in Phase 3 config (see setup section).

---

## Attacks (animation-event driven)

All attacks below are implemented as `BaseAttack` derivatives and only apply gameplay results on the server.

### 1) Earthshaker Stomp
**Telegraph**: raises foot slowly.
**Impact**: slams ground → expanding circular shockwave.
**Scaling**:
- Phase 1: 1 ring
- Phase 2: 1 ring (stronger pressure via other attacks)
- Phase 3: **double shockwaves**

Implementation:
- **Script**: `EarthshakerStompAttack`
- **Animation event**: `StompImpact`
- **Spawns**: `OngloShockwaveRing` (networked)

### 2) Ground Split
**Telegraph**: punches ground.
**Effect**: linear fissure travels forward → after delay erupts.

Implementation:
- **Script**: `GroundSplitAttack`
- **Animation event**: `GroundSplitImpact`
- **Spawns**: `OngloFissure` (networked)
- **Notes**: in MVP, the eruption is at the fissure’s current endpoint when it reaches max distance or delay.

### 3) Boulder Barrage
**Telegraph**: pulls rocks from arena.
**Effect**: throws multiple in an arc.
**Scaling**:
- Phase 2: introduced
- Phase 3: increased throw count (“rain”)

Implementation:
- **Script**: `BoulderBarrageAttack`
- **Animation event**: `BoulderThrow`
- **Spawns**: `OngloBoulder` (networked)

---

## Phase design

### Phase 1 — Territorial Dominance (100%–60%)
- **Attacks**: Earthshaker Stomp, Ground Split
- **Behavior**: slow pacing, clear telegraphs

### Phase 2 — Structural Collapse (60%–30%)
- **Attacks**: Boulder Barrage, Ground Split
- **Behavior**: more environmental chaos (boulders + fissure pressure)

### Phase 3 — Primal Rage (30%–0%)
- **Changes**:
  - Faster stomp animation
  - Double shockwaves
  - Boulder barrage “rain”
  - Short enraged bursts (movement)
- **Pressure**: less telegraph time, more punishment

---

## Unity setup guide (step-by-step, with inspector values)

This guide assumes your boss framework exists (it does) and you’re creating Onglo as a networked boss prefab.

### 0) Create folders (recommended)
- Scripts already live in:
  - `Assets/Scripts/Boss/Core/` (framework)
  - `Assets/Scripts/Boss/Bosses/Onglo/` (Onglo content)
- Create prefab folders:
  - `Assets/Prefabs/Bosses/Onglo/`
  - `Assets/Prefabs/Bosses/Onglo/Projectiles/` (rings/fissures/boulders)

### 1) Create the Onglo prefab
1. Create a root GameObject named **`Onglo`**
2. Add/ensure an `Animator` exists (commonly on a child like `Model`)
   - `BossController` will auto-find `Animator` in children if its `animator` field is empty.
3. Add colliders (for body + hitboxes as needed)

### 2) Add Mirror / Physics components (root `Onglo`)
- `NetworkIdentity`
- `NetworkTransform` (Sync Direction: Server To Client, Interpolate enabled)
- `Rigidbody` (Is Kinematic: true, Use Gravity: false - needed for MovePosition sync)
- `CapsuleCollider` (for hit detection)

### 3) Add core boss components (root `Onglo`)
Add these components:
- `BossController`
- `BossHealth`
- `BossPhaseManager`
- `BossAttackManager`
- `BossAnimationRelay`

Recommended `BossController` values:
- **Think Interval**: `0.25`
  (heavy cadence; later phases can feel faster via animation and attack mix)

Recommended `BossAttackManager` values:
- **Global Cooldown Between Attacks**: `0.75`
  (prevents rapid chaining; supports “heavy cooldown but devastating”)

Recommended `BossHealth` values:
- **Max Health**: `3000`
  (tune later; phases are defined as percentages)

### 4) Add Onglo movement component (root `Onglo`)
Add:
- `OngloMovement`

Create a center anchor in the scene:
- In your arena scene, create an empty GameObject **`ArenaCenter`**.
- Assign it to `OngloMovement.arenaCenter`.

Recommended `OngloMovement` values:
- **Preferred Radius**: `4`
- **Walk Speed**: `1.2`
- **Enable Footstep Tremor**: `true` (optional)
- **Tremor Interval**: `1.1`
- **Tremor Damage**: `5`
- **Tremor Max Radius**: `2.5`
- **Tremor Expand Speed**: `8`
- **Player Layer**: set to your player layer mask
  (the same layer you use for enemy punch checks)

Phase 3 burst values:
- **Burst Speed**: `4.5`
- **Burst Duration**: `0.8`
- **Burst Cooldown**: `6`

### 5) Add Onglo attack components (root `Onglo`)
Add these components:
- `EarthshakerStompAttack`
- `GroundSplitAttack`
- `BoulderBarrageAttack`

Recommended cooldowns (set on each attack component):
- **EarthshakerStompAttack.cooldown**: `6`, **maxRange**: `8`
- **GroundSplitAttack.cooldown**: `10`, **maxRange**: `12`
- **BoulderBarrageAttack.cooldown**: `14`, **maxRange**: `20`

Recommended `animationTriggerName` values:
- `EarthshakerStompAttack.animationTriggerName`: **`Onglo_Stomp`**
- `GroundSplitAttack.animationTriggerName`: **`Onglo_GroundSplit`**
- `BoulderBarrageAttack.animationTriggerName`: **`Onglo_BoulderBarrage`**

Recommended `requiresMovementLock`:
- Stomp: `true`
- GroundSplit: `true`
- BoulderBarrage: `true`

### 6) Create and assign spawned prefabs (rings / fissures / boulders)

You need 3 networked prefabs (each must have `NetworkIdentity`):

#### A) Shockwave ring prefab
Create prefab **`OngloShockwaveRing`** with:
- `NetworkIdentity`
- `NetworkTransform` (optional; it’s mostly stationary)
- `SphereCollider` (isTrigger = true)
- `OngloShockwaveRing` script

Assign it to:
- `EarthshakerStompAttack.ringPrefab`
- (optional) `OngloMovement.tremorRingPrefab`

#### B) Fissure prefab
Create prefab **`OngloFissure`** with:
- `NetworkIdentity`
- `NetworkTransform`
- `BoxCollider` (isTrigger = true)
- `OngloFissure` script

Assign it to:
- `GroundSplitAttack.fissurePrefab`

#### C) Boulder prefab
Create prefab **`OngloBoulder`** with:
- `NetworkIdentity`
- `NetworkTransform`
- `SphereCollider` (isTrigger = true)
- `OngloBoulder` script

Assign it to:
- `BoulderBarrageAttack.boulderPrefab`

**Mirror spawning requirement**:
Add all three prefabs to `NetworkManager` → **Spawnable Prefabs** list.

### 7) Create spawn-point child objects on Onglo
Under `Onglo`, create:
- `StompOrigin` (at feet / ground contact)
  Assign to `EarthshakerStompAttack.ringOrigin`
- `GroundSplitOrigin` (in front of fist / ground contact)
  Assign to `GroundSplitAttack.fissureOrigin`
- `TremorOrigin` (optional; usually feet)
  Assign to `OngloMovement.tremorOrigin`
- `BoulderSpawns` (empty parent)
  Add several children around upper body/hands (e.g. `BoulderSpawn_01..04`)
  Assign all child transforms to `BoulderBarrageAttack.boulderSpawnPoints`

### 8) Configure `BossPhaseManager` phases (exact values)
On the Onglo root, set `BossPhaseManager.phases` list size to **3**.
Order matters: use **[1.0, 0.6, 0.3]** (highest → lowest).

Phase 1:
- **Enter At Health Percent**: `1.0`
- **Allowed Attacks**: `EarthshakerStompAttack`, `GroundSplitAttack`
- **Transition Trigger Name**: `Onglo_Phase2`
- **Special Behavior Flag**: `false`

Phase 2:
- **Enter At Health Percent**: `0.6`
- **Allowed Attacks**: `BoulderBarrageAttack`, `GroundSplitAttack`
- **Transition Trigger Name**: `Onglo_Phase3`
- **Special Behavior Flag**: `false`

Phase 3:
- **Enter At Health Percent**: `0.3`
- **Allowed Attacks**: `EarthshakerStompAttack`, `GroundSplitAttack`, `BoulderBarrageAttack`
- **Transition Trigger Name**: *(optional, can be empty if you don’t want a transition anim)*
  If you do: `Onglo_Enrage`
 - **Special Behavior Flag**: `true`
  (enables OngloMovement phase-3 bursts)

### 9) Boss UI Controller Setup (Health Bar)
Add a proximity-based health UI to the Boss:
- Create a `Canvas` child under `Onglo` (World Space or Screen Space - Overlay).
- Assign the `BossUIController` script.
- Link the `BossHealth` reference, a UI `CanvasGroup` (for fading), the `Slider`, and `TextMeshProUGUI` for the name.
- Set `Activation Range` to `15` (UI appears when players get close).

### 9) Animator parameters + animation events

Add **Trigger** parameters:
- `Onglo_Stomp`
- `Onglo_GroundSplit`
- `Onglo_BoulderBarrage`
- `Onglo_Phase2`
- `Onglo_Phase3`
- `Die` (used by `BossController.Server_Die()`)

Add animation events:
- On stomp clip, at impact frame:
  Call `BossAnimationRelay.AnimationEvent` with string: **`StompImpact`**
- On ground split clip, at punch-impact frame:
  Call `BossAnimationRelay.AnimationEvent` with string: **`GroundSplitImpact`**
- On boulder barrage clip, at each throw frame:
  Call `BossAnimationRelay.AnimationEvent` with string: **`BoulderThrow`**
- At the end of each attack clip:
  Call `BossAnimationRelay.AttackAnimationComplete()`

### 10) Quick multiplayer verification checklist
- Host + 1 client:
  - Stomp ring damages each player **once per ring**
  - Phase transitions occur at 60% and 30%
  - Only one attack runs at a time
- Ensure the 3 spawned prefabs are registered as spawnables and appear on clients.

