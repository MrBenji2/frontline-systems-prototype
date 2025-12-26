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
