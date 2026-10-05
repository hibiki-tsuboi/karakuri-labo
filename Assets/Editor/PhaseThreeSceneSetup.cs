using System;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class PhaseThreeSceneSetup
    {
        [MenuItem("Karakuri Labo/Set Up Phase 3 Play and Reset")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity")
            {
                throw new InvalidOperationException("Open Main in Edit Mode before applying Phase 3 setup.");
            }

            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud != null ? hud.transform.Find("SafeArea") : null;
            if (safeArea == null || safeArea.Find("RotateButton") == null)
            {
                throw new InvalidOperationException("Apply Phase 2 Editing before Phase 3 setup.");
            }

            foreach (string part in new[] { "Ball", "Ramp", "Goal" })
            {
                string path = $"Assets/Prefabs/{part}/{part}.prefab";
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    GetOrAdd<PhysicsObject>(prefab);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            manager.ConfigureResetObjects(
                GameObject.Find("Ball").GetComponent<PhysicsObject>(),
                GameObject.Find("Ramp").GetComponent<PhysicsObject>(),
                GameObject.Find("Goal").GetComponent<PhysicsObject>());
            EditorUtility.SetDirty(manager);

            var rotate = safeArea.Find("RotateButton").GetComponent<RectTransform>();
            Place(rotate, new Vector2(-284, 32), new Vector2(220, 68));

            Transform existing = safeArea.Find("SimulationButton");
            GameObject buttonObject = existing != null ? existing.gameObject :
                new GameObject("SimulationButton", typeof(RectTransform));
            buttonObject.transform.SetParent(safeArea, false);
            Place(buttonObject.GetComponent<RectTransform>(), new Vector2(-48, 32), new Vector2(220, 68));
            Image background = GetOrAdd<Image>(buttonObject);
            background.raycastTarget = true;
            Button button = GetOrAdd<Button>(buttonObject);
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = rotate.GetComponent<Button>().colors;

            Transform existingLabel = buttonObject.transform.Find("Label");
            GameObject labelObject = existingLabel != null ? existingLabel.gameObject :
                new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            Text label = GetOrAdd<Text>(labelObject);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 25;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

            SimulationHUD simulationHud = GetOrAdd<SimulationHUD>(hud);
            simulationHud.Configure(manager, button, label);
            EditorUtility.SetDirty(simulationHud);
            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 3 ready: PLAY starts physics; RESET restores the layout saved immediately before PLAY.");
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
