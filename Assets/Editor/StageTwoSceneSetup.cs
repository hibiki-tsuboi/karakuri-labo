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
    public static class StageTwoSceneSetup
    {
        private const string FirstStagePath = "Assets/Scenes/StageOne.unity";
        private const string SecondStagePath = "Assets/Scenes/StageTwo.unity";
        private const string RampPath = "Assets/Prefabs/Placement/PlaceableRamp.prefab";
        private const string DominoPath = "Assets/Prefabs/Placement/PlaceableDomino.prefab";
        private const string StageDominoPath = "Assets/Prefabs/Placement/StageTwoDomino.prefab";

        [MenuItem("Karakuri Labo/Set Up Stage 2 Puzzle")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before setting up Stage 2.");
            }
            if (File.Exists(SecondStagePath))
            {
                Debug.Log($"Stage 2 already exists. Open {SecondStagePath} to continue; its authored layout was preserved.");
                return;
            }
            if (!File.Exists(FirstStagePath))
            {
                throw new InvalidOperationException("Create StageOne.unity before setting up Stage 2.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before setting up Stage 2.");
                }
            }
            GameObject rampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RampPath);
            GameObject dominoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DominoPath);
            if (rampPrefab == null || rampPrefab.GetComponent<PhysicsObject>() == null ||
                dominoPrefab == null || dominoPrefab.GetComponent<DraggableObject>() == null ||
                dominoPrefab.GetComponent<PhysicsObject>() == null)
            {
                throw new InvalidOperationException("The placement catalog needs its ramp and domino prefabs.");
            }

            Scene firstStage = SceneManager.GetActiveScene();
            if (firstStage.path != FirstStagePath)
            {
                firstStage = EditorSceneManager.OpenScene(FirstStagePath, OpenSceneMode.Single);
            }
            ValidateSource();
            CreateDominoPrefab(dominoPrefab);

            // Stage 1 keeps its puzzle and inventory; only the Clear-only NEXT
            // control and its scene transition component are added here.
            ConfigureNext(SecondStagePath);
            EditorSceneManager.MarkSceneDirty(firstStage);
            if (!EditorSceneManager.SaveScene(firstStage) ||
                !EditorSceneManager.SaveScene(firstStage, SecondStagePath, true))
            {
                throw new InvalidOperationException("Stage 1 could not be saved and copied into Stage 2.");
            }

            Scene stage = EditorSceneManager.OpenScene(SecondStagePath, OpenSceneMode.Single);
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
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

            GameObject ramp = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab);
            PrefabUtility.UnpackPrefabInstance(ramp, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            ramp.name = "FixedRamp";
            ramp.transform.SetPositionAndRotation(new Vector3(-3f, 1.5f, 0f),
                Quaternion.Euler(0f, 0f, -16f));
            foreach (string name in new[] { "Rolling surface", "Back rail", "Front rail" })
            {
                Transform piece = ramp.transform.Find(name);
                Vector3 scale = piece.localScale;
                scale.x = 5f;
                piece.localScale = scale;
            }
            int supportIndex = 0;
            foreach (Transform support in ramp.transform)
            {
                if (support.name == "Ramp support")
                {
                    Vector3 top = ramp.transform.TransformPoint(new Vector3(
                        supportIndex++ == 0 ? -1.7f : 1.7f, -0.12f, 0f));
                    support.SetPositionAndRotation(new Vector3(top.x, top.y * 0.5f, 0f), Quaternion.identity);
                    support.localScale = new Vector3(0.3f, top.y, 1.3f);
                }
            }
            DraggableObject rampDrag = ramp.GetComponent<DraggableObject>();
            if (rampDrag != null)
            {
                Object.DestroyImmediate(rampDrag);
            }
            Transform outline = ramp.transform.Find("SelectionOutline");
            if (outline != null)
            {
                Object.DestroyImmediate(outline.gameObject);
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            BallController ball = Object.FindAnyObjectByType<BallController>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            UIManager ui = Object.FindAnyObjectByType<UIManager>();
            ball.transform.position = ramp.transform.TransformPoint(new Vector3(-2f, 0.47f, 0f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(ball.transform);
            manager.Configure(ball, goal, ui);
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(),
                goal.GetComponent<PhysicsObject>(), ramp.GetComponent<PhysicsObject>());
            // Saving and opening the copied scene can invalidate a freshly created
            // prefab's in-memory wrapper. Resolve the imported asset only now.
            AssetDatabase.ImportAsset(StageDominoPath, ImportAssetOptions.ForceSynchronousImport);
            GameObject stageDominoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageDominoPath);
            DraggableObject stageDomino = stageDominoPrefab != null
                ? stageDominoPrefab.GetComponent<DraggableObject>() : null;
            if (stageDomino == null)
            {
                throw new InvalidOperationException("The Stage 2 domino prefab could not be loaded after opening the new scene.");
            }
            spawner.Configure(manager, placement, new[] { stageDomino }, new[] { 3 });

            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud.transform.Find("SafeArea");
            RectTransform toolbar = safeArea.Find("PartToolbar").GetComponent<RectTransform>();
            toolbar.Find("AddRampButton").gameObject.SetActive(false);
            toolbar.Find("AddSeesawButton").gameObject.SetActive(false);
            Button addDomino = toolbar.Find("AddDominoButton").GetComponent<Button>();
            addDomino.gameObject.SetActive(true);
            LayoutElement size = addDomino.GetComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 192;
            Text addLabel = addDomino.transform.Find("Label").GetComponent<Text>();
            addLabel.text = "+ DOMINO";
            addLabel.fontSize = 23;
            PartPaletteHUD palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, new[] { addDomino });

            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.Configure(manager, placement,
                toolbar.Find("RotateButton").GetComponent<Button>(),
                safeArea.Find("GoalHint").GetComponent<Text>(),
                safeArea.Find("ModeLabel").GetComponent<Text>(),
                toolbar.Find("DeleteButton").GetComponent<Button>());
            placementHud.ConfigureHints("PLACE 3 DOMINOES  /  TOPPLE THEM WITH THE BALL",
                "DRAG TO BUILD A CHAIN  /  THEN PLAY");
            Text stageLabel = safeArea.Find("StageLabel").GetComponent<Text>();
            stageLabel.text = "02   /   DOMINO CHAIN";
            Text progress = ConfigureProgressLabel(safeArea);
            DominoObjective objective = GetOrAdd<DominoObjective>(manager.gameObject);
            objective.Configure(spawner.transform, 3, progress);
            manager.ConfigureObjective(objective);
            ConfigureNext(File.Exists("Assets/Scenes/StageThree.unity")
                ? "Assets/Scenes/StageThree.unity" : string.Empty);
            StageTwoGateSetup.ConfigureCurrentScene();

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
                throw new InvalidOperationException("StageTwo.unity could not be saved.");
            }
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 2 ready: connect three dominoes to the switch and open the ball's gate. Stage 1 now offers NEXT after CLEAR.");
        }

        private static void ValidateSource()
        {
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud != null ? hud.transform.Find("SafeArea") : null;
            Transform toolbar = safeArea != null ? safeArea.Find("PartToolbar") : null;
            BallController ball = Object.FindAnyObjectByType<BallController>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            if (toolbar == null || ball == null || goal == null ||
                ball.GetComponent<PhysicsObject>() == null || goal.GetComponent<PhysicsObject>() == null ||
                Object.FindAnyObjectByType<GameManager>() == null ||
                Object.FindAnyObjectByType<PlacementManager>() == null ||
                Object.FindAnyObjectByType<PartSpawner>() == null ||
                hud.GetComponent<PartPaletteHUD>() == null || hud.GetComponent<PlacementHUD>() == null ||
                hud.GetComponent<UIManager>() == null)
            {
                throw new InvalidOperationException("Stage 1 needs its completed puzzle and placement controls.");
            }
            foreach (string name in new[] { "AddRampButton", "AddDominoButton", "AddSeesawButton", "RotateButton", "DeleteButton", "SimulationButton" })
            {
                if (toolbar.Find(name) == null)
                {
                    throw new InvalidOperationException($"Stage 1 is missing the {name} control.");
                }
            }
            foreach (string name in new[] { "GoalHint", "ModeLabel", "StageLabel" })
            {
                if (safeArea.Find(name) == null)
                {
                    throw new InvalidOperationException($"Stage 1 is missing its {name} label.");
                }
            }
        }

        private static void CreateDominoPrefab(GameObject source)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(StageDominoPath);
            if (existing != null)
            {
                if (existing.GetComponent<DraggableObject>() == null || existing.GetComponent<DominoChainMember>() == null)
                {
                    throw new InvalidOperationException("StageTwoDomino.prefab must have DraggableObject and DominoChainMember components.");
                }
                return;
            }

            GameObject copy = Object.Instantiate(source);
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(copy))
                {
                    PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }
                copy.name = "StageTwoDomino";
                copy.transform.SetPositionAndRotation(new Vector3(0, source.transform.position.y, 0),
                    source.transform.rotation);
                copy.GetComponent<DraggableObject>().SetSelected(false);
                GetOrAdd<DominoChainMember>(copy);
                PrefabUtility.SaveAsPrefabAsset(copy, StageDominoPath);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
            AssetDatabase.ImportAsset(StageDominoPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureNext(string scenePath)
        {
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud.transform.Find("SafeArea");
            Transform toolbar = safeArea.Find("PartToolbar");
            Transform existing = toolbar.Find("NextButton");
            if (existing == null)
            {
                existing = safeArea.Find("NextButton");
            }
            GameObject target = existing != null ? existing.gameObject :
                new GameObject("NextButton", typeof(RectTransform));
            target.transform.SetParent(toolbar, false);
            Transform spacer = toolbar.Find("FlexibleSpace");
            if (spacer != null)
            {
                target.transform.SetAsLastSibling();
                target.transform.SetSiblingIndex(spacer.GetSiblingIndex());
            }
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);
            LayoutElement size = GetOrAdd<LayoutElement>(target);
            size.minWidth = size.preferredWidth = 200;
            size.minHeight = size.preferredHeight = 100;
            size.flexibleWidth = size.flexibleHeight = 0;
            Image image = GetOrAdd<Image>(target);
            image.color = new Color32(37, 112, 88, 255);
            image.raycastTarget = true;
            Button button = GetOrAdd<Button>(target);
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = safeArea.Find("PartToolbar/SimulationButton").GetComponent<Button>().colors;

            Transform existingLabel = target.transform.Find("Label");
            GameObject labelObject = existingLabel != null ? existingLabel.gameObject :
                new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(target.transform, false);
            Text label = GetOrAdd<Text>(labelObject);
            label.text = "NEXT";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 27;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

            StageManager stages = GetOrAdd<StageManager>(hud);
            stages.Configure(Object.FindAnyObjectByType<GameManager>(), button, scenePath);
            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.ConfigureClearHint(string.IsNullOrEmpty(scenePath)
                ? "ALL STAGES CLEAR!  /  RESET TO PLAY AGAIN"
                : "CLEAR!  /  NEXT TO CONTINUE");
            EditorUtility.SetDirty(stages);
            EditorUtility.SetDirty(placementHud);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar.GetComponent<RectTransform>());
        }

        private static Text ConfigureProgressLabel(Transform safeArea)
        {
            Transform existing = safeArea.Find("DominoProgress");
            GameObject target = existing != null ? existing.gameObject :
                new GameObject("DominoProgress", typeof(RectTransform));
            target.transform.SetParent(safeArea, false);
            Text label = GetOrAdd<Text>(target);
            label.text = "DOMINOES 0 / 3";
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
                new EditorBuildSettingsScene(FirstStagePath, true),
                new EditorBuildSettingsScene(SecondStagePath, true)
            };
            if (File.Exists("Assets/Scenes/StageThree.unity"))
            {
                list.Add(new EditorBuildSettingsScene("Assets/Scenes/StageThree.unity", true));
            }
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
