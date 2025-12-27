# Frontline System – Master Working Document for Opus

## Purpose

This file is the living, append‑only working document for Frontline System, a modern persistent war game inspired by Foxhole. It acts as the memory and context for all future development work. Every new feature, change or clarification MUST be appended here rather than rewriting existing entries. The goal is to keep Opus 4.5 and GPT‑5.2 aligned on vision, constraints and prior decisions so they can build upon the same foundations.

---

## Vision

Frontline System is a persistent war game inspired by Foxhole but modernized. Players fight across huge maps in a war that continues whether they are online or not—wars last days or weeks, and success depends on coordination, logistics and infrastructure rather than individual K/D ratio. In Foxhole, the war persists and players must mine and transport resources manually; supply chains win wars. Frontline System aims to improve on that formula with modern weapons, deeper building and crafting systems, a certification‑based progression model and built‑in anti‑griefing.

### Key points from the original inspiration (for context)

- Foxhole is an RTS/MMO hybrid with a persistent war that continues when players log off. Large maps take 20–30 minutes to cross on foot and require coordination across front lines.
- Players must gather resources manually, refine them into explosives, weapons and equipment, and transport them to outposts. There are no automated harvesters—logistics are handled entirely by players. Armies with better supply chains win.
- Infrastructure is as valuable as weaponry. Cargo trucks, trains and manufacturing plants matter as much as tanks and rifles.

Frontline System will retain these principles but update them: modern weaponry (drone strikes, anti‑tank missiles, body armor), modular base‑building, and improved crafting/logistics interfaces to reduce needless friction. The game will run persistently across large regions and emphasise team‑driven logistics, construction and strategy.

---

## Direction

We intentionally avoid RPG power creep. Progression is about permission and responsibility, not numerical stat boosts. The design takes Star Wars Galaxies–style certification systems (players unlock roles through training) and strips away bloat:

- **Certifications grant access, not power.** Unlocking engineer certification allows you to build fortifications; medic certification allows you to heal; tanker certification allows you to operate armored vehicles. Certifications expire or must be renewed after balance changes.
- **There are no traditional XP levels or stat bonuses.** Your rifle accuracy remains unchanged at certification level 10 or level 1.
- **The only "progression" is trust and responsibility.** High‑impact actions (demolishing buildings, driving supply trains, issuing orders to AI) are gated behind certifications and rank to prevent griefing.

**Philosophy: systems teach players.** The game should implicitly instruct players how to contribute—when you first join, you spawn as an untrained recruit and can only carry supplies. After performing tasks and training, you receive certifications unlocking new roles. Missions and AI helpers (future milestones) will guide players, not a written tutorial.

---

## Design Pillars (Immutable)

- **Responsibility over power** – Progression grants responsibility and permissions, not raw stat boosts.
- **Permission, not stats** – Certifications unlock access to roles, vehicles and tools. They never modify weapon damage, armor values or speed.
- **Persistence across wars** – Wars run continuously across large maps and persist whether you are online or not. Structures, supply caches and logistics networks persist through server restarts. Player identity and trust persists across wars.
- **Anti‑griefing by design** – Systems should naturally limit the damage a malicious player can inflict. High‑impact actions require multiple players or prior certifications. Most actions are reversible or have soft caps.
- **Systems teach the game** – Players learn through doing. Missions, tasks and AI helpers show them how to gather, build, transport and fight.
- **Resume‑safe** – Any feature can be paused and resumed without wiping progress. The project itself must be developed in resume‑safe slices.
- **Bots and AI abide by the same rules** – AI helpers (BenjiBot and squad assistants) use the same certification and permission system as players. They cannot bypass restrictions.

---

## Core Game Loop

At a high level, Frontline System's loop mirrors Foxhole but modernized:

1. **Spawn and Deploy** – New players spawn at a logistics hub on their faction's side of the war. They select or are assigned a mission (gathering, building, logistics, front‑line combat, recon) and are guided by AI helpers.

2. **Gather Resources** – Players mine scrap, sulfur, components and fuel (or modern equivalents like steel, electronics, chemicals). Resource nodes are distributed across the map; gathering requires tools and vehicles. Resources have weight and must be hauled manually or via vehicles, similar to Foxhole.

3. **Process and Craft** – Raw resources are taken to refineries to be processed into building materials, explosives, weapon parts or fuel. Processed materials are then used at manufacturing plants to craft weapons, vehicles, equipment, medical supplies and structures. Crafting times and recipes can be tuned for balance.

4. **Build and Fortify** – Engineers and builders construct bases, walls, watchtowers, factories, supply depots and advanced fortifications (modern equivalents like radar arrays, anti‑air batteries, drone launchers). Building uses processed materials and requires certification.

5. **Supply and Transport** – Completed items and materials must be transported from the rear lines to the front via cargo trucks, trains, boats or aircraft. Logistics players plan routes, form convoys, schedule trains and protect supply lines. Efficient supply chains win wars.

6. **Fight and Hold** – Combat units use supplied equipment to capture and hold territory. Combat emphasises squad tactics, communication and combined arms (infantry, armor, artillery, drones). Respawning costs soldier supplies, which must also be delivered from the rear.

7. **Recon and Sabotage** – Specialized players carry out reconnaissance, sabotage and electronic warfare. Destroying or raiding enemy supply lines (fuel depots, factories, trains) can cripple them.

8. **Persist and Iterate** – The war continues when you log off. Bases decay over time if not maintained; supply caches run down; the front line shifts. Players return to repair, rebuild and adapt. After a war ends, the map resets but player certifications, trust and ranks persist.

---

## Player Roles and Certifications

To prevent griefing and create meaningful progression, every role requires a certification that must be earned or trained. Certifications confer permission to perform specific actions. They do not confer power or stat bonuses.

### Certification Examples

- **Recruit** – basic certification granted automatically on first login. Can carry resources, operate small vehicles, run missions and fire basic weapons. Cannot build structures or access heavy vehicles.
- **Logistician** – permission to operate cargo trucks, trains and supply depots. Must complete training missions (driving, loading, route planning) and pass an exam. Failure leads to decertification.
- **Engineer/Builder** – permission to place and upgrade fortifications, operate construction tools, and manage building sites. Must learn blueprint placement, structural integrity and team coordination.
- **Medic** – permission to use medical equipment, heal downed soldiers and operate field hospitals.
- **Armorer** – permission to craft and maintain heavy weapons and armor, including tanks, artillery, drones and anti‑aircraft systems. Must learn maintenance and safety protocols.
- **Field Commander** – ability to issue orders, set waypoints, manage squads and assign missions. Requires trust and high rank. Commands are suggestions; players can ignore them, but AI assistants will prioritise commands.
- **Specialist (Recon/Electronic Warfare)** – permission to use advanced devices such as drones, radar, hacking tools, guided missiles and jammers. Highly restricted and can cause great harm if misused.

### Certification Rules

- **Train to unlock** – Each certification has training missions or tasks supervised by AI. Once completed, players receive the cert. Some may require approval from field commanders.
- **Decay and renewal** – Certifications expire after a number of wars or after major balance updates. Players must retake training to stay current.
- **Revocation** – Misusing a permission (e.g., crashing a cargo train into a friendly depot) triggers an automated review. AI logs and human players can vote to revoke a cert temporarily. Severe griefing leads to permanent revocation.
- **No stat bonuses** – A medic can heal because they carry medkits, not because they have +20% healing skill. An engineer can build because they have tools and knowledge, not because they place structures 50% faster.

---

## Building and Crafting System

Building in Frontline System expands on Foxhole's engineering but modernizes it:

- **Modular Blueprints** – Structures are composed of modules (walls, gates, turrets, hangars, pipelines, radar dishes). Players can design bases using a blueprint editor. Blueprints require specific materials and time to construct.
- **Advanced Infrastructure** – In addition to trenches and pillboxes, players can build radar installations, satellite uplinks, drone launch pads, power plants and logistics hubs. These require certifications and multiple players to complete.
- **Upgrades and Maintenance** – Structures degrade over time and must be repaired. Upgrades allow installation of modern sensors, anti‑missile systems and improved fortification.
- **Manufacturing Lines** – Factories can be configured to produce different weapon types, ammunition, vehicles or drones. The manufacturing queue uses recipes and can be optimised by logistic players.
- **User Interface** – Building and crafting menus present clear recipes, material requirements and progress indicators. Players can queue tasks, schedule deliveries and manage factory output. A global supply dashboard shows resource levels across the faction.

---

## Logistics and Transport

Logistics are the backbone of victory. The design emphasises cooperation, scheduling and protection:

- **Resource Gathering** – Salvagers gather raw materials using hammers, drills, excavators or specialized harvest vehicles. Each resource has weight and must be carried to trucks or depots.
- **Refining and Production** – Refineries process raw materials into intermediate goods (e.g., crude oil to fuel, scrap to steel). Factories use these goods to craft equipment. Large facilities require power and may become targets.
- **Transportation Vehicles** – Cargo trucks, armored supply vehicles, trains, barges, helicopters and later drones carry supplies. Each has capacity, speed, fuel consumption and vulnerability. Players must plan convoys and schedule trains to avoid bottlenecks. Convoys should have escorts to deter ambushes.
- **Logistics Network** – Rails, roads, bridges and pipelines are constructed by players. Supply depots and warehouses act as nodes. The network persists across wars but can be sabotaged or captured.
- **Automation and AI** – Some repetitive tasks (e.g., ferrying resources along a safe rail line) may be assisted by AI drivers, but these AI require logistician certification and are subject to the same rules.

---

## Modern Weapons and Combat

Frontline System uses modern era weaponry while remaining balanced and skill‑based:

- **Small Arms** – Assault rifles, battle rifles, SMGs, LMGs, shotguns and precision rifles, each with realistic ballistics. Weapons jam or overheat if abused.
- **Heavy Weapons** – Machine guns, mortars, anti‑tank missiles, MANPADS (shoulder‑fired anti‑air), recoilless rifles. Require ammo and may require certifications.
- **Vehicles** – Light armored vehicles, APCs, MBTs (main battle tanks), SPAAGs (self‑propelled anti‑air guns), IFVs (infantry fighting vehicles), self‑propelled artillery and MLRS. Vehicles require fuel and maintenance.
- **Aircraft and Drones** – Transport helicopters, attack helicopters, reconnaissance drones, armed drones. Drones require specialist certification and have limited endurance.
- **Support Tools** – Smoke grenades, gas masks, ballistic shields, laser designators for air strikes. Electronic warfare tools can jam communications or hack enemy drones.
- **Combat Mechanics** – Combat emphasises line of sight, cover, suppression and combined arms. Realistic ballistics (bullet drop, penetration) and limited health. Friendly fire is possible but mitigated by certifications.

---

## Anti‑Griefing Architecture

Malicious players can ruin persistent worlds. Anti‑griefing is built into every system:

- **Permission Gating** – High‑impact actions require certifications or multi‑player consensus. Example: demolishing a building requires two engineers or a commander's authorization.
- **Structural Interlocks** – Buildings and vehicles can include interlock codes. Only authorized players can operate heavy vehicles; unauthorized use triggers alarms.
- **Limited Friendly Fire** – Weapon fire can harm friendly players but quickly flags the shooter. Repeated friendly fire disables the weapon and suspends the shooter's certification.
- **Reversible Actions** – Most actions (plowing roads, moving supplies) are reversible. If someone dumps supplies in a river, the supplies can be recovered.
- **Audit Logs and Reputation** – All high‑impact actions generate logs. Players can review logs to identify saboteurs. Reputation/Trust score influences certification eligibility. A low trust score limits roles.
- **Structural Decay** – Structures degrade without maintenance, preventing players from permanently blocking progress. Abandoned player‑built roadblocks collapse over time.

---

## Persistence and Data

- **Persistent World** – The war runs 24/7 across large regions. The server stores the state of every building, vehicle, inventory and supply cache. War states (front line positions, player ranks) persist across sessions.
- **War Cycles** – A war ends when one faction meets its victory conditions (capture all control nodes). After a war, the map resets but certain infrastructure (major rail lines, towns) may persist. Player certifications, trust and rank persist.
- **Data Storage** – Use authoritative server simulation with determinism and snapshotting. Use SQL/NoSQL databases for persistent objects. Provide periodic checkpointing for crash recovery.
- **Resume‑Safe Development** – Development tasks must be delivered in slices that can be tested, paused and resumed without world wipes. Feature toggles and migrations ensure backward compatibility.

---

## AI Helpers and Bot Parity (Future Milestones)

- **BenjiBot and Squad AI** – AI assistants will help new players gather, build and fight. They are subordinate to player orders and abide by the same certification restrictions. They cannot perform actions players are forbidden from performing.
- **Mission System** – A dynamic mission generator assigns tasks to players (e.g., "deliver 200 steel to Outpost Bravo," "repair the radar at Hill 17," "defend convoy under attack"). Completing missions increases trust and certification progression.
- **AI Limitations** – AI cannot circumvent logistics or supply requirements. They rely on delivered resources. They should not become a substitute for human players; they are helpers only.

---

## Milestone Ledger (Append‑Only)

The project is built in milestones. Each milestone is self‑contained, testable and does not break prior milestones. Below is a high‑level ledger of discussed milestones (assumed from previous conversations). Future milestones should append to this ledger with date and summary.

| Milestone | Summary | Systems Touched | What It Enables | Do Not Break |
|-----------|---------|-----------------|-----------------|--------------|
| **1. Core War Loop & Resources** | Implement basic persistent war world with resource nodes, gathering mechanics, inventories, and manual transport. Build minimal networking and server persistence; wars persist when players are offline. | World simulation, resource harvesting, inventory system, server persistence, basic vehicles (handcarts, small trucks). | Enables players to gather resources and deliver them to depots. Establishes persistent server loop. | Do not introduce combat yet. Keep world state minimal but persistent. |
| **2. Building & Crafting** | Introduce simple building and crafting: trenches, sandbags, supply depots. Add crafting recipes for ammo and simple weapons. Implement blueprint placement and build times. | Building system, crafting recipes, resource consumption. | Allows players to construct defenses and produce basic arms. | Do not allow heavy fortifications or advanced weapons. Limit build radius. |
| **3. Modern Combat & Weapons** | Add modern small arms, heavy weapons and simple vehicles (jeeps, APCs). Implement ballistics, recoil, suppression and simple armor. Introduce respawn mechanics and soldier supplies. | Combat system, weapon models, respawn/medical system. | Enables players to fight and hold territory. | Avoid adding heavy tanks or aircraft. Ensure combat remains balanced without certifications. |
| **4. Logistics & Transport** | Expand logistics: cargo trucks, trains, boats. Add rail/road construction, supply depots, fuel stations. Implement weight limits and fuel consumption. | Logistics network, vehicles, infrastructure building. | Enables large‑scale supply chains and convoys. | Do not allow sabotage or bombing yet. Keep logistic routes safe. |
| **5. UI & User Experience** | Improve interfaces: inventory management, crafting menus, logistics dashboard, map overlays showing supply flow and control nodes. Add quick callouts, proximity voice and text chat. | UI/UX, comms, map system. | Helps players coordinate and understand supply lines and missions. | Avoid adding advanced mission system. Keep UI minimal but functional. |
| **6. Persistence & Data Management** | Implement war cycles, data storage, and checkpointing. Add decay mechanics for structures. Ensure server restarts do not wipe progress. | Database integration, snapshot system, decay mechanics. | Enables long wars and resets while preserving player certifications and trust. | Do not introduce rank/certification gating yet. |
| **7. Certification & Rank System** | Introduce certification framework: training missions, permission gates, rank/trust scoring, certification decay and revocation. Implement first certifications (logistician, engineer, medic). | Certification backend, training missions, trust/reputation system, anti‑griefing mechanisms. | Enables gating high‑impact actions; begins anti‑griefing. | Do not yet add AI assistants; ensure gating does not lock players out of progression. |
| **8. AI Helpers & Mission System** | Introduce AI assistants (BenjiBot) and dynamic missions. AI can accompany players, drive vehicles, defend convoys and instruct new recruits. Mission generator assigns tasks and tracks success. | AI system, mission framework, pathfinding, dialogue. | Helps new players, improves retention, and guides players into roles. | Ensure AI obeys certifications and cannot be used to circumvent restrictions. |
| **9. Advanced Structures & Specializations** | Add advanced structures (radar, anti‑air, drone pads, bridges) and specializations (armorer, commander, recon specialist). Introduce basic aircraft and drones. | Building system expansion, vehicle classes, specialization certifications. | Expands gameplay into modern warfare (air & EW). | Ensure these systems respect logistics and supply; do not overpower ground combat. |
| **10. Endgame & War Resolution** | Define victory conditions, war cycles, and post‑war resets. Implement legacy systems (e.g., persistent rail lines) and player‑built monuments commemorating contributions. | War resolution logic, reward system. | Gives wars an end and provides long‑term goals. | Do not wipe player certifications or trust; provide continuity into next war. |

*(Future milestones should be appended below with date, description and constraints.)*

---

## Implementation Approach

- **Architecture** – Use a client/server model. The server authoritatively simulates the world, persists state and resolves conflicts. Clients render the world and send input commands. Use deterministic simulation where possible to facilitate replays and debugging.
- **Networking** – Implement UDP‑based networking with reliable messaging for important events. Use interest management and server regions to reduce bandwidth. Provide anti‑cheat via server authority.
- **Data Persistence** – Use a relational database (e.g., PostgreSQL) for persistent objects (players, certifications, structures) and an in‑memory cache for fast simulation state. Snapshots and logs allow restoring after crashes.
- **Modular Codebase** – Organize the code by systems (resources, building, combat, logistics, certifications, AI). Each milestone should add or extend a module without rewriting others.
- **Testing & Tools** – Provide debug commands and simulation playback tools. Use unit tests for utility functions and integration tests for network and persistence. Build small maps for testing features before deploying them to the large persistent world.
- **Resume‑Safe Development** – Each milestone must be integrated behind a feature flag until complete. Data migrations must allow upgrading from previous versions without wiping progress. Document any required data changes in this file.

---

## Append‑Only Guidelines

1. **Do not delete or rewrite previous entries.** Use `### Update:` headings to clarify or supersede information.
2. **Record decisions and constraints along with the date.** Explain why a change was made or a feature was modified.
3. **Cite external sources** when describing mechanics inspired by other games or when documenting real‑world data (e.g., ballistic properties).
4. **Keep entries concise but specific.** Include enough detail for another developer (AI or human) to implement without assumption.
5. **Use headings and bullet points** for readability. Avoid dense paragraphs.

---

## Closing Note

This document should be loaded into Opus 4.5 (via Cursor) whenever new systems are implemented. It contains the authoritative vision, design pillars, milestone ledger and implementation notes. All future updates should append to this document to ensure consistency and prevent model drift. If any instruction here conflicts with a later milestone, the later milestone must explicitly override it and document the reason. All updates should be dated and cited. This ensures that Frontline System grows coherently into the persistent, modern war game envisioned at the outset.

---

## Update 2025‑12‑27 – Ranks & Progression, Cross‑Platform, Communication & Prison System

### Ranks & Progression

- **Long‑form progression**: Introduce an extensive rank ladder for both enlisted and officer tiers to maintain long‑term progression. The climb should be slow and tied to trust, certifications and contribution metrics (e.g., time served, missions completed, materials delivered, structures built, revives). Players should not "tap out" after a few sessions.

- **Enlisted ranks**: A suggested ladder might be Recruit → Private → Lance Corporal → Corporal → Sergeant → Staff Sergeant → Sergeant First Class → Master Sergeant → Sergeant Major. Each step unlocks small organisational permissions (e.g., ability to supervise a squad, manage a depot) but no combat stat bonuses.

- **Officer ranks**: A separate ladder for officers ensures responsibilities scale appropriately. An example ladder: Second Lieutenant → First Lieutenant → Captain → Major → Lieutenant Colonel → Colonel → Brigadier General → Major General → Lieutenant General → General → Field Marshal. Officers unlock permissions to command larger formations, access strategic maps and authorise high‑impact actions. Promotion requires existing officers of the target rank and a division size threshold.

- **Rank advancement conditions**: Officers and enlisted personnel do not automatically promote upon completing training. Promotions are conditional on the size of their division and the presence of peers. Officers gain rank when their division increases in numbers and there are other officers at the same rank. Factors such as time served, commendations, mission success, trust score and contributions will determine eligibility. Detailed factors are to be defined later.

- **War Commander / Field Marshal**: Each faction appoints a War Commander (Field Marshal) to coordinate high‑level strategy. The role may rotate each war based on rank, trust and contribution metrics. This ensures leadership stays fresh and accountable.

- **Player card**: A player's rank and comprehensive statistics (medals earned, kills, builds constructed, salvage delivered, revives, deaths, time served, name changes) are displayed on their Player Card. Name changes remain visible unless changed by administrative order, providing an additional layer of accountability for griefing.

### Cross‑Platform & Interactive Universe

- **Platform‑specific playstyles**: Frontline System is cross‑platform, with each platform tailored to its strengths:
  - **PC**: Focuses on logistics, base building and command. PC players have a full UI for planning, resource management and strategic overview.
  - **Console**: Emphasises immersive combat and simplified construction/logistics. Controller‑friendly interfaces allow console players to participate actively on the front line without complex UI.
  - **Mobile**: Supports quick drop‑in sessions. Mobile players take on roles like medic, courier or spotter, or manage simple tactical tasks (e.g., operating drones, performing reconnaissance) without long playtime commitments.

- **Shared universe**: Regardless of platform, actions and progress persist across the same persistent war. Certifications, trust scores and ranks carry over. Platform‑specific features should complement each other rather than provide advantages. Cross‑platform communication is essential for coordination.

### In‑Game Communication & Messaging

- **Text‑based communication**: To support cross‑platform compatibility, communication is text‑only. Players can send messages within squads, divisions or across the faction.

- **Keyword flagging**: A logging system scans messages for important keywords (e.g., "attack," "sabotage," "betray," "help"). Messages containing these terms are flagged and stored with ±N characters of context. Flagged messages persist for review by officers and administrators.

- **Auto‑deletion**: Messages without flagged keywords automatically delete from both sender and reader after 72 hours or when a war resets. This preserves privacy and storage.

- **Review log**: Officers and admins can access a communication log showing flagged messages. Access should be limited to protect privacy. Only context around flagged terms is stored, not full conversations.

### Prison System

- **Alternative to bans**: Severe infractions (e.g., intentional sabotage, repeated friendly fire) result in players being placed in a Prison System rather than banned outright. Prisoners remain within the game but have restricted interactions.

- **Prison locations**: Prison environments may rotate between rear‑line logistics hubs (assigning prisoners to low‑impact tasks like crafting ammo or sorting resources) and penal combat zones in enemy‑controlled regions filled with bots. This allows prisoners to contribute while being separated from mainstream play.

- **Sentence mechanics**: Prison sentences can be time‑based or task‑based. Completing assigned tasks (e.g., producing X ammunition crates, defending a penal convoy) reduces the sentence. Communication is limited while in prison, and prisoners cannot hold certifications or ranks. Their Player Card notes the imprisonment.

### Permissions Registry (Example)

Below is a non‑exhaustive sample of permission strings used in the certification system:

- `inf.basic` – wield basic rifles and sidearms.
- `log.carry` – carry raw resources and processed materials.
- `eng.build_basic` – place and upgrade trenches, sandbags and supply depots.
- `eng.demolish` – execute controlled demolition of friendly structures (requires consensus of two engineers or one engineer plus commander).
- `veh.drive_truck` – operate cargo trucks and small vehicles.
- `veh.drive_train` – operate locomotives and schedule trains.
- `med.heal` – administer first aid and operate field hospitals.
- `cmd.issue_orders` – set waypoints and mission objectives for squads/divisions.
- `spec.drone` – deploy and control drones and guided munitions.

### Trust Score & Earning

- **Earning trust**: Trust score increases through completing missions without infractions, sustained activity (gathering, building, supplying) without griefing, positive peer and commander commendations, and successful leadership with minimal friendly casualties.

- **Losing trust**: Trust decreases through friendly fire incidents, sabotage, unauthorized actions, abandoning missions or convoys, and negative reviews.

- **Use of trust**: High trust scores are required for officer promotions and advanced certifications. Low trust restricts access to high‑impact roles.

### Milestone Ledger Updates

- **7.1–7.5 Completed (2025‑XX‑XX)**: Trust & Certification System implemented with ladders for Infantry, Logistics, Engineering and Vehicles. Permission‑based access control (PermissionGate), rank progression (Recruit → Lieutenant → Captain), certification expiration/versioning, audit logging for anti‑griefing and Closed Economy (DestroyedPool/CreatedPool). Modular buildables, logistics & transport, and weight‑based inventory added in earlier milestones.

- **11. Rank & Progression System**: Define and implement the long rank ladder with progression rules, officer promotion criteria, stat tracking and player card. Integrate with existing certification and trust systems.

- **12. Cross‑Platform & Communication**: Implement cross‑platform support (PC, console, mobile) with platform‑specific UI/UX. Add text‑based communication system with keyword flagging and auto‑deletion. Ensure persistent and interoperable universe across platforms.

- **13. Prison & Discipline System**: Introduce prison system as an alternative to bans. Implement penal tasks, restricted capabilities, sentence mechanics and stat card recording. Provide tools for officers/admins to manage disciplinary actions.

---

## Update 2025‑12‑27 – Implementation: Recruit Certification & Extended Rank Ladder

This update documents the specific implementation details for the recruit certification restrictions and extended rank ladder, aligning the codebase with the design principles established above.

### Recruit Certification (Implementation)

**Rationale**: The Direction section states "when you first join, you spawn as an untrained recruit and can only carry supplies." To enforce this, new players start with restricted permissions and must complete training to unlock weapon use.

**Implementation** (`definitions.json`):

```json
{
  "ladderId": "recruit",
  "displayName": "Recruit",
  "category": "Basic",
  "tiers": [
    {
      "tier": 1,
      "certId": "recruit_basic",
      "displayName": "Recruit",
      "permissions": ["log.carry", "inf.unarmed"],
      "requirements": { "type": "auto_grant" },
      "version": 1,
      "expires": { "mode": "never", "days": null }
    }
  ]
}
```

**Permissions granted at spawn**:
- `log.carry` – Can carry raw resources and processed materials
- `inf.unarmed` – Can move, interact, and perform basic actions (no weapons)

**Unlocking weapons**: Players earn `inf.basic` (rifle use) by completing the `training_basic_rifle` mission. The Infantry I (Rifleman) certification now requires this training:

```json
{
  "tier": 1,
  "certId": "infantry_1_rifleman",
  "displayName": "Rifleman",
  "permissions": ["inf.basic"],
  "requirements": { "type": "training_mission", "missionId": "training_basic_rifle" },
  ...
}
```

**Code change** (`TrustService.cs`): New player profiles are granted `recruit_basic` instead of `infantry_1_rifleman`.

### Extended Rank Ladder (Implementation)

**Rationale**: The Update 2025‑12‑27 section specifies a long‑form progression with separate enlisted and officer tracks. This prevents players from "tapping out" quickly and ties advancement to trust, contributions, and peer presence.

**Implementation** (`definitions.json`):

#### Enlisted Ranks (12 tiers)

| Rank ID | Display Name | Min Trust | Track |
|---------|--------------|-----------|-------|
| `e0` | Recruit | 0 | enlisted |
| `e1` | Private | 10 | enlisted |
| `e2` | Private First Class | 18 | enlisted |
| `e3` | Lance Corporal | 28 | enlisted |
| `e4` | Corporal | 40 | enlisted |
| `e5` | Sergeant | 55 | enlisted |
| `e6` | Staff Sergeant | 75 | enlisted |
| `e7` | Sergeant First Class | 100 | enlisted |
| `e8` | Master Sergeant | 130 | enlisted |
| `e9` | First Sergeant | 165 | enlisted |
| `e10` | Sergeant Major | 200 | enlisted |
| `e11` | Command Sergeant Major | 250 | enlisted |

#### Officer Ranks (11 tiers)

| Rank ID | Display Name | Min Trust | Track |
|---------|--------------|-----------|-------|
| `o1` | Second Lieutenant | 300 | officer |
| `o2` | First Lieutenant | 400 | officer |
| `o3` | Captain | 500 | officer |
| `o4` | Major | 650 | officer |
| `o5` | Lieutenant Colonel | 800 | officer |
| `o6` | Colonel | 1000 | officer |
| `o7` | Brigadier General | 1500 | officer |
| `o8` | Major General | 2000 | officer |
| `o9` | Lieutenant General | 3000 | officer |
| `o10` | General | 5000 | officer |
| `o11` | Field Marshal | 10000 | officer |

**Code changes**:
- `RankDefinition.cs`: Added `track` field ("enlisted" or "officer")
- `TrustService.cs`: Updated fallback rank ID from `r0` to `e0`
- `PlayerTrustState.cs`: Updated default rank ID to `e0`

**Future work**: Officer promotions should additionally require:
- Division size threshold (number of players under command)
- Presence of peer officers at the same rank
- Mission success rate and commendations
- Time served requirements

These conditions are documented in the design but not yet implemented in code.

---

## Update 2025‑12‑27 – Implementation: Mission System (Milestone 8.1)

This update documents the implementation of the training mission system that gates the `infantry_1_rifleman` certification, completing the recruit-to-rifleman progression loop.

### Mission System Overview

The mission system provides:
- **Data-driven missions** defined in `definitions.json`
- **Objective tracking** with multiple objective types
- **Automatic reward grants** (trust points, certifications, items)
- **Persistence** across sessions
- **Auto-assignment** for training missions

### Core Components

| Component | File | Purpose |
|-----------|------|---------|
| `MissionDefinition` | `MissionDefinition.cs` | Data structure for mission definitions |
| `MissionObjectiveDefinition` | `MissionDefinition.cs` | Data structure for objective definitions |
| `MissionRewardsDefinition` | `MissionDefinition.cs` | Data structure for rewards |
| `PlayerMissionState` | `PlayerMissionState.cs` | Tracks player's mission progress |
| `MissionService` | `MissionService.cs` | Central service for mission management |
| `MissionHudPanel` | `MissionHudPanel.cs` | UI for mission tracking |
| `TrainingTarget` | `TrainingTarget.cs` | Target that reports hits to missions |
| `MissionLocationTrigger` | `MissionLocationTrigger.cs` | Trigger zone for reach objectives |
| `TrainingRangeSpawner` | `TrainingRangeSpawner.cs` | Spawns the rifle training range |
| `MissionDebugHotkeys` | `MissionDebugHotkeys.cs` | Debug controls for testing |
| `Milestone8Bootstrap` | `Milestone8Bootstrap.cs` | Initializes mission system |

### Objective Types

| Type | Description | Example |
|------|-------------|---------|
| `hit_target` | Hit a training target | Hit 5 training targets |
| `reach_location` | Enter a trigger zone | Reach the training range |
| `gather_resource` | Gather a specific resource | Gather 10 scrap |
| `deliver_resource` | Deliver resources to a depot | Deliver 10 scrap |
| `kill_npc` | Kill NPCs | Kill 3 enemies |
| `craft_item` | Craft an item | Craft a hammer |
| `build_structure` | Build a structure | Build a wall |

### Training Mission: Basic Rifle Training

**Mission ID**: `training_basic_rifle`

**Flow**:
1. New player spawns with `recruit_basic` certification (only `log.carry` + `inf.unarmed`)
2. Mission is **auto-assigned** on first login
3. Player must:
   - Approach the training range (reach_location objective)
   - Hit 5 training targets (hit_target objective)
4. On completion:
   - +5 Trust points awarded
   - `infantry_1_rifleman` certification granted
   - Player can now use rifles (`inf.basic` permission)

### Controls

| Key | Action |
|-----|--------|
| **M** | Toggle mission panel (full view) |
| **F9** | (Debug) Complete rifle training |
| **F10** | (Debug) Reset missions + revoke certs |
| **F11** | (Debug) Grant rifleman cert directly |

### Mission State Persistence

Mission progress saves to:
- `Application.persistentDataPath/player_missions_v1`

Saves on:
- Mission accept
- Objective progress
- Mission completion
- Mission abandon

### Integration Points

**MissionService → TrustService**:
- Rewards call `TrustService.GrantCertification()` and increment `trustScore`

**TrainingTarget → MissionService**:
- `Health.Died` event triggers `MissionService.ReportProgress("hit_target", targetId)`

**MissionLocationTrigger → MissionService**:
- `OnTriggerEnter` calls `MissionService.ReportProgress("reach_location", locationId)`

### JSON Schema (missions in definitions.json)

```json
{
  "missions": {
    "missions": [
      {
        "missionId": "training_basic_rifle",
        "displayName": "Basic Rifle Training",
        "description": "Complete the shooting course to earn your Rifleman certification.",
        "category": "training",
        "autoAssign": true,
        "repeatable": false,
        "requiredCertId": "",
        "requiredPermission": "",
        "objectives": [
          {
            "objectiveId": "approach_range",
            "description": "Approach the firing range",
            "type": "reach_location",
            "targetId": "training_range",
            "requiredCount": 1,
            "optional": false,
            "order": 1
          },
          {
            "objectiveId": "hit_targets",
            "description": "Hit training targets (5)",
            "type": "hit_target",
            "targetId": "training_target",
            "requiredCount": 5,
            "optional": false,
            "order": 2
          }
        ],
        "rewards": {
          "trustPoints": 5,
          "grantCertification": "infantry_1_rifleman",
          "items": []
        }
      }
    ]
  }
}
```

### Files Created/Modified

**New Files**:
- `Scripts/Systems/Missions/MissionDefinition.cs`
- `Scripts/Systems/Missions/PlayerMissionState.cs`
- `Scripts/Systems/Missions/MissionService.cs`
- `Scripts/Systems/Missions/TrainingTarget.cs`
- `Scripts/Systems/Missions/MissionLocationTrigger.cs`
- `Scripts/Systems/Missions/TrainingRangeSpawner.cs`
- `Scripts/Systems/Missions/MissionDebugHotkeys.cs`
- `Scripts/Core/Milestone8Bootstrap.cs`
- `Scripts/UI/MissionHudPanel.cs`

**Modified Files**:
- `Scripts/Definitions/GameDefinitions.cs` - Added `MissionsDefinitions missions` field
- `Resources/definitions.json` - Added `missions` section with training missions

### Verification Checklist

1. **New player flow**:
   - Start game with fresh save → player has `recruit_basic` only
   - `training_basic_rifle` mission auto-assigned
   - Cannot use weapons (no `inf.basic` permission)

2. **Training range**:
   - Navigate to training range at position (25, 0, 25)
   - First objective completes on entering trigger zone
   - 5 targets visible with bullseye textures

3. **Target shooting**:
   - Attack targets (LMB ranged or approach and melee)
   - Each target hit increments objective progress
   - Targets respawn after 2 seconds

4. **Mission completion**:
   - After 5 hits, mission completes
   - Notification shows "+5 Trust" and "Certification unlocked: infantry_1_rifleman"
   - Player can now use rifles

5. **Debug verification**:
   - F9 force-completes mission
   - F10 resets all progress
   - F11 grants cert directly (bypass mission)

### Future Enhancements

- **More training missions**: logistics, engineering, medic
- **Mission chains**: sequential missions that unlock next steps
- **Timed missions**: time limits with failure states
- **Dynamic missions**: generated based on war state
- **AI mission assignment**: BenjiBot suggests missions based on player skills

---

## Update 2025‑12‑27 – Implementation: Player Card System (Milestone 8.2)

This update documents the implementation of the Player Card system, which provides comprehensive player statistics tracking, name change history, and accountability features.

### Player Card Overview

The Player Card system provides:
- **Comprehensive stat tracking** across combat, logistics, engineering, and leadership
- **Name change history** with timestamps and admin-forced flags
- **Time served tracking** across sessions
- **Achievement and medal tracking**
- **Discipline record** (friendly fire, imprisonment, griefing reports)

### Core Components

| Component | File | Purpose |
|-----------|------|---------|
| `PlayerStats` | `PlayerStats.cs` | Data structure for all player statistics |
| `NameChangeRecord` | `PlayerStats.cs` | Records individual name changes |
| `PlayerCardData` | `PlayerStats.cs` | Combined data for UI display |
| `PlayerStatsService` | `PlayerStatsService.cs` | Central service for tracking and persisting stats |
| `CombatStatsTracker` | `CombatStatsTracker.cs` | Hooks into Health system for combat stats |
| `PlayerCardPanel` | `PlayerCardPanel.cs` | UI panel with tabs for different stat categories |
| `PlayerCardDebugHotkeys` | `PlayerCardDebugHotkeys.cs` | Debug controls for testing |

### Statistics Tracked

#### Combat Stats
- Kills (total, player, NPC)
- Deaths
- Damage dealt/taken
- Revives
- Healing done
- Friendly fire incidents

#### Logistics Stats
- Resources gathered
- Resources delivered
- Supply runs completed
- Cargo distance traveled

#### Engineering Stats
- Structures built
- Structures repaired
- Structures demolished
- Fortifications upgraded

#### Vehicle Stats
- Distance driven
- Vehicles operated
- Vehicle collisions

#### Mission Stats
- Missions completed (total, training)
- Missions abandoned
- Missions failed

#### War Stats
- Wars participated
- Wars won
- Time served (total across sessions)

#### Leadership Stats
- Orders issued
- Squad members commanded
- Largest division size
- Commendations received/given

#### Discipline Stats
- Times imprisoned
- Prison time served
- Certifications revoked
- Griefing reports (received, upheld)

### Controls

| Key | Action |
|-----|--------|
| **P** | Toggle Player Card panel |
| **Shift+K** | (Debug) Add 5 kills |
| **Shift+T** | (Debug) Add 10 trust |
| **F12** | (Debug) Reset all stats |

### Player Card UI Tabs

1. **Overview**: Quick stats, active certifications, medals
2. **Combat**: Kills, deaths, damage, medical stats, discipline
3. **Logistics**: Gathering, delivery, engineering, vehicles, missions
4. **Leadership**: Command stats, reputation, war record, discipline record
5. **History**: Account info, name change input, name history, achievements

### Name Change System

- Players can change their name via the Player Card → History tab
- Name must be 2-24 characters
- All name changes are recorded with:
  - Previous name
  - New name
  - UTC timestamp
  - Reason (optional)
  - Admin-forced flag
- Name history is visible on Player Card for accountability

### Persistence

Stats save to: `Application.persistentDataPath/player_stats_v1`

Auto-saves:
- Every 30 seconds
- On application quit
- On application pause

### Integration Points

**PlayerCombatController → CombatStatsTracker → PlayerStatsService**:
- Damage dealt/kills tracked on successful attacks

**BuildablesService → PlayerStatsService**:
- Structure built/repaired stats tracked

**MissionService → PlayerStatsService**:
- Mission completed/abandoned stats tracked

**Health.Damaged/Died → CombatStatsTracker**:
- Player damage taken/deaths tracked

### Files Created

- `Scripts/Systems/PlayerCard/PlayerStats.cs`
- `Scripts/Systems/PlayerCard/PlayerStatsService.cs`
- `Scripts/Systems/PlayerCard/CombatStatsTracker.cs`
- `Scripts/Systems/PlayerCard/PlayerCardDebugHotkeys.cs`
- `Scripts/UI/PlayerCardPanel.cs`

### Files Modified

- `Scripts/Core/Milestone8Bootstrap.cs` - Added PlayerCard services
- `Scripts/Combat/PlayerCombatController.cs` - Added stat tracking for damage/kills
- `Scripts/Buildables/BuildablesService.cs` - Added stat tracking for builds/repairs
- `Scripts/Systems/Missions/MissionService.cs` - Added stat tracking for mission completions

### Verification Checklist

1. **Player Card UI**:
   - Press P → Player Card opens
   - All 5 tabs display correctly
   - Stats update in real-time

2. **Combat tracking**:
   - Kill NPCs → kills stat increases
   - Take damage → damage taken stat increases

3. **Building tracking**:
   - Place structure → structures built stat increases
   - Repair structure → structures repaired stat increases

4. **Mission tracking**:
   - Complete mission → missions completed stat increases
   - Abandon mission → missions abandoned stat increases

5. **Name change**:
   - Change name in History tab → name updates
   - Name history shows previous names

6. **Time served**:
   - Play session → time served accumulates
   - Restart game → time served persists

### Future Enhancements

- **Officer promotion conditions**: Use stats (division size, time served, commendations) for eligibility
- **Medal system**: Award medals for stat milestones (100 kills, 1000 resources delivered, etc.)
- **Achievement system**: Unlock achievements for specific accomplishments
- **Leaderboards**: Compare stats across faction/war
- **API for external tools**: Export stats for websites/Discord bots

---

## Update 2025‑12‑27 – Implementation: Mission Terminal System (Milestone 8.3)

This update documents the SWG-inspired mission terminal system, enhancing the mission framework with terminal-based browsing, time limits, difficulty scaling, and location data.

### Star Wars Galaxies Inspiration

The terminal system draws from SWG's mission terminals:
- Players interact with terminals to browse available missions
- Missions display title, description, time limit, and rewards
- Difficulty scales with better rewards
- Destroy missions require eliminating targets; delivery missions involve transport
- Terminals filter missions by type (training, combat, logistics, etc.)

### Enhanced Mission Schema

#### MissionDefinition (New Fields)

| Field | Type | Description |
|-------|------|-------------|
| `difficulty` | int | Difficulty 1-10 (affects rewards, terminal filtering) |
| `requiredCertifications` | List<string> | All certs required to accept |
| `requiredPermissions` | List<string> | All permissions required to accept |
| `minTrustScore` | int | Minimum trust score required |
| `minRankId` | string | Minimum rank required (e.g., "e3") |
| `timeLimit` | float | Time limit in seconds (0 = no limit) |
| `repeatCooldown` | float | Cooldown between repeats (seconds) |
| `terminalAvailable` | bool | Whether this mission appears at terminals |
| `faction` | string | Faction alignment (empty = neutral) |
| `terminalTypes` | List<string> | Terminal types that can offer this mission |

#### MissionObjectiveDefinition (New Fields)

| Field | Type | Description |
|-------|------|-------------|
| `location` | MissionLocation | World coordinates (x, y, z) for waypoints |
| `locationRadius` | float | Radius around location to satisfy reach objectives |
| `duration` | float | Duration for timed objectives (e.g., defend_location) |

#### MissionRewardsDefinition (New Fields)

| Field | Type | Description |
|-------|------|-------------|
| `credits` | int | Currency awarded |
| `experience` | int | XP awarded (future system) |

#### MissionLocation Structure

```csharp
public sealed class MissionLocation
{
    public float x;
    public float y;
    public float z;
    public bool IsValid => x != 0 || y != 0 || z != 0;
    public Vector3 ToVector3();
    public float DistanceTo(Vector3 pos);
}
```

#### New Objective Types

| Type | Description |
|------|-------------|
| `defend_location` | Defend an area for a duration |
| `escort_target` | Escort a target to a destination |
| `sabotage_target` | Sabotage an enemy structure |
| `repair_structure` | Repair a damaged structure |

### Mission Terminal Component

Place `MissionTerminal` on interactable world objects:

```csharp
public sealed class MissionTerminal : MonoBehaviour
{
    [SerializeField] private string _terminalType = "general";
    [SerializeField] private string _displayName = "Mission Terminal";
    [SerializeField] private string _faction = "";
    [SerializeField] private float _interactionRange = 3f;
}
```

**Features**:
- Player proximity detection with range check
- Press [E] to interact when in range
- Filters missions by terminal type
- Caches available missions for performance
- Opens `MissionTerminalPanel` UI on interaction

### Mission Terminal UI

The `MissionTerminalPanel` provides an SWG-style interface:

| Section | Content |
|---------|---------|
| Category Filter | Filter by training, combat, logistics, etc. |
| Mission List | Scrollable list with difficulty stars |
| Mission Details | Full description, objectives, rewards |
| Accept Button | Accept selected mission |

**UI Features**:
- Difficulty shown as ★★★☆☆ (1-5 scale)
- Time limit displayed when applicable
- Objective locations shown with coordinates
- Rewards breakdown (trust, credits, XP, items, certs)

### Time Limit System

Missions with time limits are tracked automatically:

- `MissionService` checks time limits every 5 seconds
- `OnMissionTimeUpdate` event fires with seconds remaining
- `OnMissionFailed` event fires when time expires
- HUD displays time remaining with color coding:
  - Green: > 3 minutes
  - Yellow: 1-3 minutes
  - Red: < 1 minute

### Controls

| Key | Action |
|-----|--------|
| **E** | Interact with terminal (when in range) |
| **M** | Toggle mission HUD panel |
| **Escape** | Close terminal panel |

### Example Mission Definition (JSON)

```json
{
  "missionId": "training_basic_logistics",
  "displayName": "Basic Logistics Training",
  "description": "Learn to gather and transport resources.",
  "category": "training",
  "difficulty": 1,
  "requiredCertifications": ["recruit_basic"],
  "requiredPermissions": [],
  "minTrustScore": 0,
  "minRankId": "",
  "timeLimit": 900,
  "autoAssign": false,
  "repeatable": false,
  "repeatCooldown": 0,
  "terminalAvailable": true,
  "faction": "",
  "terminalTypes": ["training", "logistics"],
  "objectives": [
    {
      "objectiveId": "gather_scrap",
      "description": "Gather 10 units of scrap",
      "type": "gather_resource",
      "targetId": "mat_scrap",
      "requiredCount": 10,
      "optional": false,
      "order": 1,
      "location": { "x": 315.5, "y": 0.0, "z": 220.1 },
      "locationRadius": 25.0,
      "duration": 0
    },
    {
      "objectiveId": "deliver_scrap",
      "description": "Deliver the scrap to the logistics hub",
      "type": "deliver_resource",
      "targetId": "logistics_hub",
      "requiredCount": 10,
      "optional": false,
      "order": 2,
      "location": { "x": 298.0, "y": 0.0, "z": 205.0 },
      "locationRadius": 10.0,
      "duration": 0
    }
  ],
  "rewards": {
    "trustPoints": 5,
    "grantCertification": "logistics_1_runner",
    "items": [{ "itemId": "tool_wrench_wood", "quantity": 1 }],
    "credits": 50,
    "experience": 15
  }
}
```

### Files Created

- `Scripts/Systems/Missions/MissionTerminal.cs` - Terminal interaction component
- `Scripts/UI/MissionTerminalPanel.cs` - Terminal UI panel

### Files Modified

- `Scripts/Systems/Missions/MissionDefinition.cs` - Added new fields and MissionLocation
- `Scripts/Systems/Missions/MissionService.cs` - Added time tracking, location checking, terminal filtering
- `Scripts/UI/MissionHudPanel.cs` - Added time display, failure notifications
- `Scripts/Core/Milestone8Bootstrap.cs` - Added MissionTerminalPanel initialization
- `Resources/definitions.json` - Enhanced mission definitions with new schema

### New Missions Added

| Mission ID | Category | Difficulty | Time Limit | Rewards |
|------------|----------|------------|------------|---------|
| `training_basic_engineering` | training | 2 | 10 min | Builder cert, 75 credits |
| `training_basic_medic` | training | 2 | 5 min | 50 credits |
| `combat_destroy_outpost` | combat | 5 | 30 min | 300 credits, 15 trust |
| `recon_survey_area` | recon | 3 | 15 min | 150 credits |
| `defense_hold_position` | combat | 6 | 10 min | 400 credits, 20 trust |

### Verification Checklist

1. **Mission Terminal Interaction**:
   - Walk near terminal → prompt appears
   - Press E → terminal panel opens
   - Browse missions by category
   - Accept mission → panel updates

2. **Time Limits**:
   - Accept timed mission → timer shows in HUD
   - Timer counts down in real-time
   - Time expires → mission fails with notification

3. **Difficulty and Rewards**:
   - Higher difficulty missions show more stars
   - Rewards scale with difficulty
   - Credits and XP shown in preview

4. **Location Data**:
   - Objectives show coordinates when available
   - `GetObjectiveLocation()` returns world position

### Future Enhancements

- **Waypoint markers**: Draw objective locations on HUD/map
- **Terminal placement editor**: Designer tool for placing terminals
- **Dynamic mission generation**: Procedural missions based on war state
- **Mission chains**: Sequential missions that unlock next steps
- **Faction-specific terminals**: Different missions per faction
- **Elite missions**: High-difficulty, high-reward missions for experienced players
