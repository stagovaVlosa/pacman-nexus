using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Makes buildings semi-transparent while they stand between the camera and Pac-Man,
    /// so Pac-Man is never hidden. Sits on the "Walls" root; every direct child is one building.
    /// Visual only: uses bounding boxes and rays (no physics), the grid is not touched.
    /// </summary>
    public class BuildingFader : MonoBehaviour
    {
        public float fadedAlpha = 0.25f;
        public float fadeSpeed = 5f;      // alpha per second
        public float checkRadius = 3f;    // only buildings this close to Pac-Man (XZ) are tested

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
            if (src == null) return null;
            if (transparentCopies.TryGetValue(src, out var copy) && copy != null) return copy;
            copy = new Material(src) { name = src.name + " (Faded)" };
            Visuals.MakeTransparent(copy);
            transparentCopies[src] = copy;
            return copy;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            var player = PlayerMovement.Instance;
            if (cam == null || player == null) return;

            // Test several points on Pac-Man (center, top, bottom with the hover engine, left, right)
            Vector3 camPos = cam.transform.position;
            Vector3 target = player.transform.position;
            Vector3 side = cam.transform.right * 0.4f;
            var points = new[] { target, target + Vector3.up * 0.4f, target + Vector3.down * 0.45f, target + side, target - side };

            float dt = Time.deltaTime;
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

        static bool Blocks(Bounds bounds, Vector3 camPos, Vector3[] points)
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
            if (!fade) return;

            // Per-building alpha through a property block, so the shared faded materials stay unchanged
            for (int i = 0; i < b.renderers.Length; i++)
            {
                var mats = b.faded[i];
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
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
