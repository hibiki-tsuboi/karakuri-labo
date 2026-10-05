using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class ToyWorkshopSetup
    {
        public static readonly string[] SceneNames = { "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };
        private static readonly string[] PartNames = { "SpringPad", "CurveRail", "Funnel", "BallLift", "WindFan" };
        private static readonly string[] Labels = { "SPRING", "CURVE", "FUNNEL", "LIFT", "FAN" };
        private static readonly float[] Heights = { 0.2f, 0.55f, 0.55f, 0, 0 };
        private static readonly string[] Hints =
        {
            "BRIDGE THE GAP WITH A SPRING JUMP", "TURN THE CORNER INTO THE BACK LANE",
            "CATCH THE BALL AND DROP IT THROUGH THE FUNNEL", "LIFT THE BALL TO THE UPPER LANDING",
            "POINT THE WIND UP THE SLOPE"
        };

        [MenuItem("Karakuri Labo/Set Up New Toys and Stages")]
        public static void Apply() => Build(false);

        public static void Build(bool rebuildScenes)
        {
            if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Stop Play and save scenes before authoring toys.");
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for (int i = 0; i < PartNames.Length; i++) CreatePrefab(i);
                AssetDatabase.SaveAssets();
                for (int i = 0; i < SceneNames.Length; i++)
                {
                    if (!rebuildScenes && File.Exists($"Assets/Scenes/{SceneNames[i]}.unity")) continue;
                    CreateScene(i);
                }
                CampaignSceneSetup.Apply();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
            Debug.Log("Five toys ready: spring, curve, funnel, lift and fan, with five playable stages.");
        }

        private static void CreatePrefab(int index)
        {
            var root = new GameObject(PartNames[index]);
            root.transform.position = new Vector3(0, Heights[index], 0);
            root.AddComponent<PhysicsObject>();
            var draggable = root.AddComponent<DraggableObject>();
            GameObject selection = Box(root.transform, "SelectionOutline", new Vector3(0, -0.18f, 0),
                new Vector3(index == 1 || index == 2 ? 3.8f : 2.1f, 0.025f, index == 1 || index == 2 ? 3.8f : 2.1f), "Selection_Gold", false);
            if (index == 1) selection.transform.localPosition = new Vector3(-1, -0.15f, 1);
            draggable.Configure(selection, Labels[index]);
            switch (index)
            {
                case 0: BuildSpring(root); break;
                case 1: BuildCurve(root); break;
                case 2: BuildFunnel(root); break;
                case 3: BuildLift(root); break;
                case 4: BuildFan(root); break;
            }
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Prefabs/Placement/{PartNames[index]}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildSpring(GameObject root)
        {
            Box(root.transform, "Oak foot", new Vector3(0, -0.34f, 0), new Vector3(1.7f, 0.18f, 1.7f), "Oak");
            Legs(root.transform, new Vector3(0, -0.43f, 0), 0.64f, 0.64f, -2.8f);
            Transform coil = MeshPart(root.transform, "Brass spring", ToyGeometry.Spring(), "Brass", false).transform;
            Transform plate = Child(root.transform, "Moving plate");
            plate.localPosition = new Vector3(0, 0.34f, 0);
            Box(plate, "Spring deck", Vector3.zero, new Vector3(1.65f, 0.18f, 1.65f), "Seesaw_Violet");
            Box(plate, "Porcelain inset", new Vector3(0, 0.097f, 0), new Vector3(1.35f, 0.016f, 1.35f), "Porcelain", false);
            Arrow(plate, new Vector3(0, 0.11f, 0), "Ball_Coral");
            SpringPad spring = root.AddComponent<SpringPad>();
            spring.Configure(plate, coil);
            Sensor(root.transform, spring, 0, new Vector3(0, 0.46f, 0), new Vector3(1.4f, 0.06f, 1.4f));
        }

        private static void BuildCurve(GameObject root)
        {
            GuideTrack guide = root.AddComponent<GuideTrack>();
            MeshPart(root.transform, "Curved oak deck", ToyGeometry.Curve("ToyCurveDeck", 1.3f, 2.7f, -0.18f, 0), "Oak", true);
            MeshPart(root.transform, "Porcelain lane", ToyGeometry.Curve("ToyCurveLane", 1.44f, 2.56f, 0.002f, 0.012f), "Porcelain", false);
            MeshPart(root.transform, "Inner rail", ToyGeometry.Curve("ToyCurveInner", 1.3f, 1.43f, 0, 0.6f), "Ramp_Blue", true);
            MeshPart(root.transform, "Outer rail", ToyGeometry.Curve("ToyCurveOuter", 2.57f, 2.7f, 0, 0.6f), "Ramp_Blue", true);
            MeshPart(root.transform, "Inner brass trim", ToyGeometry.Curve("ToyCurveInnerTrim", 1.30f, 1.43f, 0.602f, 0.614f), "Brass", false);
            MeshPart(root.transform, "Outer brass trim", ToyGeometry.Curve("ToyCurveOuterTrim", 2.57f, 2.7f, 0.602f, 0.614f), "Brass", false);
            Sensor(root.transform, guide, 0, new Vector3(-1.8f, 0.95f, 0.05f), new Vector3(0.45f, 0.65f, 1.2f));
            Sensor(root.transform, guide, 1, new Vector3(0, 0.42f, 1.8f), new Vector3(1.2f, 0.65f, 0.45f));
            Legs(root.transform, new Vector3(-1.72f, 0.41f, 0.1f), 0.14f, 0.6f, -3.15f);
            Legs(root.transform, new Vector3(0, -0.05f, 1.8f), 0.6f, 0.14f, -3.15f);
        }

        private static void BuildFunnel(GameObject root)
        {
            GuideTrack guide = root.AddComponent<GuideTrack>();
            MeshPart(root.transform, "Porcelain bowl", ToyGeometry.Funnel(), "Porcelain", true);
            var lip = MeshPart(root.transform, "Brass rim", AtelierGeometry.Ring(1.60f, 1.725f), "Brass", false);
            lip.transform.localPosition = new Vector3(0, 1.405f, 0);
            foreach (float x in new[] { -1.35f, 1.35f })
            {
                Box(root.transform, "Oak stand", new Vector3(x, 0.21f, 0), new Vector3(0.16f, 1.95f, 0.3f), "Oak");
                Box(root.transform, "Teal foot", new Vector3(x, -0.76f, 0), new Vector3(0.6f, 0.14f, 1.5f), "Ramp_Blue");
            }
            Sensor(root.transform, guide, 0, new Vector3(0, 1.4f, 0), new Vector3(2.8f, 0.2f, 2.8f));
            Sensor(root.transform, guide, 1, new Vector3(0, -0.55f, 0), new Vector3(0.8f, 0.16f, 0.8f));
        }

        private static void BuildLift(GameObject root)
        {
            Box(root.transform, "Oak plinth", new Vector3(0, -0.18f, 0), new Vector3(2.1f, 0.28f, 2.2f), "Oak");
            Legs(root.transform, new Vector3(0, -0.32f, 0), 0.82f, 0.90f, -2.6f);
            foreach (float z in new[] { -0.98f, 0.98f })
            {
                Box(root.transform, "Guide column", new Vector3(-0.5f, 1.95f, z), new Vector3(0.18f, 4.1f, 0.18f), "Ramp_Support");
                Box(root.transform, "Brass lift screw", new Vector3(0.1f, 1.95f, z), new Vector3(0.075f, 3.95f, 0.075f), "Brass", false);
            }
            Transform carriage = Child(root.transform, "Carriage");
            carriage.localPosition = new Vector3(0, 0.2f, 0);
            carriage.localRotation = Quaternion.Euler(0, 0, -7);
            Rigidbody body = carriage.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            carriage.gameObject.AddComponent<PhysicsObject>();
            Box(carriage, "Moving deck", Vector3.zero, new Vector3(1.9f, 0.22f, 1.7f), "Seesaw_Violet");
            Box(carriage, "Deck inset", new Vector3(0, 0.118f, 0), new Vector3(1.65f, 0.014f, 1.4f), "Porcelain", false);
            foreach (float z in new[] { -0.85f, 0.85f }) Box(carriage, "Side rail", new Vector3(0, 0.35f, z), new Vector3(1.9f, 0.5f, 0.12f), "Ramp_Blue");
            Transform gate = Child(carriage, "Exit gate");
            gate.localPosition = new Vector3(0.9f, 0.1f, 0);
            Box(gate, "Gate panel", new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.6f, 1.6f), "Brass");
            Arrow(carriage, new Vector3(0, 0.13f, 0), "Ramp_Blue");
            BallLift lift = root.AddComponent<BallLift>();
            lift.Configure(body, gate);
            Sensor(carriage, lift, 0, new Vector3(0, 0.45f, 0), new Vector3(1.55f, 0.6f, 1.35f));
        }

        private static void BuildFan(GameObject root)
        {
            Box(root.transform, "Oak base", new Vector3(0, -0.04f, 0), new Vector3(1.3f, 0.24f, 1.5f), "Oak");
            Box(root.transform, "Fan stand", new Vector3(0, 0.38f, 0), new Vector3(0.3f, 0.65f, 0.36f), "Ramp_Blue");
            Transform housing = Child(root.transform, "Housing");
            housing.localPosition = new Vector3(0, 0.85f, 0);
            housing.localRotation = Quaternion.Euler(0, 0, -90);
            GameObject rim = MeshPart(housing, "Brass cage rim", AtelierGeometry.Ring(0.58f, 0.68f), "Brass", false);
            for (int i = 0; i < 8; i++)
            {
                var spoke = Box(housing, "Cage spoke", Vector3.zero, new Vector3(1.3f, 0.025f, 0.02f), "Ramp_Support", false);
                spoke.transform.localRotation = Quaternion.Euler(0, i * 22.5f, 0);
            }
            Transform rotor = Child(root.transform, "Rotor");
            rotor.localPosition = new Vector3(-0.07f, 0.85f, 0);
            for (int i = 0; i < 4; i++)
            {
                Transform blade = Child(rotor, "Blade pivot");
                blade.localRotation = Quaternion.Euler(i * 90, 0, 0);
                Box(blade, "Ivory blade", new Vector3(0, 0.29f, 0), new Vector3(0.09f, 0.48f, 0.20f), "Porcelain", false);
            }
            Box(root.transform, "Fan body", new Vector3(-0.16f, 0.85f, 0), new Vector3(0.42f, 0.38f, 0.38f), "Ramp_Blue");
            var ribbons = new Transform[3];
            for (int i = 0; i < ribbons.Length; i++)
            {
                ribbons[i] = Child(root.transform, "Wind ribbon " + i);
                ribbons[i].localPosition = new Vector3(0.62f, 0.65f + i * 0.19f, (i - 1) * 0.30f);
                Box(ribbons[i], "Ribbon", new Vector3(0.56f, 0, 0), new Vector3(1.12f, 0.035f, 0.085f), "Goal_Mint", false);
            }
            Arrow(root.transform, new Vector3(0, 0.09f, 0), "Porcelain");
            WindFan fan = root.AddComponent<WindFan>();
            fan.Configure(rotor, ribbons);
            Sensor(root.transform, fan, 0, new Vector3(3.75f, 0.8f, 0), new Vector3(6.5f, 2.1f, 2.1f));
        }

        private static void CreateScene(int index)
        {
            string path = $"Assets/Scenes/{SceneNames[index]}.unity";
            Scene source = EditorSceneManager.OpenScene("Assets/Scenes/StageOne.unity");
            if (!EditorSceneManager.SaveScene(source, path, true)) throw new InvalidOperationException("Could not copy the stage template.");
            Scene scene = EditorSceneManager.OpenScene(path);
            Transform stage = GameObject.Find("Stage").transform;
            foreach (Transform child in stage.Cast<Transform>().ToArray())
            {
                if (child.name != "Backdrop") Object.DestroyImmediate(child.gameObject);
            }
            stage.Find("Backdrop").position = new Vector3(0, -3.35f, 0);
            Box(stage, "Workshop foundation", new Vector3(0, -2.95f, 0), new Vector3(13, 0.7f, 10), "Oak");
            Box(stage, "Felt basin", new Vector3(0, -2.59f, 0), new Vector3(12.65f, 0.02f, 9.65f), "Felt", false);
            var manager = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var goal = Object.FindAnyObjectByType<GoalController>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            foreach (Transform child in spawner.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var objective = manager.gameObject.AddComponent<MechanismObjective>();
            objective.Configure(spawner);
            manager.ConfigureObjective(objective);
            Quaternion yaw = Quaternion.Euler(0, 30, 0);
            Vector3 goalPosition;
            Quaternion goalRotation = Quaternion.identity;
            if (index == 0)
            {
                Vector3 pad = new Vector3(-2, 0.2f, 1.3f);
                StartRamp(stage, pad + yaw * new Vector3(-2.25f, 1.35f, 0), 30, ball);
                goalPosition = pad + yaw * new Vector3(5.8f, 1.75f, 0);
                goalRotation = yaw;
            }
            else if (index == 1)
            {
                StartRamp(stage, new Vector3(-3.7f, 1.78f, -1.3f), 0, ball);
                goalPosition = new Vector3(0.1f, -0.05f, 2.55f);
                goalRotation = Quaternion.Euler(0, -90, 0);
            }
            else if (index == 2)
            {
                StartRamp(stage, new Vector3(-2.8f, 3.1f, 0), 0, ball, 0.8f);
                goalPosition = new Vector3(-0.7f, -0.5f, 0);
                goalRotation = Quaternion.Euler(0, 90, 0);
            }
            else if (index == 3)
            {
                StartRamp(stage, new Vector3(-3.25f, 1.18f, 0), 0, ball);
                goalPosition = new Vector3(1.7f, 3.15f, 0);
            }
            else
            {
                Vector3 fan = new Vector3(-4.1f, 0, 1.2f);
                Transform runway = Child(stage, "Wind runway");
                runway.SetPositionAndRotation(fan + yaw * new Vector3(2.1f, -0.05f, 0), yaw);
                Box(runway, "Flat lane", Vector3.zero, new Vector3(3.2f, 0.2f, 1.7f), "Porcelain");
                foreach (float z in new[] { -0.88f, 0.88f }) Box(runway, "Lane rail", new Vector3(0, 0.35f, z), new Vector3(3.2f, 0.55f, 0.12f), "Ramp_Blue");
                FixedRamp(stage, "Uphill ramp", fan + yaw * new Vector3(4.5f, 0.35f, 0), yaw * Quaternion.Euler(0, 0, 10));
                ball.transform.position = fan + yaw * new Vector3(1.7f, 0.43f, 0);
                goalPosition = fan + yaw * new Vector3(7.4f, 0.55f, 0);
                goalRotation = yaw;
                Tower(stage, fan, new Vector3(1.5f, 0, 1.7f));
            }
            goal.transform.SetPositionAndRotation(goalPosition, goalRotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal.transform);
            Tower(stage, goalPosition - new Vector3(0, 0.1f, 0), new Vector3(2.7f, 0, 2.1f), goalRotation);
            Transform hud = GameObject.Find("HUD").transform;
            Transform safe = hud.Find("SafeArea");
            Transform toolbar = safe.Find("PartToolbar");
            manager.Configure(ball, goal, hud.GetComponent<UIManager>());
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(), goal.GetComponent<PhysicsObject>());
            Camera camera = Camera.main;
            Vector3 focus = new Vector3(-0.4f, index == 3 ? 1.5f : 0.8f, 0);
            camera.transform.position = focus + new Vector3(7.5f, 10.5f, -17);
            camera.transform.LookAt(focus);
            camera.orthographicSize = index == 3 ? 6.3f : 6.6f;
            CameraOrbitSceneSetup.ConfigureScene(focus);
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            placement.Configure(manager, camera, new Vector2(-5.5f, -3.5f), new Vector2(5.5f, 3.5f));
            spawner.Configure(manager, placement, new[] { AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Placement/{PartNames[index]}.prefab").GetComponent<DraggableObject>() }, new[] { 1 });
            spawner.ConfigureSpawnPositions(new Vector2(index == 4 ? -3.5f : 1.2f, -2.4f));
            var palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, Array.Empty<Button>());
            Button button = toolbar.Find("AddRampButton").GetComponent<Button>();
            button.name = "AddToyButton";
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = "+ " + Labels[index];
            button.GetComponent<LayoutElement>().preferredWidth = 168;
            palette.Configure(manager, spawner, new[] { button });
            hud.GetComponent<PlacementHUD>().ConfigureHints(Hints[index], index == 2
                ? "FUNNEL  /  PLACE THE WIDE MOUTH UNDER THE BALL" : Labels[index] + "  /  DRAG TO MOVE · ROTATE TO AIM");
            safe.Find("GoalHint").GetComponent<Text>().fontSize = 19;
            safe.Find("StageLabel").GetComponent<Text>().text = "NEW TOY";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Could not save {path}.");
        }

        private static void StartRamp(Transform stage, Vector3 position, float yaw, BallController ball, float supportOffset = 0)
        {
            Quaternion rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -16);
            FixedRamp(stage, "Launch ramp", position, rotation, supportOffset);
            ball.transform.position = position + rotation * new Vector3(-1.2f, 0.47f, 0);
        }

        private static void FixedRamp(Transform stage, string name, Vector3 position, Quaternion rotation, float supportOffset = 0)
        {
            GameObject ramp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Placement/HighBridgeRamp.prefab"));
            PrefabUtility.UnpackPrefabInstance(ramp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            ramp.name = name;
            ramp.transform.SetParent(stage, true);
            ramp.transform.SetPositionAndRotation(position, rotation);
            Object.DestroyImmediate(ramp.GetComponent<DraggableObject>());
            Object.DestroyImmediate(ramp.GetComponent<PhysicsObject>());
            Object.DestroyImmediate(ramp.transform.Find("SelectionOutline").gameObject);
            Quaternion yaw = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            Tower(stage, position - new Vector3(0, 0.8f, 0) - yaw * Vector3.right * supportOffset,
                new Vector3(supportOffset > 0 ? 2f : 2.3f, 0, 1.9f), yaw);
        }

        private static void Tower(Transform parent, Vector3 top, Vector3 footprint, Quaternion? rotation = null)
        {
            float height = top.y + 2.6f;
            GameObject tower = Box(parent, "Oak pedestal", new Vector3(top.x, -2.6f + height * 0.5f, top.z), new Vector3(footprint.x, height, footprint.z), "Oak");
            tower.transform.localRotation = rotation ?? Quaternion.identity;
        }

        private static void Sensor(Transform parent, BallMechanism mechanism, int channel, Vector3 center, Vector3 size)
        {
            Transform sensor = Child(parent, "Passage sensor " + channel);
            sensor.localPosition = center;
            var collider = sensor.gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            collider.isTrigger = true;
            sensor.gameObject.AddComponent<MechanismSensor>().Configure(mechanism, channel);
        }

        private static void Arrow(Transform parent, Vector3 center, string material)
        {
            Box(parent, "Direction shaft", center - new Vector3(0.12f, 0, 0), new Vector3(0.53f, 0.012f, 0.085f), material, false);
            foreach (float sign in new[] { -1f, 1f })
            {
                GameObject wing = Box(parent, "Direction arrow", center + new Vector3(0.2f, 0, sign * 0.095f), new Vector3(0.3f, 0.014f, 0.075f), material, false);
                wing.transform.localRotation = Quaternion.Euler(0, sign * 42, 0);
            }
        }

        private static void Legs(Transform parent, Vector3 top, float halfX, float halfZ, float bottom)
        {
            foreach (float x in new[] { -halfX, halfX })
            foreach (float z in new[] { -halfZ, halfZ })
            {
                Box(parent, "Oak support leg", new Vector3(top.x + x, (top.y + bottom) * 0.5f, top.z + z),
                    new Vector3(0.14f, top.y - bottom, 0.14f), "Oak");
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Material Material(string name)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{name}.mat");
            return material != null ? material : AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, string material, bool collision = true)
        {
            GameObject box = MeshPart(parent, name, AtelierGeometry.Box(size), material, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            if (collision) box.AddComponent<BoxCollider>();
            return box;
        }

        private static GameObject MeshPart(Transform parent, string name, Mesh mesh, string material, bool collision)
        {
            Transform part = Child(parent, name);
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Material(material);
            if (collision)
            {
                // Generated .asset meshes have no ModelImporter; enable Unity 6's serialized bake flag.
                var settings = new SerializedObject(mesh);
                settings.FindProperty("m_PreBakeTriangleCollisionMesh").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Physics.BakeMesh(mesh.GetEntityId(), false);
                part.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            return part.gameObject;
        }
    }
}
