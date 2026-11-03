# PAC-MAN: NEXUS — THE LOST CITY

Project folder: `C:\zhvillimi_Pac-Man\Pac-Man3D` (Unity 6000.3.26f1). Started as a team classic Pac-Man; now solo.
The old classic scripts in `Assets/Scripts/*.cs` (root level) are wrapped in `#if PACMAN_CLASSIC` and do not compile. Do not edit them.

Solo developer, Unity beginner, ~3 hours/day, 8 weeks. Full design: `Docs/GDD.md`.
Explain Unity Editor steps simply and exactly (the developer is learning Unity).
The developer speaks Albanian — reply in Albanian, keep code, comments and in-game text in English.

## Project facts
- Unity 6, URP, **new Input System only** (use `UnityEngine.InputSystem.Keyboard.current`, never `Input.GetKey`).
- All game code is in `Assets/Scripts/`, namespace `Nexus`. Editor tools in `Assets/Scripts/Editor/`.
- Menu **Nexus > Create Level 1 Scene** rebuilds the test scene from code.

## Architecture rules (do not change without asking)
- **Grid-based**, no NavMesh, no physics for movement or pickups.
- Levels are text files in `Assets/Levels/`. Legend: `#` wall, `.` orb, `C` Nexus Core (exactly 10),
  `P` player start, `G` generator, `-` enemy door, space = empty floor.
  Bugs (orb under `B` and digits): `H` Chaser spawn, `B` Blocker post, `1`-`9` Patroller route (2 cells, same digit).
- All 5 levels use the same grid size (29x29; Level 1 was enlarged from 21x21 on 2026-11-03). Each level = layout + `LevelTheme` (visual skin, planned).
- `LevelBuilder` owns the grid: `IsWalkable`, `CellToWorld`, `WorldToCell`. Player, enemies and minimap use it.
- Grid y grows downward (row 0 = top of text = far side of the maze).
- `GameManager` owns score, energy, cores, level state. Others read from it.
- Placeholder shapes come from `Visuals`; real art later replaces them through prefab fields on `LevelBuilder`.

## Visual direction
The game must look like a **top-down cyberpunk city at night**, NOT a maze of plain cubes (GDD §11).
- **Walls = buildings**: each `#` cell is a city block/building (2-4 high in the maze) with glowing windows,
  neon trim, vertical neon signs and billboards that cast colored light on the wet street.
- **Camera**: angled 3D view like `Docs/ConceptArt.png` (pitch 75°, distance 8.5, FOV 55). Buildings between
  the camera and Pac-Man fade out (`World/BuildingFader.cs`), so Pac-Man is always visible.
- **Pac-Man = flying spherical robot** (`Docs/PacManRobot_Design.md`), no face; Hunter = corrupted red drone.
- **Corridors = streets**: dark road surface with glowing lane lines / road markings.
- **Enemies = ghosts**: colored ghost bodies (Hunter red, Sentinel purple, Glitch magenta, Eraser black/red) with glowing eyes.
- **Minimap**: round (circular), top-right corner.
- **HUD**: dark panels with a cyan border/frame.
- Palette: Midnight Navy `#080D1B`, Dark Blue `#111A2E`, Electric Cyan `#00D9FF`, Neon Yellow `#FFCA28`,
  Corrupted Red `#FF3158`, Purple `#A855F7`. Use emissive materials + bloom; paths and objectives must stay readable.
- Visuals stay separate from gameplay: grid logic in `LevelBuilder` does not change because of art.

## Status notes
- 2026-11-03: Pac-Man robot (`Player/PacManModel.cs`), Hunter drone (`Enemies/HunterDroneModel.cs`),
  `BuildingFader`, lower 3D camera, taller buildings, neon signs/billboards/lights, wet street + reflection probe,
  runtime post FX (`CityVisuals.ApplyAtmosphere`: bloom, color adjustments, purple/cyan split toning, vignette,
  purple fog). Checked with the batch screenshot tool, not yet played interactively.
- Procedural meshes: `Core/MeshBuilder.cs`; shared materials in `Visuals` (`Lit`, `Glass`, `Glow`);
  additive glow shader `Assets/Resources/Shaders/NexusGlow.shader`.
- Week 8 build note: transparent materials are created in code; the Windows build may need a material
  in the project that uses URP Lit Transparent so the shader variant is not stripped.

## Tooling
- Claude is connected to the Unity Editor through **MCP for Unity** (UnityMCP tools): it can read the scene
  hierarchy, read the Console, run menu items and check compilation. Ignore "NoSubscription" errors from
  generators.ai.unity.com in the Console (Unity AI, not our code).
  It must be started in Unity (Window > MCP for Unity > Start Server, http://127.0.0.1:8080/mcp), then `/mcp`.
- Screenshots: menu **Nexus > Capture Game Screenshot** (in Play mode, saves to `Docs/Screenshots/`).
  Without MCP: run a second Unity in batch mode on a git worktree with
  `-executeMethod Nexus.EditorTools.NexusScreenshot.CaptureBatch -shotPath out.png -quit` (renders the game camera).

## Scope (solo version)
Keep: movement, camera, orbs, 10 cores, generator, lives, Energy Blast, minimap, Hunter, Sentinel, Glitch,
Eraser, BYTE (follow + text dialogue), 5 themed levels, menus, save.
Cut: Ending B, Lost Archives, Dash, Devour Mode, Ghost Vision, Magnetic Field, puzzles, moving trains.

## Progress
- [x] Week 1: grid builder, Pac-Man movement, camera, orbs, 10 Nexus Cores, generator, prototype HUD
- [ ] Week 2-3: Hunter (BFS chase), lives + respawn, Energy Blast (Space)
  - [x] Hunter: `Enemies/HunterAI.cs` (chase within 8 cells by maze route via `World/GridPath.cs` BFS, else wander),
        `Enemies/HunterDroneModel.cs` (corrupted red drone, flickering lights, glitch effect), spawns from `H` cells.
        No damage yet. `Enemies/GhostModel.cs` is no longer used.
        Note: the `-` door is currently walkable for Pac-Man too.
- [x] Level 1 layout 29x29 (generated with a seeded maze script, then checked: 10 cores, all cells reachable,
      8 dead ends, extra loops). Energy Orb = +10 score, +1 energy (max 100). Level ends only at the Generator.
- [x] Generator: reaching `G` with all cores = activate + Level Complete (no key). Too early = HUD message
      "Collect all 10 Nexus Cores" (`GameHUD.ShowMessage`). 5 of the 10 cores are in dead ends.
- [x] Bugs (ghosts renamed): `Enemies/BugAI.cs` (`BugType` Chaser/Patroller/Blocker, all slower than Pac-Man 4.5:
      Chaser 3.6 always BFS-chases, Patroller 3.2 back and forth, Blocker 2.8 guards 3 cells around its post,
      chases at 3.9 within 4 cells while Pac-Man is within 7 cells of the post), `Enemies/BugModel.cs` (drone,
      color per type: red / purple / magenta). Level 1: 1 Chaser, 2 Patrollers, 2 Blockers (Generator, a core).
- [x] Minimap: `UI/Minimap.cs` (OnGUI, added by GameHUD), round, top-right, whole maze, fog of war:
      cells within 3 of Pac-Man get discovered; cores in discovered cells (gone when collected), Bugs on
      discovered cells within 5 of Pac-Man, Generator always (blinks cyan when all cores are collected).
- [x] HUD panels (`GameHUD.DrawPanel`): top-left level/cores x/10/score/lives, bottom energy bar 0-100 with number.
- [x] BYTE: `Player/ByteCompanion.cs` (primitives + cyan glow, follows behind/above, created by LevelBuilder),
      3 lines in a dialogue panel: start, all cores collected, Level Complete. No other dialogue yet.
- [x] Neon District visual pass: roof neon pink/cyan/purple, curb lines merged into one mesh (performance),
      orbs brighter (shared material), cores pulse (own materials + aura + light, `Collectible`),
      Generator light off until activated, then flickers on (cyan). Fog = RenderSettings (URP has no fog override).
- [ ] Week 4: minimap (gradual reveal), LevelTheme ScriptableObject, LevelManager, 5 layouts
- [ ] Week 5: Neon District art, BYTE + dialogue UI
- [ ] Week 6-7: Sentinel, Glitch, Eraser, other 4 themes, story
- [ ] Week 8: menus, audio, save, bug fixing, Windows build

## Workflow
Small steps. After each step: say which files changed, how to test in Unity, then the developer commits to Git.
