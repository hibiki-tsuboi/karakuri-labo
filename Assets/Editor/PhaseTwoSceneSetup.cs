using System;
using System.Linq;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class PhaseTwoSceneSetup
    {
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Karakuri Labo/Set Up Phase 2 Editing")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity")
            {
                throw new InvalidOperationException("Open Main in Edit Mode before applying Phase 2 setup.");
            }

            UpgradeRampPrefab();
            Transform ramp = GameObject.Find("Ramp").transform;
            Transform stage = GameObject.Find("Stage").transform;
            foreach (Transform support in stage.Cast<Transform>().Where(child => child.name == "Ramp support").ToArray())
            {
                // Preserve the vertical supports while making them follow ramp translation and yaw.
                Undo.SetTransformParent(support, ramp, "Attach ramp supports");
            }

            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            if (placement == null)
            {
                placement = new GameObject("PlacementManager").AddComponent<PlacementManager>();
            }
            placement.Configure(manager, Camera.main, new Vector2(-4.8f, -1.7f), new Vector2(4.8f, 1.7f));
            EditorUtility.SetDirty(placement);

            GameObject hud = GameObject.Find("HUD");
            GetOrAdd<GraphicRaycaster>(hud);
            RectTransform safeArea = GetRect(hud.transform, "SafeArea");
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            GetOrAdd<SafeAreaPanel>(safeArea.gameObject);
            MoveLabel(hud.transform, safeArea, "Title", new Vector2(48, -30));
            MoveLabel(hud.transform, safeArea, "StageLabel", new Vector2(49, -73));
            Text hint = MoveLabel(hud.transform, safeArea, "GoalHint", new Vector2(48, 42));
            Place(hint.rectTransform, Vector2.zero, Vector2.zero, new Vector2(48, 42), new Vector2(700, 44));
            hint.alignment = TextAnchor.MiddleLeft;
            hint.fontSize = 17;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text mode = Label(safeArea, "ModeLabel", "EDIT MODE", 19, font, new Color32(52, 68, 95, 255));
            Place(mode.rectTransform, Vector2.one, Vector2.one, new Vector2(-48, -38), new Vector2(240, 42));
            mode.alignment = TextAnchor.MiddleRight;
            mode.fontStyle = FontStyle.Bold;

            RectTransform rotateRect = GetRect(safeArea, "RotateButton");
            Place(rotateRect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-48, 32), new Vector2(250, 68));
            Image background = GetOrAdd<Image>(rotateRect.gameObject);
            background.color = new Color32(52, 76, 112, 255);
            background.raycastTarget = true;
            Button rotate = GetOrAdd<Button>(rotateRect.gameObject);
            rotate.targetGraphic = background;
            ColorBlock colors = rotate.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.35f);
            rotate.colors = colors;
            rotate.navigation = new Navigation { mode = Navigation.Mode.None };
            Text rotateLabel = Label(rotateRect, "Label", "ROTATE +15°", 23, font, Color.white);
            rotateLabel.alignment = TextAnchor.MiddleCenter;
            rotateLabel.fontStyle = FontStyle.Bold;
            rotateLabel.rectTransform.anchorMin = Vector2.zero;
            rotateLabel.rectTransform.anchorMax = Vector2.one;
            rotateLabel.rectTransform.offsetMin = rotateLabel.rectTransform.offsetMax = Vector2.zero;

            PlacementHUD placementHud = GetOrAdd<PlacementHUD>(hud);
            placementHud.Configure(manager, placement, rotate, hint, mode);
            EditorUtility.SetDirty(placementHud);
            EditorUtility.SetDirty(rotate);

            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }
            InputSystemUIInputModule module = GetOrAdd<InputSystemUIInputModule>(eventSystem.gameObject);
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            module.point = Action("UI/Point");
            module.leftClick = Action("UI/Click");
            module.move = Action("UI/Navigate");
            module.submit = Action("UI/Submit");
            module.cancel = Action("UI/Cancel");
            module.scrollWheel = Action("UI/ScrollWheel");
            module.rightClick = Action("UI/RightClick");
            module.middleClick = Action("UI/MiddleClick");
            EditorUtility.SetDirty(module);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 2 ready: enter Play Mode, drag the ramp, then use ROTATE +15°.");
        }

        private static InputActionReference Action(string name)
        {
            InputAction action = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath).FindAction(name, true);
            return AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .First(reference => reference.action != null && reference.action.id == action.id);
        }

        private static void UpgradeRampPrefab()
        {
            const string path = "Assets/Prefabs/Ramp/Ramp.prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                DraggableObject draggable = GetOrAdd<DraggableObject>(prefab);
                Transform existing = prefab.transform.Find("SelectionOutline");
                GameObject outline = existing != null ? existing.gameObject : new GameObject("SelectionOutline");
                outline.transform.SetParent(prefab.transform, false);
                LineRenderer line = GetOrAdd<LineRenderer>(outline);
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 4;
                line.SetPositions(new[]
                {
                    new Vector3(-3.66f, 0.36f, -0.92f), new Vector3(3.66f, 0.36f, -0.92f),
                    new Vector3(3.66f, 0.36f, 0.92f), new Vector3(-3.66f, 0.36f, 0.92f)
                });
                line.widthMultiplier = 0.055f;
                line.numCornerVertices = 3;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                const string materialPath = "Assets/Materials/Selection_Gold.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    material.SetColor("_BaseColor", new Color32(255, 202, 82, 255));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                line.sharedMaterial = material;
                draggable.Configure(outline);
                draggable.SetSelected(false);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static Text MoveLabel(Transform originalParent, Transform safeArea, string name, Vector2 position)
        {
            Transform label = originalParent.Find(name) ?? safeArea.Find(name);
            label.SetParent(safeArea, false);
            label.GetComponent<RectTransform>().anchoredPosition = position;
            return label.GetComponent<Text>();
        }

        private static RectTransform GetRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text Label(Transform parent, string name, string text, int size, Font font, Color color)
        {
            Text label = GetOrAdd<Text>(GetRect(parent, name).gameObject);
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
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
