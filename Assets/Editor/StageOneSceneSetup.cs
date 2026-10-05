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
    public static class StageOneSceneSetup
    {
        private const string SourceScenePath = "Assets/Scenes/Main.unity";
        private const string StageScenePath = "Assets/Scenes/StageOne.unity";
        private const string RampPath = "Assets/Prefabs/Placement/PlaceableRamp.prefab";

        [MenuItem("Karakuri Labo/Set Up Stage 1 Puzzle")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before setting up Stage 1.");
            }
            if (File.Exists(StageScenePath))
            {
                Debug.Log($"Stage 1 already exists. Open {StageScenePath} to continue; its authored layout was preserved.");
                return;
            }
            if (!File.Exists(SourceScenePath))
            {
                throw new InvalidOperationException("Create the Main scene before setting up Stage 1.");
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before copying Main into Stage 1.");
                }
            }
            GameObject rampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RampPath);
            if (rampPrefab == null || rampPrefab.GetComponent<DraggableObject>() == null)
            {
                throw new InvalidOperationException("Set up the Phase 6 placement catalog before Stage 1.");
            }

            Scene source = SceneManager.GetActiveScene();
            if (source.path != SourceScenePath)
            {
                source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            }
            ValidateSource();
            if (!EditorSceneManager.SaveScene(source, StageScenePath, true))
            {
                throw new InvalidOperationException("Main could not be copied to StageOne.unity.");
            }

            // All edits below target the newly opened copy. Main is never saved,
            // unpacked or modified by this builder.
            Scene stage = EditorSceneManager.OpenScene(StageScenePath, OpenSceneMode.Single);
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
            foreach (DraggableObject part in Object.FindObjectsByType<DraggableObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (part != null)
                {
                    Object.DestroyImmediate(part.gameObject);
                }
            }
            foreach (GameObject root in stage.GetRootGameObjects())
            {
                if (root.name == "Dominos")
                {
                    Object.DestroyImmediate(root);
                }
            }
            while (spawner.transform.childCount > 0)
            {
                Object.DestroyImmediate(spawner.transform.GetChild(0).gameObject);
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            BallController ball = Object.FindAnyObjectByType<BallController>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            UIManager ui = Object.FindAnyObjectByType<UIManager>();
            manager.Configure(ball, goal, ui);
            manager.ConfigureResetObjects(ball.GetComponent<PhysicsObject>(), goal.GetComponent<PhysicsObject>());
            spawner.Configure(manager, placement, new[] { rampPrefab.GetComponent<DraggableObject>() }, new[] { 1 });

            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud.transform.Find("SafeArea");
            RectTransform toolbar = safeArea.Find("PartToolbar").GetComponent<RectTransform>();
            Button addRamp = toolbar.Find("AddRampButton").GetComponent<Button>();
            toolbar.Find("AddDominoButton").gameObject.SetActive(false);
            toolbar.Find("AddSeesawButton").gameObject.SetActive(false);
            LayoutElement rampSize = addRamp.GetComponent<LayoutElement>();
            rampSize.minWidth = rampSize.preferredWidth = 192;
            Text rampLabel = addRamp.transform.Find("Label").GetComponent<Text>();
            rampLabel.text = "+ RAMP";
            rampLabel.fontSize = 23;
            PartPaletteHUD palette = hud.GetComponent<PartPaletteHUD>();
            palette.Configure(manager, spawner, new[] { addRamp });

            PlacementHUD placementHud = hud.GetComponent<PlacementHUD>();
            placementHud.Configure(manager, placement,
                toolbar.Find("RotateButton").GetComponent<Button>(),
                safeArea.Find("GoalHint").GetComponent<Text>(),
                safeArea.Find("ModeLabel").GetComponent<Text>(),
                toolbar.Find("DeleteButton").GetComponent<Button>());
            placementHud.ConfigureHints("PLACE THE RAMP  /  GUIDE THE BALL TO THE GOAL",
                "DRAG BELOW THE BALL  /  THEN PLAY");
            Text stageLabel = safeArea.Find("StageLabel").GetComponent<Text>();
            stageLabel.text = "01   /   FIRST ROLL";
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(palette);
            EditorUtility.SetDirty(placementHud);
            EditorUtility.SetDirty(stageLabel);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar);
            EditorSceneManager.MarkSceneDirty(stage);
            if (!EditorSceneManager.SaveScene(stage))
            {
                throw new InvalidOperationException("StageOne.unity could not be saved.");
            }
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 1 ready: place the single ramp to guide the ball into the goal. Main remains available as the sandbox scene.");
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
                throw new InvalidOperationException("Main needs the completed placement controls, ball and goal before Stage 1.");
            }
            foreach (string button in new[] { "AddRampButton", "AddDominoButton", "AddSeesawButton", "RotateButton", "DeleteButton" })
            {
                if (toolbar.Find(button) == null)
                {
                    throw new InvalidOperationException($"Main is missing the {button} placement control.");
                }
            }
        }

        private static void ConfigureBuildScenes()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(StageScenePath, true),
                new EditorBuildSettingsScene(SourceScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            BuildProfile profile = BuildProfile.GetActiveBuildProfile();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS &&
                profile != null && profile.overrideGlobalScenes)
            {
                profile.scenes = scenes;
                EditorUtility.SetDirty(profile);
            }
        }
    }
}
