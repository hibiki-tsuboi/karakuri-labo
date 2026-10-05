using System;
using System.IO;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    /// <summary>Creates the first authored level through Unity's asset and scene APIs.</summary>
    public static class PhaseOneSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Karakuri Labo/Create Phase 1 Scene")]
        public static void Create()
        {
            if (Application.isPlaying || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Create requires Edit Mode and no existing Main scene; existing work is never replaced.");
            }

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before creating the Phase 1 scene.");
                }
            }

            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Prefabs/Ball");
            EnsureFolder("Assets/Prefabs/Ramp");
            EnsureFolder("Assets/Prefabs/Goal");
            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material coral = Material("Ball_Coral", "FF705C");
            Material blue = Material("Ramp_Blue", "628BD5");
            Material blueDark = Material("Ramp_Support", "3C5C91");
            Material mint = Material("Goal_Mint", "80CDB2");
            Material mintDark = Material("Goal_Trim", "3C8878");
            Material cream = Material("Stage_Cream", "F7EAD2");
            Material ground = Material("Backdrop_Lilac", "DCDDEC");
            Material white = Material("Ball_Stripe", "FFF5DD");

            PhysicsMaterial rolling = new PhysicsMaterial("Rolling")
            {
                dynamicFriction = 0.45f,
                staticFriction = 0.5f,
                bounciness = 0.08f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(rolling, "Assets/Materials/Rolling.physicMaterial");

            GameObject stage = new GameObject("Stage");
            Cube("Tabletop", stage.transform, new Vector3(0, -0.35f, 0), new Vector3(12.8f, 0.7f, 5.4f), cream);
            Cube("Backdrop", stage.transform, new Vector3(0, -0.8f, 0), new Vector3(200, 0.15f, 200), ground);

            GameObject rampSource = new GameObject("Ramp");
            Cube("Rolling surface", rampSource.transform, Vector3.zero, new Vector3(7.2f, 0.24f, 1.5f), blue)
                .GetComponent<Collider>().sharedMaterial = rolling;
            Cube("Back rail", rampSource.transform, new Vector3(0, 0.21f, 0.81f), new Vector3(7.2f, 0.25f, 0.12f), blueDark);
            Cube("Front rail", rampSource.transform, new Vector3(0, 0.21f, -0.81f), new Vector3(7.2f, 0.25f, 0.12f), blueDark);
            GameObject rampPrefab = PrefabUtility.SaveAsPrefabAsset(rampSource, "Assets/Prefabs/Ramp/Ramp.prefab");
            Object.DestroyImmediate(rampSource);
            GameObject ramp = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab);
            ramp.transform.SetPositionAndRotation(new Vector3(-1.8f, 1.55f, 0), Quaternion.Euler(0, 0, -16));
            CreateSupport(stage.transform, ramp.transform, -2.6f, blueDark);
            CreateSupport(stage.transform, ramp.transform, 2.4f, blueDark);

            GameObject ballSource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballSource.name = "Ball";
            ballSource.transform.localScale = Vector3.one * 0.66f;
            ballSource.GetComponent<Renderer>().sharedMaterial = coral;
            ballSource.GetComponent<Collider>().sharedMaterial = rolling;
            Rigidbody body = ballSource.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 30;
            ballSource.AddComponent<BallController>();
            // A contrasting equator makes real rotation readable while the ball rolls.
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stripe.name = "Equator";
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            stripe.transform.SetParent(ballSource.transform, false);
            stripe.transform.localRotation = Quaternion.Euler(90, 0, 0);
            stripe.transform.localScale = new Vector3(1.007f, 0.04f, 1.007f);
            stripe.GetComponent<Renderer>().sharedMaterial = white;
            GameObject ballPrefab = PrefabUtility.SaveAsPrefabAsset(ballSource, "Assets/Prefabs/Ball/Ball.prefab");
            Object.DestroyImmediate(ballSource);
            GameObject ball = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
            ball.transform.position = ramp.transform.TransformPoint(new Vector3(-3.0f, 0.47f, 0));

            GameObject goalSource = new GameObject("Goal");
            BoxCollider trigger = goalSource.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.65f, 0);
            trigger.size = new Vector3(2.4f, 0.9f, 1.4f);
            goalSource.AddComponent<GoalController>();
            Cube("Tray", goalSource.transform, new Vector3(0, 0.1f, 0), new Vector3(2.9f, 0.2f, 2.0f), mint)
                .GetComponent<Collider>().sharedMaterial = rolling;
            Cube("End wall", goalSource.transform, new Vector3(1.4f, 0.5f, 0), new Vector3(0.18f, 1, 2.0f), mintDark);
            Cube("Back wall", goalSource.transform, new Vector3(0, 0.3f, 1), new Vector3(2.9f, 0.6f, 0.12f), mintDark);
            Cube("Front wall", goalSource.transform, new Vector3(0, 0.3f, -1), new Vector3(2.9f, 0.6f, 0.12f), mintDark);
            GameObject goalPrefab = PrefabUtility.SaveAsPrefabAsset(goalSource, "Assets/Prefabs/Goal/Goal.prefab");
            Object.DestroyImmediate(goalSource);
            GameObject goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
            goal.transform.position = new Vector3(3.65f, 0.22f, 0);

            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(7.5f, 10.5f, -17);
            camera.transform.LookAt(new Vector3(0, 0.8f, 0));
            camera.orthographic = true;
            camera.orthographicSize = 5.3f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("DCDDEC");
            camera.allowHDR = false;
            camera.allowMSAA = true;

            Light light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(48, -35, 0);
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.5f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.45f);
            RenderSettings.sun = light;

            UIManager ui = CreateUI();
            GameManager manager = new GameObject("GameManager").AddComponent<GameManager>();
            manager.Configure(ball.GetComponent<BallController>(), goal.GetComponent<GoalController>(), ui);

            PlayerSettings.productName = "KarakuriLabo";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = manager.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0.8f, 0), camera.transform.rotation, 9);
            }
            Debug.Log("Base scene created. Run Karakuri Labo > Set Up Phase 2 Editing before entering Play Mode.");
        }

        private static UIManager CreateUI()
        {
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text title = Label("Title", canvasObject.transform, "KARAKURI LABO", 26, font, Hex("34445F"));
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -46), new Vector2(500, 42));
            Text subtitle = Label("StageLabel", canvasObject.transform, "01   /   THE FIRST ROLL", 15, font, Hex("67748C"));
            Place(subtitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(65, -91), new Vector2(500, 28));
            Text footer = Label("GoalHint", canvasObject.transform, "BALL  >  RAMP  >  GOAL", 16, font, Hex("53637C"));
            Place(footer.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(700, 32));
            footer.alignment = TextAnchor.MiddleCenter;
            Text clear = Label("ClearText", canvasObject.transform, "CLEAR!", 70, font, Hex("294D4A"));
            clear.fontStyle = FontStyle.Bold;
            clear.alignment = TextAnchor.MiddleCenter;
            Place(clear.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 35), new Vector2(650, 140));
            Outline outline = clear.gameObject.AddComponent<Outline>();
            outline.effectColor = Hex("FFF5DD");
            outline.effectDistance = new Vector2(3, -3);
            UIManager ui = canvasObject.AddComponent<UIManager>();
            ui.Configure(clear);
            ui.SetCleared(false);
            return ui;
        }

        private static Text Label(string name, Transform parent, string content, int size, Font font, Color color)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void CreateSupport(Transform parent, Transform ramp, float localX, Material material)
        {
            Vector3 surface = ramp.TransformPoint(new Vector3(localX, -0.12f, 0));
            Cube("Ramp support", parent, new Vector3(surface.x, surface.y / 2, 0), new Vector3(0.45f, surface.y, 1.2f), material);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Material Material(string name, string hex)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP/Lit is required for this project.");
            }
            Material material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", Hex(hex));
            material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, "Assets/Materials/" + name + ".mat");
            return material;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
