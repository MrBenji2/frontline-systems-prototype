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
