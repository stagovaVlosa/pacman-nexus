# PAC-MAN: NEXUS — Progress

Source of truth: `Docs/GDD.md` and `Docs/ConceptArt.png`. Every new chat reads these first.

## State (3 November 2026)
- Level 1 Neon District plays in Unity: maze from `Assets/Levels/Level1_NeonDistrict.txt`, Energy Orbs, 10 Nexus Cores, Generator, Bugs (chaser, patroller, blocker), BYTE, HUD, minimap with fog of war, Energy Blast, Game Over / Level Complete.
- Changes on 3 Nov (Claude):
  - Maze walls are low blocks with neon lines (`CityVisuals.cs`), closer camera (`CameraFollow.cs`), simple yellow Pac-Man (`PacManSolid.cs`, used in `LevelBuilder.cs`), Bugs wait for the first key press (`PlayerMovement.cs`, `BugAI.cs`).
  - The user does NOT like the result: it lost the city feel and looks like an old arcade game.
- A browser demo (claude.ai artifact "Pac-Man Nexus: Neon District") was also rejected for the same reason: too simple, Pac-Man wrong, BYTE not visible, no intro.

## Problems to solve
1. The look must match `Docs/ConceptArt.png`: a dense neon city, not a classic board. Procedural shapes are not enough; it needs real 3D assets (Asset Store cyberpunk city pack or Blender models).
2. Pac-Man design: decide between the GDD/concept art (glossy yellow Pac-Man with eyes and big mouth) and `Docs/PacManRobot_Design.md` (glass robot, no face).
3. BYTE must be clearly visible next to Pac-Man (GDD 3.2: rounded rectangular hologram, one big digital eye).
4. Intro: Pac-Man wakes in an abandoned lab, BYTE appears (GDD 2.2, 2.3).
5. Only one Claude edits the project at a time (not VS Code and the Claude app together).

## Next step
Decide the Pac-Man design and the asset approach for the city, then rebuild the look of Level 1 on top of the working gameplay.

## Decision (3 Nov)
- Pac-Man = the **robot** from `Docs/PacManRobot_Design.md` (glass shell, wedge mouth, no face). `LevelBuilder` uses `PacManModel` again; glass made less transparent and glow lowered so he reads as yellow, not a white blob. `PacManSolid.cs` is no longer used.

## Week 1 started (3 Nov, afternoon)
Done (compiles without errors):
- City is back (tall buildings, windows, neon, signs) with lower heights near streets and less window noise (`CityVisuals.cs`).
- Buildings in front of Pac-Man / Bugs fade again (`BuildingFader` re-enabled); their neon rims and signs now hide while faded, so they no longer float in the air.
- X-ray outline: Pac-Man (yellow) and every Bug (own color) show through buildings (`World/Silhouette.cs` + `Resources/Shaders/Silhouette.shader`).
- BYTE rebuilt per GDD 3.2: bigger rounded-rectangle hologram, one big blinking eye, always turned to the camera (`ByteCompanion.cs`).
- Camera steeper and a bit further (66 degrees, 9.5) so the streets around Pac-Man are readable.
- Pac-Man robot: glass more opaque yellow, weaker ground ring, scene bloom lowered (it turned yellow into white).
Not yet verified in play: the last tweak batch (yellow Pac-Man, bloom). Keyboard input from Claude's remote control did not reach the Game view, so movement was not tested by Claude.
Test tool: menu Nexus > Capture Game Screenshot (in Play) saves `Docs/Screenshots/game_*.png`, which Claude can read.

Next: verify the batch above, then week 1 remaining items (automatic play test, Unity MCP fix), then install packages/asset pack for week 2.

## Unity MCP connected (3 Nov, evening)
- MCP for Unity now uses **Stdio** transport (the Claude desktop app does not support HTTP). Unity window: Window > MCP for Unity > Session Active must be green. Claude Desktop config: uvx --from mcpforunityserver==10.3.0 mcp-for-unity --transport stdio. The package was pre-downloaded once in PowerShell (first start timed out).
- Claude can now: read the Console, play/stop, run C# in the editor (execute_code), and take game screenshots (manage_camera screenshot, saved in Assets/Screenshots).
- `PlayerSettings.runInBackground = true` so the game keeps running while Unity is not focused (needed for remote testing).
- Movement test without keyboard: set PlayerMovement's private `queued` field + `HasStarted` via reflection.
- Scene `Level1_NeonDistrict` saved with CameraFollow viewPitch 66, viewDistance 9.5 (scene values override script defaults!).
- Buildings in front of Pac-Man fade almost completely (fadedAlpha 0.08); Pac-Man glass is now clearly yellow.
- Verified in play: Pac-Man moves, collects orbs, BYTE follows and is visible, minimap fills in, a Bug took one life.

## Automatic play test (3 Nov, evening)
- `Assets/Scripts/Debug/Autopilot.cs`: plays Level 1 by itself (shortest route to the nearest remaining Core, then the Generator) and logs PASS/FAIL to the Console. Bugs are switched off during the test, so it checks the level itself.
- How to run (Claude via Unity MCP): Play, then `new GameObject("Autopilot").AddComponent<Nexus.Autopilot>()`, then read the Console for "Autopilot:".
- Result: **PASS** - all 10 Nexus Cores reachable, Generator completes the level, "REGION RESTORED" shows, 60.6 s, score 6930.
- New in PlayerMovement: `SetQueuedDirection()` and `TargetCell` (used by the test).
- File-writing note: device commits sometimes land with the previous version. Always compare sizes with `device_list_dir` and recommit (with force) if they differ. This happened twice today.

Week 1 remaining: visibility check from all parts of the maze; then week 2 (asset pack).

## Visibility check (3 Nov, evening)
- Checked with camera screenshots at the bottom edge (5,27), corner (1,27), right edge (27,3), Generator area (14,17) and the middle (9,11).
- Problem found: buildings on the camera side of a street were 1.6-2.2 high and hid the street itself (Pac-Man visible, the way forward not).
- Fix (`CityVisuals.cs`): buildings directly south of a street (camera side) are now 0.85-1.15 high; buildings deeper in a block 2.2-3.4 (city skyline); low skyline band south of the maze 0.4-0.9.
- After the fix: streets around Pac-Man readable in all checked spots, Bugs visible with their color ring.
- How to check a spot: in Play, move PlayerMovement's private `cell`/`target` + transform to the cell, snap the camera (`target + Offset`), `BuildingFader.Refresh(10)`, then manage_camera screenshot.

Week 1 done except: play with Bugs on for difficulty (manual). Next: week 2, asset pack.

## Decision (3 Nov, late): Level 1 = real city map, not a maze
- The user does not like maze dead ends (you get cornered). Level 1 Neon District is now a city grid modelled on Barcelona's Eixample: 6x6 octagonal blocks (4x4 with cut corners), streets every 5 cells, two wide avenues (Gran Via row 12, Passeig de Gracia column 22), a Diagonal staircase avenue across the city, a central plaza with the Generator (18,18). Map 33x33. **Zero dead ends**, fully connected.
- Start P (16,31), Hunter (16,1), 2 Blockers next to the two farthest cores, 2 Patrollers (Gran Via end to end, Passeig de Gracia).
- This changes GDD 8 (Level 1 maze with dead ends). Other levels still to be decided.
- Files: `Assets/Levels/Level1_NeonDistrict.txt` (new), old maze in `Docs/LevelBackups/Level1_NeonDistrict_OldMaze.txt`, generator script `Docs/LevelBackups/Level1_CityGenerator.py` (python3, prints the layout).
- NOT yet tested in Unity: the MCP connection dropped right after saving the layout.

## Unity MCP permanent fix (10 Nov)
- Problem: unityMCP showed "Failed / Server disconnected" at almost every start. Cause: `uvx --from mcpforunityserver==10.3.0` resolves the package online on every start and Claude Desktop times out.
- Fix (done once): `uv tool install mcpforunityserver==10.3.0`, and in `claude_desktop_config.json` the server now runs directly:
  `"command": "C:\\Users\\PULSE Electronics\\.local\\bin\\mcp-for-unity.exe", "args": ["--transport", "stdio"]`
- Rule: open Unity first, then Claude. Do not toggle Session in MCP for Unity while Unity reloads (port 6400 gets stuck and it moves to 6401; that is harmless, clients follow the status file).
- Verified 10 Nov: MCP connected, active scene Level1_NeonDistrict, Console readable.


## Level 1 city layout tested (10 Nov)
- The layout file on disk was the WRONG version (no patrollers, Generator at 16,16): the 3 Nov commit had landed badly. Regenerated from `Docs/LevelBackups/Level1_CityGenerator.py` (Generator 18,18, start 16,31, Hunter 16,1, patrollers 1 = Gran Via row 12, 2 = column 22, 2 Blockers next to the corner cores). Wrong version kept in `Docs/LevelBackups/Level1_NeonDistrict_WrongVersion.txt`.
- Verified the content inside Unity (same byte size, so the size check alone cannot catch this; read lines back with execute_code).
- Autopilot with **Bugs ON**: **PASS**, 10/10 cores, level complete in 53.7 s, score 6750, 3 lives kept, 4 Energy Blasts. 5 Bugs spawned.
- Screenshot: `Assets/Screenshots/level1_city_complete.png`.
- Seen in the screenshot: the plaza around the Generator is very bright (bloom washes out), buildings are still procedural blocks; the Diagonal leaves some single-cell building fragments.

Next: week 2, Level 1 look (cyberpunk city asset pack from the Asset Store, lighting, fog), and a manual play by the user to judge difficulty.

## Minimap + turn buffer (10 Nov, user feedback)
- User: too hard because you cannot see where the cores are at the start. Decision: **whole minimap visible from the start** (changes GDD "progressive exploration"). `Minimap.revealAll = true` (default): all streets, all 10 cores, all Bugs, Generator. Cores are now big pulsing yellow diamonds with a dark outline, drawn above the Bugs (corner cores were hidden under Blocker icons). Set revealAll false to get fog of war back.
- User: turns should be "predicted" - press the key a few seconds before the corner. Cause found: with an arrow held, the old if/else order ignored a new arrow (Up always won). `PlayerMovement` now: the most recently pressed key wins (wasPressedThisFrame), a held key keeps the turn waiting, and a pressed turn is remembered for `turnBufferSeconds = 2.5` and taken at the next opening; after that it is forgotten so Pac-Man does not turn unexpectedly much later. Autopilot uses SetQueuedDirection (never expires).
- Tests: compiles without errors; minimap checked with screenshot (all 10 cores visible); Autopilot with Bugs ON: PASS, 65.4 s, 10/10, lives 2.
- NOT tested by Claude: the turn buffer with the real keyboard (remote key presses do not reach the Game view). The user should try it.
- Commit note: 3 of 4 commits today first landed with the old file; always check the size.

Next: user plays Level 1 to judge the turn buffer and difficulty, then week 2 (Level 1 look, city asset pack).

## Turn fix + Blocker fix (10 Nov, evening, user feedback)
- User: pre-pressed turns still did not work (top right too); one core (top right) could not be taken because its Bug never left.
- Cause of the turn problem: the octagonal blocks had 1-cell "chamfer" nooks at every block corner. A pre-pressed turn was taken at the first opening, which was usually a nook: Pac-Man went 1 cell in and stopped at a wall.
- Layout v2 (`Docs/LevelBackups/Level1_CityGenerator.py`, regenerated `Level1_NeonDistrict.txt`): no chamfers, square 4x4 blocks; Gran Via, Passeig de Gracia, Diagonal and the central plaza kept; zero dead ends; all cores on real streets. Blockers now stand 5 cells from the two farthest cores (corners top left / top right) on the route towards the Generator, so the other side of the core stays open. (The octagonal look can come back later in the building art only, not in the walkable grid.)
- `PlayerMovement.ShouldTakeTurn()` (smart turn): while Pac-Man can go straight, a pre-pressed turn is only taken into a real street (2 open cells in the new direction); with a wall ahead any opening is taken.
- `BugAI` Blocker defaults: alertRange 4 -> 3, guardRadius 3 -> 2, territoryRadius 7 -> 5.
- Tests: compiles; turn-buffer test in play (Pac-Man moving left on row 31, Up queued 3 cells before column 11 with real expiry): turned up at column 11 and kept going. Autopilot with Bugs ON: PASS 53.6 s, 10/10, 3 lives, 1 blast.
- Still to be confirmed by the user with the real keyboard.

## Intro cinematic + title screen (10 Nov, for the professor demo)
- Asset decision: Kenney City Kit (Commercial), free CC0, neon added by code. Not imported yet (user downloads the zip from kenney.nl into Downloads).
- New `Assets/Scripts/Core/IntroSequence.cs` (GDD 2.2 + 2.3), starts by itself once per Play (RuntimeInitializeOnLoadMethod), not again after a restart:
  1. black screen with 3 story lines; 2. abandoned lab (built in code at (-70,0,60)): lights flicker on, terminal "PAC-MAN ONLINE", pod with Pac-Man;
  3. glass lifts, flash, Pac-Man floats out; 4. BYTE appears and says 5 lines (dialogue box, typewriter);
  5. title over the orbiting city: PAC-MAN / NEXUS / THE LOST CITY / LEVEL 1 - NEON DISTRICT / PRESS ENTER TO START;
  6. camera swoops down to Pac-Man, HUD appears, BYTE: "Arrow keys to move. Find the 10 Nexus Cores!".
  ENTER/click = next line, ESC = skip to the title.
- While `IntroSequence.Active`: GameManager.IsPlaying is false, GameHUD / Minimap / ByteCompanion OnGUI hidden, CameraFollow disabled.
- Tested in Play with screenshots of every phase (Assets/Screenshots/intro_*.png): no errors, ends in normal gameplay.

Next: Kenney city import (week 2), sound/music for the intro, pause menu.

## Kenney City Kit imported (10 Nov, 17:45)
- `Assets/ThirdParty/KenneyCityCommercial/`: 41 FBX (14 buildings a-n, 5 skyscrapers a-e, 16 low-detail buildings, 6 details: awnings, overhangs, parasols) + `Textures/colormap.png` + License.txt (CC0). Imported with URP/Lit materials.
- Sizes: building-a 0.88 x 1.29 x 0.94, skyscraper-a 1.36 x 2.88 x 1.36, low-detail 0.5 x 2.0 x 0.5 (units = 1 grid cell), pivot at the bottom centre.
- NOT used in the level yet (kept the working demo unchanged before the professor presentation).
- Next: `CityVisuals.BuildBuilding` places Kenney models per city block (low buildings on the camera side, skyscrapers inside the blocks), darker night tint on colormap, neon trims/signs/window glow added by code, BuildingFader must handle the new renderers.

## Level 2 - Skyline Zone, first playable (10 Nov, evening)
- Layout `Assets/Levels/Level2_SkylineZone.txt` (31x31) from `Docs/LevelBackups/Level2_SkylineGenerator.py`: narrow 1-cell walkways every 4 cells between 3x3 skyscraper blocks, 26 walkways closed + a few dead ends (3 dead-end pockets with a core), 1 hidden route through a block, Generator platform 5x5 in the centre (15,15), start (15,29).
- Enemies: 7 = Hunter (H), 2 Patrollers (1 = row 9, 2 = column 21), 4 Sentinels (S).
- New `BugType.Sentinel` ('S', GDD 4.2, purple): like the Blocker, but its post is the nearest Nexus Core; circles it (guardRadius 2), attacks within 3 cells, speed 3.0 / chase 3.8. Patroller colour changed to blue so purple = Sentinel only.
- `LevelBuilder`: levelName, levelNumber, theme (`LevelTheme` NeonDistrict / SkylineZone), nextScenePath. Level 2+ starts a title card (`IntroSequence.StartLevelCard`).
- `CityVisuals` Skyline theme: blue glass towers (narrower, 0.8) that go 9 units down to a deep "city lights" ground, walkways are bridge decks, taller interior/border towers, blue fog and colour grading.
- Level card: orbiting city, "PAC-MAN: NEXUS / SKYLINE ZONE / LEVEL 2", 5 BYTE memory lines (GDD story events: transport+communication, regions cut on purpose, central system confusion, Sentinels warning), then PRESS ENTER and the swoop. Restart after game over skips the card.
- Progression: after the Generator, HUD shows "Press ENTER for the next region" -> loads Level 2. Last level shows "More regions coming soon". Build Settings: Level 1, Level 2.
- Editor menu: Nexus > Create Level 2 Scene (also links Level 1 -> Level 2), Nexus > Link Level 1 -> Level 2.
- Tests: compiles; Level 2 Autopilot PASS 61.7 s, 10/10, 3 lives, 4 blasts (harder than Level 1: 1 blast). Full flow Level 1 complete -> ENTER -> Level 2 card: OK, no errors. Screenshots: level2_card*.png, level2_play.png, level1_complete_next.png.
- Not done yet for Level 2: Sentinel model is the normal Bug model in purple (GDD: angular + small shield), Skyline music, moving lights / billboards, Kenney models.

## Level 2 v2 after user feedback (10 Nov, 21:30)
- User: too hard; top-right and bottom-left cores unreachable (Sentinels never left them); dislikes dead ends; walkways too narrow.
- Layout v2 (`Level2_SkylineGenerator.py`, 34x34): **2-cell wide sky bridges** (street pairs at 1,7,13,19,25,31) between 4x4 tower blocks, 16 bridge segments closed but every intersection keeps >= 2 exits (generator asserts **no dead-end cells**), 2 narrow 1-cell shortcuts through blocks, central 4x4 Generator platform (G 16,16), start (16,32), Hunter (16,1).
- Enemies v2.1: 5 = Hunter, 1 Patroller (row 7), 3 Sentinels (guarding the 3 cores farthest from the start). Second Patroller removed after test (too hard).
- Sentinel AI v2 (`BugAI.SentinelStep`): never camps on the core; walks back and forth between two cells 4 steps from the core on opposite sides (path goes through the core), attacks within alertRange 3, gives up beyond territoryRadius 5, speed 2.9 / chase 3.5.
- Test: Autopilot PASS 109.9 s, 10/10, 3 lives (no life lost), 6 blasts.
- Note: Unity's Console has "Error Pause" on, so the Unity AI panel's "NoSubscription" errors pause Play mode. Turn off Error Pause in the Console toolbar (or remove the AI Assistant package) to avoid random pauses.

## Level identities - phases A and B done (10 Nov, 22:00-23:00)
- Living plan doc (Claude Docs): "PAC-MAN: NEXUS - Plani dhe gjendja e projektit" https://claude.ai/artifact/N7PCp2Urs1m9jQ7i9iEZJu (how to continue in a new chat, status, 5-level vision, phases A-H, decisions, technical rules). Keep it updated together with this file.
- Decision: every level has its own palette, architecture, ground and signature effect; HUD/minimap/title accent follow the level (`GameHUD.Accent`, `CityVisuals.AccentFor`). BYTE stays cyan. Level 2 changed from GDD "dark blue + cyan" to "sunset above the clouds".
- **Phase A - Level 2 sunset** (`CityVisuals` Sky theme): low orange sun (directional, 2.0, soft shadows, Euler 34/-62), coral haze fog #F09A78, purple ambient, pale teal glass texture with warm glints (`EnsureSkyGlassTextures`), warm stone roofs, cream bridge deck with amber curbs, slim pillars under the bridges, drifting sunset cloud sea at y -6 (`UvScroll`), silver hover cars over the skyline band (`FlyingTraffic`), no vertical neon signs, warm post FX, amber HUD. Autopilot PASS.
- **Phase B - Level 1 rainy night market**: Kenney models moved to `Assets/ThirdParty/Resources/KenneyCity` (loaded with Resources). Night textures `Textures/colormap_night.png` + `colormap_night_emission.png` (dark violet walls, warm/cyan lit windows, neon awnings). `CityVisuals.PlaceKenney`: one model per wall cell, scaled to the footprint, random 90 degree turn: low models on the camera side, mid inside blocks, skyscrapers on the top border and the band (916 instances). Neon rims/signs kept on top. `NeonDistrictFX`: slanted rain following Pac-Man (hidden in the intro), puddles on ~11% of street cells, 7 steam grates. Street asphalt darker violet. Autopilot PASS 63.8 s, 3 lives, 0 blasts.
- Performance note: ~34 fps in the unfocused editor with 916 Kenney buildings; if it is low on the user's PC, static-batch the Walls root or merge meshes per block.
- Next: phase C (music + sounds per level), then D (Sentinel/Hunter models), E (Level 3).

## User feedback round (10 Nov, 22:45) - written, compile + test pending
- Level 1 "a bit overwhelming": rain is now a light drizzle (800/s, alpha 0.28, thinner), roof neon glow 2.2 -> 1.6, bloom 1.4 -> 0.95, saturation 20 -> 6 (Level 1 only). User likes Level 2's clarity: use it as the reference.
- Level 2 lane change: on 2-wide streets the sideways press was ignored (smart-turn rule needed 2 open cells). `PlayerMovement.IsLaneChange()` now allows stepping into the parallel lane and then re-queues the previous direction, so Pac-Man keeps going.
- Bug colours per level (`BugAI.ColorFor` uses `CityVisuals.CurrentTheme`): Neon District Hunter #FF2A2A, Patroller lime #9DFF3A, Blocker orange #FF7A1A; Skyline Zone Hunter crimson #E8004F, Patroller cyan #1EF2FF, Blocker magenta #FF2BD6, Sentinel violet #7B2CFF. New levels get colours that contrast with their palette.
- Compiled and tested (23:00): lane change test on Level 2 (moving left in row 32, Up -> crossed to row 31 and kept going left). Autopilot Level 1 PASS 53.5 s, 3 lives, 1 blast; Level 2 PASS 84.4 s, 3 lives, 5 blasts. Screenshots l1_calm.png, l2_lifecores.png.
- Lives HUD redesigned as "LIFE CORES" (`GameHUD.DrawLifeCores`): hexagonal energy cells with Pac-Man's silhouette, yellow glow + pulse, frame in the level accent; a lost cell flashes red, cracks and fades, the status panel border flashes red; the last cell blinks red. (User: the classic Pac-Man icons did not fit the levels.)
- Next: phase C (music + sounds per level).

## FPS fix + power-ups + Level 1 look (12 Nov)
User: Level 2 smooth, Level 1 low FPS; make Level 1 more interesting; add power-ups like the real Pac-Man.
- **FPS (Level 1)**. Measured: ~3500 renderers, the skyline band (`City`, ~1300 renderers of Kenney models) cost the most CPU, 67 point lights.
  - `CityVisuals.Optimize` (called at the end of `LevelBuilder.Build`, Play mode only): skyline buildings merged into one mesh per material per 10x10-cell chunk (181 meshes instead of ~1300 renderers); maze buildings (`Walls`) static-batched (BuildingFader still fades each building).
  - Kenney FBX models now imported with Read/Write ON (needed for merging/static batching). Keep it on if they are re-imported.
  - `MaxNeonLights` 80 -> 36. Energy Orbs bob only within 11 units of Pac-Man (`Collectible`).
  - Result in the editor: Level 1 main thread ~18-33 ms -> ~9-10 ms (now lighter than Level 2, which measured ~20 ms in the same test).
- **Power-ups** (GDD 3.1 / 5.5). New `World/PowerUp.cs`, `Player/PlayerPowers.cs`.
  - Devour Mode (magenta blinking pellet, the classic power pellet): 8 s, all Bugs turn blue with a glowing blue shell, turn around, flee at 55% speed; eaten Bug = 200/400/800/1600, burst effect, back at its spawn after 5 s. Blink white in the last 2 s. Screen edge glows magenta.
  - Speed Burst (green chevrons): 6 s, speed x1.45, light trail. Energy Shield (blue hexagon): bubble absorbs one hit (Bug pushed away + stunned). Magnetic Field (violet magnet): 10 s, orbs within 3 cells fly to Pac-Man.
  - Layout chars: `*` Devour, `>` Speed, `+` Shield, `%` Magnet. Levels without them get a default set (4 Devour near corners, 2 Speed, 2 Shield, 1 Magnet on street crossings, never on start/Generator/cores/Bug spawns). Both levels use the default set now.
  - HUD: active power rows with timers under the status panel, floating score/name popups, BYTE comments each pickup; minimap shows power-ups. Losing a life ends Devour/Speed/Magnet (shield stays).
- **Level 1 look** (`NeonDistrictFX`): night hover traffic with pink/cyan/violet light trails over the skyline band, billboards scroll and flicker, glowing zebra crossings at street crossings, 3 holographic rings turning above the Generator plaza + glowing plaza circle.
- Tests: compiles clean; Devour eat test OK (score +200, Bug respawns); Autopilot Bugs ON: Level 1 PASS 40.4 s, 3 lives; Level 2 PASS 76.4 s, 3 lives. Screenshots: l1_after_perf, l1_plaza_devour, l1_autopilot_mid, l2_powerups.
- NOT tested by Claude: playing with the real keyboard (feel of Devour duration, Speed Burst turning at high speed). The user should try it.

Next: user plays both levels (FPS on their PC, power-up balance); then phase C (music + sounds: power-up pickup, eat Bug, Devour loop), D (Sentinel/Hunter models), E (Level 3).

## Review + fixes before the midterm evaluation (15 Nov)
- Full review in Play: intro (story, lab, BYTE), title, Level 1 Autopilot Bugs ON PASS 48.0 s (10/10, 3 lives, 1 blast), Level 2 card + play. No game errors in the Console. Level 1 ~34 fps in the editor (~2400 renderers).
- **Pause menu** (new `UI/PauseMenu.cs`, created automatically in every scene): ESC pauses (Time.timeScale 0), Resume / Restart region / Quit game, keyboard (arrows + ENTER, ESC = resume) or mouse, shows the controls. `GameManager.IsPlaying` now also checks `!PauseMenu.Paused`. Tested: Pac-Man does not move while paused; after resume the Autopilot still PASSES (53.3 s).
- Level 2 title card: dark band behind "PRESS ENTER TO START" so it is readable on the orange sky (`IntroSequence`). Checked with a screenshot.
- Build Settings: Level1_NeonDistrict + Level2_SkylineZone (SampleScene removed). Needed for a Windows build.
- Console "Error Pause" switched off (Unity AI "NoSubscription" errors could pause the game during a demo).
- Review screenshots `Assets/Screenshots/review_*.png` can be deleted.
- Git: last commit is from 3 Nov; everything after (intro, Level 2, power-ups, pause) is NOT committed yet. The user commits + pushes.
- Still missing: main menu (Start / Controls / Quit), sound and music, save of progress / high score (Java 10 Memory & I/O), own models for Sentinel/Hunter, levels 3-5, PBT 7 documents (architecture diagram, interface notes, code review record).

## Minimap Pac-Man, bigger fruits, lane change (17 Nov, user feedback)
- Note: several scripts were changed on 16 Nov (PlayerMovement, CityVisuals, BuildingFader, GameHUD, IntroSequence, PlayerPowers, PowerUp = "power fruits" Apple/Lime/Plum/Grapes, HUD "POWER FRUITS" legend) without an entry here.
- User: on the minimap Pac-Man looked like the dots to collect; fruits could be bigger; on Level 2 lane change still did not work.
- `Minimap.cs`: Pac-Man is now a real Pac-Man shape (wedge mouth, chomps, turns to his direction), 3.4 cells big, white outline and a pulsing white ring. Cores (yellow diamonds) and fruits (dots) unchanged.
- `PowerUp.cs`: fruits 1.5x bigger (`VisualScale`), a bit higher (0.6), ground ring scaled too.
- `PlayerMovement.cs`: on 16 Nov lane change had been switched off (`allowLaneChange = false`) because the hop made Pac-Man forget the turn. Now always on and the turn is kept: a sideways press on a 2-wide street steps into the other lane (`laneReturn` = old direction, continued after the one-cell step) and the turn stays queued for the next real junction (2.5 s buffer, longer while held). The `allowLaneChange` inspector field was removed.
- Tests in Unity (Level 2, Bugs off): moving left in row 32, Up pressed -> stepped to row 31, kept going left, turned up at column 10. At column 20 (real street up) it turned at once. Compiles, no Nexus errors. Screenshots `Assets/Screenshots/minimap_fruit_17nov.png`, `fruit_close2_17nov.png`.
- Not tested by Claude: the real keyboard feel. The user should try lane change on Level 2.
- Note: recompiling during Play breaks the running game (NullReference in BugAI). Stop Play before scripts change.
- Minimap fruits (17 Nov, later): power-ups are now small fruit pictures instead of dots (`Minimap.FruitIcon`, public, so the HUD legend can use it too): Apple magenta with leaf/stem (still blinks = Devour), Lime green oval, Plum blue tall oval with leaf, Grapes violet berries. Dark outline + white shine, 2.8 cells. Checked with screenshot on Level 1 (`Assets/Screenshots/minimap_fruits2_17nov.png`).
- Note for testing: skipping the intro with reflection (IntroSequence.Finish) leaves Pac-Man in the lab at (-70,0,60); call PlayerMovement.Respawn() after it. Normal play is not affected.

Next: user tries Level 2 lane change and the new minimap; then main menu, sound/music, high score, Sentinel/Hunter models, Level 3.

## Realistic look for Level 1 - first version (21 Nov)
- User: the city "looks like Lego"; wants it more realistic, **free only**. Keep Pac-Man and the Bugs as they are.
- Causes found: one small Kenney toy model per maze cell (rows of identical cubes), flat colormap colours, everything glowing, flat ground, steep top-down camera.
- New `Assets/Scripts/World/RealisticCity.cs`, switched on with the new checkbox **LevelBuilder > Look > realisticLook** (Level 1 scene saved with it ON; untick = the previous neon look, CityVisuals unchanged). Backup of the old LevelBuilder: `Docs/LevelBackups/LevelBuilder_before_realistic_21nov.cs.txt`.
  - One building per city block (greedy rectangles up to 4x4 cells): low podium with lit shopfronts + sign bands, tower on the camera (south) side of the block, height limited so the street behind stays visible at the 66 degree camera (h * 0.445 <= podium depth). Tall towers on the far border/skyline, low buildings near the camera.
  - Procedural PBR textures generated at load (no downloads, no assets): concrete office, glass curtain wall, brick flats, shopfront podium, roof gravel/tar, wet asphalt, sidewalk slabs - each with normal map + metallic/smoothness map, world-space UVs.
  - Rooftop clutter (AC units, fan grilles, water tanks, antennas with red lights, parapets, glowing skylights), sidewalks with curbs, ~120 street lamps (sodium + some LED, Forward+ point lights), a few neon signs as accents.
  - Lighting: moonlight 1.1 with soft shadows, Trilight ambient, fog 0.018, URP SSAO turned on (`RealisticCity.SetSSAO`, off again for non-realistic levels), calmer bloom, filmic grading, film grain, slight chromatic aberration.
- Tested in Play: compiles, no game errors, 89 maze buildings, ~2200 renderers, ~30 ms/frame in the unfocused editor (about the same as before). Screenshots: `Assets/Screenshots/real_before_start.png` (before), `real_v3_mid.png` (after), `real_v2_pitch54.png` (lower camera test).
- Not tested yet: Autopilot full run with the new look, BuildingFader on the bigger block buildings in all spots, Level 2 (unchanged, still CityVisuals), a Windows build (runtime materials use _NORMALMAP / _METALLICSPECGLOSSMAP keywords; for a build these shader variants may need to be kept, e.g. save the materials as assets or add a shader variant collection).
- Ideas next: lower camera (about 56-58 degrees) for a more cinematic look (towers would need to be a bit lower), parked/moving cars, road markings, steam and puddle reflections, apply the same approach to Level 2.

## Roads, calmer colours, Blur-inspired HUD (21 Nov, user feedback)
- User: roads must be clearer; the neon colours (cyan HUD boxes, big coloured arrows, pink/cyan rings) did not match the realistic city; the HUD "board" too big; take inspiration from Blur for the HUD.
- Roads (`RealisticCity.BuildStreets`): white painted edge line along every curb, dashed yellow centre line on straight stretches, lighter asphalt and sidewalks, darker roofs (streets now read lighter than roofs). Puddle discs are skipped in the realistic look (they looked like black holes; the asphalt has wet patches).
- World colours (only when `RealisticCity.Active`): no holo rings over the plaza, zebra crossings are white paint, hover-car trails are red tail lights, Energy Orbs smaller and warm pale (#FFE2B0, glow 1.5 instead of cyan 2.8), skylights dimmer. HUD accent = Pac-Man yellow #FFC93C.
- Clean HUD (`GameHUD.DrawCleanHud`, used only with the realistic look; Level 2 keeps the old HUD): no boxes, Bahnschrift font (Windows OS font, fallback Segoe UI/Arial), scales with screen height (designed for 1080p): level line "01  N E O N  D I S T R I C T", big core count + 10 pips, score, small Pac-Man lives, power-fruit legend only for the first 14 s then only active powers, thin energy bar + ring blast button at the bottom, small soft Bug arrows, new Game Over / Region Restored screens. `DrawPanel` (BYTE, intro, messages) = translucent strip with hairline and accent bar. Minimap smaller (22% of height), thin white frame, grey streets. BYTE box scaled the same way.
- Backups: `Docs/LevelBackups/GameHUD_before_blur_21nov.cs.txt`, `Minimap_before_blur_21nov.cs.txt`.
- Tests: compiles, no game errors; Autopilot Bugs ON **PASS** 44.0 s, 10/10, 3 lives, 1 blast. Screenshots `real_v5_hud.png`, `real_v5_complete.png`.
- Next: user feedback on the look; then the intro/title in the same style, sound + game feel, Level 2 realistic pass.

## Power cards, minimap legend, cool street lights, BYTE voice-only (21 Nov, user feedback)
- User: street lights looked like Nexus Cores; fruit shapes/effects unreadable; wants Blur-style power display with countdown and what it does; on the minimap can't tell me / cores / powers apart; BYTE following during play is confusing.
- Street lamps (`RealisticCity`): cool white LED lights and flat dim LED heads (yellow now only means collectibles). Each Nexus Core has a thin yellow beam rising above it (`LevelBuilder`, realistic look only).
- Fruits (`PowerUp.BuildIcon`): in the realistic look the 3D fruit is replaced by the clear fruit picture (same as minimap), floating and always facing the camera.
- Power cards (`GameHUD.DrawPowerCards` / `DrawPowerAnnounce`, realistic look): on pickup a big announcement for 1.8 s (fruit picture, spaced name, "APPLE · Bugs turn blue and flee - eat them!"); then one card per active power bottom-left: fruit inside a 40-tick countdown ring, name in the power colour, effect text, seconds left, thin bar, blinks in the last 2 s, slides in. Shield shows "1 HIT". The old floating text + BYTE line on pickup are skipped in the realistic look. Old fruit legend removed (cards replace it). `HookPowers()` runs in Update.
- Minimap (clean mode): YOU = white round badge with Pac-Man, CORE = yellow diamond, POWER = fruit picture (bigger), BUG = red danger halo, GENERATOR = square; small legend under the map.
- BYTE (`ByteCompanion`): model + light hidden during play in the realistic look; still talks in the dialogue box; still visible in the intro.
- Tests: compiles, no errors; pickup announcement + cards checked with screenshots (`real_v6_powers.png`, `real_v6_announce2.png`); Autopilot Bugs ON PASS 41.5 s, 10/10, 3 lives.
- Note: a recompile happened while Level 2 was playing (user) and broke that session (BugAI NullReference) - normal Unity behaviour, not a bug in the code.

## Blur-style intro (21 Nov, user: "make the intro like Blur, story stays, city cards, show the Bugs and their powers")
- New `Assets/Scripts/Core/IntroSequence.Blur.cs` (IntroSequence is now `partial`) + `Core/IntroAudio.cs`. Used only with the realistic look; the old intro is untouched for other looks (backup `Docs/LevelBackups/IntroSequence_before_blur_21nov.cs.txt`).
- Flow (~70 s, ENTER = next/skip ahead, ESC = jump to district select):
  1. Cold open in the real Neon District: aerial drift, Gran Via street dolly, crane up col 16, blackout from above (`RealisticCity.SetCityPower` flickers windows/lamps/signs off), Hunter close-up, tilted run down col 21, "SYSTEM FAILURE" glitch, story line on black. Letterbox, Blur-style lower-left captions (tag + line), glitch strips.
  2. Lab: macro slide past the pod while the terminal reboots (heartbeat thumps), ONLINE close-up hit, pod opens with low hero angle, BYTE's 3 lines (shortened), Pac-Man launches up.
  3. Title slam: city lights switch on in a wave from Pac-Man's start (riser), impact + flash, "PAC-MAN" letters track in, "NEXUS" yellow, line, "THE LOST CITY".
  4. Know Your Enemy: Bug showroom at (-70,0,170) with live BugModels, Blur rival cards (name, class chip, first seen, description, Speed/Aggression/Detection segment bars, 2 powers, weakness Apple + Energy Blast, auto-advance bar, tip "every Bug is slower than you"); then 3 CLASSIFIED cards (districts 03/04/05 boss).
  5. Select District: 5 cards (01 Neon District AVAILABLE with picture, 02 Skyline Zone LOCKED greyed, 03-05 SIGNAL LOST static), arrows browse (tick), ENTER on a locked card = shake + deny buzz, ENTER on 01 = impact + camera swoop into play. Shows "0 / 5 generators online". `DistrictState()` already supports later levels (restored / current).
- Card pictures: `Assets/Resources/IntroCards/district1.png`, `district2.png` (rendered with a temp camera, no HUD). Level 2 picture is the old look; re-render when Level 2 gets the realistic pass.
- Sound: synthesised at start (drone loop, whoosh, impact, riser, thump, glitch, tick, deny) - the first sound in the game.
- Intro UI is scaled from 1080p without the HUD clamp so it fits any window.
- Tests: compiles, no errors; screenshots of every phase (`blur_intro_*.png` in Assets/Screenshots); hand-off checked: lab + showroom removed, fog on, camera follow on, game playing.
- Ideas next: Level 2 realistic pass + district card intro for Level 2 (reuse the card screen with 01 RESTORED), gameplay sounds (pickups, cores, eat Bug), music.

## Level 2 Skyline Zone - realistic look + Blur-style level intro (21 Nov)
- Level 2 scene saved with `realisticLook = true` (untick to get the old sunset look back; backup of the city script before this: `RealisticCity_before_sky.bak` in the session outputs only).
- `RealisticCity` Skyline theme (`Sky` = CityVisuals.CurrentTheme == SkylineZone):
  - same block buildings, but walls start 9 units below the bridges (`BaseY`), bigger gaps between towers (0.14), taller skyline (4-8), no neon signs;
  - daylight PBR textures made in code: 3 glass curtain walls reflecting the sky, 2 light stone/concrete offices, warm sandstone, light roof gravel; roof clutter as in Level 1;
  - `BuildSkyBridges`: asphalt bridge decks (0.28 thick, no wet patches), concrete parapets + metal rail on every edge, white edge lines, dashed lane dividers between the two lanes, pillars every 4 cells down into the clouds, unlit lamp posts; cloud sea + silver hover cars still from CityVisuals.BuildGround;
  - `ApplySkyAtmosphere`: low warm sun (26/-58, 1.9, soft shadows), procedural sky with sun disk, warm haze fog, filmic grade, light grain. SSAO on (shared with Level 1).
- Level 2+ start (`IntroSequence.BlurLevelCard`): ~20 s cinematic with BYTE's Level 2 story lines as Blur captions (aerial approach over the clouds, low dolly on a sky bridge, the offline Generator, the new Sentinel), district name slam (SKYLINE ZONE / DISTRICT 02 / RESTORE THE GENERATOR), then the district cards (01 RESTORED + GENERATOR ONLINE, 02 AVAILABLE, 03-05 signal lost), ENTER swoops into play. ENTER now also skips the current shot in every intro shot.
- Card picture `Resources/IntroCards/district2.png` re-rendered with the new look.
- Tests: compiles, no errors; screenshots `l2_real_intro_a/b/c.png`; the user played Level 2 right away (score 1840, 2 cores, ~25 ms/frame, no errors). NOT checked by Claude yet: the Level 2 district card screen, Autopilot on the new Level 2, the Level 1 -> Level 2 flow.
- Possible polish: the low buildings on the camera side of the bottom street show big brown roofs - could be lowered / given more varied roofs.
