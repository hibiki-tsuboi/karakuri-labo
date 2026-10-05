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
    public static class PhaseFiveSceneSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Seesaw/Seesaw.prefab";

        [MenuItem("Karakuri Labo/Set Up Phase 5 Seesaw")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity" ||
                GameObject.Find("Dominos") == null)
            {
                throw new InvalidOperationException("Open the Phase 4 Main scene in Edit Mode first.");
            }
            if (GameObject.Find("Seesaw") != null)
            {
                Debug.Log("Seesaw setup is already present; the edited layout was preserved.");
                return;
            }

            var seesaw = (GameObject)PrefabUtility.InstantiatePrefab(CreatePrefab());
            seesaw.name = "Seesaw";
            seesaw.transform.position = new Vector3(-1.3f, 0, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(seesaw.transform);

            Transform ramp = GameObject.Find("Ramp").transform;
            ramp.position = new Vector3(-4.15f, 1.5f, 0);
            foreach (string name in new[] { "Rolling surface", "Back rail", "Front rail" })
            {
                Transform part = ramp.Find(name);
                Vector3 scale = part.localScale;
                scale.x = 3.4f;
                part.localScale = scale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }
            LineRenderer outline = ramp.Find("SelectionOutline").GetComponent<LineRenderer>();
            outline.SetPositions(new[]
            {
                new Vector3(-1.76f, 0.36f, -0.92f), new Vector3(1.76f, 0.36f, -0.92f),
                new Vector3(1.76f, 0.36f, 0.92f), new Vector3(-1.76f, 0.36f, 0.92f)
            });
            PrefabUtility.RecordPrefabInstancePropertyModifications(outline);
            int supportIndex = 0;
            foreach (Transform support in ramp.Cast<Transform>().Where(child => child.name == "Ramp support"))
            {
                Vector3 top = ramp.TransformPoint(new Vector3(supportIndex++ == 0 ? -1.1f : 1.1f, -0.12f, 0));
                support.SetPositionAndRotation(new Vector3(top.x, top.y * 0.5f, 0), Quaternion.identity);
                support.localScale = new Vector3(0.3f, top.y, 1.3f);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(ramp);
            Transform ball = GameObject.Find("Ball").transform;
            ball.position = ramp.TransformPoint(new Vector3(-1.2f, 0.47f, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball);

            int dominoIndex = 0;
            foreach (Transform domino in GameObject.Find("Dominos").transform)
            {
                domino.position = new Vector3(0.15f + dominoIndex++ * 0.58f, 0.455f, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(domino);
            }

            // Restore the movable assembly's fixed root before its dynamic board.
            var parts = new[]
            {
                ball.GetComponent<PhysicsObject>(), ramp.GetComponent<PhysicsObject>(),
                GameObject.Find("Goal").GetComponent<PhysicsObject>(),
                seesaw.GetComponent<PhysicsObject>(),
                seesaw.transform.Find("Board").GetComponent<PhysicsObject>()
            }.Concat(GameObject.Find("Dominos").GetComponentsInChildren<PhysicsObject>()).ToArray();
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            manager.ConfigureResetObjects(parts);
            EditorUtility.SetDirty(manager);
            Text label = GameObject.Find("HUD/SafeArea/StageLabel").GetComponent<Text>();
            label.text = "01   /   BALANCE & CHAIN";
            EditorUtility.SetDirty(label);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 5 ready: the ball tips a hinged seesaw before the domino chain. RESET restores the whole assembly.");
        }

        private static GameObject CreatePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                return existing;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Seesaw"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Seesaw");
            }
            Material violet = Material("Seesaw_Violet", "AC8BD4");
            Material dark = Material("Seesaw_Base", "69568F");
            Material stripe = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ball_Stripe.mat");
            Material selected = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Selection_Gold.mat");
            PhysicsMaterial rolling = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/Rolling.physicMaterial");
            GameObject root = new GameObject("Seesaw");
            try
            {
                Cube(root.transform, "Foot", new Vector3(0, 0.08f, 0), new Vector3(0.8f, 0.16f, 1.4f), dark, true);
                foreach (float z in new[] { -0.58f, 0.58f })
                {
                    Cube(root.transform, "Pivot support", new Vector3(0, 0.35f, z), new Vector3(0.28f, 0.55f, 0.16f), dark, true);
                }
                GameObject axle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                axle.name = "Axle";
                Object.DestroyImmediate(axle.GetComponent<Collider>());
                axle.transform.SetParent(root.transform, false);
                axle.transform.localPosition = new Vector3(0, 0.65f, 0);
                axle.transform.localRotation = Quaternion.Euler(90, 0, 0);
                axle.transform.localScale = new Vector3(0.18f, 0.73f, 0.18f);
                axle.GetComponent<Renderer>().sharedMaterial = stripe;

                Rigidbody pivot = root.AddComponent<Rigidbody>();
                pivot.isKinematic = true;
                pivot.useGravity = false;
                pivot.interpolation = RigidbodyInterpolation.None;
                root.AddComponent<PhysicsObject>();

                GameObject board = new GameObject("Board");
                board.transform.SetParent(root.transform, false);
                board.transform.localPosition = new Vector3(0, 0.65f, 0);
                Cube(board.transform, "Deck", Vector3.zero, new Vector3(2.2f, 0.16f, 1.2f), violet, false);
                Cube(board.transform, "Center mark", new Vector3(0, 0.084f, 0), new Vector3(0.06f, 0.008f, 1.2f), stripe, false);
                BoxCollider collider = board.AddComponent<BoxCollider>();
                collider.size = new Vector3(2.2f, 0.16f, 1.2f);
                collider.sharedMaterial = rolling;
                Rigidbody body = board.AddComponent<Rigidbody>();
                body.mass = 0.6f;
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.angularDamping = 0.15f;
                board.AddComponent<PhysicsObject>().ConfigureSimulation(true);
                HingeJoint hinge = board.AddComponent<HingeJoint>();
                hinge.connectedBody = pivot;
                hinge.autoConfigureConnectedAnchor = false;
                hinge.anchor = Vector3.zero;
                hinge.connectedAnchor = new Vector3(0, 0.65f, 0);
                hinge.axis = Vector3.forward;
                hinge.useLimits = true;
                hinge.limits = new JointLimits { min = -18, max = 18, bounciness = 0, contactDistance = 1 };
                hinge.enableCollision = false;

                GameObject selection = new GameObject("SelectionOutline");
                selection.transform.SetParent(board.transform, false);
                LineRenderer line = selection.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.widthMultiplier = 0.04f;
                line.sharedMaterial = selected;
                line.positionCount = 4;
                line.SetPositions(new[]
                {
                    new Vector3(-1.14f, 0.12f, -0.64f), new Vector3(1.14f, 0.12f, -0.64f),
                    new Vector3(1.14f, 0.12f, 0.64f), new Vector3(-1.14f, 0.12f, 0.64f)
                });
                root.AddComponent<DraggableObject>().Configure(selection, "SEESAW");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void Cube(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material, bool collider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }
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
