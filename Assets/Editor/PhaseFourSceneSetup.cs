using System;
using System.Linq;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class PhaseFourSceneSetup
    {
        private const string DominoPath = "Assets/Prefabs/Domino/Domino.prefab";

        [MenuItem("Karakuri Labo/Set Up Phase 4 Dominoes")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity" ||
                Object.FindAnyObjectByType<SimulationHUD>() == null)
            {
                throw new InvalidOperationException("Open the Phase 3 Main scene in Edit Mode first.");
            }
            if (GameObject.Find("Dominos") != null)
            {
                Debug.Log("Domino setup is already present; the edited layout was preserved.");
                return;
            }

            GameObject prefab = CreateDominoPrefab();
            Transform ramp = GameObject.Find("Ramp").transform;
            ramp.SetPositionAndRotation(new Vector3(-3f, 1.5f, 0), Quaternion.Euler(0, 0, -16));
            foreach (string name in new[] { "Rolling surface", "Back rail", "Front rail" })
            {
                Transform piece = ramp.Find(name);
                Vector3 scale = piece.localScale;
                scale.x = 5f;
                piece.localScale = scale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
            }
            LineRenderer selection = ramp.Find("SelectionOutline").GetComponent<LineRenderer>();
            selection.SetPositions(new[]
            {
                new Vector3(-2.56f, 0.36f, -0.92f), new Vector3(2.56f, 0.36f, -0.92f),
                new Vector3(2.56f, 0.36f, 0.92f), new Vector3(-2.56f, 0.36f, 0.92f)
            });
            PrefabUtility.RecordPrefabInstancePropertyModifications(selection);
            Transform[] supports = ramp.Cast<Transform>().Where(child => child.name == "Ramp support").ToArray();
            for (int i = 0; i < supports.Length; i++)
            {
                Vector3 top = ramp.TransformPoint(new Vector3(i == 0 ? -1.7f : 1.7f, -0.12f, 0));
                supports[i].SetPositionAndRotation(new Vector3(top.x, top.y * 0.5f, 0), Quaternion.identity);
                supports[i].localScale = new Vector3(0.3f, top.y, 1.3f);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(ramp);
            Transform ball = GameObject.Find("Ball").transform;
            ball.position = ramp.TransformPoint(new Vector3(-2f, 0.47f, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball);
            Transform goal = GameObject.Find("Goal").transform;
            goal.position = new Vector3(3.65f, -0.18f, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal);

            Transform group = new GameObject("Dominos").transform;
            for (int i = 0; i < 4; i++)
            {
                var domino = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                domino.name = $"Domino {i + 1}";
                domino.transform.position = new Vector3(i * 0.58f, 0.455f, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(domino.transform);
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            manager.ConfigureResetObjects(new[]
            {
                ball.GetComponent<PhysicsObject>(), ramp.GetComponent<PhysicsObject>(),
                goal.GetComponent<PhysicsObject>()
            }.Concat(group.GetComponentsInChildren<PhysicsObject>()).ToArray());
            EditorUtility.SetDirty(manager);
            Text stageLabel = GameObject.Find("HUD/SafeArea/StageLabel").GetComponent<Text>();
            stageLabel.text = "01   /   DOMINO CHAIN";
            EditorUtility.SetDirty(stageLabel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 4 ready: move individual dominoes, PLAY the chain, and RESET to restore the layout.");
        }

        private static GameObject CreateDominoPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DominoPath) != null)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(DominoPath);
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Domino"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Domino");
            }
            Material amber = Material("Domino_Amber", "EAB866");
            Material ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ramp_Support.mat");
            Material selected = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Selection_Gold.mat");
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/Domino.physicMaterial");
            if (friction == null)
            {
                friction = new PhysicsMaterial("Domino")
                {
                    staticFriction = 0.55f, dynamicFriction = 0.45f, bounciness = 0,
                    frictionCombine = PhysicsMaterialCombine.Average,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                AssetDatabase.CreateAsset(friction, "Assets/Materials/Domino.physicMaterial");
            }

            GameObject root = new GameObject("Domino");
            try
            {
                Visual(root.transform, "Block", PrimitiveType.Cube, Vector3.zero,
                    new Vector3(0.16f, 0.9f, 0.9f), amber);
                foreach (float side in new[] { -1f, 1f })
                {
                    Visual(root.transform, "Divider", PrimitiveType.Cube, new Vector3(side * 0.082f, 0, 0),
                        new Vector3(0.008f, 0.018f, 0.7f), ink);
                    foreach (float y in new[] { -0.22f, 0.22f })
                    {
                        foreach (float z in new[] { -0.2f, 0.2f })
                        {
                            Visual(root.transform, "Pip", PrimitiveType.Sphere, new Vector3(side * 0.083f, y, z),
                                new Vector3(0.012f, 0.09f, 0.09f), ink);
                        }
                    }
                }
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.16f, 0.9f, 0.9f);
                collider.sharedMaterial = friction;
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 0.04f;
                body.isKinematic = true;
                body.useGravity = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.solverIterations = 12;
                body.solverVelocityIterations = 4;
                root.AddComponent<PhysicsObject>().ConfigureSimulation(true);

                GameObject outline = new GameObject("SelectionOutline");
                outline.transform.SetParent(root.transform, false);
                LineRenderer line = outline.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.widthMultiplier = 0.025f;
                line.sharedMaterial = selected;
                Vector3 a = new Vector3(-0.10f, -0.46f, -0.47f);
                Vector3 b = new Vector3(0.10f, -0.46f, -0.47f);
                Vector3 c = new Vector3(0.10f, -0.46f, 0.47f);
                Vector3 d = new Vector3(-0.10f, -0.46f, 0.47f);
                Vector3 up = Vector3.up * 0.92f;
                Vector3[] points = { a, b, c, d, a, a + up, b + up, b, b + up, c + up, c,
                    c + up, d + up, d, d + up, a + up };
                line.positionCount = points.Length;
                line.SetPositions(points);
                root.AddComponent<DraggableObject>().Configure(outline, "DOMINO");
                PrefabUtility.SaveAsPrefabAsset(root, DominoPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(DominoPath);
        }

        private static void Visual(Transform parent, string name, PrimitiveType type,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material Material(string name, string hex)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
