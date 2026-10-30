using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Helper for creating simple placeholder shapes with URP materials.
    /// Used while the game has no real art yet. Later, prefabs replace these.
    /// </summary>
    public static class Visuals
    {
        // GDD palette
        public static readonly Color MidnightNavy = Hex("#080D1B");
        public static readonly Color DarkBlue     = Hex("#111A2E");
        public static readonly Color Cyan         = Hex("#00D9FF");
        public static readonly Color NeonYellow   = Hex("#FFCA28");
        public static readonly Color CorruptedRed = Hex("#FF3158");
        public static readonly Color Purple       = Hex("#A855F7");

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// <summary>Returns a shared material (cached) with a base color and optional glow.</summary>
        public static Material GetMaterial(Color baseColor, Color emission, float glow)
        {
            string key = baseColor + "|" + emission + "|" + glow;
            if (cache.TryGetValue(key, out var existing) && existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", baseColor);
            mat.color = baseColor;
            if (glow > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission * glow);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            cache[key] = mat;
            return mat;
        }

        /// <summary>Creates a primitive without a collider (we use the grid, not physics).</summary>
        public static GameObject Shape(PrimitiveType type, string name, Transform parent,
                                       Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static void SetMaterial(GameObject go, Material mat)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        }
    }
}
