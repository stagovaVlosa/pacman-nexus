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
  `P` player start, `G` generator, `H` enemy spawn, `-` enemy door, space = empty floor.
- All 5 levels use the same grid size (21x21). Each level = layout + `LevelTheme` (visual skin, planned).
- `LevelBuilder` owns the grid: `IsWalkable`, `CellToWorld`, `WorldToCell`. Player, enemies and minimap use it.
- Grid y grows downward (row 0 = top of text = far side of the maze).
- `GameManager` owns score, energy, cores, level state. Others read from it.
- Placeholder shapes come from `Visuals`; real art later replaces them through prefab fields on `LevelBuilder`.

## Visual direction
The game must look like a **top-down cyberpunk city at night**, NOT a maze of plain cubes (GDD §11).
- **Walls = buildings**: each `#` cell is a city block/building with glowing windows and neon trim/signs.
- **Corridors = streets**: dark road surface with glowing lane lines / road markings.
- **Enemies = ghosts**: colored ghost bodies (Hunter red, Sentinel purple, Glitch magenta, Eraser black/red) with glowing eyes.
- **Minimap**: round (circular), top-right corner.
- **HUD**: dark panels with a cyan border/frame.
- Palette: Midnight Navy `#080D1B`, Dark Blue `#111A2E`, Electric Cyan `#00D9FF`, Neon Yellow `#FFCA28`,
  Corrupted Red `#FF3158`, Purple `#A855F7`. Use emissive materials + bloom; paths and objectives must stay readable.
- Visuals stay separate from gameplay: grid logic in `LevelBuilder` does not change because of art.

## Status notes
- `Assets/Scripts/World/CityVisuals.cs` and `Assets/Scripts/Player/PacManModel.cs` were added but are
  **not yet tested visually** in the Unity Editor. Check them in Play mode before building on them.

## Tooling
- Claude is connected to the Unity Editor through **MCP for Unity** (UnityMCP tools): it can read the scene
  hierarchy, read the Console, run menu items and check compilation. Ignore "NoSubscription" errors from
  generators.ai.unity.com in the Console (Unity AI, not our code).

## Scope (solo version)
Keep: movement, camera, orbs, 10 cores, generator, lives, Energy Blast, minimap, Hunter, Sentinel, Glitch,
Eraser, BYTE (follow + text dialogue), 5 themed levels, menus, save.
Cut: Ending B, Lost Archives, Dash, Devour Mode, Ghost Vision, Magnetic Field, puzzles, moving trains.

## Progress
- [x] Week 1: grid builder, Pac-Man movement, camera, orbs, 10 Nexus Cores, generator, prototype HUD
- [ ] Week 2-3: Hunter (BFS chase), lives + respawn, Energy Blast (Space)
- [ ] Week 4: minimap (gradual reveal), LevelTheme ScriptableObject, LevelManager, 5 layouts
- [ ] Week 5: Neon District art, BYTE + dialogue UI
- [ ] Week 6-7: Sentinel, Glitch, Eraser, other 4 themes, story
- [ ] Week 8: menus, audio, save, bug fixing, Windows build

## Workflow
Small steps. After each step: say which files changed, how to test in Unity, then the developer commits to Git.
