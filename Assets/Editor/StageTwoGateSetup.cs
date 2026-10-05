using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class StageTwoGateSetup
    {
        private const string ScenePath = "Assets/Scenes/StageTwo.unity";

        [MenuItem("Karakuri Labo/Refine Stage 2 Domino Gate")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before refining Stage 2.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before refining Stage 2.");
                }
            }
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ConfigureCurrentScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("The domino gate puzzle could not be saved.");
            }
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureCurrentScene()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                throw new InvalidOperationException("The domino gate belongs only to StageTwo.");
            }
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            DominoObjective objective = Object.FindAnyObjectByType<DominoObjective>();
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            if (manager == null || objective == null || spawner == null || goal == null)
            {
                throw new InvalidOperationException("Set up the Stage 2 puzzle before adding its gate.");
            }
            Transform ramp = GameObject.Find("FixedRamp").transform;
            ramp.position = new Vector3(-3f, 1.3f, 0f);
            int supportIndex = 0;
            foreach (Transform support in ramp)
            {
                if (support.name == "Ramp support")
                {
                    Vector3 supportTop = ramp.TransformPoint(new Vector3(
                        supportIndex++ == 0 ? -1.7f : 1.7f, -0.12f, 0f));
                    support.SetPositionAndRotation(new Vector3(supportTop.x, supportTop.y * 0.5f, 0f), Quaternion.identity);
                    support.localScale = new Vector3(0.3f, supportTop.y, 1.3f);
                }
            }
            BallController ball = Object.FindAnyObjectByType<BallController>();
            ball.transform.position = ramp.TransformPoint(new Vector3(-2f, 0.47f, 0f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            objective.ResetProgress();
            GameObject previous = GameObject.Find("DominoGatePuzzle");
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            Material mint = Material("Goal_Mint");
            Material trim = Material("Goal_Trim");
            Material amber = Material("Domino_Amber");
            Material coral = Material("Ball_Coral");
            Material cream = Material("Ball_Stripe");
            GameObject puzzle = new GameObject("DominoGatePuzzle");
            Transform root = puzzle.transform;

            // The ball grazes the first domino and enters a separate, gently
            // descending lane. Gravity carries it on after the gate opens.
            GameObject lane = Cube("BallLane", root, new Vector3(1.45f, 0.087f, -0.6f),
                new Vector3(2.9f, 0.08f, 1.45f), mint);
            lane.transform.rotation = Quaternion.Euler(0, 0, -5);
            Cube("FrontRail", root, new Vector3(1.45f, 0.222f, -1.35f),
                new Vector3(2.9f, 0.26f, 0.08f), trim).transform.rotation = lane.transform.rotation;
            Cube("BackRail", root, new Vector3(1.45f, 0.222f, 0.16f),
                new Vector3(2.9f, 0.26f, 0.08f), trim).transform.rotation = lane.transform.rotation;

            // Flat markings suggest the chain's direction without acting as
            // invisible constraints on the freely placed dominoes.
            foreach (float z in new[] { 0.25f, 1.2f })
            {
                Cube("ChainGuide", root, new Vector3(0.8f, 0.012f, z),
                    new Vector3(2.05f, 0.012f, 0.025f), amber, false);
            }
            for (int index = 0; index < 3; index++)
            {
                Cube("ChainDirection", root, new Vector3(0.15f + 0.58f * index, 0.014f, 1.2f),
                    new Vector3(0.12f, 0.013f, 0.08f), amber, false);
            }

            foreach (float z in new[] { -1.43f, 0.23f })
            {
                Cube("GatePost", root, new Vector3(2.6f, 1.4f, z),
                    new Vector3(0.18f, 2.8f, 0.18f), trim);
            }
            Cube("GateHeader", root, new Vector3(2.6f, 2.8f, -0.6f),
                new Vector3(0.22f, 0.13f, 1.94f), trim);
            GameObject panel = Cube("GateBarrier", root, new Vector3(2.6f, 0.63f, -0.6f),
                new Vector3(0.16f, 1.25f, 1.4f), coral);
            Rigidbody body = panel.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            foreach (float y in new[] { -0.3f, 0f, 0.3f })
            {
                Cube("GateStripe", panel.transform, new Vector3(0.51f, y, 0),
                    new Vector3(0.015f, 0.09f, 0.96f), cream, false);
            }
            GameObject lamp = Cube("GateIndicator", root, new Vector3(2.725f, 2.8f, -0.6f),
                new Vector3(0.05f, 0.09f, 0.4f), amber, false);

            Cube("SwitchBase", root, new Vector3(2.03f, 0.045f, 0.6f),
                new Vector3(0.62f, 0.09f, 0.8f), trim);
            GameObject top = Cube("SwitchButton", root, new Vector3(2.03f, 0.13f, 0.6f),
                new Vector3(0.46f, 0.1f, 0.54f), amber);
            Cube("SwitchCable", root, new Vector3(2.03f, 0.02f, 0.37f),
                new Vector3(0.045f, 0.025f, 0.46f), amber, false);
            Cube("CableToGate", root, new Vector3(2.315f, 0.02f, 0.23f),
                new Vector3(0.57f, 0.025f, 0.045f), amber, false);
            DominoGate gate = puzzle.AddComponent<DominoGate>();
            gate.Configure(body, top.transform, lamp.GetComponent<Renderer>());
            GameObject sensor = new GameObject("DominoSwitch");
            sensor.transform.SetParent(root, false);
            sensor.transform.localPosition = new Vector3(2.03f, 0.19f, 0.6f);
            BoxCollider trigger = sensor.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.48f, 0.12f, 0.56f);
            sensor.AddComponent<DominoSwitch>().Configure(gate);

            goal.transform.position = new Vector3(4.1f, -0.18f, -0.6f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal.transform);
            Transform hud = GameObject.Find("HUD").transform;
            Transform safeArea = hud.Find("SafeArea");
            Text progress = safeArea.Find("DominoProgress").GetComponent<Text>();
            objective.Configure(spawner.transform, 3, progress, gate);
            manager.ConfigureObjective(objective);
            safeArea.Find("StageLabel").GetComponent<Text>().text = "02   /   CHAIN REACTION";
            hud.GetComponent<PlacementHUD>().ConfigureHints(
                "CONNECT THE DOMINOES TO THE SWITCH  /  OPEN THE GATE",
                "BUILD A CHAIN BESIDE THE BALL LANE  /  THEN PLAY");
            EditorUtility.SetDirty(objective);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(hud.GetComponent<PlacementHUD>());
        }

        private static Material Material(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");
            if (material == null)
            {
                throw new InvalidOperationException($"Missing puzzle material: {name}");
            }
            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool collides = true)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            if (!collides)
            {
                Object.DestroyImmediate(piece.GetComponent<Collider>());
            }
            return piece;
        }
    }
}
