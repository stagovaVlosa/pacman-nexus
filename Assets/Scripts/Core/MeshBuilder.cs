using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus
{
    /// <summary>
    /// Collects vertices and triangles from simple shapes into one mesh.
    /// Used to build the procedural robots (Pac-Man, enemy drones) from code.
    /// </summary>
    public class MeshBuilder
    {
        // phi: 0 = -X, PI/2 = +Z (front), PI = +X (right), 3PI/2 = -Z (back). theta: 0 = top pole.
        public static Vector3 DirAt(float phi) => new Vector3(-Mathf.Cos(phi), 0f, Mathf.Sin(phi));
        public static Vector3 SpherePoint(float phi, float theta) =>
            new Vector3(-Mathf.Cos(phi) * Mathf.Sin(theta), Mathf.Cos(theta), Mathf.Sin(phi) * Mathf.Sin(theta));

        /// <summary>Closed loop of points: a circle in the XY plane, rotated and moved.</summary>
        public static List<Vector3> Circle(float r, int seg, Vector3 center, Quaternion rot)
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                pts.Add(center + rot * new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }
            return pts;
        }

        /// <summary>Rotation that lays a Circle flat in the XZ plane.</summary>
        public static readonly Quaternion Flat = Quaternion.Euler(90f, 0f, 0f);

        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> tris = new List<int>();

        int Add(Vector3 p, Vector3 normal, Vector2 texcoord)
        {
            v.Add(p); n.Add(normal); uv.Add(texcoord);
            return v.Count - 1;
        }

        /// <summary>Adds a triangle wound so its visible side faces the same way as its vertex normals.</summary>
        void Tri(int a, int b, int c)
        {
            var fn = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (fn.sqrMagnitude < 1e-16f) return; // degenerate (poles, cone tips)
            if (Vector3.Dot(fn, n[a] + n[b] + n[c]) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        /// <summary>Parametric surface: f(u, w) returns position and normal for u, w in 0..1.</summary>
        void Grid(int cols, int rows, System.Func<float, float, (Vector3, Vector3)> f)
        {
            int start = v.Count;
            for (int j = 0; j <= rows; j++)
                for (int i = 0; i <= cols; i++)
                {
                    float u = (float)i / cols, w = (float)j / rows;
                    var (p, normal) = f(u, w);
                    Add(p, normal, new Vector2(u, w));
                }
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    int a = start + j * (cols + 1) + i, b = a + 1, c = a + cols + 1, d = c + 1;
                    Tri(a, c, b);
                    Tri(b, c, d);
                }
        }

        public void SpherePart(float r, float phi0, float phiLen, float th0, float thLen, int cols, int rows)
        {
            Grid(cols, rows, (u, w) =>
            {
                var d = SpherePoint(phi0 + u * phiLen, th0 + w * thLen);
                return (d * r, d);
            });
        }

        /// <summary>Open vertical band (part of a cylinder wall) centered at height y.</summary>
        public void Band(float r, float h, float y, float phi0, float phiLen, int cols)
        {
            Grid(cols, 1, (u, w) =>
            {
                var d = DirAt(phi0 + u * phiLen);
                return (d * r + Vector3.up * (y + h / 2f - w * h), d);
            });
        }

        /// <summary>Vertical cylinder/cone centered at height y, optionally closed with flat caps.</summary>
        public void Frustum(float rTop, float rBot, float h, float y, int sides, bool caps)
        {
            float slope = (rBot - rTop) / h;
            Grid(sides, 1, (u, w) =>
            {
                float a = u * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                return (d * Mathf.Lerp(rTop, rBot, w) + Vector3.up * (y + h / 2f - w * h), (d + Vector3.up * slope).normalized);
            });
            if (!caps) return;
            Disc(new Vector3(0f, y + h / 2f, 0f), rTop, Vector3.up, sides);
            Disc(new Vector3(0f, y - h / 2f, 0f), rBot, Vector3.down, sides);
        }

        /// <summary>Flat horizontal disc facing up or down.</summary>
        public void Disc(Vector3 center, float r, Vector3 normal, int sides)
        {
            int c = Add(center, normal, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                Add(center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r, normal,
                    new Vector2(0.5f + Mathf.Sin(a) * 0.5f, 0.5f + Mathf.Cos(a) * 0.5f));
            }
            for (int i = 0; i < sides; i++) Tri(c, c + 1 + i, c + 2 + i);
        }

        /// <summary>Round tube along a path (open or closed loop).</summary>
        public void Tube(List<Vector3> path, float radius, int sides, bool closed)
        {
            var pts = new List<Vector3>(path);
            if (closed) pts.Add(path[0]);
            int count = pts.Count;
            var tang = new Vector3[count];
            var nrm = new Vector3[count];
            for (int k = 0; k < count; k++)
            {
                var prev = k > 0 ? pts[k - 1] : (closed ? path[path.Count - 1] : pts[k]);
                var next = k < count - 1 ? pts[k + 1] : (closed ? path[1] : pts[k]);
                tang[k] = (next - prev).normalized;
            }
            // Parallel transport keeps the tube from twisting
            nrm[0] = Vector3.Cross(tang[0], Mathf.Abs(tang[0].y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            for (int k = 1; k < count; k++)
                nrm[k] = (nrm[k - 1] - tang[k] * Vector3.Dot(nrm[k - 1], tang[k])).normalized;

            Grid(sides, count - 1, (u, w) =>
            {
                int k = Mathf.RoundToInt(w * (count - 1));
                float a = u * Mathf.PI * 2f;
                var d = nrm[k] * Mathf.Cos(a) + Vector3.Cross(tang[k], nrm[k]) * Mathf.Sin(a);
                return (pts[k] + d * radius, d);
            });
        }

        public void Box(Vector3 center, Vector3 size, Quaternion rot)
        {
            var axes = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var ax in axes)
            {
                var t1 = Vector3.Cross(ax, Mathf.Abs(ax.y) < 0.9f ? Vector3.up : Vector3.right);
                var t2 = Vector3.Cross(ax, t1);
                var normal = rot * ax;
                int a = Add(center + rot * Vector3.Scale(ax - t1 - t2, size * 0.5f), normal, new Vector2(0, 0));
                int b = Add(center + rot * Vector3.Scale(ax + t1 - t2, size * 0.5f), normal, new Vector2(1, 0));
                int c = Add(center + rot * Vector3.Scale(ax + t1 + t2, size * 0.5f), normal, new Vector2(1, 1));
                int d = Add(center + rot * Vector3.Scale(ax - t1 + t2, size * 0.5f), normal, new Vector2(0, 1));
                Tri(a, b, c);
                Tri(a, c, d);
            }
        }

        /// <summary>Icosphere. flat = faceted (each face its own normal), else smooth.</summary>
        public void Icosahedron(Vector3 center, float r, int detail, bool flat)
        {
            foreach (var f in IcoFaces(detail))
            {
                if (flat)
                {
                    var fn = (f[0] + f[1] + f[2]).normalized;
                    Tri(Add(center + f[0] * r, fn, Vector2.zero), Add(center + f[1] * r, fn, Vector2.zero),
                        Add(center + f[2] * r, fn, Vector2.zero));
                }
                else
                {
                    Tri(Add(center + f[0] * r, f[0], Vector2.zero), Add(center + f[1] * r, f[1], Vector2.zero),
                        Add(center + f[2] * r, f[2], Vector2.zero));
                }
            }
        }

        public static List<Vector3[]> IcoFaces(int detail)
        {
            float g = (1f + Mathf.Sqrt(5f)) / 2f;
            var p = new[]
            {
                new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1),
            };
            int[] idx =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var faces = new List<Vector3[]>();
            for (int i = 0; i < idx.Length; i += 3)
                faces.Add(new[] { p[idx[i]].normalized, p[idx[i + 1]].normalized, p[idx[i + 2]].normalized });
            for (int d = 0; d < detail; d++)
            {
                var next = new List<Vector3[]>();
                foreach (var f in faces)
                {
                    var ab = (f[0] + f[1]).normalized;
                    var bc = (f[1] + f[2]).normalized;
                    var ca = (f[2] + f[0]).normalized;
                    next.Add(new[] { f[0], ab, ca });
                    next.Add(new[] { f[1], bc, ab });
                    next.Add(new[] { f[2], ca, bc });
                    next.Add(new[] { ab, bc, ca });
                }
                faces = next;
            }
            return faces;
        }

        /// <summary>The 30 edges of an icosahedron with radius r (for the wireframe cage).</summary>
        public static List<(Vector3, Vector3)> IcoEdges(float r)
        {
            var edges = new List<(Vector3, Vector3)>();
            foreach (var f in IcoFaces(0))
                for (int i = 0; i < 3; i++)
                {
                    Vector3 a = f[i] * r, b = f[(i + 1) % 3] * r;
                    bool seen = false;
                    foreach (var (x, y) in edges)
                        if ((x == a && y == b) || (x == b && y == a)) { seen = true; break; }
                    if (!seen) edges.Add((a, b));
                }
            return edges;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
