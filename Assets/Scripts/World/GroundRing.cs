using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Glowing ring with a soft spot on the street under a character, so it is always easy to find.
    /// Drawn on top of everything (also through buildings). Stays at street level while the character bobs.
    /// </summary>
    public class GroundRing : MonoBehaviour
    {
        const float GroundY = 0.03f;

        static Mesh ringMesh;
        static Texture2D spotMask;

        Material ringMat, spotMat;
        float intensity, phase;

        public static GroundRing Create(Transform character, Color color, float radius, float intensity)
        {
            if (ringMesh == null)
            {
                var mb = new MeshBuilder();
                mb.Tube(MeshBuilder.Circle(1f, 64, Vector3.zero, MeshBuilder.Flat), 0.07f, 6, true);
                ringMesh = mb.ToMesh("GroundRing");
            }
            if (spotMask == null) spotMask = Visuals.GradientTexture(true);

            var go = new GameObject("GroundRing");
            go.transform.SetParent(character, false);
            go.transform.localScale = Vector3.one * radius;
            var ring = go.AddComponent<GroundRing>();
            ring.intensity = intensity;
            ring.phase = Random.value * 6f;
            ring.ringMat = Visuals.Glow(color, intensity, 0f, null, true, true);
            Visuals.MeshPart("Ring", go.transform, ringMesh, ring.ringMat);
            ring.spotMat = Visuals.Glow(color, intensity * 0.35f, 0f, spotMask, true, true);
            var spot = Visuals.Shape(PrimitiveType.Quad, "Spot", go.transform, Vector3.zero, Vector3.one * 2.4f, ring.spotMat);
            spot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            spot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.LateUpdate();
            return ring;
        }

        /// <summary>Changes the ring colour (Bugs turn blue in Devour Mode).</summary>
        public void SetColor(Color c)
        {
            ringMat.SetColor("_Color", c);
            spotMat.SetColor("_Color", c);
        }

        void LateUpdate()
        {
            var p = transform.parent != null ? transform.parent.position : transform.position;
            transform.position = new Vector3(p.x, GroundY, p.z);
            float pulse = 0.85f + Mathf.Sin(Time.time * 4f + phase) * 0.15f;
            ringMat.SetFloat("_Intensity", intensity * pulse);
        }
    }
}
