using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class FailureSceneSetup
    {
        [MenuItem("Karakuri Labo/Set Up Attempt Failure")]
        public static void Apply()
        {
            if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Stop Play and save scenes before setting up failure handling.");

            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
                {
                    Scene scene = EditorSceneManager.OpenScene(entry.path);
                    Configure(GameObject.Find("HUD").transform);
                    JapaneseSceneSetup.Configure(GameObject.Find("HUD").transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException($"Could not save {scene.path}.");
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        internal static void Configure(Transform hud)
        {
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            BallController ball = Object.FindAnyObjectByType<BallController>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            var placementData = new SerializedObject(placement);
            Vector2 minimum = placementData.FindProperty("minimumXZ").vector2Value;
            Vector2 maximum = placementData.FindProperty("maximumXZ").vector2Value;
            // Use the course footprint, not the decorative floor spanning 200 units.
            Vector3 low = Vector3.Min(new Vector3(minimum.x, -1.5f, minimum.y), ball.transform.position);
            Vector3 high = Vector3.Max(new Vector3(maximum.x, 0f, maximum.y), ball.transform.position);
            low = Vector3.Min(low, goal.transform.position);
            high = Vector3.Max(high, goal.transform.position);
            low -= new Vector3(2.5f, 0f, 2.5f);
            high += new Vector3(2.5f, 12f, 2.5f);
            var bounds = new Bounds();
            bounds.SetMinMax(low, high);
            AttemptMonitor monitor = manager.GetComponent<AttemptMonitor>();
            if (monitor == null) monitor = manager.gameObject.AddComponent<AttemptMonitor>();
            monitor.Configure(manager, ball, bounds);
            EditorUtility.SetDirty(monitor);

            Transform safe = hud.Find("SafeArea");
            Transform toolbar = safe.Find("PartToolbar");
            FailureHUD controller = hud.GetComponent<FailureHUD>();
            if (controller == null) controller = hud.gameObject.AddComponent<FailureHUD>();
            controller.enabled = false;
            foreach (Transform old in new[] { safe.Find("FailureCard"), toolbar.Find("RetryAttemptButton") })
                if (old != null) Object.DestroyImmediate(old.gameObject);

            Button template = safe.Find("SoundButton").GetComponent<Button>();
            Button retry = Object.Instantiate(toolbar.Find("SimulationButton").GetComponent<Button>(), toolbar);
            retry.name = "RetryAttemptButton";
            retry.onClick = new Button.ButtonClickedEvent();
            retry.transform.SetSiblingIndex(toolbar.Find("FlexibleSpace").GetSiblingIndex());
            retry.GetComponentInChildren<Text>().text = "RETRY";
            var layout = retry.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 176;
            layout.flexibleWidth = 0;

            RectTransform card = new GameObject("FailureCard", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup)).GetComponent<RectTransform>();
            card.SetParent(safe, false);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0, -100);
            card.sizeDelta = new Vector2(400, 150);
            Image background = card.GetComponent<Image>();
            background.sprite = template.GetComponent<Image>().sprite;
            background.type = Image.Type.Sliced;
            background.color = new Color32(249, 242, 228, 250);
            Text title = CopyText(template, card, "FailureTitle", "TRY AGAIN", 30, -33, 46);
            title.color = new Color32(194, 83, 59, 255);
            Text reason = CopyText(template, card, "FailureReason", "", 18, -96, 70);
            controller.Configure(manager, placement, card.GetComponent<CanvasGroup>(), reason, retry);
            controller.enabled = true;
            hud.GetComponent<SimulationHUD>().ConfigurePlacement(placement);
            EditorUtility.SetDirty(hud.GetComponent<SimulationHUD>());
            EditorUtility.SetDirty(controller);
            safe.Find("StageMenu")?.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)toolbar);
        }

        private static Text CopyText(Button template, Transform parent, string name, string value,
            int size, float y, float height)
        {
            Text text = Object.Instantiate(template.GetComponentInChildren<Text>(), parent);
            text.name = name;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(376, height);
            return text;
        }
    }
}
