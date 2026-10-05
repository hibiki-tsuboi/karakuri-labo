using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class StageFiveSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/StageFive.unity";
        private static readonly Vector3[] Route =
        {
            new Vector3(-2.6f, 5.28f, 6f), new Vector3(-0.368f, 4.36f, 3.768f),
            new Vector3(0.45f, 3.44f, 0.716f), new Vector3(-0.368f, 2.52f, -2.336f),
            new Vector3(-1.186f, 1.6f, -5.388f), new Vector3(-0.368f, 0.68f, -8.44f)
        };
        private static readonly float[] Yaws = { 30, 60, 90, 120, 90, 60 };
        private static readonly Vector3[] Centers =
        {
            new Vector3(-1.1f, 4.9f, 4.7f), new Vector3(-0.15f, 2.9f, -1.5f),
            new Vector3(-0.6f, 0.6f, -8f), new Vector3(-0.6f, 1.8f, -3.2f)
        };
        private static readonly string[] Names = { "Upper", "Middle", "Lower" };

        [MenuItem("Karakuri Labo/Set Up Stage 5 Long Descent")]
        public static void Apply()
        {
            if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty))
            {
                throw new InvalidOperationException("Stop Play and save open scenes before creating Stage 5.");
            }
            if (File.Exists(ScenePath))
            {
                Debug.Log("StageFive already exists; its authored layout was preserved.");
                return;
            }
            for (int i = 0; i < 3; i++) CreatePart(i);
            Scene previous = EditorSceneManager.OpenScene("Assets/Scenes/StageFour.unity");
            ConfigureNext(ScenePath);
            if (!EditorSceneManager.SaveScene(previous) || !EditorSceneManager.SaveScene(previous, ScenePath, true))
            {
                throw new InvalidOperationException("Could not save and copy Stage 4.");
            }
            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            Transform stage = GameObject.Find("Stage").transform;
            Object.DestroyImmediate(stage.Find("StartIsland").gameObject);
            Transform floor = stage.Find("ValleyFloor");
            // Remove generated decorations before resizing their physical parent.
            Object.DestroyImmediate(floor.Find("Atelier Details").gameObject);
            floor.position = new Vector3(0, -2.85f, -2.2f);
            floor.localScale = new Vector3(12.8f, 0.7f, 26f);
            Transform goalIsland = stage.Find("GoalIsland");
            goalIsland.position = new Vector3(1.1f, -1.3f, -10.981f);
            stage.Find("Backdrop").position = new Vector3(0, -3.3f, -2.2f);
            for (int i = 0; i < 3; i++)
            {
                int routeIndex = i * 2;
                GameObject ramp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Placement/HighBridgeRamp.prefab"));
                PrefabUtility.UnpackPrefabInstance(ramp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                ramp.name = "Fixed" + Names[i] + "Ramp";
                RaiseRails(ramp);
                ramp.transform.SetPositionAndRotation(Route[routeIndex], Rotation(Yaws[routeIndex]));
                Object.DestroyImmediate(ramp.GetComponent<DraggableObject>());
                Object.DestroyImmediate(ramp.GetComponent<PhysicsObject>());
                Object.DestroyImmediate(ramp.transform.Find("SelectionOutline").gameObject);
                float top = Route[routeIndex].y - 0.9f;
                float height = top + 2.5f;
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tower.name = Names[i] + "Tower";
                tower.transform.SetParent(stage);
                tower.transform.SetPositionAndRotation(new Vector3(Route[routeIndex].x, -2.5f + height * 0.5f,
                    Route[routeIndex].z), Quaternion.Euler(0, Yaws[routeIndex], 0));
                tower.transform.localScale = new Vector3(2.4f, height, 2f);
                tower.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Oak.mat");
            }
            var ball = Object.FindAnyObjectByType<BallController>();
            ball.transform.position = Route[0] + Rotation(Yaws[0]) * new Vector3(-1.2f, 0.47f, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            var goal = Object.FindAnyObjectByType<GoalController>();
            goal.transform.SetPositionAndRotation(new Vector3(1.1f, -0.18f, -10.981f), Quaternion.Euler(0, 60, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal.transform);
            Transform hud = GameObject.Find("HUD").transform;
            Transform safe = hud.Find("SafeArea");
            var manager = Object.FindAnyObjectByType<GameManager>();
            manager.Configure(ball, goal, hud.GetComponent<UIManager>());
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(), goal.GetComponent<PhysicsObject>());
            var camera = Camera.main;
            camera.transform.position = Centers[0] + new Vector3(7.5f, 10.3f, -16.3f);
            camera.transform.LookAt(Centers[0]);
            camera.orthographicSize = 4.8f;
            camera.farClipPlane = 100;
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            placement.Configure(manager, camera, new Vector2(-5.2f, -11f), new Vector2(5.2f, 8f));
            CameraOrbitSceneSetup.ConfigureScene(Centers[0]);
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            spawner.Configure(manager, placement, Names.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Placement/{n}DescentRamp.prefab").GetComponent<DraggableObject>()).ToArray(), new[] { 1, 1, 1 });
            spawner.ConfigureSpawnPositions(new Vector2(-3.2f, 3.3f), new Vector2(-3.2f, -2.6f), new Vector2(-3.2f, -8.2f));
            Transform toolbar = safe.Find("PartToolbar");
            var palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, Array.Empty<Button>());
            Transform middleButton = toolbar.Find("AddDominoButton");
            if (middleButton == null)
            {
                middleButton = Object.Instantiate(toolbar.Find("AddHighRampButton"), toolbar);
                middleButton.name = "AddMiddleRampButton";
            }
            string[] previousButtons = { "AddHighRampButton", middleButton.name, "AddLowRampButton" };
            var buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = toolbar.Find(previousButtons[i]).GetComponent<Button>();
                buttons[i].name = "Add" + Names[i] + "RampButton";
                buttons[i].gameObject.SetActive(true);
                buttons[i].transform.SetSiblingIndex(i);
                LayoutElement layout = buttons[i].GetComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 136;
                buttons[i].GetComponentInChildren<Text>().text = "+ " + Names[i].ToUpperInvariant();
            }
            palette.Configure(manager, spawner, buttons);
            safe.Find("StageLabel").GetComponent<Text>().text = "05   /   THE LONG DESCENT";
            hud.GetComponent<PlacementHUD>().ConfigureHints("EXPLORE ALL THREE LEVELS  /  CONNECT EVERY GAP",
                "MATCH THE HEIGHT  /  ROTATE TO FOLLOW THE COURSE");
            ConfigureNext(string.Empty);
            Button[] views = new Button[4];
            string[] labels = { "01 UPPER", "02 MIDDLE", "03 LOWER", "ALL / MAP" };
            for (int i = 0; i < 4; i++)
            {
                views[i] = CopyButton(safe.Find("SoundButton").GetComponent<Button>(), safe, "AreaView" + i, labels[i],
                    new Vector2(24 + (i % 2) * 142, -164 - (i / 2) * 70), new Vector2(132, 60), false);
            }
            Button follow = CopyButton(safe.Find("SoundButton").GetComponent<Button>(), safe, "FollowBallButton", "FOLLOW BALL",
                new Vector2(-24, -232), new Vector2(176, 56), true);
            hud.gameObject.AddComponent<StageViewNavigator>().Configure(camera.GetComponent<StageCameraOrbit>(), placement,
                spawner, manager, ball, Centers, new[] { 4.8f, 4.8f, 4.8f, 12f }, views, follow);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Could not save Stage 5.");
            }
            EditorBuildSettings.scenes = new[] { "StageOne", "StageTwo", "StageThree", "StageFour", "StageFive", "Main" }
                .Select(n => new EditorBuildSettingsScene($"Assets/Scenes/{n}.unity", true)).ToArray();
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes && EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                profile.scenes = EditorBuildSettings.scenes;
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            AtelierArtSetup.Apply();
            CampaignSceneSetup.Apply();
            Debug.Log("Stage 5 created: three gaps, three altitude bands, area navigation and ball following.");
        }

        private static void CreatePart(int index)
        {
            string path = $"Assets/Prefabs/Placement/{Names[index]}DescentRamp.prefab";
            if (File.Exists(path))
            {
                return;
            }
            GameObject copy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Placement/HighBridgeRamp.prefab"));
            try
            {
                copy.name = Names[index] + "DescentRamp";
                RaiseRails(copy);
                copy.transform.SetPositionAndRotation(new Vector3(0, Route[index * 2 + 1].y, 0), Rotation(0));
                copy.GetComponent<DraggableObject>().Configure(copy.transform.Find("SelectionOutline").gameObject,
                    Names[index].ToUpperInvariant());
                string[] materials = { "Ramp_Blue", "Domino_Amber", "Seesaw_Violet" };
                copy.transform.Find("Rolling surface").GetComponent<Renderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{materials[index]}.mat");
                if (PrefabUtility.SaveAsPrefabAsset(copy, path) == null)
                {
                    throw new InvalidOperationException(path);
                }
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        private static void RaiseRails(GameObject ramp)
        {
            foreach (string name in new[] { "Front rail", "Back rail" })
            {
                Transform rail = ramp.transform.Find(name);
                rail.localPosition = new Vector3(0, 0.435f, rail.localPosition.z);
                rail.localScale = new Vector3(3.4f, 0.65f, 0.12f);
            }
        }

        private static Quaternion Rotation(float yaw) => Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -16);

        private static Button CopyButton(Button source, Transform parent, string name, string label,
            Vector2 position, Vector2 size, bool right)
        {
            Button button = Object.Instantiate(source, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            button.GetComponentInChildren<Text>().text = label;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return button;
        }

        private static void ConfigureNext(string path)
        {
            var hud = GameObject.Find("HUD");
            if (SceneManager.GetActiveScene().name == "StageFour")
            {
                foreach (string buttonName in new[] { "AddHighRampButton", "AddLowRampButton", "NextButton" })
                {
                    var layout = hud.transform.Find("SafeArea/PartToolbar/" + buttonName).GetComponent<LayoutElement>();
                    layout.minWidth = layout.preferredWidth = buttonName == "NextButton" ? 168 : 136;
                }
            }
            hud.GetComponent<StageManager>().Configure(Object.FindAnyObjectByType<GameManager>(),
                hud.transform.Find("SafeArea/PartToolbar/NextButton").GetComponent<Button>(), path);
            hud.GetComponent<PlacementHUD>().ConfigureClearHint(string.IsNullOrEmpty(path)
                ? "ALL STAGES CLEAR!  /  RESET TO PLAY AGAIN" : "CLEAR!  /  NEXT TO CONTINUE");
        }
    }
}
