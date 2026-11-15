using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus
{
    /// <summary>
    /// "X-ray" outline: a sphere slightly larger than the character, drawn ONLY where something
    /// (a building) is in front of it (shader Nexus/Silhouette, ZTest Greater). So Pac-Man and the
    /// Bugs never disappear behind buildings; when nothing hides them, the sphere is invisible.
    /// The sphere is bigger than the character so the character never hides its own outline.
    /// </summary>
    public static class Silhouette
    {
        public static GameObject Add(Transform parent, float radius, Color color)
        {
            var shader = Shader.Find("Nexus/Silhouette");
            if (shader == null)
            {
                Debug.LogWarning("Silhouette: shader Nexus/Silhouette not found (Assets/Resources/Shaders).");
                return null;
            }
            var mat = new Material(shader);
            mat.SetColor("_Color", new Color(color.r, color.g, color.b, 0.75f));
            var go = Visuals.Shape(PrimitiveType.Sphere, "Silhouette", parent, Vector3.zero, Vector3.one * radius * 2f, mat);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }
}
