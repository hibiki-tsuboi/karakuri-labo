using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace KarakuriLabo.Editor
{
    /// <summary>Small reusable display meshes; physics keeps its original primitive colliders.</summary>
    internal static class AtelierGeometry
    {
        internal const string MeshFolder = "Assets/Art/Meshes";

        internal static Mesh Box(Vector3 size, float planRadius = 0)
        {
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            string key = string.Format(CultureInfo.InvariantCulture, "AtelierBox_{0:F3}_{1:F3}_{2:F3}", size.x, size.y, size.z);
            if (planRadius > 0)
            {
                key += string.Format(CultureInfo.InvariantCulture, "_R{0:F3}", planRadius);
            }
            string path = $"{MeshFolder}/{key}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }
            float radius = Mathf.Min(0.16f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.23f);
            Vector3 half = size * 0.5f;
            Vector3 radii = new Vector3(planRadius > 0 ? Mathf.Min(planRadius, size.x * 0.4f) : radius,
                radius, planRadius > 0 ? Mathf.Min(planRadius, size.z * 0.4f) : radius);
            Vector3 inner = half - radii;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            // Face-local axes have an outward cross product. Extra samples live at
            // the bevel, keeping the broad planar surfaces perfectly flat.
            Vector3[] normalAxes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] uAxes = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] vAxes = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
            for (int face = 0; face < 6; face++)
            {
                Vector3 n = normalAxes[face];
                Vector3 u = uAxes[face];
                Vector3 v = vAxes[face];
                float[] us = Coordinates(Vector3.Scale(u, half).magnitude, Vector3.Scale(u, radii).magnitude);
                float[] vs = Coordinates(Vector3.Scale(v, half).magnitude, Vector3.Scale(v, radii).magnitude);
                int start = vertices.Count;
                for (int y = 0; y < vs.Length; y++)
                {
                    for (int x = 0; x < us.Length; x++)
                    {
                        Vector3 cube = Vector3.Scale(n, half) + u * us[x] + v * vs[y];
                        Vector3 closest = new Vector3(Mathf.Clamp(cube.x, -inner.x, inner.x),
                            Mathf.Clamp(cube.y, -inner.y, inner.y), Mathf.Clamp(cube.z, -inner.z, inner.z));
                        Vector3 delta = cube - closest;
                        Vector3 unit = new Vector3(delta.x / radii.x, delta.y / radii.y, delta.z / radii.z).normalized;
                        Vector3 outward = new Vector3(unit.x / radii.x, unit.y / radii.y, unit.z / radii.z).normalized;
                        Vector3 point = closest + Vector3.Scale(unit, radii);
                        vertices.Add(new Vector3(point.x / size.x, point.y / size.y, point.z / size.z));
                        // Unity's inverse-transpose restores this world-space normal
                        // when the unit-bounds mesh uses a non-uniform object scale.
                        normals.Add(Vector3.Scale(outward, size).normalized);
                        uv.Add(new Vector2(us[x], vs[y]));
                    }
                }
                for (int y = 0; y < vs.Length - 1; y++)
                {
                    for (int x = 0; x < us.Length - 1; x++)
                    {
                        int a = start + y * us.Length + x;
                        triangles.AddRange(new[] { a, a + 1, a + us.Length + 1, a, a + us.Length + 1, a + us.Length });
                    }
                }
            }
            var mesh = new Mesh { name = key };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static float[] Coordinates(float half, float radius) => new[]
        {
            -half, -half + radius * 0.12f, -half + radius * 0.42f, -half + radius,
            half - radius, half - radius * 0.42f, half - radius * 0.12f, half
        };

        internal static Mesh SphereBand(string name, float minimum, float maximum, float radius, int rows)
        {
            string path = $"{MeshFolder}/{name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }
            const int segments = 64;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int y = 0; y <= rows; y++)
            {
                float latitude = Mathf.Lerp(minimum, maximum, (float)y / rows);
                for (int x = 0; x <= segments; x++)
                {
                    float angle = (float)x / segments * Mathf.PI * 2;
                    Vector3 normal = new Vector3(Mathf.Cos(latitude) * Mathf.Cos(angle), Mathf.Sin(latitude),
                        Mathf.Cos(latitude) * Mathf.Sin(angle));
                    vertices.Add(normal * radius);
                    normals.Add(normal);
                    uv.Add(new Vector2((float)x / segments, (float)y / rows));
                    if (y < rows && x < segments)
                    {
                        int a = y * (segments + 1) + x;
                        triangles.AddRange(new[] { a, a + segments + 1, a + 1, a + 1, a + segments + 1, a + segments + 2 });
                    }
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        internal static Mesh Ring(float innerRadius, float outerRadius)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "AtelierRing_{0:F3}_{1:F3}", innerRadius, outerRadius);
            string path = $"{MeshFolder}/{name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (int x = 0; x <= 64; x++)
            {
                float angle = x / 64f * Mathf.PI * 2;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices.Add(direction * innerRadius);
                vertices.Add(direction * outerRadius);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                if (x < 64)
                {
                    int a = x * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
