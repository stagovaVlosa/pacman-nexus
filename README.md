# PAC-MAN: NEXUS — THE LOST CITY (Unity)

A modern 3D cyberpunk reimagining of Pac-Man. Pac-Man, a forgotten energy-collection robot, wakes up in Nexus City after its central AI shut everything down, and has to restart the generators of five districts, one maze at a time. His companion BYTE, a small hologram robot, guides him.

Team: Vjola Gallapeni, Vlosa Stagova, Enita Halimi
Course: Zhvillimi i Lojërave (Game Development), project-based teaching (PBT)

## How to run
1. Open the project with **Unity 6000.3.26f1** (Unity Hub > Add project from disk).
2. Open `Assets/Scenes/Level1_NeonDistrict.unity`.
3. Press **Play**. The intro starts; ENTER goes to the next line, ESC skips the intro.

## Controls
| Key | Action |
| --- | --- |
| Arrow keys / WASD | Move (a turn pressed early is taken at the next street) |
| SPACE | Energy Blast: stuns nearby Bugs (costs 30 energy) |
| ESC | Pause menu (Resume / Restart region / Quit) |
| ENTER | Next dialogue line, next region after a level |
| SPACE (Game Over) | Restart |

## Gameplay
1. Explore the city maze and collect Energy Orbs (score + energy).
2. Find the 10 Nexus Cores (shown on the minimap).
3. Avoid the Bugs, or stun them with an Energy Blast. Power-ups: Devour Mode (eat the Bugs), Speed Burst, Energy Shield, Magnetic Field.
4. Bring the cores back to the Generator and activate it. The next region unlocks.

You have 3 Life Cores. A Bug's touch costs one; collected Nexus Cores stay collected.

## Current state (vertical slice)
- Intro cinematic (story text, abandoned lab, Pac-Man wakes up, BYTE appears) and title screen
- **Level 1 – Neon District**: rainy neon night city (Kenney City Kit + neon by code), Hunter, Patrollers, Blockers
- **Level 2 – Skyline Zone**: sky bridges above a sunset cloud sea, new enemy: Sentinel
- HUD (Life Cores, Nexus Cores x/10, score, energy, Energy Blast), round minimap, BYTE dialogue, pause menu
- Buildings in front of Pac-Man fade; Pac-Man and Bugs show as outlines behind walls
- Automatic play test (Autopilot) that completes a level by itself with the Bugs on

Planned: Level 3 Underground Transit Maze (Glitch), Level 4 Data Core (Interceptor), Level 5 The Nexus Core (Eraser boss and ending), main menu, music and sound, saved progress and high score.

## Code (`Assets/Scripts`, namespace `Nexus`)
| Folder | Responsibility |
| --- | --- |
| `Core/` | `GameManager` (score, energy, lives, cores, level state), `IntroSequence` (intro and level cards), `Visuals`, `MeshBuilder` |
| `World/` | `LevelBuilder` (reads the text layout, owns the grid), `GridPath` (BFS shortest path), `Collectible`, `Generator`, `PowerUp`, city visuals and effects |
| `Player/` | `PlayerMovement` (grid movement, turn buffer), `PlayerLives`, `EnergyBlast`, `PlayerPowers`, `ByteCompanion`, `CameraFollow`, models |
| `Enemies/` | `BugAI` (Chaser, Patroller, Blocker, Sentinel), `BugModel` |
| `UI/` | `GameHUD`, `Minimap`, `PauseMenu` |
| `Editor/`, `Debug/` | Scene setup menus (Nexus > Create Level 1/2 Scene), play tests, `Autopilot` |

Levels are text files in `Assets/Levels/` (`#` wall, `.` orb, `C` Nexus Core, `P` start, `G` generator, `H`/`B`/`S`/digits = Bugs, `*` `>` `+` `%` = power-ups).

Documentation: `Docs/GDD.md` (design), `Docs/Progress.md` (development log), `Docs/PBT7_Arkitektura.md` (architecture, interfaces, code review).

## Credits
- City buildings: [Kenney City Kit (Commercial)](https://kenney.nl), CC0
- Everything else (code, procedural models, effects) by the team
