using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Clear, solid 3D Pac-Man: a glossy yellow sphere whose two halves (left/right) open and close
    /// around the vertical axis, so the mouth is visible from the top-down camera.
    /// A dark sphere inside shows as the mouth. Faces +Z (PlayerMovement turns the whole object).
    /// </summary>
    public class PacManSolid : MonoBehaviour
    {
        public float maxOpen = 42f;     // degrees per half
        public float minOpen = 3f;
        public float idleOpen = 28f;
        public float chompSpeed = 10f;

        const float Seam = 1f * Mathf.Deg2Rad;

        Transform left, right;
        float phase;

        public static PacManSolid Create(Transform body, float radius)
        {
            var model = body.gameObject.AddComponent<PacManSolid>();
            var root = new GameObject("PacManMesh").transform;
            root.SetParent(body, false);
            root.localScale = Vector3.one * radius;

            var yellow = Visuals.Lit(Visuals.NeonYellow, 0f, 0.8f, Visuals.NeonYellow, 0.55f);
            yellow.SetFloat("_Cull", 0f); // both sides, so the cut edges never look hollow

            model.right = Half("Right", root, true, yellow);
            model.left = Half("Left", root, false, yellow);

            var mouth = Visuals.Lit(Visuals.Hex("#2A0710"), 0f, 0.3f);
            var inside = Visuals.Shape(PrimitiveType.Sphere, "Mouth", root, Vector3.zero, Vector3.one * 1.9f, mouth);
            inside.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return model;
        }

        static Transform Half(string name, Transform parent, bool isRight, Material mat)
        {
            var half = new GameObject(name).transform;
            half.SetParent(parent, false);
            float p0 = isRight ? Mathf.PI / 2f + Seam : 3f * Mathf.PI / 2f;
            float len = Mathf.PI - Seam;
            var mb = new MeshBuilder();
            mb.SpherePart(1f, p0, len, 0f, Mathf.PI, 40, 28);
            Visuals.MeshPart("Shell", half, mb.ToMesh(name + "_PacMan"), mat, true);
            return half;
        }

        void Update()
        {
            var player = PlayerMovement.Instance;
            bool moving = player != null && player.IsMoving;
            float open;
            if (moving)
            {
                phase += Time.deltaTime * chompSpeed;
                open = Mathf.Lerp(minOpen, maxOpen, (Mathf.Sin(phase) + 1f) * 0.5f);
            }
            else
            {
                open = idleOpen;
            }
            right.localRotation = Quaternion.Euler(0f, open, 0f);
            left.localRotation = Quaternion.Euler(0f, -open, 0f);
        }
    }
}
