using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Makes buildings semi-transparent (~20%) while they stand between the camera and Pac-Man,
    /// the street ahead of him (up to 'aheadCells' in his movement direction) or a Bug,
    /// so the important things are never hidden. Sits on the "Walls" root; every direct child is one building.
    /// Visual only: uses bounding boxes and rays (no physics), the grid is not touched.
    /// </summary>
    public class BuildingFader : MonoBehaviour
    {
        public float fadedAlpha = 0.08f;
        public float fadeSpeed = 5f;      // alpha per second
        public float checkRadius = 10f;   // only buildings this close to Pac-Man (XZ) are tested
        public int aheadCells = 2;

        class Building
        {
            public Bounds bounds;
            public Renderer[] renderers;
            public Material[][] opaque, faded;
            public float alpha = 1f;
            public bool usingFaded;
        }

        readonly List<Building> buildings = new List<Building>();
        static readonly Dictionary<Material, Material> transparentCopies = new Dictionary<Material, Material>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        Camera cam;
        readonly List<Vector3> points = new List<Vector3>();
        Vector2Int lastDir;

        void Start()
        {
            block = new MaterialPropertyBlock();
            foreach (Transform child in transform)
            {
                var rs = child.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = new Building { renderers = rs, bounds = rs[0].bounds };
                b.opaque = new Material[rs.Length][];
                b.faded = new Material[rs.Length][];
                for (int i = 0; i < rs.Length; i++)
                {
                    b.bounds.Encapsulate(rs[i].bounds);
                    b.opaque[i] = rs[i].sharedMaterials;
                    b.faded[i] = new Material[b.opaque[i].Length];
                    for (int m = 0; m < b.opaque[i].Length; m++) b.faded[i][m] = TransparentCopy(b.opaque[i][m]);
                }
                buildings.Add(b);
            }
        }

        static Material TransparentCopy(Material src)
        {
            // Only URP Lit materials get a transparent copy; other shaders stay as they are
            if (src == null || src.shader == null || !src.shader.name.EndsWith("/Lit")) return src;
            if (transparentCopies.TryGetValue(src, out var copy) && copy != null) return copy;
            copy = new Material(src) { name = src.name + " (Faded)" };
            Visuals.MakeTransparent(copy);
            transparentCopies[src] = copy;
            return copy;
        }

        void LateUpdate() => Refresh(Time.deltaTime);

        /// <summary>Checks which buildings hide Pac-Man and moves their alpha by up to fadeSpeed * dt.</summary>
        public void Refresh(float dt)
        {
            if (cam == null) cam = Camera.main;
            var player = PlayerMovement.Instance;
            if (cam == null || player == null) return;

            // Several points on Pac-Man (center, top, bottom with the hover engine, left, right)
            Vector3 camPos = cam.transform.position;
            Vector3 target = player.transform.position;
            Vector3 side = cam.transform.right * 0.45f;
            points.Clear();
            points.Add(target);
            points.Add(target + Vector3.up * 0.45f);
            points.Add(target + Vector3.down * 0.5f);
            points.Add(target + side);
            points.Add(target - side);

            // The street ahead, cell by cell until a wall (keeps the last direction while standing still)
            var level = LevelBuilder.Instance;
            if (player.Direction != Vector2Int.zero) lastDir = player.Direction;
            if (level != null && lastDir != Vector2Int.zero)
            {
                var c = player.CurrentCell;
                for (int i = 0; i < aheadCells; i++)
                {
                    c += lastDir;
                    if (!level.IsWalkable(c)) break;
                    points.Add(level.CellToWorld(c, 0.15f));
                    points.Add(level.CellToWorld(c, 0.6f));
                }
            }

            // Bugs
            foreach (var h in BugAI.All) points.Add(h.transform.position);

            foreach (var b in buildings)
            {
                var c = b.bounds.center;
                bool blocks = new Vector2(c.x - target.x, c.z - target.z).sqrMagnitude < checkRadius * checkRadius
                              && Blocks(b.bounds, camPos, points);
                float want = blocks ? fadedAlpha : 1f;
                if (Mathf.Approximately(b.alpha, want) && b.usingFaded == (want < 1f)) continue;
                b.alpha = Mathf.MoveTowards(b.alpha, want, fadeSpeed * dt);
                Apply(b);
            }
        }

        static bool Blocks(Bounds bounds, Vector3 camPos, List<Vector3> points)
        {
            foreach (var p in points)
            {
                var toPoint = p - camPos;
                if (bounds.IntersectRay(new Ray(camPos, toPoint), out float d) && d < toPoint.magnitude) return true;
            }
            return false;
        }

        void Apply(Building b)
        {
            bool fade = b.alpha < 0.999f;
            if (fade != b.usingFaded)
            {
                b.usingFaded = fade;
                for (int i = 0; i < b.renderers.Length; i++)
                {
                    b.renderers[i].sharedMaterials = fade ? b.faded[i] : b.opaque[i];
                    if (fade) continue;
                    b.renderers[i].SetPropertyBlock(null);
                    for (int m = 0; m < b.opaque[i].Length; m++) b.renderers[i].SetPropertyBlock(null, m);
                }
            }
            // Neon rims and signs (not Lit, so no transparent copy) disappear while the building is faded,
            // otherwise they float in the air and clutter the view around Pac-Man
            bool showGlow = b.alpha > 0.6f;
            for (int i = 0; i < b.renderers.Length; i++)
            {
                bool allGlow = true;
                for (int m = 0; m < b.opaque[i].Length; m++)
                    if (b.faded[i][m] != b.opaque[i][m]) { allGlow = false; break; }
                if (allGlow) b.renderers[i].enabled = showGlow;
            }
            if (!fade) return;

            // Per-building alpha through a property block, so the shared faded materials stay unchanged
            for (int i = 0; i < b.renderers.Length; i++)
            {
                var mats = b.faded[i];
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m] == b.opaque[i][m]) continue;
                    var col = mats[m].GetColor(BaseColorId);
                    col.a = b.alpha;
                    block.Clear();
                    block.SetColor(BaseColorId, col);
                    b.renderers[i].SetPropertyBlock(block, m);
                }
            }
        }
    }
}
