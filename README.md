# Frontline Systems Prototype

War sandbox prototype with a **closed-economy** NPC loot system (the `DestroyedPool`).

## Unity project

- **Path**: `UnityProject/`
- **Target editor**: Unity **latest LTS** (project currently pinned in `ProjectSettings/ProjectVersion.txt`)

### Running the DestroyedPool milestone

1. Open `UnityProject/` in Unity.
2. Create/open any scene and press Play.
3. The **DestroyedPool debug panel** appears automatically (toggle with **F1**).
4. Use **Seed** / **Spawn+Destroy** buttons to verify:
   - Destruction events flow through `Health` → `Destructible` → `DestroyedPoolService`.
   - Items only enter the eligible pool after being **crafted at least once** (`Mark Crafted` or Seed buttons).

## Tactical Mode milestone

- Open scene: `Assets/_Project/Scenes/TacticalTest.unity`
- Controls:
  - **WASD**: move
  - **F1**: toggle DestroyedPool debug
  - **I**: toggle Inventory + Crafting (inventory-only recipes)
  - **F2**: toggle fog overlay

Fog-of-war uses LOS raycasts against obstacles (Default layer). The test map spawns a ground plane (Ignore Raycast layer) and several occluder walls/blocks at runtime.

## Milestone 4: UI patch + crafting stations + NPC combat + destroyed-pool loot

### Controls (TacticalTest)

- **I**: Inventory + Crafting panel (inventory-only crafting)
- **F1**: DestroyedPool debug panel (includes NPC spawn buttons)
- **E**: interact (crafting stations, loot pickup)
- **LMB**: ranged attack (only when not harvesting a `HarvestNode` under cursor)
- **RMB**: melee attack
- **Esc**: close station/loot windows
 - **B**: Build Mode (Milestone 5)

### Phase 0 (UI-only patch)

- Both `I` and `F1` panels are now:
  - **Screen-safe**: width clamped to \( \min(520, \text{Screen.width}-20) \), height clamped to \( \min(720, \text{Screen.height}-20) \)
  - **Scrollable**: wrapped in an outer vertical scroll view

### Phase 1 (world crafting stations)

- Two stations auto-spawn in `TacticalTest` near the player:
  - **Workbench**
  - **Foundry**
- When within ~**1.5m**, you’ll see:
  - `E: Use Workbench`
  - `E: Use Foundry`

Recipe rules:
- **Inventory crafting** (`I`): **Wood-tier tools only**
- **Workbench**: **Stone-tier tools** + **Gas Can**
- **Foundry**: **Iron-tier tools**

### Phase 2 (NPC combat)

- Use **F1** → **NPC Spawns** to spawn:
  - Easy/Medium/Hard **Ranged** and **Melee** NPCs
- NPCs:
  - Aggro within their configured range, move toward player, face/aim, attack on interval, take damage, die cleanly.

### Phase 3 (DestroyedPool-governed loot)

Loot rules:
- NPC loot can only roll from items that are:
  - **Crafted at least once** AND
  - Have **DestroyedPool count > 0**
- Loot selection is **weighted by destroyed count**
- If the eligible pool is empty, NPCs drop **nothing**
- On a successful drop roll:
  - A single loot pickup spawns (max **1** item)
  - The chosen item’s DestroyedPool count is **decremented by 1** (closed economy)

Pickup UX:
- Stand within ~**1m** of the loot cube → `E: Loot`
- Press **E** → Loot Window → **Loot All**

Logging:
- Loot rolls append to:
  - `Application.persistentDataPath/loot_roll_log.txt`
- The file is **append-only** (no truncation/rotation in-game).
- Each entry includes:
  - timestamp, npcType, eligibleCount, chosenItemId, weight, poolBefore, poolAfter

### Quick verification checklist

1) **UI scroll**: Press **I** and scroll; press **F1** and scroll to reach all buttons.
2) **Stations**:
   - Craft **Wood Axe** in inventory (`I`)
   - Verify **Stone Axe** is not craftable in inventory, but is craftable at **Workbench**
   - Verify **Iron Axe** is craftable at **Foundry**
3) **Empty pool loot**:
   - Press **F1** → Reset Pool
   - Spawn any NPC and kill it → no loot
4) **Seed pool loot**:
   - Craft a tool, then break it (DestroyedPool increments)
   - Kill an NPC → loot drops from eligible items only; pool decrements; log updates

## Milestone 5: buildables v1 (placement + repair + destruction + storage + persistence)

### Controls (TacticalTest)

- **B**: toggle Build Mode (combat/harvest are disabled while in build mode)
- **Build Mode**:
  - **1** Foundation, **2** Wall, **3** Gate, **4** Storage Crate
  - **R** rotate (90° steps)
  - **LMB** place (only when preview is green + you can afford the cost)
  - **RMB** or **Esc** exit build mode
- **Repair**:
  - Equip **Hammer** (hotkey **4**)
  - Hold **LMB** on a damaged buildable to repair in small ticks (consumes small resources per tick)
- **Storage Crate**:
  - **E** within ~**1.5m** to open crate UI
  - Use **Transfer All** buttons to move resources in/out

### Persistence (local)

- World buildables save to:
  - `Application.persistentDataPath/buildables_world.json`
- Save triggers:
  - placement, repair, destruction, crate transfers
- Manual debug actions:
  - **F1** → “Milestone 5 (Buildables)” → **Save World / Load World / Clear All Buildables**

### Quick verification checklist

1) **Placement + economy**:
   - Press **B**, select **1** (Foundation), place it (LMB)
   - Verify resources are deducted and **CreatedPool** increments for `build_foundation`
2) **Damage + destruction**:
   - Place a **Wall** (2), then destroy it with player attacks
   - Verify **DestroyedPool** increments for `build_wall`
3) **Repair**:
   - Damage a buildable, equip **Hammer** (4), hold **LMB** to repair
   - Verify resources are consumed and HP increases up to Max HP
4) **Storage destruction accounting**:
   - Place **Storage Crate** (4), transfer some `mat_*` resources into it
   - Destroy the crate
   - Verify `build_storage` is registered destroyed, and stored materials are also registered destroyed (no world drops)
5) **Persistence**:
   - Place multiple buildables, press **F1** → **Save World**
   - Stop Play, press Play again, then **F1** → **Load World**
   - Verify buildables reappear at correct positions/rotations with correct HP (and crate contents if used)

## Milestone 7.1: Bug Fixes and Improvements

### Bug Fixes

1. **Floor/ceiling placement beside walls**:
   - Fixed overlap detection for thin buildables (walls, floors).
   - Improved skin calculation allows flush adjacency between different buildable shapes.
   - Added `IsTrulyOverlapping()` check with tolerance for touching faces.

2. **Ramps floating**:
   - Improved ramp placement geometry so lowest edge sits flush on the support surface.
   - Added root BoxCollider for reliable physics settling.

3. **Build menu click-through**:
   - Fixed issue where clicking buttons in Build Catalog panel (V) would also trigger placement.
   - Added explicit check for `BuildCatalogPanel.IsOpen` in `BuildablesService.Update()`.

4. **Weapon slot system (1-5)**:
   - Implemented proper 5-slot equipment system with categories:
     - **Slot 1 (Primary)**: Main weapons and tools (axes, hammers, melee weapons)
     - **Slot 2 (Secondary)**: Sidearms and small weapons (knives)
     - **Slot 3 (Throwable)**: Grenades, throwing weapons
     - **Slot 4 (Deployable)**: Portable shield, camp/tent, workbench, gas cans
     - **Slot 5 (Medical)**: Medkits, bandages
   - Added `EquipmentSlot` enum for slot categories.
   - Tools auto-equip to appropriate slot when added.
   - Number keys 1-5 switch between slots (only when not in build mode).

5. **Truck entry**:
   - Fixed E key interaction detection for entering transport trucks.
   - Added cursor raycast check (`IsPlayerLookingAtTruck()`) for more reliable entry.
   - Increased interaction range slightly for better UX.
   - Blocked truck inputs while in build mode.

6. **Melee attacks doing ranged damage**:
   - Changed melee weapon attacks from SphereCast (ranged-style) to OverlapSphere (true melee).
   - Added 120° cone/arc filter so attacks only hit targets in front of player.
   - Rebalanced melee weapon stats:
     - **Knife**: 1.2m range, 12 damage, 3 attacks/sec
     - **Sword**: 1.8m range, 18 damage, 2 attacks/sec
     - **Pole**: 2.5m range, 25 damage, 1.2 attacks/sec

### New Features

- **EquipmentSlot enum**: Defines slot categories (Primary, Secondary, Throwable, Deployable, Medical).
- **Extended ToolType enum**: Added Pickaxe, Knife, Throwable, Deployable, Medical types.
- **Slot-based equipping**: `AddToolToSlot()`, `EquipToolToSlot()`, `GetToolInSlot()`, `SetActiveSlot()` methods.

### Controls Update

- **1-5**: Switch equipment slots (when not in build mode)
- **E**: Enter/exit truck (when looking at truck or nearby)
- **F**: Open truck storage (when near truck or inside)

## Milestone 7.2: Inventory Weight, Movement, Ramps, UI Input, Truck Inventory

### Weight System (Foxhole-style)

Player inventory now has a weight-based carry system:
- **Max carry capacity**: 100 units (configurable)
- **Weight affects**:
  - Movement speed (0-30% = no penalty, 30-60% = slight, 60-90% = heavy, >90% = extreme)
  - Step/climb height (heavier = reduced climb ability)
  - Stamina drain (hook for future stamina system)

Weight values:
- Resources: 0.5 units each
- Tools: 2-10 units depending on type (melee weapons = 5, deployables = 10)
- Unslotted (packaged) items: 1.5x weight multiplier

### Weight-Aware Movement

- Player step height dynamically adjusts based on carried weight
- Lighter players can climb/step higher
- CharacterController slope limit set to 45° for ramp walking
- Hooks added for future AI pathfinding (`CanStepUp()`, `GetEffectiveMoveSpeed()`)

### Ramp Improvements

- Ramps now use proper rotated BoxCollider for slope walking
- CharacterController can walk up ramps reliably
- Ramps can be placed beside walls (improved adjacency detection)
- No floating or invisible step gaps

### UI Input Blocking (Global Fix)

- Added centralized `IsUIBlockingInput` check in `UiModalManager`
- When ANY UI panel is open:
  - Mouse clicks do NOT place buildables
  - Mouse clicks do NOT trigger world actions
- Covers: modals, Build Catalog (V), Dev Panel (F1), Truck Storage (F), Storage Crate (E)

### Openables E Key Toggle

- All openable interfaces now properly toggle with E:
  - Workbench: E opens, E closes
  - Foundry: E opens, E closes
  - Storage Crate: E opens, E closes
- ESC still closes all modals

### Ground Loot Salvage System

- Dropped items on ground have a lifetime (default: 120 seconds)
- After timeout:
  - Original item is destroyed (registered in DestroyedPool)
  - Salvage pickup spawns at same location
- Salvage is a generic resource (`mat_salvage`)
- Future: Scrap Yard for chance-based reprocessing

### Truck Entry Fix

- Fixed bug where entering truck would push it backward
- CharacterController is now disabled BEFORE moving player to seat
- Truck physics frozen during entry transition

### Truck Inventory Weight

- Truck inventory now uses same weight system as player
- Max cargo weight: 500 units (configurable)
- Weight affects:
  - Acceleration (8% reduction per 100 units)
  - Max speed (5% reduction per 100 units)
- Weight check prevents exceeding capacity
- Debug HUD shows cargo weight and speed multiplier

### Files Changed

1. `PlayerInventoryService.cs` - Weight system, constants, calculation methods
2. `TacticalPlayerController.cs` - Weight-aware movement and step height
3. `BuildablesService.cs` - Centralized UI blocking, improved ramp spawning
4. `UiModalManager.cs` - `IsUIBlockingInput` check, IMGUI rect tracking
5. `DestroyedPoolDebugPanel.cs` - Added Instance singleton, IsVisible property
6. `TransportTruckController.cs` - Weight system, entry fix, weight-affected physics
7. `LootPickup.cs` - Lifetime parameter, auto-add GroundLootLifetime
8. `GroundLootLifetime.cs` (NEW) - Lifetime tracking, salvage conversion
9. `SalvagePickup.cs` (NEW) - Salvage pickup for expired loot

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | Player carrying 1 weapon moves faster than player carrying 3+ | PASS |
| B | Over-encumbered player has reduced step/climb ability | PASS |
| C | Ramps can be placed beside walls | PASS |
| D | Player can walk up ramps reliably | PASS |
| E | Clicking UI never places buildables | PASS |
| F | "E" toggles all openable panels open/closed | PASS |
| G | Ground loot converts to Salvage after timeout | PASS |
| H | Entering truck no longer pushes it | PASS |
| I | Truck inventory respects weight limits | PASS |

## Milestone 7.3: Storage Inventories, Buildable Adjacency, Camera Lock, Vehicle Interaction & Damage

### Storage Box Inventory System

Storage boxes now have full inventory support:
- **Weight-based capacity**: 200 kg base (same weight system as player/truck)
- **Upgradeable**: Click "Upgrade" button to increase capacity by 50 kg per level
- **Custom labels**: Click "Rename" to set a custom name for each crate
- **Destruction behavior**:
  - All contents drop onto the ground as loot pickups
  - Crate entity moves to Destroyed Pool
  - No inventory data remains attached

### Storage Box UI

New dedicated storage UI panel (not reused crafting UI):
- Displays inventory contents with weight/slot usage
- Shows crate label (editable)
- "Upgrade" button for capacity increase
- "Destroy Crate" button to manually destroy

### Buildable Adjacency Improvements

- **Increased tolerance**: Adjacency detection now allows clean placement of:
  - Floors beside ramps
  - Floors in front of ramps
  - Walls beside floors
  - Floors on top of other floors
- **Step-up behavior**: Floors (0.25m height) are within player step height (0.5m base)
  - Players can step up onto floors the same way as ramps
  - Step height respects weight-based movement rules

### Camera Lock Mode

New optional camera mode (toggle with **C** key):
- **When enabled**:
  - Camera locks behind player character
  - Camera smoothly follows player facing direction
  - A/D keys strafe left/right (player-relative movement)
  - Player faces mouse cursor
- **When disabled**:
  - Camera behaves as before (free look)
  - Movement is world-space (default)
- Visual indicator shows "Camera: LOCKED (C)" when enabled

### Vehicle Interaction Keys

Key bindings clarified and swapped:
- **F = Enter/Exit vehicle** (was E)
- **E = Use/Open/Interact** (open truck inventory, was F)
- More intuitive: F for "getting in/out", E for "using"

### Vehicle Collision Damage

Trucks now deal and receive collision damage:
- **Damage triggers** when colliding with:
  - Players
  - Bots (NPCs)
  - Player-built structures
- **Damage applied** to both truck and target
- **Damage calculation**:
  - Based on collision impulse force
  - Minimum speed threshold: 3 m/s
  - Minimum impulse threshold: 500 units
  - 5 damage per 100 impulse units
- **Exemptions**:
  - Ramps and floors are exempt at normal driving speeds
  - Only take damage at very high impulse (crash, not driving)
- **Cooldown**: 0.5s between damage events (prevents spam)

### Controls Update

| Key | Action |
|-----|--------|
| C | Toggle camera lock mode |
| F | Enter/exit truck |
| E | Open truck inventory (when near truck) |
| E | Open storage crate (when near crate) |

### Files Changed

1. `StorageCrate.cs` - Weight system, labels, upgrade, destruction drops loot
2. `StorageCratePanel.cs` - New UI with label editing, weight display, upgrade/destroy buttons
3. `BuildablesWorldSnapshot.cs` - Added crateLabel and crateUpgradeLevel fields
4. `BuildablesService.cs` - Save/load crate label and upgrade level, improved adjacency tolerance
5. `TopDownCameraController.cs` - Camera lock mode implementation
6. `TacticalPlayerController.cs` - Camera-relative movement, strafe support, mouse cursor facing
7. `TransportTruckController.cs` - Swapped E/F keys, collision damage system
8. `TransportTruckPanel.cs` - Updated to close with E, shows weight capacity

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | Storage boxes have inventories with limited capacity | PASS |
| B | Storage box UI shows contents, name, and actions | PASS |
| C | Destroying a storage box drops its inventory and moves it to destroyed pool | PASS |
| D | Floors, walls, and ramps can all be placed touching each other cleanly | PASS |
| E | Player can step up onto floors the same way as ramps | PASS |
| F | Camera lock toggle works and keeps camera behind player | PASS |
| G | F enters/exits truck, E opens truck inventory | PASS |
| H | Truck inventory shows cargo and respects weight limits | PASS |
| I | Truck collision damages both truck and hit target | PASS |
| J | Truck can drive up ramps and onto floors without damaging them at low speed | PASS |

## Milestone 7.4: Camera Unlock Spin Fix + Fortnite-Style Build Snapping + Melee Regression Check + Truck UI Close

### Camera Unlock Spinning Bug (CRITICAL FIX)

**Problem**: When camera was unlocked (C key), the camera would spin endlessly as the mouse moved, making gameplay unplayable.

**Root Cause**: The unlocked camera mode was using a fixed `yawDegrees` value (0°) instead of responding to mouse input. When transitioning from locked to unlocked, there was no proper mouse-driven rotation.

**Fix**:
- Added mouse-based camera rotation for free (unlocked) mode using `Input.GetAxis("Mouse X")`
- Separate yaw tracking: `_lockedYaw` (follows player) and `_freeYaw` (mouse-controlled)
- When unlocking, `_freeYaw` is initialized to current locked yaw to prevent jarring transitions
- Camera stops immediately when mouse stops moving
- No drift or runaway spinning

**Behavior**:
- **Locked mode (C)**: Camera follows behind player, A/D strafes, player faces cursor
- **Free mode (default)**: Camera rotates with mouse, WASD moves relative to camera direction

### Fortnite-Style Build Snapping

**Problem**: Buildable placement was awkward with weird ramp snapping and micro-gap placement failures.

**Fix**: Implemented attachment-point based snapping system:
- **Snap-to-neighbor**: Pieces snap to edges of existing buildables
  - Walls snap to floor edges
  - Ramps snap to floor edges
  - Gates align with walls
  - Floors stack on floors
- **Grid fallback**: If no nearby buildables, snap to 0.5m grid
- **Search radius**: 4m for nearby buildables, 1.5m snap distance

**Attachment Points**:
- Foundation → Foundation (adjacent in 4 directions, stacked on top)
- Foundation → Wall/Gate (4 edge positions with correct rotation)
- Foundation → Ramp (4 edge positions)
- Wall → Wall (side-by-side, stacked)
- Wall → Foundation (front/back)
- Ramp → Foundation (bottom/top of ramp)

### Melee Regression Fix

**Problem**: Melee weapons were potentially hitting targets outside intended arc.

**Fix**: Added 120° cone filter to RMB melee attack (matching LMB weapon melee):
- Melee only hits targets in front of player
- Uses mouse cursor for aim direction
- Cannot hit targets behind player
- Cannot hit at range (OverlapSphere with range limit)

### Truck Inventory Window Close with E

**Problem**: E key opened truck inventory but didn't close it consistently.

**Fix**: Fixed race condition where:
1. TransportTruckPanel.Update() would close the panel on E
2. TransportTruckController.Update() would immediately reopen it

**Solution**: Controller now only OPENS the panel; panel handles its own closing. Controller checks `HasOpenModal` before processing E key.

### Files Changed

1. `TopDownCameraController.cs` - Mouse-driven free camera, separate yaw tracking
2. `TacticalPlayerController.cs` - Camera-relative movement in free mode
3. `BuildablesService.cs` - Fortnite-style attachment point snapping
4. `PlayerCombatController.cs` - Cone filter for RMB melee
5. `TransportTruckController.cs` - Fixed E key race condition

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | Unlock camera, move mouse, stop → camera stops immediately | PASS |
| B | Unlock camera, rotate around player → no runaway spinning | PASS |
| C | Place floor → place wall on edge → snaps cleanly | PASS |
| D | Place floor → place ramp on edge → snaps cleanly | PASS |
| E | Place gate aligned with wall → snaps cleanly | PASS |
| F | Rotating pieces snap to 90° increments | PASS |
| G | Melee cannot hit targets outside intended range | PASS |
| H | Melee cannot hit targets behind player | PASS |
| I | Press E to open truck inventory → Press E again → closes | PASS |

## Milestone 7.5: Camera + Inventory Cleanup

### Camera — Foxhole-Style Fix

**Problem**: Camera lock caused twitch/jitter while locked. Unlock caused a weird spin.

**Solution**: Complete rewrite of camera rotation logic:
- **Single source of truth**: One `_cameraYaw` variable controls camera orientation
- **Locked mode**: Camera yaw is directly set to player yaw (no smoothing = no twitch)
- **Free mode**: Mouse X input accumulates into camera yaw
- **Transition locked→free**: Camera yaw stays exactly where it was (no spin)
- **Transition free→locked**: Camera snaps instantly to player facing

**Behavior**:
- **Lock (C)**: Camera instantly snaps behind player and follows cleanly
- **Unlock (C)**: Camera stays exactly where it was, no auto-rotate

### Truck Inventory — Count/Slots Fix

**Problem**: Player can carry 427 wood, but truck stops at 80. UI shows "count 80/80". Resources should NOT consume "count".

**Root Cause**: `TotalCount` was defined as sum of all quantities (`_items.Values.Sum()`), but was intended to represent distinct item types.

**Solution**:
- **Removed quantity-based count limit**: Resources are now limited by weight only
- **Slots = distinct item types**: Adding 427 wood uses 1 slot, not 427
- **New properties**:
  - `SlotsUsed`: Number of distinct item types
  - `DistinctItemCount`: Same as SlotsUsed
  - `TotalQuantity`: Sum of all item quantities (for display)
- **UI updated**: Shows "types X/Y" and "items N" instead of confusing "count"

**Rules**:
- Wood x 427 → uses 1 slot, adds appropriate weight
- 10 different items → 10 slots used
- Adding more of existing item → no new slot, just weight

### Storage Crates — Accessible Inventory

**Problem**: Storage boxes appeared to not have accessible inventory.

**Solution**:
- **Increased interaction range**: 2.5m (was 1.5m)
- **Dual detection**: Cursor raycast OR proximity search
  - Look at crate and press E, OR
  - Stand near any crate and press E (finds nearest)
- **Two-panel layout**: Player inventory on left, crate contents on right

### Inventory Transfer UX — Click-to-Transfer

**Problem**: Need a way to move items between inventories smoothly.

**Solution** (IMGUI-compatible):
- **Two-panel layout**: All inventory UIs now show both inventories side by side
- **Click transfer**: Click item name to transfer 1
- **Shift+Click transfer**: Shift+Click item name to transfer entire stack
- **Button shortcuts**: +5/-5/All buttons for quick amounts
- **Transfer All**: Buttons to move all resources in either direction
- **Error display**: Transfer failures show message at bottom of screen (auto-clears after 3s)

**New Files**:
- `InventoryTransferService.cs`: Shared transfer logic and error handling
  - `TryTransfer()`: Move items between any two inventories
  - `ITransferableInventory`: Interface for inventory systems
  - `InventoryDragState`: State tracking for future drag-drop

### Controls Update

| Key | Action |
|-----|--------|
| C | Toggle camera lock mode |
| E | Open nearby storage crate / Open truck inventory |
| Shift+Click | Quick-transfer entire stack |
| Click | Transfer 1 item |

### Files Changed

1. `TopDownCameraController.cs` - Foxhole-style camera with single yaw variable
2. `TacticalPlayerController.cs` - Unchanged (already camera-relative)
3. `TransportTruckController.cs` - Fixed count semantics (SlotsUsed, DistinctItemCount, TotalQuantity)
4. `TransportTruckPanel.cs` - Two-panel layout, shift+click, fixed display
5. `StorageCrate.cs` - Fixed count semantics
6. `StorageCratePanel.cs` - Two-panel layout, shift+click, fixed display
7. `BuildablesService.cs` - Improved crate interaction (proximity + raycast)
8. `InventoryTransferService.cs` (NEW) - Shared transfer logic and interfaces

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | Toggle lock on/off repeatedly while moving → zero twitch, zero spin | PASS |
| B | Lock results in camera aligned cleanly to player forward | PASS |
| C | Unlock leaves camera exactly where it was (no auto-rotate) | PASS |
| D | Put Wood x 427 into truck → count increases by 1 | PASS |
| E | Add 10 different item types → count=10 | PASS |
| F | Add more quantity of existing type → count unchanged | PASS |
| G | Place crate, interact → UI opens consistently | PASS |
| H | Items placed in crate remain after close/reopen | PASS |
| I | Shift+Click transfers entire stack | PASS |
| J | Click transfers 1 item | PASS |
| K | Transfer failures show error message | PASS |
| L | Two-panel layout shows both inventories | PASS |

## Milestone 8.1: Training Mission System

### Overview

The mission system gates the `infantry_1_rifleman` certification behind a training mission. New players spawn with only `recruit_basic` (can carry resources, cannot use weapons) and must complete "Basic Rifle Training" to unlock weapon use.

### Training Range

- **Location**: Position (25, 0, 25) in TacticalTest scene
- **Entry trigger**: Walking into the range area completes first objective
- **Targets**: 5 bullseye targets that respawn after being destroyed

### Controls

| Key | Action |
|-----|--------|
| **M** | Toggle mission panel (full view) |
| **F9** | (Debug) Complete rifle training mission |
| **F10** | (Debug) Reset all missions and revoke certs |
| **F11** | (Debug) Grant rifleman certification directly |

### Mission Flow

1. New player spawns with `recruit_basic` certification
2. `training_basic_rifle` mission auto-assigned
3. Player approaches training range → first objective completes
4. Player destroys 5 training targets → mission completes
5. Rewards granted: +5 Trust, `infantry_1_rifleman` certification
6. Player can now use rifles (`inf.basic` permission)

### Persistence

Mission progress saves to `Application.persistentDataPath/player_missions_v1`

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | New player starts with only `recruit_basic` | PASS |
| B | Training mission auto-assigned on start | PASS |
| C | Entering training range completes first objective | PASS |
| D | Destroying targets increments hit counter | PASS |
| E | After 5 hits, mission completes | PASS |
| F | Completion grants `infantry_1_rifleman` cert | PASS |
| G | Mission HUD shows active objectives | PASS |
| H | Debug hotkeys work (F9/F10/F11) | PASS |
| I | Mission progress persists across sessions | PASS |

## Milestone 8.2: Player Card System

### Overview

The Player Card system provides comprehensive player statistics tracking, name change history, and accountability features. It tracks everything from combat kills to logistics deliveries, mission completions, and leadership stats.

### Controls

| Key | Action |
|-----|--------|
| **P** | Toggle Player Card panel |
| **Shift+K** | (Debug) Add 5 kills |
| **Shift+T** | (Debug) Add 10 trust |
| **F12** | (Debug) Reset all stats |

### UI Tabs

1. **Overview**: Quick stats, active certifications, medals
2. **Combat**: Kills, deaths, damage, discipline
3. **Logistics**: Gathering, engineering, vehicles, missions
4. **Leadership**: Command stats, war record, discipline record
5. **History**: Account info, name change, name history

### Statistics Tracked

- **Combat**: Kills, deaths, damage dealt/taken, revives, friendly fire
- **Logistics**: Resources gathered/delivered, structures built/repaired
- **Leadership**: Orders issued, commendations, war participation
- **Discipline**: Imprisonment, certifications revoked, griefing reports
- **Meta**: Time served, name change history

### Persistence

Stats save to `Application.persistentDataPath/player_stats_v1`

### Acceptance Tests

| Test | Description | Status |
|------|-------------|--------|
| A | Press P opens Player Card panel | PASS |
| B | All 5 tabs display correctly | PASS |
| C | Killing NPCs increases kill count | PASS |
| D | Building structures increases build count | PASS |
| E | Completing missions increases mission count | PASS |
| F | Name change works and records history | PASS |
| G | Time served accumulates during session | PASS |
| H | Stats persist across sessions | PASS |
| I | Debug hotkeys work (Shift+K, Shift+T, F12) | PASS |
