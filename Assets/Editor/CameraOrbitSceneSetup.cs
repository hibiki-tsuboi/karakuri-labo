using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class CameraOrbitSceneSetup
    {
        [MenuItem("Karakuri Labo/Set Up Swipe Camera")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before setting up the camera.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before setting up the camera.");
                }
            }
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in new[] { "Main", "StageOne", "StageTwo", "StageThree", "StageFour", "StageFive",
                    "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" })
                {
                    string path = $"Assets/Scenes/{name}.unity";
                    if (!File.Exists(path))
                    {
                        continue;
                    }
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    StageCameraOrbit existing = Camera.main.GetComponent<StageCameraOrbit>();
                    Vector3 fallback = name == "StageFive" ? new Vector3(-1.1f, 4.9f, 4.7f) :
                        name == "StageFour" ? new Vector3(-0.5f, 0.2f, -0.7f) : new Vector3(0, 0.8f, 0);
                    ConfigureScene(existing != null ? existing.HomeFocus : fallback);
                    JapaneseSceneSetup.Configure(GameObject.Find("HUD").transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException($"Could not save {path}.");
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
            Debug.Log("Swipe empty space to orbit; VIEW RESET restores the starting view.");
        }

        public static void ConfigureScene(Vector3 focus)
        {
            Camera camera = Camera.main;
            StageCameraOrbit orbit = GetOrAdd<StageCameraOrbit>(camera.gameObject);
            orbit.Configure(focus);
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            placement.ConfigureOrbit(orbit);
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud.transform.Find("SafeArea");
            Transform existing = safeArea.Find("ResetViewButton");
            GameObject buttonObject = existing != null ? existing.gameObject :
                new GameObject("ResetViewButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(safeArea, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24, -206);
            rect.sizeDelta = new Vector2(176, 68);
            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color32(52, 76, 112, 255);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text label = Label(rect, "Label");
            label.text = "VIEW RESET";
            label.fontSize = 21;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            Text hint = Label(safeArea, "CameraHint");
            hint.text = "1 FINGER: ROTATE\n2 FINGERS: PAN / ZOOM";
            hint.fontSize = 16;
            hint.alignment = TextAnchor.UpperRight;
            hint.color = new Color32(52, 68, 95, 255);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = Vector2.one;
            hint.rectTransform.anchoredPosition = new Vector2(-24, -282);
            hint.rectTransform.sizeDelta = new Vector2(200, 48);
            CameraHUD controls = GetOrAdd<CameraHUD>(hud);
            controls.Configure(orbit, placement, button);
            EditorUtility.SetDirty(orbit);
            EditorUtility.SetDirty(placement);
            EditorUtility.SetDirty(controls);
        }

        private static Text Label(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject target = existing != null ? existing.gameObject :
                new GameObject(name, typeof(RectTransform), typeof(Text));
            target.transform.SetParent(parent, false);
            Text label = target.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.raycastTarget = false;
            return label;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }
    }
}
