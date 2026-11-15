# PAC-MAN: NEXUS — 8-week plan

Design: `Docs/GDD.md` + `Docs/ConceptArt.png`. Pac-Man = robot (`Docs/PacManRobot_Design.md`).
Progress and decisions: `Docs/Progress.md`. Every new chat reads these three files first.

## How we work
- One Claude edits the project at a time: Claude in the Claude app, linked to this computer. It edits the scripts, runs Unity, plays and checks screenshots. Claude Code in VS Code only if the app is unavailable, never both at the same time.
- Every step ends with a test in Play mode. "Done" means tested; anything untested is called untested.
- Visibility rules checked in every level:
  1. Pac-Man and every Bug are always visible: silhouette through buildings, buildings in front of them fade.
  2. Nothing moves through walls: movement stays on the grid (already true).
  3. Camera always shows at least 5 cells around Pac-Man.
  4. Minimap matches the real maze.

## To install (the user installs; Claude chooses and sets up)
- Unity packages (free, Package Manager): Cinemachine (camera), TextMeshPro / UI Toolkit (HUD and menus), Visual Effect Graph or Particle packs (neon particles, sparks).
- Asset Store: a modular cyberpunk / sci-fi city kit (buildings, streets, props), neon signs, and later sets for subway, server room and reactor. Free first, small paid pack only if the free ones look too plain.
- Unity MCP (already configured, currently failing): fix it so Claude can read the Console and the scene directly.
- Blender (free), only if a model has to be made by hand.

## Weeks
1. Foundation: fix Unity MCP, lock Level 1 gameplay, add the visibility rules (silhouettes, fading, camera), automatic play test (`NexusPlayTests`).
2. Level 1 look: city kit, neon, lighting, fog, particles. Pac-Man robot polish, BYTE as in GDD 3.2, Hunter Bug model.
3. Intro (lab awakening, BYTE meets Pac-Man), main menu, pause, game over, level complete, proper HUD and minimap, music and sound.
4. Level 2 Skyline Zone (+ Sentinel).
5. Level 3 Underground Transit Maze (+ Glitch).
6. Level 4 Data Core (+ Interceptor).
7. Level 5 The Nexus Core (+ Eraser boss, ending).
8. Polish, bug fixing, Windows build, presentation material.

All levels reuse the same systems (maze from text layout, cores, generator, Bugs, minimap); each level adds a layout, an art set and one new Bug type.
