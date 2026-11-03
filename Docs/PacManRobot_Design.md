# Pac-Man robot design (approved direction)

Pac-Man is a **flying spherical robot** (an energy-collection unit), not a cartoon character.
**No human features at all**: no eyes, brows, lips, tongue or face.
It keeps the Pac-Man silhouette through a **wedge opening at the front** (like a pie with a slice missing),
clearly visible from the top-down game camera.

Interactive reference built in three.js: `Docs/PacManRobot_Reference.html`
(open it in a browser; all geometry, sizes, colors and animation values are in its script).

## Structure (radius R = 1 in the reference; scale to fit one grid cell in the game)
- **Two shell halves, left and right**, hinged on the **vertical (Y) axis** through the poles.
  Front = forward/movement direction. Each half covers 180° minus a tiny seam.
  Opening: right half rotates +angle around Y, left half -angle, so a wedge opens at the front.
  Chomp: angle = |sin(t * 3.2)| * 32° while moving; about 25° when idle.
- **Shell material**: semi-transparent yellow glass (#FFC400, opacity ~0.28, smooth, clearcoat)
  plus a **fresnel rim glow** (orange-yellow, additive) so the silhouette reads against the dark city.
- **On each half**: dark metal lip along the front cut edge with a thin amber glow line,
  3 thin amber panel seams (meridians) on the glass, a dark metal **belt segment** at the equator
  (height ~0.16) with amber glowing top/bottom edges and 4 small cyan vents.
- **Hubs (do not rotate)**: dark metal top cap (polar angle ~0.32 rad) with a hexagonal plate and a cyan ring;
  bottom cap with the **hover engine**: metal housing, cyan glowing ring, bright cyan disc,
  faint additive cyan light cone pointing down, small cyan particles falling, cyan glow on the ground.
- **Interior (static, visible through the glass and the wedge)**:
  - Glowing energy **core** (icosahedron r 0.2, warm yellow-white emissive, pulsing) inside a cyan wireframe cage.
  - Two thin metal **gyroscope rings** (r 0.42 and 0.5) with small cyan lights, rotating.
  - Three thin dark metal **frame ribs** (r 0.86) at different angles.
  - ~7 small dark **circuit modules** with cyan/amber light strips.
  - ~18 **cables** (tubes) curving from the core to the shell: 1/3 dark rubber, 1/3 glowing cyan, 1/3 glowing amber.
  - Small bright **energy pulses** travelling along the glowing cables from the core outward.
- Floats with a gentle bob (~0.07 R). Faces the movement direction.

## Palette
Yellow glass #FFC400, amber glow #FF8C00, cyan #00D9FF, dark metal #1A1F2B, mid metal #3A4252.

## Unity notes
- Build it procedurally in code (like the current PacManModel) or as a Blender model; transparency needs a
  URP transparent material, glow lines use emissive materials so Bloom picks them up.
- Keep gameplay untouched: PlayerMovement stays the same, only the visual changes.
