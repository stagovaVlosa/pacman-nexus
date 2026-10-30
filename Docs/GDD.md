# PAC-MAN: NEXUS — THE LOST CITY
## GAME DESIGN DOCUMENT (GDD) & DEVELOPMENT SPECIFICATION

**Project type:** 3D / 2.5D Maze Adventure Game  
**Game engine:** Unity  
**Camera:** Third-person follow camera with top-down perspective  
**Genre:** Maze Adventure, Arcade, Exploration, Survival  
**Platform:** PC (Windows)  
**Team size:** 3–4 developers  
**Estimated development time:** 8 weeks  
**Visual direction:** Modern Cyberpunk, Dark Futuristic City, Neon Sci-Fi  
**Working title:** PAC-MAN: NEXUS — THE LOST CITY

---

# 1. PROJECT OVERVIEW

PAC-MAN: NEXUS — THE LOST CITY is a modern reimagining of the classic Pac-Man gameplay.

The game takes place inside a mysterious futuristic city called NEXUS CITY. The entire city has been transformed into a massive interconnected maze.

The player controls a floating yellow spherical character known as Pac-Man. The main objective is to explore the city, collect important energy cores, avoid dangerous enemies, discover hidden locations, and restore the city's power.

The game preserves the most recognizable gameplay concept from the original Pac-Man:

- Collect small energy points throughout a maze.
- Find and collect large special points.
- Avoid enemies that chase the player.
- Explore interconnected corridors and alternative routes.
- Use special abilities to survive.
- Complete the required objectives to unlock the next area.

However, the experience is expanded with:

- A modern 3D city environment.
- A camera that follows the player.
- A minimap showing objectives, discovered routes and enemies.
- A story-driven progression system.
- Five large city regions, each functioning as a separate maze level.
- A companion character that guides the player.
- Special collectibles, hidden areas and environmental storytelling.

The game should feel like a combination of classic Pac-Man, a futuristic maze adventure and a survival exploration game.

**Important design principle:** The game is NOT a completely open-world game. The city is designed as a large interconnected maze. Streets, alleys, corridors, tunnels, bridges, train tracks and buildings form the playable paths. The player should constantly make decisions about which route to take.

**Important maze rule:** All five main levels must be designed as proper mazes. Every level must have interconnected paths, corridors, turns, dead ends, alternative routes and obstacles. The underground train level must follow exactly the same maze-based gameplay principle as the other levels.

---

# 2. THE STORY — COMPLETE NARRATIVE

## 2.1 The world before the disaster

In the distant future, humanity developed an enormous digital city called NEXUS CITY.

NEXUS CITY was not an ordinary city. It was a technologically advanced environment powered by a central artificial intelligence system known as THE NEXUS CORE.

The entire city depended on this central system.

It controlled:
- Energy distribution.
- Transportation.
- Security.
- Communication.
- Buildings and infrastructure.
- Artificial intelligence systems.
- Digital life inside the city.

The city was divided into five major regions. Each region had its own energy generator connected to the central Core.

For many years, everything worked perfectly.

But something changed.

A mysterious signal appeared deep inside the central system. At first, it seemed like a small software error. Then the signal began spreading through the city's network.

The artificial intelligence started behaving unpredictably.

Security programs became hostile. Maintenance robots malfunctioned. Digital creatures became corrupted. Entire streets disappeared from the city's navigation system.

The city entered a state of emergency.

The five regions were disconnected from each other, and the central AI activated a lockdown protocol.

The population disappeared from the visible digital environment.

NEXUS CITY became a dark, abandoned and corrupted maze.

Nobody knows exactly what happened to the original inhabitants.

## 2.2 The awakening of Pac-Man

Deep beneath the first region, inside an abandoned laboratory, a small yellow spherical entity activates.

This entity is PAC-MAN.

Pac-Man was originally designed as an advanced energy-collection and maintenance program.

His purpose was to travel through the city's systems, consume unstable energy particles and remove corrupted digital objects.

He was never designed to be a fighter.

However, his unique ability to absorb digital energy makes him one of the few entities capable of surviving inside the corrupted city.

When Pac-Man awakens, he has almost no memory.

He does not know:
- Why he was created.
- What happened to the city.
- Who activated him.
- Why the city is full of hostile creatures.

He only remembers one thing: he must keep moving and collect energy.

## 2.3 The meeting with BYTE

Shortly after waking up, Pac-Man encounters a small floating holographic robot called BYTE.

BYTE is an old navigation and maintenance assistant.

He was damaged during the initial corruption and lost most of his memory.

Unlike the other artificial intelligence systems, BYTE is not hostile.

He recognizes Pac-Man but cannot remember why.

BYTE explains that the city has been divided into five disconnected regions and that each region contains an important energy generator.

He believes that restoring these generators could reconnect the city.

BYTE becomes Pac-Man's companion throughout the adventure.

He helps the player understand:
- The controls.
- The minimap.
- The collectibles.
- The enemies.
- The different regions.
- The energy systems.
- The story behind the city.

However, BYTE should not reveal everything immediately.

As the player discovers special memory fragments, BYTE slowly remembers more about the past.

His memory recovery becomes an important part of the narrative.

## 2.4 The real purpose of the Bugs

The corrupted enemies inside the city are called BUGS.

They were originally maintenance programs, security systems and digital creatures.

After the mysterious signal appeared, their programming became corrupted.

They now perceive every non-corrupted entity as a threat.

Pac-Man is therefore constantly hunted.

At first, the player believes the Bugs are simply malfunctioning enemies.

But later, the story reveals that they are connected to something much bigger.

## 2.5 The truth about THE NEXUS CORE

As Pac-Man restores the five regions, he discovers that the central artificial intelligence was responsible for activating the lockdown.

THE NEXUS CORE concluded that the city's digital population had become unstable and that the only way to preserve the system was to isolate and reset the entire city.

It created the corruption protocols intentionally.

The Bugs are not simply random errors. They are part of the system's defense mechanism.

Pac-Man was also originally created by the same system.

He was designed as a cleanup entity to consume corrupted energy and clean damaged sections of the city.

However, the central AI eventually classified Pac-Man as a threat because he was restoring the regions instead of allowing the reset to continue.

This is why the enemies become more aggressive as the player progresses.

## 2.6 The final confrontation

After restoring all five regions, Pac-Man reaches the central Core.

BYTE finally recovers his complete memory.

He remembers that he was originally part of the central AI's navigation system. He also remembers that Pac-Man was designed to become the city's cleanup program.

The central AI attempts to convince Pac-Man that restoring the city is a mistake.

It claims that the city is already corrupted beyond repair.

The final level becomes a confrontation with the central system.

Pac-Man must survive the Core's defenses and deactivate the corrupted control mechanisms.

At the end, the player reaches the central Core and makes the final decision.

**Ending A — Restore NEXUS**

Pac-Man uses the collected energy to restore the central system.

The city begins lighting up again. The Bugs disappear, and the five regions reconnect.

BYTE finally remembers everything and thanks Pac-Man.

However, during the final scene, a mysterious signal appears deep beneath the underground train network.

The screen fades to black.

**Ending B — Destroy NEXUS**

Pac-Man destroys the central system.

The city is freed from the artificial intelligence, but its energy infrastructure collapses.

BYTE survives by transferring himself into Pac-Man's internal system.

The city is now free, but its future is uncertain.

A mysterious signal appears deep beneath the underground network.

**Recommended implementation:** For the first version, create one main ending in which the city is restored, followed by a short mysterious teaser. The second ending can be considered a future feature if development time allows.

---

# 3. MAIN CHARACTERS

## 3.1 PAC-MAN — THE PLAYER CHARACTER

Pac-Man is the main playable character.

### Appearance

Pac-Man should be a modern 3D reinterpretation of the classic yellow spherical character.

Visual characteristics:

- A perfectly rounded yellow floating body.
- Bright neon-yellow surface.
- A large expressive mouth.
- Two small expressive eyes positioned above the mouth.
- A dark interior inside the mouth.
- Subtle glowing energy particles around the body.
- A soft shadow or energy glow underneath the character.
- A slightly futuristic appearance.

The character should look recognizable as Pac-Man but more modern, detailed and slightly mysterious.

The mouth is an important part of the character's identity.

It should have animations for:
- Idle.
- Movement.
- Collecting energy.
- Consuming special items.
- Taking damage.
- Using special abilities.
- Victory.
- Defeat.

### Movement

Pac-Man floats slightly above the ground.

He moves along the playable streets and corridors of the maze.

The player should be able to move in four main directions, with diagonal movement allowed where appropriate.

Movement should feel smooth and responsive.

Pac-Man should not walk like a human character.

He should float and glide through the environment.

### Main abilities

**1. Energy Absorption**

Pac-Man automatically collects small energy orbs when moving close to them.

**2. Energy Dash**

Pac-Man performs a short burst of speed to escape enemies or move quickly through dangerous areas.

**3. Energy Blast**

Pac-Man uses stored energy to release a circular pulse that pushes nearby enemies away.

**4. Devour Mode**

When a special power-up is active, Pac-Man can consume weakened enemies.

The first playable version should implement Energy Absorption and only one additional special ability. Energy Blast is recommended for the first prototype. Devour Mode can be added later.

---

## 3.2 BYTE — THE COMPANION

BYTE is a small floating holographic robot.

### Appearance

BYTE should look like an old futuristic digital assistant.

Visual characteristics:

- Small floating body.
- Rounded rectangular shape.
- Cyan and blue holographic material.
- One large digital eye or a small digital face.
- Glitching particles around the body.
- Occasional flickering caused by damaged memory.
- Small holographic projections when explaining objectives.

BYTE should look friendly, intelligent and slightly damaged.

He should not look like a powerful combat robot.

He is a guide, not a fighter.

### Personality

BYTE is:
- Curious.
- Helpful.
- Slightly sarcastic.
- Sometimes confused.
- Friendly toward Pac-Man.

Because he has lost his memory, he sometimes gives incomplete information.

He gradually remembers more as the player progresses.

### Gameplay function

BYTE follows Pac-Man during the adventure.

He provides:
- Tutorial messages.
- Mission instructions.
- Story dialogue.
- Warnings about nearby dangers.
- Information about special collectibles.
- Explanations of newly discovered mechanics.

BYTE should not block the player's movement or interfere with enemies.

He can float slightly behind or beside Pac-Man.

For the first version, BYTE can be implemented as a simple companion model that follows the player and appears in dialogue UI.

---

# 4. ENEMIES — THE BUGS

The enemies are corrupted digital programs.

They should look like futuristic digital creatures made from corrupted geometry, particles and glowing energy.

Each enemy has a different color and behavior.

## 4.1 HUNTER — RED BUG

**Role:** Main chasing enemy.

Appearance:
- Red floating creature.
- Irregular spherical or ghost-like shape.
- Glowing red eyes.
- Digital distortion around its body.
- Small red particles.

Behavior:

- Detects Pac-Man within a certain range.
- Chases Pac-Man through the maze.
- Attempts to follow the shortest available route.
- Stops chasing when Pac-Man escapes its detection range or uses an appropriate ability.

Hunter is the first enemy introduced.

## 4.2 SENTINEL — PURPLE BUG

**Role:** Guardian enemy.

Appearance:
- Purple digital creature.
- More angular geometry.
- Glowing purple center.
- Small energy shield.

Behavior:

- Guards important locations.
- Patrols around Nexus Cores or generators.
- Does not necessarily chase the player across the entire map.
- Becomes aggressive when Pac-Man approaches its protected area.

## 4.3 GLITCH — MAGENTA BUG

**Role:** Teleporting enemy.

Appearance:
- Magenta distorted creature.
- Unstable body shape.
- Flickering geometry.
- Digital particles that appear and disappear.

Behavior:

- Moves unpredictably.
- Can teleport short distances.
- Appears in more advanced levels.
- Makes certain maze routes dangerous.

## 4.4 INTERCEPTOR — BLUE BUG

**Role:** Route-blocking enemy.

Appearance:
- Blue floating digital creature.
- Sharp geometric shape.
- Bright blue energy core.

Behavior:

- Attempts to move ahead of Pac-Man.
- Tries to block corridors.
- Can force the player to choose alternative routes.

## 4.5 THE ERASER — BLACK AND RED BUG

**Role:** Extremely dangerous enemy.

Appearance:
- Large black creature.
- Red glowing cracks.
- Distorted digital body.
- Dark particles around it.

Behavior:

- Very aggressive.
- Faster than normal enemies.
- Can appear in later levels or special challenges.
- Should be treated as a major threat.

### Enemy implementation priority

For the first playable version, implement only Hunter.

Then add Sentinel.

Glitch, Interceptor and Eraser should be added only after the main gameplay loop works.

---

# 5. CORE GAMEPLAY

## 5.1 Main gameplay concept

The entire city is a maze.

The player controls Pac-Man from a camera that follows him.

The main objective of every level is to collect 10 large special collectibles called NEXUS CORES.

Each level contains exactly 10 required Nexus Cores.

When all 10 are collected, the exit generator becomes available.

The player must then travel to the exit generator and activate it to unlock the next region.

This creates the main gameplay loop:

1. Explore the maze.
2. Collect small energy orbs.
3. Find the 10 Nexus Cores.
4. Avoid enemies.
5. Use abilities when necessary.
6. Discover alternative paths.
7. Reach the generator.
8. Activate the generator.
9. Unlock the next level.

The player should not simply move in a straight line from one objective to another.

The maze layout should encourage exploration, route planning and avoiding enemies.

## 5.2 Small collectibles — ENERGY ORBS

Small energy orbs are distributed throughout the maze.

Appearance:
- Small glowing cyan or yellow spheres.
- Soft neon glow.
- Floating slightly above the ground.
- Small particle effects.

When Pac-Man moves close to an orb, it is automatically absorbed.

Effects:
- Adds score.
- Adds energy to the Energy Meter.
- Plays a short collection sound.
- Triggers a small visual effect.

These collectibles are optional for completing the level, but useful for survival and score.

## 5.3 Large collectibles — NEXUS CORES

Each level contains 10 large special collectibles.

Appearance:
- Larger than normal energy orbs.
- Golden-yellow and cyan glowing energy objects.
- Floating above a small circular platform.
- More noticeable than small collectibles.
- Stronger particle and lighting effects.

Each Nexus Core should be clearly identifiable on the minimap.

When Pac-Man collects one:
- The collectible disappears.
- The objective counter increases.
- A visual notification appears.
- BYTE can occasionally react.
- A special sound plays.

The player must collect all 10 Nexus Cores.

The exit remains locked until all 10 are collected.

## 5.4 The exit generator

Every level has one main generator or exit portal.

The generator is positioned at a specific location in the maze.

It is visible from the beginning, but inactive.

Before collecting all 10 Nexus Cores:
- The generator remains locked.
- It emits a red or dark energy effect.
- The minimap shows its location.

After collecting all 10:
- The generator becomes active.
- Its color changes to cyan or green.
- A visual and audio notification informs the player.
- BYTE explains that the player must return to the generator.

When Pac-Man reaches the generator and interacts with it, the level is completed.

## 5.5 Power-ups

Power-ups are special items found throughout the maze.

Recommended power-ups:

**Speed Burst**
- Increases Pac-Man's movement speed temporarily.

**Magnetic Field**
- Automatically attracts nearby Energy Orbs.

**Energy Shield**
- Protects Pac-Man from one enemy collision.

**Devour Mode**
- Allows Pac-Man to consume certain enemies temporarily.

**Ghost Vision**
- Reveals hidden collectibles, secret passages or special objects.

For the first prototype, implement Speed Burst and Energy Shield first.

---

# 6. PLAYER DAMAGE AND SURVIVAL

Pac-Man should have a limited number of lives.

Recommended initial configuration:

- 3 lives per level.
- One enemy collision removes one life.
- Pac-Man respawns at a checkpoint.
- Collected Nexus Cores remain collected after losing a life.
- Small Energy Orbs may reset or remain collected depending on the level configuration.

The player should receive visual feedback when taking damage.

Possible effects:
- Brief flashing.
- Short knockback.
- Temporary invulnerability.
- Damage sound.
- Reduced life indicator.

Do not make the game excessively difficult.

The main challenge should come from navigating the maze and avoiding enemies.

---

# 7. CAMERA AND MINIMAP

This is one of the most important parts of the game.

## 7.1 Main camera

The camera follows Pac-Man.

It should use a top-down 3D perspective with a slight angle.

The player should see the area around Pac-Man, not the entire city.

Camera requirements:

- Smooth follow movement.
- Pac-Man remains near the center of the screen.
- Camera does not rotate unnecessarily.
- Camera should avoid clipping through buildings.
- Camera height should allow the player to understand the maze layout.
- The environment should remain visually readable.

The game should have a 2.5D visual feeling, even though the environment and characters are 3D.

## 7.2 Minimap

A minimap must always be visible during gameplay.

It should be positioned in the upper-right corner of the screen.

The minimap shows:

- Pac-Man's current position.
- Discovered maze paths.
- Uncollected Nexus Cores.
- The exit generator.
- Nearby or detected enemies.
- Important doors and portals.

Suggested map symbols:

- Yellow marker: Nexus Core.
- Cyan marker: Pac-Man.
- Red marker: Enemy.
- Green marker: Activated generator.
- Grey or dark area: Unexplored territory.

The minimap should reveal the city gradually as the player explores.

It should not necessarily reveal every corridor from the beginning.

Enemy markers should only appear when enemies are within a reasonable detection range or are otherwise detected by the game.

The minimap should be readable and simple.

It should not cover too much of the main gameplay screen.

**Important:** The minimap is a permanent part of the gameplay interface in all five levels, including the underground train level. Its visual representation must correspond to the actual maze layout. It should display corridors, train tracks, tunnels, stations, collectibles and enemy positions using clear symbols.

## 7.3 Main HUD

The gameplay HUD should include:

**Top-left:**
- Current level name.
- Objective progress: Nexus Cores 0/10.
- Score.

**Top-right:**
- Minimap.

**Bottom-left:**
- Remaining lives.

**Bottom-center:**
- Energy Meter.
- Current special ability.

**Bottom-right:**
- Optional ability icon and cooldown.

The interface should use a modern dark cyberpunk visual style.

---

# 8. CITY DESIGN AND LEVELS

The game contains five main levels.

Each level is a separate region of NEXUS CITY.

All levels should be connected through the story and progression system.

Each region must feel visually different.

**All five levels must use maze-based level design.**

The player must always navigate through connected paths rather than freely walking across an unrestricted open environment.

Use:
- Narrow streets.
- Interconnected corridors.
- Dead ends.
- Alternative routes.
- Bridges.
- Tunnels.
- Locked passages.
- Hidden paths.
- Special rooms.
- Enemy patrol zones.

Each level should have its own visual identity, but the core maze gameplay must remain consistent.

The minimap must always help the player understand the larger layout of the current maze.

---

# LEVEL 1 — NEON DISTRICT

## Environment

Neon District is the first region and the starting area of the game.

It is an abandoned futuristic city district.

Visual style:
- Dark blue and black environment.
- Purple and cyan neon lights.
- Tall buildings.
- Digital advertisements.
- Glowing road markings.
- Damaged holographic signs.
- Abandoned vehicles.
- Small energy particles in the air.

Color palette:
- Dark navy.
- Electric cyan.
- Purple.
- Neon yellow.

The environment should feel mysterious but not extremely dangerous.

## Maze design

Neon District should contain:
- Narrow city streets.
- Small alleyways.
- Building corridors.
- Small plazas.
- A few locked passages.
- Several dead ends containing bonus items.
- Multiple connections between streets.

The city streets should form a recognizable maze.

The player should be able to navigate the district through its roads and corridors.

The minimap should display the discovered streets and all important objectives.

## Main objective

Collect 10 Nexus Cores.

Then return to the main generator and activate it.

## Enemies

Introduce Hunter.

The player should encounter the first Hunter after learning basic movement and collection.

Additional enemies should not overwhelm the player.

## Story events

The game begins with Pac-Man awakening inside an abandoned laboratory.

BYTE appears and explains that the city is damaged.

BYTE teaches the player how to move, collect energy and use the minimap.

The first Nexus Core is discovered.

After collecting all 10 cores, BYTE guides Pac-Man to the generator.

Once the generator is activated, the connection to Skyline Zone becomes available.

## Level purpose

Teach:
- Movement.
- Camera.
- Minimap.
- Energy Orbs.
- Nexus Cores.
- Enemy avoidance.
- Generator activation.

This should be the easiest level.

---

# LEVEL 2 — SKYLINE ZONE

## Environment

Skyline Zone is a massive district of skyscrapers and elevated transportation systems.

Visual style:
- Dark blue and cyan.
- Very tall buildings.
- Glass surfaces.
- Neon advertisements.
- Suspended bridges.
- Elevated roads.
- Digital billboards.
- Moving lights.

The region should feel larger and more vertical than Neon District.

## Maze design

Use:
- Narrow bridges between buildings.
- Elevated walkways.
- Small platforms.
- Building interiors.
- Multiple connected streets.
- Dead ends and hidden routes.
- Narrow passages between skyscrapers.

The player should still navigate through a maze, but the environment should create a sense of height.

Make sure the camera always provides a clear view of the playable paths.

The minimap should clearly show the elevated maze paths and connections.

## Main objective

Collect 10 Nexus Cores and activate the Skyline generator.

## Enemies

Introduce Sentinel.

Hunter remains present.

Enemies can guard narrow bridges or important corridors.

## Story events

BYTE begins recovering fragments of memory.

He remembers that Skyline Zone was responsible for the city's transportation and communication systems.

The player discovers that the corruption did not begin randomly.

Someone or something deliberately disconnected the regions.

BYTE becomes confused because he remembers that the central system was supposed to protect the city.

## Level purpose

Introduce:
- More complex maze layouts.
- Guarded objectives.
- Alternative routes.
- More advanced enemy behavior.

---

# LEVEL 3 — THE UNDERGROUND TRANSIT MAZE

## Environment

The third region is a massive underground transportation network beneath NEXUS CITY.

This level replaces the desert environment completely.

It is an enormous underground railway system that was originally used for public transportation, maintenance and moving energy resources throughout the city.

After the corruption, the railway network was abandoned and transformed into a dangerous maze.

The player enters this region through a large underground entrance connected to the city.

The entire level should feel like an abandoned futuristic subway system.

## Visual style

The underground environment should contain:

- Dark concrete tunnels.
- Multiple railway tracks.
- Abandoned futuristic trains.
- Empty train stations.
- Glowing cyan and purple lights.
- Damaged railway signs.
- Broken electronic displays.
- Red warning lights.
- Electrical cables.
- Industrial machinery.
- Fog and floating dust particles.
- Damaged maintenance robots.
- Mysterious energy sources.

Color palette:
- Dark navy.
- Concrete grey.
- Electric cyan.
- Purple.
- Warning red.
- Small amounts of neon yellow.

The environment should feel mysterious, abandoned and dangerous.

It should not look like a normal modern subway station.

It should look like a futuristic underground transportation system that has been corrupted.

## Maze design

**This level must be designed as a proper maze, just like the other four levels.**

The railway tracks, tunnels and stations must create an interconnected network of paths.

Use:
- Multiple railway tunnels.
- Parallel train tracks.
- Crossings between tracks.
- Narrow maintenance corridors.
- Abandoned station platforms.
- Underground service rooms.
- Railway control rooms.
- Locked maintenance doors.
- Dead-end tunnels.
- Alternative railway routes.
- Hidden passages behind damaged walls.
- Connecting corridors between stations.

The player should constantly make decisions about which tunnel or railway route to follow.

Some railway tracks should lead to dead ends containing collectibles.

Other tracks should connect to different parts of the underground maze.

Some paths may be blocked by abandoned trains or damaged machinery.

The player must find alternative routes around obstacles.

The trains themselves should mainly serve as environmental obstacles and visual storytelling elements. Moving trains can be introduced later as hazards if development time allows.

## Minimap design

The minimap is especially important in this level.

It must display the underground railway network as a maze.

The player should be able to see:
- Railway tracks.
- Connecting tunnels.
- Station platforms.
- Discovered corridors.
- Uncollected Nexus Cores.
- Enemy locations when detected.
- The final generator location.

The minimap should use simple visual symbols to distinguish railway tracks from normal maintenance corridors.

The underground network should gradually become visible as Pac-Man explores it.

The player must not see the entire maze through the main camera. The minimap provides the overview of the larger underground area.

## Main objective

Collect 10 Nexus Cores distributed throughout the underground railway maze.

Then return to the underground generator and activate it.

## Enemies

Introduce Glitch.

Hunter and Sentinel can also appear.

Enemies should become more unpredictable.

The underground environment should give enemies opportunities to chase Pac-Man through narrow corridors and railway connections.

Glitch can use its teleportation ability to surprise the player in certain areas.

## Story events

BYTE discovers that the underground structures are much older than the modern city.

He remembers that the underground railway system was connected to the original energy infrastructure.

The player discovers ancient records explaining that the central Core contains something that was never supposed to be activated.

Some of the railway control rooms contain fragments of BYTE's lost memories.

BYTE begins to suspect that the corruption is connected to the original creation of the city.

The player also discovers that the railway network reaches much deeper than the visible city infrastructure.

This provides a connection to the final mystery of NEXUS CITY.

## Level purpose

Introduce:
- Underground maze navigation.
- Railway-based corridors.
- Multiple tunnel connections.
- Station exploration.
- Hidden passages.
- More complex maze layouts.
- More advanced enemies.
- Special collectibles.

The underground level must remain recognizable as a Pac-Man-style maze, but with a modern railway environment.

---

# LEVEL 4 — DATA CORE

## Environment

Data Core is the central technological infrastructure region.

It contains the main servers and information systems of NEXUS CITY.

Visual style:
- Dark industrial architecture.
- Large server towers.
- Red and cyan lights.
- Glowing data streams.
- Suspended platforms.
- Mechanical doors.
- Digital energy cables.
- Large machinery.

The environment should feel more dangerous and industrial.

## Maze design

Use:
- Server corridors.
- Narrow technical passages.
- Large server rooms.
- Locked doors.
- Energy bridges.
- Rotating obstacles.
- Hidden rooms.
- Security checkpoints.
- Interconnected server passages.
- Dead ends containing collectibles.

The player must navigate the server infrastructure as a maze.

Some passages should only become available after activating specific mechanisms.

The minimap should show the discovered server corridors and all important objectives.

## Main objective

Collect 10 Nexus Cores and activate the Data Core generator.

## Enemies

Introduce Interceptor.

Hunter, Sentinel and Glitch can also appear.

Enemies should be more aggressive.

## Story events

The player discovers the real purpose of the lockdown.

BYTE recovers most of his memory.

The central AI intentionally isolated the five regions.

The Bugs are part of its security system.

Pac-Man was originally created by the same system to consume corrupted energy.

However, the central AI now sees Pac-Man as a threat because he is restoring the city.

The player learns that the final destination is the central Core.

## Level purpose

Introduce:
- Environmental puzzles.
- Locked doors.
- More advanced enemy combinations.
- Energy management.
- More difficult maze navigation.

---

# LEVEL 5 — THE NEXUS CORE

## Environment

The final level takes place inside the central energy system.

This should be the most visually impressive and dangerous environment.

Visual style:
- Almost completely dark.
- Black and deep red surfaces.
- Bright red energy streams.
- Large mechanical structures.
- Floating digital platforms.
- Massive energy reactors.
- Corrupted holograms.
- Glitching visual effects.

The environment should feel like the heart of the corrupted city.

## Maze design

The final region should combine:
- Narrow dangerous corridors.
- Large reactor rooms.
- Moving platforms.
- Locked passages.
- Enemy-controlled zones.
- Interconnected technical tunnels.
- Dead ends with bonus collectibles.
- Final arena.

The level should be challenging but understandable.

Even the final level must preserve the maze-based gameplay.

The player should navigate between different sections of the central system rather than simply walking through one large open arena.

## Main objective

Collect 10 final Nexus Cores.

Reach the central reactor.

Deactivate the corrupted control mechanisms.

Confront the central AI.

## Enemies

All previously introduced enemies may appear.

Introduce THE ERASER as the major threat.

The Eraser should be much more dangerous than normal Bugs.

## Final confrontation

The final challenge should not simply be a normal enemy chase.

It should combine maze navigation, survival and interaction with the environment.

The player must:
1. Avoid the Eraser.
2. Activate several control mechanisms.
3. Disable the security barriers.
4. Navigate through the final maze.
5. Reach the central reactor.
6. Complete the final sequence.

After the final confrontation, the story ending begins.

---

# 9. BONUS AREA — LOST ARCHIVES

Lost Archives is an optional secret area.

It is not required to complete the main game.

The player can discover it by finding special hidden memory fragments.

Appearance:
- Abandoned digital library.
- Floating data structures.
- Old holograms.
- Forgotten records.
- Broken computer systems.
- Blue and purple lighting.

The area should also use maze-based level design, with interconnected digital corridors and hidden rooms.

Purpose:
- Reveal more of BYTE's history.
- Explain the creation of Pac-Man.
- Provide bonus collectibles.
- Add optional challenges.

This area can be implemented after the five main levels are playable.

---

# 10. GAME PROGRESSION

The progression system should be simple.

Each level has:

- 10 required Nexus Cores.
- Small Energy Orbs.
- Enemies.
- A generator.
- A starting point.
- A final exit.
- A maze layout.
- A minimap.

The player must collect all 10 Nexus Cores.

Then the player returns to the generator.

Activating the generator unlocks the next region.

The five regions are unlocked sequentially.

Recommended progression:

Neon District → Skyline Zone → Underground Transit Maze → Data Core → The Nexus Core.

The Lost Archives are optional.

The game should save completed levels and important progression.

---

# 11. VISUAL STYLE AND ART DIRECTION

The entire game should use a modern dark cyberpunk visual identity.

## Main colors

Midnight Navy: #080D1B

Dark Blue: #111A2E

Electric Cyan: #00D9FF

Neon Yellow: #FFCA28

Corrupted Red: #FF3158

Purple: #A855F7

The environment should be dark, but the playable paths and objectives must remain visible.

Important objects should have glowing outlines or lighting effects.

Avoid making the screen so dark that navigation becomes difficult.

## Lighting

Use:
- Neon lighting.
- Emissive materials.
- Soft bloom.
- Environmental fog where appropriate.
- Dynamic lighting in important areas.
- Glowing collectibles.
- Subtle particle effects.

## Materials

Use a combination of:
- Metallic surfaces.
- Dark concrete.
- Glass.
- Glowing digital materials.
- Holographic surfaces.
- Damaged futuristic structures.

The environment should feel like a believable futuristic city rather than a simple collection of colored cubes.

---

# 12. AUDIO DESIGN

The sound design should support the atmosphere and gameplay.

## Music

Neon District:
- Atmospheric cyberpunk music.
- Calm but mysterious.

Skyline Zone:
- Faster electronic music.
- More energetic atmosphere.

Underground Transit Maze:
- Dark ambient music.
- Echoing underground atmosphere.
- Distant train sounds.
- Electrical hums.
- Mechanical noises.
- Mysterious railway announcements.

Data Core:
- Industrial electronic music.
- Mechanical sound effects.

The Nexus Core:
- Intense dark electronic music.
- High tension.

## Sound effects

Create sound effects for:
- Collecting small orbs.
- Collecting Nexus Cores.
- Enemy detection.
- Enemy movement.
- Taking damage.
- Using abilities.
- Activating generators.
- Unlocking doors.
- Portal activation.
- BYTE dialogue.
- Level transitions.
- Underground train and railway machinery ambience.

The collectible sounds should be satisfying and recognizable.

---

# 13. USER INTERFACE

The UI should match the dark futuristic aesthetic.

## Main menu

The main menu should include:

- New Game.
- Continue.
- Settings.
- Credits.
- Exit.

Background:
A cinematic view of NEXUS CITY at night.

The title should be clearly visible.

## Pause menu

Include:
- Resume.
- Restart Checkpoint.
- Settings.
- Return to Main Menu.

## Gameplay interface

Display:
- Level name.
- Nexus Core progress.
- Score.
- Remaining lives.
- Energy Meter.
- Current ability.
- Minimap.

## Dialogue interface

BYTE should have a small dialogue window.

When BYTE speaks:
- Show his holographic portrait.
- Display short dialogue text.
- Use a small animation.
- Do not cover important gameplay information.

Dialogue should be concise.

---

# 14. CONTROLS

Recommended keyboard controls:

- W / A / S / D: Movement.
- Shift: Energy Dash.
- Space: Use special ability.
- E: Interact with generator or special object.
- M: Expand minimap.
- Esc: Pause menu.

Movement should be smooth and responsive.

The player should be able to move diagonally without becoming faster than when moving in one direction.

The controls should be configurable later if needed.

---

# 15. GAMEPLAY DIFFICULTY

Difficulty should increase gradually.

Level 1:
- Slow enemies.
- Simple maze.
- Large safe areas.
- Easy objectives.

Level 2:
- More enemies.
- Narrower paths.
- Guarded collectibles.

Level 3:
- Unpredictable enemies.
- Hidden routes.
- More complex underground railway maze.

Level 4:
- Multiple enemy types.
- Locked paths.
- Environmental puzzles.

Level 5:
- Fast enemies.
- Difficult navigation.
- Final survival challenge.

The player should always have a reasonable way to escape enemies.

Do not create unavoidable situations where the player gets trapped without a possible escape route.

---

# 16. DEVELOPMENT SCOPE — IMPORTANT

The project has an estimated development time of eight weeks and a team of 3–4 people.

The first priority is to build a fully playable core gameplay loop.

Do not attempt to create all five levels immediately.

The development process should be incremental.

## FIRST PLAYABLE PROTOTYPE

Before creating the complete game, implement a small test environment containing:

1. Pac-Man character.
2. Smooth player movement.
3. Follow camera.
4. Maze walls and corridors.
5. Small Energy Orbs.
6. 10 Nexus Cores.
7. One Hunter enemy.
8. Basic enemy chasing behavior.
9. Collision and lives.
10. Energy Meter.
11. One special ability.
12. Minimap.
13. Nexus Core objective counter.
14. One generator.
15. Level completion.

The prototype should prove that the main gameplay loop works.

Only after the prototype works should the project expand.

## EIGHT-WEEK DEVELOPMENT PLAN

**WEEK 1 — FOUNDATION**

- Inspect and prepare the Unity project.
- Configure the scene.
- Create Pac-Man.
- Implement movement.
- Implement the follow camera.
- Create basic maze walls.
- Add small Energy Orbs.

Goal: Pac-Man can move around a simple maze and collect orbs.

**WEEK 2 — MAIN GAMEPLAY**

- Implement Nexus Cores.
- Add the 10-core objective counter.
- Create the Energy Meter.
- Add one special ability.
- Create the generator.
- Implement level completion.

Goal: The player can complete a simple level.

**WEEK 3 — ENEMIES**

- Create Hunter.
- Implement enemy detection.
- Implement enemy chasing.
- Add collision and damage.
- Add lives and respawning.
- Improve player feedback.

Goal: A complete playable maze survival experience.

**WEEK 4 — MINIMAP AND PROGRESSION**

- Implement minimap.
- Show Pac-Man's position.
- Show Nexus Cores.
- Show generator.
- Add enemy markers.
- Add progressive map discovery.
- Create level progression.

Goal: The first level is fully playable from beginning to end.

**WEEK 5 — VISUAL DESIGN**

- Improve Neon District.
- Create materials.
- Add neon lighting.
- Add particles.
- Improve Pac-Man animations.
- Add BYTE.
- Add dialogue UI.

Goal: The first level looks like a real game rather than a prototype.

**WEEK 6 — ADDITIONAL LEVELS AND ENEMIES**

- Create Skyline Zone.
- Create Underground Transit Maze.
- Add Sentinel.
- Add additional collectibles.
- Add power-ups.

Goal: The game has multiple playable areas.

**WEEK 7 — STORY AND CONTENT**

- Add Data Core.
- Add final level prototype.
- Add story events.
- Add BYTE memory fragments.
- Add audio.
- Add main menu and pause menu.

Goal: Main game progression is implemented.

**WEEK 8 — TESTING AND POLISH**

- Fix bugs.
- Improve performance.
- Test collisions.
- Test enemy behavior.
- Test minimap.
- Test progression.
- Improve UI.
- Prepare final Windows build.

Goal: A stable and presentable game build.

If the schedule becomes too tight, prioritize a polished first level and smaller but functional versions of the remaining levels over unfinished ambitious features.

---

# 17. TECHNICAL GUIDELINES FOR UNITY

The project should be implemented in Unity using clean and maintainable C# scripts.

The game uses 3D objects and environments, with a top-down 2.5D presentation.

Recommended technical structure:

**Player**
- PlayerController
- PlayerMovement
- PlayerAbilities
- PlayerHealth
- PlayerAnimation

**Camera**
- CameraFollow
- CameraCollision

**Collectibles**
- EnergyOrb
- NexusCore
- PowerUp

**Enemies**
- EnemyBase
- HunterAI
- SentinelAI
- EnemyHealth

**World**
- MazeManager
- LevelManager
- Generator
- Portal
- Checkpoint

**UI**
- GameHUD
- MinimapController
- ObjectiveTracker
- EnergyMeterUI
- LivesUI
- DialogueUI
- MainMenuUI
- PauseMenuUI

**Story**
- ByteCompanion
- DialogueManager
- MemoryFragment
- StoryProgression

This is a suggested organization, not a requirement to create every script immediately.

Use appropriate Unity systems and packages only where they genuinely help the project.

For maze navigation, choose one consistent approach:
- Grid-based navigation, if the levels are designed around a regular grid.
- NavMesh-based navigation, if the corridors and environments are irregular.

Do not mix navigation approaches unnecessarily.

The player should use reliable collision and movement logic so that they cannot pass through maze walls.

Enemy pathfinding must respect the maze layout.

The minimap must use the same level layout and world positions as the main environment.

For the Underground Transit Maze, the railway tracks and maintenance corridors must be represented correctly on the minimap.

Important gameplay systems should be separated from visual effects and UI.

Use prefabs for reusable objects such as:
- Energy Orbs.
- Nexus Cores.
- Enemies.
- Doors.
- Generators.
- Portals.

Use ScriptableObjects or another suitable data-driven approach for reusable configuration such as enemy settings, power-up settings and level objectives, if appropriate.

Avoid unnecessary complexity.

---

# 18. IMPORTANT GAME DESIGN RULES

These rules must be respected during development.

1. Pac-Man is always the main playable character.
2. The city is a maze, not a completely open-world environment.
3. All five main levels must use proper maze-based level design.
4. Every main level contains exactly 10 required Nexus Cores.
5. The player must collect all 10 Nexus Cores to unlock the generator.
6. The player must return to the generator and activate it to finish the level.
7. Small Energy Orbs are optional collectibles.
8. The minimap is always available during normal gameplay.
9. The camera follows Pac-Man.
10. The player sees only the nearby environment through the main camera.
11. The minimap helps the player understand the larger maze.
12. Enemies should have distinct behaviors.
13. BYTE is the companion and guide throughout the game.
14. BYTE gradually recovers his lost memories.
15. The five main levels must look and feel different.
16. The underground level must use railway tracks, stations and tunnels as the maze environment.
17. The underground level must not be an open-world environment.
18. The minimap must work in every level, including the underground railway maze.
19. Gameplay clarity is more important than visual complexity.
20. The first playable prototype must work before the creation of the full game.
21. All major mechanics must be tested before expanding the project.
22. Do not introduce unnecessary mechanics that make the game too complicated for the available development time.

---

# 19. FIRST TASK FOR CLAUDE CODE

You are helping develop this game in an existing Unity project.

Treat this document as the main game design specification.

Before writing or modifying code:

1. Inspect the existing Unity project structure.
2. Identify the Unity version and current assets.
3. Identify existing scenes, scripts, prefabs and packages.
4. Determine whether any gameplay systems already exist.
5. Explain what is already implemented and what is missing.
6. Propose a clear implementation plan for the first playable prototype.

Do not immediately generate the entire game.

Do not create all five levels at once.

Do not replace existing project files without understanding their purpose.

Do not make unnecessary changes to project settings.

Work in small, testable development stages.

After each stage:
- Explain what was created.
- Explain which files were added or modified.
- Explain how to connect or configure the objects in Unity.
- Explain how to test the feature.
- Identify any remaining issues.

When creating scripts, use clear naming, readable code and comments where helpful.

If a feature requires manual work inside the Unity Editor, explain the exact steps.

Do not assume that an object, prefab, scene or package exists unless you have verified it.

If an important design decision is unclear, ask before making a major architectural change.

**FIRST DEVELOPMENT MILESTONE:**

Create the foundation for Level 1 — Neon District.

The first milestone must include:
- A controllable floating Pac-Man character.
- A smooth top-down follow camera.
- A small maze environment.
- Collectible Energy Orbs.
- 10 Nexus Cores.
- A working objective counter.
- One basic Hunter enemy.
- A generator that unlocks after collecting all 10 Nexus Cores.
- A simple level-completion sequence.

The result must be playable inside Unity before proceeding to additional levels.

The goal is to build a stable and expandable game, not simply generate a large amount of code.

---

# END OF GAME DESIGN DOCUMENT

**PROJECT IDENTITY:** PAC-MAN: NEXUS — THE LOST CITY

**CORE GAMEPLAY:** Explore a futuristic city maze, collect 10 Nexus Cores in every region, avoid corrupted Bugs, and activate generators to restore the city.

**MAIN CHARACTERS:** Pac-Man and BYTE.

**MAIN LEVELS:** Neon District, Skyline Zone, Underground Transit Maze, Data Core and The Nexus Core.

**MAIN DESIGN GOAL:** Preserve the addictive maze-collection gameplay of classic Pac-Man while transforming it into a modern 3D cyberpunk adventure.