# Pac-Man 3D (Unity)

A 3D arcade maze game built in Unity. Eat all the dots, but don't get eaten!

Team: Vjola Gallapeni, Vlosa Stagova, Enita Halimi
Course: Game Development, PBT 3 (Java 4), Phase II

## How to run
1. Open the project with **Unity 6000.3.26f1** (Unity Hub, "Add project from disk").
2. Open `Assets/Scenes/SampleScene`.
3. Press **Play**. The game builds itself at runtime, nothing else to set up.

## Controls
- Arrow keys or WASD to move
- SPACE to play again after Game Over

## Current state (prototype)
- 11x9 maze built from a text layout, tilted 55-degree camera that follows the player
- Cell-by-cell Pac-Man movement with buffered turns and a side tunnel
- 2 ghosts (one chases with BFS, one wanders), 3 lives, score, Game Over and restart
- Title screen with "How to play"

Planned: 4 ghosts with Chase/Scatter, power pellets, 10 levels, doors and portals, boss, stars, shop, saved high score.

## Code (Assets/Scripts)
- `MazeBuilder`: builds the maze from a text layout
- `PacManController`: player movement
- `GhostController`: ghost movement and chasing
- `GameManager`: score, lives, screens
- `CameraFollow`: fixed-angle follow camera
- `GameBootstrap`: builds the whole game when Play is pressed
