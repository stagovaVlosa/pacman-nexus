# PAC-MAN: NEXUS – Work log, 21 November 2026

Visual overhaul of the whole game: realistic city, cinematic HUD and intro, both levels.
Gameplay (Pac-Man, Bugs, maze, rules) is unchanged. Everything new is behind one checkbox:
**LevelBuilder › Look › Realistic Look** (on in both level scenes; untick = old neon look).

Each `##` section below is one Trello card; the bullet lists are its checklist.

---

## 1. Realistic city for Level 1 (Neon District)
- Replaced the "Lego" look (one toy cube per maze cell) with one building per city block
- Low podium with lit shops along the street + tower on the camera side of the block (streets stay visible)
- Procedural PBR textures generated in code (no downloads): concrete offices, glass towers, brick flats, shopfronts, roof gravel, wet asphalt, sidewalk slabs
- Rooftop details: AC units, water tanks, antennas with red lights, skylights, parapets
- Sidewalks with curbs, white road edge lines, dashed yellow centre lines
- Night lighting: moonlight with soft shadows, ambient occlusion (SSAO), fog, calmer bloom, film grain
- Cool white LED street lamps (warm ones looked like Nexus Cores)
- Files: `Assets/Scripts/World/RealisticCity.cs` (new), `LevelBuilder.cs`

## 2. Calmer colours in the world
- Removed the neon holo rings over the Generator plaza
- White painted zebra crossings, red tail lights on hover cars
- Energy Orbs smaller and warm pale instead of bright cyan
- Yellow beam above every Nexus Core (objective marker)
- Pac-Man yellow is the only UI accent colour
- Files: `NeonDistrictFX.cs`, `FlyingTraffic.cs`, `LevelBuilder.cs`

## 3. New clean HUD
- No boxes: Bahnschrift font, white type, thin lines, scales with the screen
- Level line, big Nexus Core counter with 10 pips, score, Pac-Man lives
- Thin energy bar and ring-style Energy Blast button
- Small, soft off-screen Bug arrows
- New Game Over and Region Restored screens
- BYTE's dialogue box in the same style
- Files: `UI/GameHUD.cs`, `Player/ByteCompanion.cs`

## 4. Power-up cards with countdown
- Big announcement for ~2 s when a fruit is taken: picture, name, what it does
- Card per active power (bottom left): fruit in a countdown ring, name, effect, seconds left, bar, blinks in the last 2 s
- Fruits on the street shown as clear camera-facing pictures (Apple, Lime, Plum, Grapes)
- Files: `GameHUD.cs`, `World/PowerUp.cs`, `Player/PlayerPowers.cs`

## 5. Minimap
- Smaller, thin white frame, grey streets
- Distinct symbols: YOU = white badge with Pac-Man, CORE = yellow diamond, POWER = fruit, BUG = red dot, GENERATOR = square
- Legend under the map
- File: `UI/Minimap.cs`

## 6. BYTE
- No longer follows Pac-Man during play (was confusing); only talks in the dialogue box
- Still appears in the intro
- File: `Player/ByteCompanion.cs`

## 7. New cinematic opening intro (~70 s)
- Cold open in the real city: aerial, street dolly, crane, city blackout, Hunter in the dark, SYSTEM FAILURE
- Lab: macro shots of Pac-Man rebooting, ONLINE hit, pod opens, BYTE's 3 lines, launch
- Title slam: city lights switch on in a wave, PAC-MAN / NEXUS / THE LOST CITY
- Know Your Enemy: rival card per Bug (live 3D model, class, 2 powers, stats, weakness) + 3 CLASSIFIED Bugs
- Select District: 5 city cards (available / locked / signal lost), ENTER swoops into play
- Synthesised sound: drone, whooshes, impacts, riser, glitch, menu ticks (first sound in the game)
- Files: `Core/IntroSequence.Cinematic.cs`, `Core/IntroAudio.cs` (new), `Core/IntroSequence.cs`, `Assets/Resources/IntroCards/`

## 8. Level 2 (Skyline Zone) – same look and feel
- Slim glass and white concrete towers; many end below the bridges so you look down to the clouds
- Asphalt sky bridges with parapets, metal rails, lane lines, pillars and lamp posts
- Procedural sky with sun disk, warm haze, soft long shadows
- Same HUD, power cards, minimap and fruit icons as Level 1
- Level intro: BYTE's story as captions over cinematic shots, SKYLINE ZONE title slam, district cards (Neon District RESTORED)

## 9. City to the horizon
- Hundreds of towers around both levels out to the horizon (lit at night in Level 1, rising from clouds in Level 2)
- Merged into a few meshes, no performance cost

## 10. Testing
- Every step compiled and checked in Unity with screenshots
- Autopilot (plays the level with Bugs ON): Level 1 PASS (44 s, 10/10 cores), Level 2 PASS (66.5 s, 10/10 cores)
- No game errors in the Console
- Backups of the replaced scripts in `Docs/LevelBackups/`

## Next steps
- Gameplay sounds and music (pickups, cores, eating Bugs, level music)
- "Satisfying" feedback: screen shake, short freeze on big moments, particles
- Levels 3–5 (Underground, Data Core, Nexus Core)
- Windows build to share the game
