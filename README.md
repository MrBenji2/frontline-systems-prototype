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
