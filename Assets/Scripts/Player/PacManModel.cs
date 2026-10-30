using System.Collections.Generic;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Pac-Man made of two half-spheres. The halves open and close like a mouth while moving.
    /// The mouth faces the object's forward (+Z) direction.
    /// </summary>
    public class PacManModel : MonoBehaviour
    {
        public float maxOpen = 40f;
        public float idleOpen = 8f;
        public float chompSpeed = 14f;

        Transform upper, lower;
        float phase, current;

        public static PacManModel Create(Transform body, float radius)
        {
            var yellow = Visuals.GetMaterial(Visuals.NeonYellow, Visuals.NeonYellow, 1.2f);
            var mouth = Visuals.GetMaterial(Visuals.Hex("#2A0E00"), Color.black, 0f);

            var model = body.gameObject.AddComponent<PacManModel>();
            model.upper = MakeHalf("Upper", body, radius, true, yellow, mouth);
            model.lower = MakeHalf("Lower", body, radius, false, yellow, mouth);

            var eyeMat = Visuals.GetMaterial(Color.black, Color.black, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                var dir = new Vector3(0.32f * side, 0.5f, 0.8f).normalized;
                Visuals.Shape(PrimitiveType.Sphere, "Eye", model.upper, dir * radius * 0.93f,
                    Vector3.one * radius * 0.26f, eyeMat);
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
            // Negative X rotation lifts the front edge, positive lowers it
            upper.localRotation = Quaternion.Euler(-current, 0f, 0f);
            lower.localRotation = Quaternion.Euler(current, 0f, 0f);
        }

        static Transform MakeHalf(string name, Transform parent, float r, bool isUpper, Material skin, Material inside)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Hemisphere(r, isUpper);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { skin, inside };
            return go.transform;
        }

        /// <summary>Half sphere (submesh 0) closed with a flat disc (submesh 1, the inside of the mouth).</summary>
        static Mesh Hemisphere(float r, bool isUpper, int seg = 32, int rings = 10)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var shell = new List<int>();
            var cap = new List<int>();
            float s = isUpper ? 1f : -1f;

            for (int i = 0; i <= rings; i++)
            {
                float phi = (float)i / rings * Mathf.PI * 0.5f;
                for (int j = 0; j <= seg; j++)
                {
                    float th = (float)j / seg * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(phi) * Mathf.Cos(th), s * Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(th));
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

            var capN = new Vector3(0f, -s, 0f);
            int start = verts.Count;
            verts.Add(Vector3.zero);
            norms.Add(capN);
            for (int j = 0; j <= seg; j++)
            {
                float th = (float)j / seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(th) * r, 0f, Mathf.Sin(th) * r));
                norms.Add(capN);
            }
            for (int j = 0; j < seg; j++) AddTri(verts, cap, start, start + 1 + j, start + 2 + j, capN);

            var mesh = new Mesh { name = isUpper ? "PacMan_Upper" : "PacMan_Lower" };
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
