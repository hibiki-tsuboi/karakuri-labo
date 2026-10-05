using System;
using System.IO;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class StageThreeSceneSetup
    {
        private const string SecondStagePath = "Assets/Scenes/StageTwo.unity";
        private const string ThirdStagePath = "Assets/Scenes/StageThree.unity";
        private const string RampPath = "Assets/Prefabs/Placement/PlaceableRamp.prefab";
        private const string SeesawPath = "Assets/Prefabs/Placement/PlaceableSeesaw.prefab";
        private const string StageSeesawPath = "Assets/Prefabs/Placement/StageThreeSeesaw.prefab";

        [MenuItem("Karakuri Labo/Set Up Stage 3 Puzzle")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before setting up Stage 3.");
            }
            if (File.Exists(ThirdStagePath))
            {
                Debug.Log($"Stage 3 already exists. Open {ThirdStagePath} to continue; its authored layout was preserved.");
                return;
            }
            if (!File.Exists(SecondStagePath))
            {
                throw new InvalidOperationException("Create StageTwo.unity before setting up Stage 3.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before setting up Stage 3.");
                }
            }
            ValidateCatalog();

            Scene secondStage = SceneManager.GetActiveScene();
            if (secondStage.path != SecondStagePath)
            {
                secondStage = EditorSceneManager.OpenScene(SecondStagePath, OpenSceneMode.Single);
            }
            ValidateSource();
            CreateSeesawPrefab();

            // Save the existing puzzle with only its NEXT destination and clear hint
            // changed before making a copy for the new authored layout.
            ConfigureNext(ThirdStagePath);
            EditorSceneManager.MarkSceneDirty(secondStage);
            if (!EditorSceneManager.SaveScene(secondStage) ||
                !EditorSceneManager.SaveScene(secondStage, ThirdStagePath, true))
            {
                throw new InvalidOperationException("Stage 2 could not be saved and copied into Stage 3.");
            }

            Scene stage = EditorSceneManager.OpenScene(ThirdStagePath, OpenSceneMode.Single);
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
            BallController ball = Object.FindAnyObjectByType<BallController>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud.transform.Find("SafeArea");
            RectTransform toolbar = safeArea.Find("PartToolbar").GetComponent<RectTransform>();

            manager.ConfigureObjective(null);
            DominoObjective previousObjective = manager.GetComponent<DominoObjective>();
            if (previousObjective != null)
            {
                Object.DestroyImmediate(previousObjective);
            }
            GameObject previousGate = GameObject.Find("DominoGatePuzzle");
            if (previousGate != null)
            {
                Object.DestroyImmediate(previousGate);
            }
            Transform previousProgress = safeArea.Find("DominoProgress");
            if (previousProgress != null)
            {
                Object.DestroyImmediate(previousProgress.gameObject);
            }
            foreach (DraggableObject part in Object.FindObjectsByType<DraggableObject>(
                FindObjectsInactive.Include))
            {
                if (part != null)
                {
                    Object.DestroyImmediate(part.gameObject);
                }
            }
            while (spawner.transform.childCount > 0)
            {
                Object.DestroyImmediate(spawner.transform.GetChild(0).gameObject);
            }
            Object.DestroyImmediate(GameObject.Find("FixedRamp"));

            GameObject ramp = InstantiateFixture(RampPath, "FixedShortRamp",
                new Vector3(-4.15f, 1.5f, 0f), Quaternion.Euler(0f, 0f, -16f));
            ball.transform.position = ramp.transform.TransformPoint(new Vector3(-1.2f, 0.47f, 0f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            goal.transform.position = new Vector3(3.65f, -0.18f, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(goal.transform);
            manager.Configure(ball, goal, hud.GetComponent<UIManager>());
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(),
                goal.GetComponent<PhysicsObject>(), ramp.GetComponent<PhysicsObject>());

            // Opening the copied scene can invalidate a freshly created prefab's
            // in-memory wrapper. Reload the imported asset immediately before use.
            AssetDatabase.ImportAsset(StageSeesawPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject stageSeesawPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageSeesawPath);
            DraggableObject stageSeesaw = stageSeesawPrefab != null
                ? stageSeesawPrefab.GetComponent<DraggableObject>() : null;
            if (stageSeesaw == null)
            {
                throw new InvalidOperationException("The Stage 3 seesaw prefab could not be loaded after opening the new scene.");
            }
            spawner.Configure(manager, placement, new[] { stageSeesaw }, new[] { 1 });

            toolbar.Find("AddRampButton").gameObject.SetActive(false);
            toolbar.Find("AddDominoButton").gameObject.SetActive(false);
            Button addSeesaw = toolbar.Find("AddSeesawButton").GetComponent<Button>();
            addSeesaw.gameObject.SetActive(true);
            LayoutElement size = addSeesaw.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 192;
            Text addLabel = addSeesaw.transform.Find("Label").GetComponent<Text>();
            addLabel.text = "+ SEESAW";
            addLabel.fontSize = 23;
            PartPaletteHUD palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, new[] { addSeesaw });

            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.Configure(manager, placement,
                toolbar.Find("RotateButton").GetComponent<Button>(),
                safeArea.Find("GoalHint").GetComponent<Text>(),
                safeArea.Find("ModeLabel").GetComponent<Text>(),
                toolbar.Find("DeleteButton").GetComponent<Button>());
            placementHud.ConfigureHints("PLACE THE SEESAW  /  GUIDE THE BALL TO THE GOAL",
                "DRAG BELOW THE RAMP  /  THEN PLAY");
            Text stageLabel = safeArea.Find("StageLabel").GetComponent<Text>();
            stageLabel.text = "03   /   BALANCE ACT";
            Text progress = CreateProgressLabel(safeArea);
            SeesawObjective objective = GetOrAdd<SeesawObjective>(manager.gameObject);
            objective.Configure(spawner.transform, ball, progress);
            manager.ConfigureObjective(objective);
            ConfigureNext(File.Exists("Assets/Scenes/StageFour.unity")
                ? "Assets/Scenes/StageFour.unity" : string.Empty);

            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(palette);
            EditorUtility.SetDirty(placementHud);
            EditorUtility.SetDirty(stageLabel);
            EditorUtility.SetDirty(objective);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar);
            EditorSceneManager.MarkSceneDirty(stage);
            if (!EditorSceneManager.SaveScene(stage))
            {
                throw new InvalidOperationException("StageThree.unity could not be saved.");
            }
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 3 ready: place one seesaw below the ramp, tip it with the ball, and reach the goal. Stage 2 now offers NEXT after CLEAR.");
        }

        private static void ValidateCatalog()
        {
            foreach (string path in new[] { RampPath, SeesawPath })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<DraggableObject>() == null ||
                    prefab.GetComponent<PhysicsObject>() == null)
                {
                    throw new InvalidOperationException($"The placement catalog needs a complete prefab at {path}.");
                }
            }
            GameObject seesaw = AssetDatabase.LoadAssetAtPath<GameObject>(SeesawPath);
            Transform board = seesaw.transform.Find("Board");
            if (board == null || board.GetComponent<Rigidbody>() == null ||
                board.GetComponent<HingeJoint>() == null || board.GetComponent<PhysicsObject>() == null)
            {
                throw new InvalidOperationException("PlaceableSeesaw.prefab needs its physical hinged Board.");
            }
        }

        private static void ValidateSource()
        {
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud != null ? hud.transform.Find("SafeArea") : null;
            Transform toolbar = safeArea != null ? safeArea.Find("PartToolbar") : null;
            BallController ball = Object.FindAnyObjectByType<BallController>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            if (toolbar == null || ball == null || goal == null || manager == null ||
                ball.GetComponent<PhysicsObject>() == null || goal.GetComponent<PhysicsObject>() == null ||
                manager.GetComponent<DominoObjective>() == null || GameObject.Find("FixedRamp") == null ||
                Object.FindAnyObjectByType<PlacementManager>() == null ||
                Object.FindAnyObjectByType<PartSpawner>() == null ||
                hud.GetComponent<PartPaletteHUD>() == null || hud.GetComponent<PlacementHUD>() == null ||
                hud.GetComponent<UIManager>() == null || hud.GetComponent<StageManager>() == null)
            {
                throw new InvalidOperationException("Stage 2 needs its completed domino puzzle and placement controls.");
            }
            foreach (string name in new[] { "AddRampButton", "AddDominoButton", "AddSeesawButton", "RotateButton", "DeleteButton", "NextButton" })
            {
                Transform control = toolbar.Find(name);
                if (control == null || control.GetComponent<Button>() == null)
                {
                    throw new InvalidOperationException($"Stage 2 is missing the {name} control.");
                }
            }
            foreach (string name in new[] { "GoalHint", "ModeLabel", "StageLabel", "DominoProgress" })
            {
                Transform label = safeArea.Find(name);
                if (label == null || label.GetComponent<Text>() == null)
                {
                    throw new InvalidOperationException($"Stage 2 is missing its {name} label.");
                }
            }
        }

        private static void CreateSeesawPrefab()
        {
            if (File.Exists(StageSeesawPath))
            {
                AssetDatabase.ImportAsset(StageSeesawPath, ImportAssetOptions.ForceSynchronousImport);
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(StageSeesawPath);
                Transform board = existing != null ? existing.transform.Find("Board") : null;
                if (existing == null || existing.GetComponent<DraggableObject>() == null ||
                    board == null || board.GetComponent<SeesawGoalMember>() == null)
                {
                    throw new InvalidOperationException("StageThreeSeesaw.prefab must have DraggableObject and a SeesawGoalMember on Board.");
                }
                return;
            }

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SeesawPath);
            GameObject copy = Object.Instantiate(source);
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(copy))
                {
                    PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }
                copy.name = "StageThreeSeesaw";
                copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                copy.GetComponent<DraggableObject>().SetSelected(false);
                GetOrAdd<SeesawGoalMember>(copy.transform.Find("Board").gameObject);
                if (PrefabUtility.SaveAsPrefabAsset(copy, StageSeesawPath) == null)
                {
                    throw new InvalidOperationException("StageThreeSeesaw.prefab could not be saved.");
                }
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
            AssetDatabase.ImportAsset(StageSeesawPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static GameObject InstantiateFixture(string prefabPath, string name,
            Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            DraggableObject draggable = instance.GetComponent<DraggableObject>();
            if (draggable != null)
            {
                Object.DestroyImmediate(draggable);
            }
            Transform outline = instance.transform.Find("SelectionOutline");
            if (outline != null)
            {
                Object.DestroyImmediate(outline.gameObject);
            }
            return instance;
        }

        private static void ConfigureNext(string scenePath)
        {
            GameObject hud = GameObject.Find("HUD");
            Transform toolbar = hud.transform.Find("SafeArea/PartToolbar");
            Button next = toolbar.Find("NextButton").GetComponent<Button>();
            next.transform.SetAsLastSibling();
            next.transform.SetSiblingIndex(toolbar.Find("FlexibleSpace").GetSiblingIndex());
            StageManager stages = hud.GetComponent<StageManager>();
            stages.Configure(Object.FindAnyObjectByType<GameManager>(), next, scenePath);
            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.ConfigureClearHint(string.IsNullOrEmpty(scenePath)
                ? "ALL STAGES CLEAR!  /  RESET TO PLAY AGAIN"
                : "CLEAR!  /  NEXT TO CONTINUE");
            EditorUtility.SetDirty(stages);
            EditorUtility.SetDirty(placementHud);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar.GetComponent<RectTransform>());
        }

        private static Text CreateProgressLabel(Transform safeArea)
        {
            GameObject target = new GameObject("SeesawProgress", typeof(RectTransform));
            target.transform.SetParent(safeArea, false);
            Text label = target.AddComponent<Text>();
            label.text = "SEESAW 0 / 1";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 21;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color32(52, 76, 112, 255);
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(48, -115);
            rect.sizeDelta = new Vector2(660, 42);
            return label;
        }

        private static void ConfigureBuildScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/StageOne.unity", true),
                new EditorBuildSettingsScene(SecondStagePath, true),
                new EditorBuildSettingsScene(ThirdStagePath, true)
            };
            if (File.Exists("Assets/Scenes/StageFour.unity"))
            {
                list.Add(new EditorBuildSettingsScene("Assets/Scenes/StageFour.unity", true));
            }
            list.Add(new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true));
            var scenes = list.ToArray();
            EditorBuildSettings.scenes = scenes;
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS &&
                profile != null && profile.overrideGlobalScenes)
            {
                profile.scenes = scenes;
                EditorUtility.SetDirty(profile);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
