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
  - **F2**: toggle fog overlay

Fog-of-war uses LOS raycasts against obstacles (Default layer). The test map spawns a ground plane (Ignore Raycast layer) and several occluder walls/blocks at runtime.
