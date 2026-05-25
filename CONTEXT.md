# Caretaker Context

Caretaker is a two-player asymmetric cooperative puzzle adventure built around separated timelines, isolated information, and causal effects from the past into the future. This context keeps the project's design language stable across DRD, DSD, level design, and implementation planning.

## Language

**Final Build**:
The expanded playable game version targeted for completion by November 2026.
_Avoid_: current DSD scope, prototype

**Prototype**:
The June 2026 validation slice that the current DSD describes.
_Avoid_: final build, demo without mechanics

**Prototype DSD**:
The current design specification that describes all intended prototype systems before implementation difficulty forces scope review.
_Avoid_: minimal slice spec, only-Must spec

**Time Causality**:
The core rule where a past player's action changes the future player's world state.
_Avoid_: time travel, timeline switching

**Immediate Causal Change**:
A causal effect that updates the future timeline as soon as the past action is accepted.
_Avoid_: delayed consequence, simulated years-later resolution

**Physical Causality**:
The one-way past-to-future change where a past action alters the future world's physical state.
_Avoid_: information exchange, hint sharing

**Major Interaction**:
A sustained cooperative interaction where both players work toward the same short-term goal through time-separated causality and information exchange.
_Avoid_: mission, minor interaction

**Mutual Dependency**:
The cooperative requirement that a problem cannot be solved by either player alone.
_Avoid_: optional cooperation, solo-solvable puzzle

**Minor Interaction**:
A smaller one-way causal interaction where one timeline affects the other without a sustained shared short-term puzzle.
_Avoid_: major interaction, mission

**Perceptible Causal Effect**:
A causal result that the affected player can clearly notice and connect to another player's action.
_Avoid_: invisible state change, flavor-only change

**Information Exchange**:
The two-way communication where players share observations, requests, codes, vulnerabilities, or experiment results.
_Avoid_: physical causality

**Radio System**:
The in-game push-to-talk voice communication system that allows only one active speaker at a time.
_Avoid_: external voice chat, always-on voice

**Radio Transmission Lock**:
The half-duplex rule where only one player can transmit voice at a time.
_Avoid_: full-duplex voice chat, simultaneous speaking

**Fixed Timeline Role**:
The rule that each player remains assigned to either past or future for the entire prototype stage.
_Avoid_: role swapping, timeline switching

**Network Role**:
The technical session authority role, either Host or Client.
_Avoid_: timeline role, past player, future player

**Timeline Role**:
The gameplay assignment, either Past or Future.
_Avoid_: host, client

**Information Isolation**:
The condition where each player can see only their own timeline and must communicate to share missing context.
_Avoid_: split-screen co-op, shared view

**Personal Minimap**:
The asymmetric minimap that shows only rooms and connections personally visited by the player in their own timeline.
_Avoid_: shared map, partner tracking map

**Personal Inventory**:
The simple bag system where each player carries items found in their own timeline.
_Avoid_: shared inventory, item trading

**Key Item**:
An inventory item used to unlock progress, satisfy an interaction condition, or identify the correct puzzle choice.
_Avoid_: consumable resource, inventory-management item

**Shared Failure**:
The rule where either player's failure sends both players back to the relevant checkpoint.
_Avoid_: individual respawn, solo fail state

**Checkpoint Spawn**:
The predefined past and future player positions used when loading a checkpoint.
_Avoid_: raw last position restore, manual save

**Split View Escape**:
The phase-3 climax presentation where both timelines are shown in a top-and-bottom split view during parallel escape routes.
_Avoid_: normal gameplay view, shared-screen puzzle mode

**Own-Character Control**:
The rule that each player can control only their assigned character even when both timelines are visible.
_Avoid_: shared control, partner control

**Phase**:
A major progression segment inside the research-lab stage.
_Avoid_: level, chapter, mission

**Stage**:
The complete research-lab play space made of three phases.
_Avoid_: phase, room

**Stage Contract**:
The shared planning document that aligns the whiteboxing map team and the planning/documentation team on rooms, phase flow, interactions, checkpoints, and alert propagation.
_Avoid_: final level design, full DSD

**Room**:
A bounded gameplay space inside the research-lab facility used for navigation, puzzle framing, and verbal location descriptions.
_Avoid_: map, scene

**Room Transition**:
Movement between rooms through doors, stairs, ducts, or collapse passages.
_Avoid_: seamless open map

**Alert State**:
A temporary facility-level danger state after detection that persists across room transitions.
_Avoid_: direct chase, permanent alarm

**Adjacent-Room Alert**:
The alarm propagation rule where the detected room and directly connected rooms enter heightened enemy behavior.
_Avoid_: global alarm, current-room-only alarm

**Room-Bounded Chase**:
The rule where normal enemies do not continue direct pursuit after the player transitions to another room.
_Avoid_: cross-room chase, instant safety

**Enemy Base State**:
The normal enemy behavior state for patrol or idle surveillance.
_Avoid_: alert, chase

**Enemy Alert State**:
The heightened enemy behavior state with wider detection range or view after an alarm.
_Avoid_: base patrol, chase

**Enemy Chase State**:
The enemy behavior state where the enemy actively pursues a detected player.
_Avoid_: alert patrol, search only

**Shared Enemy State Model**:
The common enemy FSM used by both past and future enemies, with timeline-specific sensing, movement, and presentation.
_Avoid_: separate AI systems per timeline

**Escape Room**:
The long phase-3 room built for continuous split-view escape running.
_Avoid_: normal room, room cluster

**Parallel Facility**:
The shared research-lab room structure that exists in both past and future timelines with different access, hazards, and object states.
_Avoid_: separate maps, unrelated timelines

## Relationships

- The current DSD describes the **Prototype**, not the **Final Build**.
- The **Final Build** expands the **Prototype** after June 2026.
- The **Prototype** validates a playable slice of the planned **Final Build**.
- The **Prototype DSD** should specify every intended prototype system first, then revise scope only when implementation evidence requires it.
- The **Prototype** includes one complete **Stage**.
- The **Stage Contract** is the first shared artifact between the whiteboxing map team and the planning/documentation team.
- A **Stage** contains exactly three **Phases** in the current prototype plan.
- **Time Causality** depends on **Information Isolation** to make communication necessary rather than optional.
- **Time Causality** uses **Immediate Causal Change** in the prototype.
- **Physical Causality** flows only from past to future in the prototype.
- The prototype includes Major Interactions M1 through M4, each represented by one primary cooperative puzzle.
- Every **Major Interaction** requires **Mutual Dependency**.
- **Minor Interaction** supports smaller causal moments between **Major Interactions**.
- Every **Minor Interaction** should produce a **Perceptible Causal Effect**.
- **Information Exchange** can flow both ways between past and future players.
- **Information Exchange** is primarily carried through the **Radio System** in the prototype design.
- The **Radio System** uses a **Radio Transmission Lock**.
- **Information Isolation** is the normal view rule for phases 1 and 2.
- **Split View Escape** is the phase-3 exception to normal **Information Isolation**.
- **Split View Escape** still uses **Own-Character Control**.
- The **Personal Minimap** must preserve **Information Isolation** by hiding partner position and partner-only discoveries.
- **Personal Inventory** is separate per player and does not support direct item transfer.
- **Personal Inventory** is centered on **Key Items**, not resource management.
- The prototype uses **Shared Failure** for detection, capture, and escape failure outcomes.
- Checkpoint restore uses **Checkpoint Spawn** positions rather than arbitrary last-known player coordinates.
- Each player has a **Fixed Timeline Role** for the full **Stage**.
- **Network Role** and **Timeline Role** are independent concepts.
- The past and future timelines use a **Parallel Facility** rather than unrelated layouts.
- A **Phase** contains one or more **Rooms**.
- A **Room** can contain triggers, receivers, AI threats, and information objects.
- Phases 1 and 2 use **Room Transition** for facility traversal.
- Normal direct chase can end at **Room Transition**, but **Alert State** persists across rooms.
- **Alert State** uses **Adjacent-Room Alert** in phases 1 and 2.
- Normal enemies use **Room-Bounded Chase** in phases 1 and 2.
- **Alert State** moves enemies from **Enemy Base State** into **Enemy Alert State**.
- Enemy behavior uses **Enemy Base State**, **Enemy Alert State**, and **Enemy Chase State** as the shared state model.
- Past and future enemies use the **Shared Enemy State Model** with different parameters and presentations.
- Phase 3 uses an **Escape Room** instead of normal room-by-room traversal.

## Example Dialogue

> **Dev:** "Should the DSD describe only the June prototype?"
> **Domain expert:** "Yes. The current DSD targets the June **Prototype**, and we will expand it toward the November **Final Build** after validation."

> **Dev:** "Should we leave AI, inventory, minimap, and split view vague until we know they are feasible?"
> **Domain expert:** "No. The **Prototype DSD** should specify the intended design first, then we revisit if implementation proves a system too costly."

> **Dev:** "Is the prototype only a few representative rooms?"
> **Domain expert:** "No. The **Prototype** should let players traverse the full research-lab **Stage**, with all three **Phases** present."

> **Dev:** "How do the map team and documentation team work in parallel?"
> **Domain expert:** "They first align through the **Stage Contract**, then each team elaborates its own artifacts."

> **Dev:** "Do past and future use different maps?"
> **Domain expert:** "No. They use a **Parallel Facility**: the same room structure with different access rules, hazards, and object states."

> **Dev:** "Should a past action take time before it affects the future?"
> **Domain expert:** "No. The prototype uses **Immediate Causal Change** so players can connect cause and effect clearly."

> **Dev:** "Can the future player change the past world?"
> **Domain expert:** "No. **Physical Causality** is past-to-future only, but **Information Exchange** can still go both ways."

> **Dev:** "Are M1 through M4 just missions?"
> **Domain expert:** "No. They are **Major Interactions**: dense cooperative moments around a shared short-term goal."

> **Dev:** "Does a major interaction require physical causality in both directions?"
> **Domain expert:** "No. It requires **Mutual Dependency**, usually through past-to-future physical causality plus two-way information exchange."

> **Dev:** "Can a minor interaction be a hidden backend state change?"
> **Domain expert:** "No. It should create a **Perceptible Causal Effect** that the affected player can notice."

> **Dev:** "Can players switch timelines during the prototype?"
> **Domain expert:** "No. Each player keeps a **Fixed Timeline Role** from start to finish."

> **Dev:** "Is the Host always the past player?"
> **Domain expert:** "No. **Network Role** controls authority, while **Timeline Role** controls gameplay perspective."

> **Dev:** "Can the prototype assume Discord for player communication?"
> **Domain expert:** "No. The **Prototype DSD** should specify the in-game **Radio System** with push-to-talk and one-way speaking constraints, even if implementation risk is reviewed later."

> **Dev:** "Can both players speak over each other?"
> **Domain expert:** "No. The **Radio Transmission Lock** makes the radio half-duplex: one transmitter, one listener."

> **Dev:** "Does split view replace information isolation?"
> **Domain expert:** "No. **Information Isolation** is the normal rule, while **Split View Escape** is a phase-3 climax exception for parallel escape routes."

> **Dev:** "Can a player control the partner during split view?"
> **Domain expert:** "No. Even in **Split View Escape**, each player keeps **Own-Character Control**."

> **Dev:** "Can the minimap show where the partner is?"
> **Domain expert:** "No. The **Personal Minimap** shows only the player's own visited rooms and connections."

> **Dev:** "Can players pass items to each other?"
> **Domain expert:** "No. **Personal Inventory** is a simple per-player bag for items found in that player's own timeline."

> **Dev:** "Is inventory management a core challenge?"
> **Domain expert:** "No. The inventory is mainly for **Key Items** used in progression and puzzle conditions."

> **Dev:** "If only one player gets caught, does only that player reset?"
> **Domain expert:** "No. The prototype uses **Shared Failure**, so both players return to the checkpoint."

> **Dev:** "Do checkpoints restore the exact coordinates where players last stood?"
> **Domain expert:** "No. Each checkpoint uses predefined **Checkpoint Spawn** positions for both timelines."

> **Dev:** "Can players escape detection just by moving to another room?"
> **Domain expert:** "Direct chase may stop at **Room Transition**, but **Alert State** persists and makes nearby rooms more dangerous."

> **Dev:** "What does alarm change in enemy behavior?"
> **Domain expert:** "Alarm moves enemies from **Enemy Base State** to **Enemy Alert State**, increasing detection pressure before any **Enemy Chase State** begins."

> **Dev:** "Does one detection alert the whole facility?"
> **Domain expert:** "No. **Adjacent-Room Alert** affects the detected room and directly connected rooms."

> **Dev:** "Can normal enemies chase through doors into another room?"
> **Domain expert:** "No. They use **Room-Bounded Chase**; direct pursuit stops at room transition, while alert pressure remains."

> **Dev:** "Do past guards and future drones need separate AI systems?"
> **Domain expert:** "No. They share the **Shared Enemy State Model**, then differ by movement, sensing, and visuals."

> **Dev:** "Does phase 3 use the same room transition rhythm?"
> **Domain expert:** "No. Phase 3 uses a long **Escape Room** for continuous running."

## Flagged Ambiguities

- "DSD scope" could mean either the June prototype or the November final build; resolved: the current DSD targets the June **Prototype**, then expands toward the **Final Build** after June 2026.
- "Prototype scope" could mean a minimal core-loop-only slice; resolved: the **Prototype DSD** specifies all intended prototype systems up front and defers cuts until implementation review.
- "One stage" could be confused with one phase or one room cluster; resolved: the **Stage** is the full research-lab play space, and it contains all three **Phases**.
- "Stage Contract" could be mistaken for the final level design; resolved: it is the shared coordination artifact before detailed whiteboxing and DSD completion.
- "Past map" and "future map" could imply unrelated level layouts; resolved: both timelines share a **Parallel Facility**.
- "Time causality" could imply delayed timeline simulation; resolved: prototype effects are **Immediate Causal Change**.
- "Causality" could include hints or hacking information; resolved: **Physical Causality** is distinct from **Information Exchange**.
- "M1/M2/M3/M4" could be mistaken for ordinary missions; resolved: they are **Major Interactions**.
- "Mutual interaction" could imply two-way physical causality; resolved: it means **Mutual Dependency** in solving the problem.
- "Minor" could imply unimportant or invisible; resolved: **Minor Interaction** still needs a **Perceptible Causal Effect**.
- "Past player" and "future player" are not temporary states; resolved: they are **Fixed Timeline Roles**.
- "Host" and "Past" could be conflated; resolved: **Network Role** and **Timeline Role** are separate.
- "Radio system" could be mistaken for external voice chat plus UI; resolved: it means in-game push-to-talk voice communication.
- "One-way radio" could imply only one timeline can ever talk; resolved: either player can talk, but the **Radio Transmission Lock** allows only one active speaker at a time.
- "Split view" could imply the whole game shares both screens; resolved: **Split View Escape** applies only to phase 3.
- "Split view" could imply shared control; resolved: **Own-Character Control** remains active.
- "Minimap" could imply shared team knowledge; resolved: the **Personal Minimap** is asymmetric and private.
- "Inventory" could imply shared team storage; resolved: each player has a **Personal Inventory** with no direct item transfer.
- "Item" could imply survival-resource management; resolved: prototype inventory is **Key Item** centered.
- "Failure" could imply individual respawn; resolved: either player's failure triggers **Shared Failure**.
- "Checkpoint" could imply raw position save; resolved: restore uses predefined **Checkpoint Spawn** positions.
- "Room transition" could imply instant safety; resolved: **Alert State** persists after detection.
- "Phase 3 room" could be confused with a normal room; resolved: the **Escape Room** is a long continuous-run space.
- "Alert" could mean direct pursuit; resolved: **Enemy Alert State** is heightened surveillance, while **Enemy Chase State** is active pursuit.
- "Alarm" could imply a global facility state; resolved: phases 1 and 2 use **Adjacent-Room Alert**.
- "Room transition" could imply enemies follow through every door; resolved: normal enemies use **Room-Bounded Chase**.
- "Past enemy" and "future enemy" could imply separate AI architectures; resolved: both use the **Shared Enemy State Model**.
