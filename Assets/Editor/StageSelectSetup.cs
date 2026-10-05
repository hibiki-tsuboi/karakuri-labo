using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    internal static class StageSelectSetup
    {
        internal static void Configure(Transform hud, string[] scenes, string[] titles)
        {
            Transform safe = hud.Find("SafeArea");
            var controller = hud.GetComponent<StageSelectHUD>();
            if (controller == null) controller = hud.gameObject.AddComponent<StageSelectHUD>();
            controller.enabled = false;
            foreach (string name in new[] { "StagesButton", "StageMenu" })
            {
                Transform old = safe.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Button template = safe.Find("SoundButton").GetComponent<Button>();
            Button open = CopyButton(template, safe, "StagesButton", "STAGES", new Vector2(0, 1), new Vector2(24, -104), new Vector2(176, 48));
            RectTransform overlay = new GameObject("StageMenu", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            overlay.SetParent(safe, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.10f, 0.20f, 0.22f, 0.70f);
            RectTransform panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(overlay, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one * 0.5f;
            panel.sizeDelta = new Vector2(840, 438);
            Image background = panel.GetComponent<Image>();
            background.sprite = template.GetComponent<Image>().sprite;
            background.type = Image.Type.Sliced;
            background.color = new Color32(249, 242, 228, 255);
            Button close = CopyButton(template, panel, "CloseStagesButton", "CLOSE", Vector2.one * 0.5f, new Vector2(318, 174), new Vector2(144, 48));
            Text title = Object.Instantiate(template.GetComponentInChildren<Text>(), panel);
            title.name = "StageMenuTitle";
            title.text = "CHOOSE A STAGE";
            title.alignment = TextAnchor.MiddleLeft;
            title.fontSize = 26;
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = Vector2.one * 0.5f;
            title.rectTransform.anchoredPosition = new Vector2(-110, 174);
            title.rectTransform.sizeDelta = new Vector2(550, 48);
            Button[] choices = new Button[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                choices[i] = CopyButton(template, panel, "StageChoice" + i, $"{i + 1:00}  /  {titles[i]}",
                    Vector2.one * 0.5f, new Vector2((i % 3 - 1) * 266, 77 - i / 3 * 100), new Vector2(248, 84));
                choices[i].GetComponentInChildren<Text>().fontSize = 17;
                choices[i].GetComponent<Image>().color = i >= 4 ? new Color32(193, 218, 212, 255) : new Color32(238, 226, 206, 255);
            }
            controller.Configure(Object.FindAnyObjectByType<GameManager>(), Object.FindAnyObjectByType<PlacementManager>(),
                open, close, overlay.gameObject, choices, scenes.Select(n => $"Assets/Scenes/{n}.unity").ToArray());
            controller.enabled = true;
            EditorUtility.SetDirty(controller);
        }

        private static Button CopyButton(Button source, Transform parent, string name, string label,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            Button button = Object.Instantiate(source, parent);
            button.name = name;
            button.onClick = new Button.ButtonClickedEvent();
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            button.GetComponentInChildren<Text>().text = label;
            return button;
        }
    }
}
