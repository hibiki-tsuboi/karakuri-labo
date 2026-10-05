using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class StageFourSceneSetup
    {
        private const string PreviousScene = "Assets/Scenes/StageThree.unity";
        private const string StageScene = "Assets/Scenes/StageFour.unity";
        private const string HighPrefab = "Assets/Prefabs/Placement/HighBridgeRamp.prefab";
        private const string LowPrefab = "Assets/Prefabs/Placement/LowBridgeRamp.prefab";

        [MenuItem("Karakuri Labo/Set Up Stage 4 Depth Puzzle")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before creating Stage 4.");
            }
            if (File.Exists(StageScene))
            {
                Debug.Log("StageFour.unity already exists; its authored layout was preserved.");
                return;
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before creating Stage 4.");
                }
            }
            if (!File.Exists(PreviousScene))
            {
                throw new InvalidOperationException("Create Stage 3 before Stage 4.");
            }
            CreateRamp(HighPrefab, "HIGH", 1.6f, "Ramp_Blue");
            CreateRamp(LowPrefab, "LOW", 0.68f, "Seesaw_Violet");

            Scene previous = EditorSceneManager.OpenScene(PreviousScene, OpenSceneMode.Single);
            ConfigureNext(StageScene);
            EditorSceneManager.MarkSceneDirty(previous);
            if (!EditorSceneManager.SaveScene(previous) || !EditorSceneManager.SaveScene(previous, StageScene, true))
            {
                throw new InvalidOperationException("Stage 3 could not be saved and copied.");
            }
            Scene scene = EditorSceneManager.OpenScene(StageScene, OpenSceneMode.Single);
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            manager.ConfigureObjective(null);
            Object.DestroyImmediate(manager.GetComponent<SeesawObjective>());
            Object.DestroyImmediate(GameObject.Find("FixedShortRamp"));
            Transform hud = GameObject.Find("HUD").transform;
            Transform safeArea = hud.Find("SafeArea");
            Object.DestroyImmediate(safeArea.Find("SeesawProgress").gameObject);
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
            while (spawner.transform.childCount > 0)
            {
                Object.DestroyImmediate(spawner.transform.GetChild(0).gameObject);
            }

            Transform stage = GameObject.Find("Stage").transform;
            Transform floor = stage.Find("Tabletop");
            floor.name = "ValleyFloor";
            floor.position = new Vector3(0, -2.85f, -0.4f);
            floor.localScale = new Vector3(12.8f, 0.7f, 11f);
            floor.GetComponent<Renderer>().sharedMaterial = Material("Ramp_Support");
            stage.Find("Backdrop").position = new Vector3(0, -3.3f, 0);
            Cube("StartIsland", stage, new Vector3(-4.3f, -1.3f, 2.1f),
                new Vector3(3.6f, 2.6f, 3.2f), Material("Stage_Cream"));
            Cube("GoalIsland", stage, new Vector3(1.1f, -1.3f, -3.45f),
                new Vector3(4.3f, 2.6f, 4.2f), Material("Stage_Cream"));

            BallController ball = Object.FindAnyObjectByType<BallController>();
            Quaternion highRotation = Quaternion.Euler(0, 30, 0) * Quaternion.Euler(0, 0, -16);
            ball.transform.position = new Vector3(-2.6f, 1.6f, 1.3f) +
                highRotation * new Vector3(-1.2f, 0.47f, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            goal.transform.SetPositionAndRotation(new Vector3(1.1f, -0.18f, -3.473f),
                Quaternion.Euler(0, 60, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal.transform);
            manager.Configure(ball, goal, hud.GetComponent<UIManager>());
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(), goal.GetComponent<PhysicsObject>());

            Camera camera = Camera.main;
            camera.transform.position = new Vector3(7.5f, 10.5f, -17f);
            camera.transform.LookAt(new Vector3(-0.5f, 0.2f, -0.7f));
            camera.orthographicSize = 7f;
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            placement.Configure(manager, camera, new Vector2(-5.2f, -3.7f), new Vector2(4.8f, 3.6f));
            // Reload after the scene copy, which may invalidate newly saved prefab wrappers.
            AssetDatabase.ImportAsset(HighPrefab, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(LowPrefab, ImportAssetOptions.ForceSynchronousImport);
            spawner.Configure(manager, placement, new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>(HighPrefab).GetComponent<DraggableObject>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(LowPrefab).GetComponent<DraggableObject>()
            }, new[] { 1, 1 });

            Transform toolbar = safeArea.Find("PartToolbar");
            Button highButton = toolbar.Find("AddRampButton").GetComponent<Button>();
            Button lowButton = toolbar.Find("AddSeesawButton").GetComponent<Button>();
            // Restore inherited labels before reusing the Stage 3 palette buttons.
            PartPaletteHUD palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, Array.Empty<Button>());
            ConfigurePartButton(highButton, "AddHighRampButton", "+ HIGH", Material("Ramp_Blue").color);
            ConfigurePartButton(lowButton, "AddLowRampButton", "+ LOW", Material("Seesaw_Violet").color);
            Transform obsoleteButton = toolbar.Find("AddDominoButton");
            if (obsoleteButton != null)
            {
                obsoleteButton.gameObject.SetActive(false);
            }
            palette.Configure(manager, spawner, new[] { highButton, lowButton });
            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.Configure(manager, placement, toolbar.Find("RotateButton").GetComponent<Button>(),
                safeArea.Find("GoalHint").GetComponent<Text>(), safeArea.Find("ModeLabel").GetComponent<Text>(),
                toolbar.Find("DeleteButton").GetComponent<Button>());
            placementHud.ConfigureHints("CONNECT BOTH HEIGHTS  /  CROSS FROM BACK TO FRONT",
                "DRAG IN DEPTH  /  ROTATE TO JOIN THE RAMPS");
            CreateHintBackdrop(safeArea.Find("GoalHint").GetComponent<RectTransform>());
            safeArea.Find("StageLabel").GetComponent<Text>().text = "04   /   DEPTH CROSSING";
            ConfigureNext(string.Empty);
            CameraOrbitSceneSetup.ConfigureScene(new Vector3(-0.5f, 0.2f, -0.7f));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)toolbar);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("StageFour.unity could not be saved.");
            }
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/StageOne.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/StageTwo.unity", true),
                new EditorBuildSettingsScene(PreviousScene, true),
                new EditorBuildSettingsScene(StageScene, true),
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes &&
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                profile.scenes = scenes;
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 4 ready: join high and low ramps across the valley using depth and rotation.");
        }

        private static void CreateRamp(string path, string label, float height, string material)
        {
            if (File.Exists(path))
            {
                return;
            }
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Placement/PlaceableRamp.prefab");
            GameObject ramp = Object.Instantiate(source);
            try
            {
                ramp.name = label == "HIGH" ? "HighBridgeRamp" : "LowBridgeRamp";
                ramp.transform.SetPositionAndRotation(new Vector3(0, height, 0), Quaternion.Euler(0, 0, -16));
                for (int index = ramp.transform.childCount - 1; index >= 0; index--)
                {
                    Transform child = ramp.transform.GetChild(index);
                    if (child.name == "Ramp support")
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
                ramp.transform.Find("Rolling surface").GetComponent<Renderer>().sharedMaterial = Material(material);
                ramp.GetComponent<DraggableObject>().Configure(ramp.transform.Find("SelectionOutline").gameObject, label);
                if (PrefabUtility.SaveAsPrefabAsset(ramp, path) == null)
                {
                    throw new InvalidOperationException($"Could not create {path}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(ramp);
            }
        }

        private static void ConfigurePartButton(Button button, string name, string label, Color color)
        {
            button.name = name;
            button.gameObject.SetActive(true);
            button.GetComponent<LayoutElement>().minWidth = 168;
            button.GetComponent<LayoutElement>().preferredWidth = 168;
            button.GetComponent<Image>().color = color;
            Text text = button.GetComponentInChildren<Text>();
            text.text = label;
            text.fontSize = 23;
        }

        private static void ConfigureNext(string path)
        {
            Transform hud = GameObject.Find("HUD").transform;
            Transform toolbar = hud.Find("SafeArea/PartToolbar");
            Button next = toolbar.Find("NextButton").GetComponent<Button>();
            next.transform.SetAsLastSibling();
            next.transform.SetSiblingIndex(toolbar.Find("FlexibleSpace").GetSiblingIndex());
            hud.GetComponent<StageManager>().Configure(Object.FindAnyObjectByType<GameManager>(), next, path);
            hud.GetComponent<PlacementHUD>().ConfigureClearHint(string.IsNullOrEmpty(path)
                ? "ALL STAGES CLEAR!  /  RESET TO PLAY AGAIN" : "CLEAR!  /  NEXT TO CONTINUE");
            EditorUtility.SetDirty(hud.GetComponent<StageManager>());
            EditorUtility.SetDirty(hud.GetComponent<PlacementHUD>());
        }

        private static void CreateHintBackdrop(RectTransform hint)
        {
            var background = new GameObject("HintBackdrop", typeof(RectTransform), typeof(Image));
            var rect = background.GetComponent<RectTransform>();
            rect.SetParent(hint.parent, false);
            rect.anchorMin = hint.anchorMin;
            rect.anchorMax = hint.anchorMax;
            rect.pivot = hint.pivot;
            rect.anchoredPosition = hint.anchoredPosition;
            rect.sizeDelta = hint.sizeDelta;
            rect.SetSiblingIndex(hint.GetSiblingIndex());
            var image = background.GetComponent<Image>();
            image.color = new Color(0.96f, 0.97f, 1f, 0.9f);
            image.raycastTarget = false;
        }

        private static Material Material(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

        private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
