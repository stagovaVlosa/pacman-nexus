using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Ghost built from primitives: round head, short body, a wavy skirt and glowing eyes.
    /// The eyes face the object's forward (+Z) direction. The color is chosen per enemy type,
    /// so Sentinel, Glitch and Eraser can reuse this until real models exist.
    /// </summary>
    public class GhostModel : MonoBehaviour
    {
        public float wiggleSpeed = 9f;
        public float wiggleAmount = 0.04f;

        Transform[] skirt;
        float[] skirtBaseY;
        float phase;

        /// <param name="color">Body color. Keep it saturated: strong glow on light colors tonemaps to pastel.</param>
        /// <param name="glow">Body emission color (used at a low intensity).</param>
        /// <param name="eyeGlow">Pupil color, glows strongly against the white eyes.</param>
        public static GhostModel Create(Transform body, float radius, Color color, Color glow, Color eyeGlow)
        {
            var skin = Visuals.GetMaterial(color, glow, 0.8f);
            var eyeWhite = Visuals.GetMaterial(Color.white, Color.white, 2f);
            var pupil = Visuals.GetMaterial(eyeGlow, eyeGlow, 3f);

            var model = body.gameObject.AddComponent<GhostModel>();
            float d = radius * 2f;

            // Head: sphere whose lower half sits inside the body cylinder
            Visuals.Shape(PrimitiveType.Sphere, "Head", body, new Vector3(0f, radius * 0.3f, 0f), Vector3.one * d, skin);
            Visuals.Shape(PrimitiveType.Cylinder, "Body", body, new Vector3(0f, -radius * 0.25f, 0f),
                new Vector3(d, radius * 0.55f, d), skin);

            // Skirt: a ring of small spheres along the bottom edge that bob up and down
            const int count = 8;
            model.skirt = new Transform[count];
            model.skirtBaseY = new float[count];
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var pos = new Vector3(Mathf.Cos(a) * radius * 0.78f, -radius * 0.8f, Mathf.Sin(a) * radius * 0.78f);
                model.skirt[i] = Visuals.Shape(PrimitiveType.Sphere, "Skirt", body, pos,
                    Vector3.one * radius * 0.55f, skin).transform;
                model.skirtBaseY[i] = pos.y;
            }

            // Eyes high on the front of the head, so the top-down camera sees them
            var headCenter = new Vector3(0f, radius * 0.3f, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                var dir = new Vector3(0.38f * side, 0.75f, 0.55f).normalized;
                var eye = Visuals.Shape(PrimitiveType.Sphere, "Eye", body, headCenter + dir * radius * 0.82f,
                    Vector3.one * radius * 0.5f, eyeWhite).transform;
                Visuals.Shape(PrimitiveType.Sphere, "Pupil", eye, dir * 0.3f, Vector3.one * 0.6f, pupil);
            }
            return model;
        }

        void Update()
        {
            phase += Time.deltaTime * wiggleSpeed;
            for (int i = 0; i < skirt.Length; i++)
            {
                var p = skirt[i].localPosition;
                // Neighbouring skirt bumps move in opposite directions, like a wave
                p.y = skirtBaseY[i] + Mathf.Sin(phase + i * Mathf.PI) * wiggleAmount;
                skirt[i].localPosition = p;
            }
        }
    }
}
