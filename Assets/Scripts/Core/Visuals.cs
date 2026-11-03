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

        /// <summary>Switches a URP Lit material to Transparent (alpha blend from _BaseColor alpha, no depth write).</summary>
        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
        }

        /// <summary>New URP Lit material with metallic/smoothness and optional glow (not cached).</summary>
        public static Material Lit(Color color, float metallic, float smoothness, Color emission = default, float glow = 0f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (glow > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission * glow);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        /// <summary>URP Lit switched to Transparent (alpha blend, no depth write).</summary>
        public static Material Glass(Color color, float alpha, Color emission)
        {
            color.a = alpha;
            var m = Lit(color, 0f, 0.95f, emission, 1f);
            MakeTransparent(m);
            m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        /// <summary>Additive glow (Nexus/Glow shader). fresnel > 0 = rim glow only, 0 = flat glow.</summary>
        public static Material Glow(Color color, float intensity, float fresnel, Texture2D mask = null, bool twoSided = false)
        {
            var shader = Shader.Find("Nexus/Glow");
            if (shader == null) return Lit(color, 0f, 0.5f, color, intensity); // fallback, should not happen
            var m = new Material(shader);
            m.SetColor("_Color", color);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_FresnelPower", fresnel);
            if (mask != null) m.SetTexture("_MainTex", mask);
            m.SetFloat("_Cull", twoSided ? (float)UnityEngine.Rendering.CullMode.Off : (float)UnityEngine.Rendering.CullMode.Back);
            return m;
        }

        /// <summary>Soft gradient in the alpha channel: radial (round spot) or vertical (fades along v).</summary>
        public static Texture2D GradientTexture(bool radial)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = radial
                        ? 1f - Mathf.Clamp01(new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude / (size / 2f))
                        : 1f - (float)y / (size - 1);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>GameObject with a procedural mesh. Shadows off by default (glow parts, small details).</summary>
        public static GameObject MeshPart(string name, Transform parent, Mesh mesh, Material mat, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
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
