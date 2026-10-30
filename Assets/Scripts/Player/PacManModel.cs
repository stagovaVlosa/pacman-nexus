using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Pac-Man made of two half-spheres split left/right. The halves swing open around the
    /// vertical (Y) axis like a mouth while moving, so the mouth is visible from the top-down camera.
    /// The mouth faces the object's forward (+Z) direction.
    /// </summary>
    public class PacManModel : MonoBehaviour
    {
        public float maxOpen = 38f;   // degrees per half
        public float idleOpen = 8f;
        public float chompSpeed = 14f;

        Transform left, right;
        float phase, current;

        public static PacManModel Create(Transform body, float radius)
        {
            // Saturated yellow with an orange-leaning glow: plain NeonYellow at high intensity
            // tonemaps to pale cream, this stays neon yellow under bloom
            var yellow = Visuals.GetMaterial(Visuals.Hex("#FFCC00"), Visuals.Hex("#FF9E00"), 1.8f);
            var mouth = Visuals.GetMaterial(Visuals.Hex("#2A0E00"), Color.black, 0f);

            var model = body.gameObject.AddComponent<PacManModel>();
            model.right = MakeHalf("Right", body, radius, true, yellow, mouth);
            model.left = MakeHalf("Left", body, radius, false, yellow, mouth);

            // One eye on top of each half, so the eyes move with the jaws
            var eyeMat = Visuals.GetMaterial(Color.black, Color.black, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                var dir = new Vector3(0.32f * side, 0.8f, 0.5f).normalized;
                Visuals.Shape(PrimitiveType.Sphere, "Eye", side > 0f ? model.right : model.left,
                    dir * radius * 0.93f, Vector3.one * radius * 0.26f, eyeMat);
            }
            return model;
        }

        void Update()
        {
            bool moving = PlayerMovement.Instance != null && PlayerMovement.Instance.IsMoving;
            if (moving)
            {
                phase += Time.deltaTime * chompSpeed;
                current = Mathf.Abs(Mathf.Sin(phase)) * maxOpen;
            }
            else
            {
                current = Mathf.MoveTowards(current, idleOpen, 120f * Time.deltaTime);
            }
            // Positive Y rotation swings the front edge toward +X (right), negative toward -X (left)
            right.localRotation = Quaternion.Euler(0f, current, 0f);
            left.localRotation = Quaternion.Euler(0f, -current, 0f);
        }

        static Transform MakeHalf(string name, Transform parent, float r, bool isRight, Material skin, Material inside)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Hemisphere(r, isRight);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { skin, inside };
            return go.transform;
        }

        /// <summary>
        /// Half sphere on the +X (right) or -X (left) side (submesh 0),
        /// closed with a flat vertical disc (submesh 1, the inside of the mouth).
        /// </summary>
        static Mesh Hemisphere(float r, bool isRight, int seg = 32, int rings = 10)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var shell = new List<int>();
            var cap = new List<int>();
            float s = isRight ? 1f : -1f;

            for (int i = 0; i <= rings; i++)
            {
                float phi = (float)i / rings * Mathf.PI * 0.5f;
                for (int j = 0; j <= seg; j++)
                {
                    float th = (float)j / seg * Mathf.PI * 2f;
                    var n = new Vector3(s * Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Cos(th), Mathf.Cos(phi) * Mathf.Sin(th));
                    verts.Add(n * r);
                    norms.Add(n);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < seg; j++)
                {
                    int a = i * (seg + 1) + j, b = a + 1, c = a + seg + 1, d = c + 1;
                    AddTri(verts, shell, a, c, b, ((verts[a] + verts[b] + verts[c]) / 3f).normalized);
                    AddTri(verts, shell, b, c, d, ((verts[b] + verts[c] + verts[d]) / 3f).normalized);
                }

            var capN = new Vector3(-s, 0f, 0f);
            int start = verts.Count;
            verts.Add(Vector3.zero);
            norms.Add(capN);
            for (int j = 0; j <= seg; j++)
            {
                float th = (float)j / seg * Mathf.PI * 2f;
                verts.Add(new Vector3(0f, Mathf.Cos(th) * r, Mathf.Sin(th) * r));
                norms.Add(capN);
            }
            for (int j = 0; j < seg; j++) AddTri(verts, cap, start, start + 1 + j, start + 2 + j, capN);

            var mesh = new Mesh { name = isRight ? "PacMan_Right" : "PacMan_Left" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(shell, 0);
            mesh.SetTriangles(cap, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Adds a triangle wound so its visible side points along 'want'.</summary>
        static void AddTri(List<Vector3> v, List<int> tris, int a, int b, int c, Vector3 want)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, want) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }
    }
}
