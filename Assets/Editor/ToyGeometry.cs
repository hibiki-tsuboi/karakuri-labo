using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KarakuriLabo.Editor
{
    internal static class ToyGeometry
    {
        internal static Mesh Funnel()
        {
            Vector2[] profile = { new Vector2(0.48f, -0.65f), new Vector2(0.48f, 0.1f),
                new Vector2(1.6f, 1.4f), new Vector2(1.72f, 1.4f),
                new Vector2(0.60f, 0.1f), new Vector2(0.60f, -0.65f) };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int steps = 64;
            for (int ring = 0; ring < profile.Length; ring++)
            {
                for (int step = 0; step < steps; step++)
                {
                    float angle = step * Mathf.PI * 2 / steps;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y,
                        Mathf.Sin(angle) * profile[ring].x));
                }
            }
            for (int ring = 0; ring < profile.Length; ring++)
            {
                int next = (ring + 1) % profile.Length;
                for (int step = 0; step < steps; step++)
                {
                    int following = (step + 1) % steps;
                    Quad(triangles, ring * steps + step, ring * steps + following,
                        next * steps + following, next * steps + step);
                }
            }
            return Save("ToyFunnel", vertices, triangles);
        }

        internal static Mesh Curve(string name, float inner, float outer, float bottom, float top)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int steps = 40;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float theta = t * Mathf.PI * 0.5f;
                foreach (Vector2 edge in new[] { new Vector2(inner, bottom), new Vector2(outer, bottom),
                    new Vector2(outer, top), new Vector2(inner, top) })
                {
                    vertices.Add(new Vector3(-2 + edge.x * Mathf.Sin(theta), 0.65f - t * 0.55f + edge.y,
                        2 - edge.x * Mathf.Cos(theta)));
                }
            }
            for (int i = 0; i < steps; i++)
            {
                for (int side = 0; side < 4; side++)
                {
                    int next = (side + 1) % 4;
                    Quad(triangles, i * 4 + side, (i + 1) * 4 + side, (i + 1) * 4 + next, i * 4 + next);
                }
            }
            Quad(triangles, 0, 1, 2, 3);
            int end = steps * 4;
            Quad(triangles, end + 3, end + 2, end + 1, end);
            return Save(name, vertices, triangles);
        }

        internal static Mesh Spring()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int steps = 144;
            const int sides = 8;
            for (int i = 0; i <= steps; i++)
            {
                float angle = (float)i / steps * Mathf.PI * 8;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 center = radial * 0.50f + Vector3.up * (-0.22f + (float)i / steps * 0.49f);
                for (int j = 0; j < sides; j++)
                {
                    float cross = j * Mathf.PI * 2 / sides;
                    vertices.Add(center + 0.026f * (radial * Mathf.Cos(cross) + Vector3.up * Mathf.Sin(cross)));
                }
            }
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int next = (j + 1) % sides;
                    Quad(triangles, i * sides + j, i * sides + next, (i + 1) * sides + next, (i + 1) * sides + j);
                }
            }
            return Save("ToySpringCoil", vertices, triangles);
        }

        private static void Quad(List<int> indices, int a, int b, int c, int d)
        {
            indices.AddRange(new[] { a, b, c, a, c, d });
        }

        private static Mesh Save(string name, List<Vector3> vertices, List<int> triangles)
        {
            string path = $"Assets/Art/Meshes/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create = mesh == null;
            if (create) mesh = new Mesh { name = name };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (create) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
