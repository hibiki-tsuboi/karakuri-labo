using System;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class PhaseSixSceneSetup
    {
        private const string CatalogFolder = "Assets/Prefabs/Placement";

        [MenuItem("Karakuri Labo/Set Up Phase 6 Part Controls")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud != null ? hud.transform.Find("SafeArea") : null;
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            PlacementManager placement = Object.FindAnyObjectByType<PlacementManager>();
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity" ||
                safeArea == null || manager == null || placement == null)
            {
                throw new InvalidOperationException("Open the Phase 5 Main scene in Edit Mode first.");
            }

            // Copy authored dimensions, supports, heights and rotations. Existing
            // catalog assets and every object in the playable layout are preserved.
            if (!AssetDatabase.IsValidFolder(CatalogFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Placement");
            }
            DraggableObject[] catalog =
            {
                CatalogPart("Ramp", GameObject.Find("Ramp")),
                CatalogPart("Domino", GameObject.Find("Dominos/Domino 1")),
                CatalogPart("Seesaw", GameObject.Find("Seesaw"))
            };
            PartSpawner spawner = Object.FindAnyObjectByType<PartSpawner>();
            if (spawner == null)
            {
                spawner = new GameObject("PartSpawner").AddComponent<PartSpawner>();
            }
            spawner.Configure(manager, placement, catalog);
            EditorUtility.SetDirty(spawner);

            RectTransform toolbar = GetRect(safeArea, "PartToolbar");
            toolbar.anchorMin = Vector2.zero;
            toolbar.anchorMax = Vector2.right;
            toolbar.pivot = new Vector2(0.5f, 0);
            toolbar.offsetMin = new Vector2(24, 18);
            toolbar.offsetMax = new Vector2(-24, 118);
            Image backdrop = GetOrAdd<Image>(toolbar.gameObject);
            backdrop.color = new Color(1, 1, 1, 0.3f);
            backdrop.raycastTarget = true;
            HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(toolbar.gameObject);
            layout.padding = new RectOffset();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childScaleWidth = layout.childScaleHeight = false;

            Button addRamp = ConfigureButton(toolbar, "AddRampButton", "+ RAMP", 144,
                new Color32(71, 104, 155, 255), 21);
            Button addDomino = ConfigureButton(toolbar, "AddDominoButton", "+ DOMINO", 144,
                new Color32(143, 99, 44, 255), 21);
            Button addSeesaw = ConfigureButton(toolbar, "AddSeesawButton", "+ SEESAW", 144,
                new Color32(105, 86, 143, 255), 21);
            RectTransform space = GetRect(toolbar, "FlexibleSpace");
            LayoutElement spacer = GetOrAdd<LayoutElement>(space.gameObject);
            spacer.minWidth = spacer.preferredWidth = 0;
            spacer.flexibleWidth = 1;
            space.SetAsLastSibling();

            MoveExistingButton(safeArea, toolbar, "RotateButton");
            Button rotate = ConfigureButton(toolbar, "RotateButton", "ROTATE +15°", 150,
                new Color32(52, 76, 112, 255), 19);
            Button delete = ConfigureButton(toolbar, "DeleteButton", "DELETE", 112,
                new Color32(159, 71, 62, 255), 21);
            MoveExistingButton(safeArea, toolbar, "SimulationButton");
            Button simulation = ConfigureButton(toolbar, "SimulationButton", "PLAY", 176,
                new Color32(37, 112, 88, 255), 25);

            Text hint = safeArea.Find("GoalHint").GetComponent<Text>();
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.right;
            hint.rectTransform.pivot = Vector2.zero;
            hint.rectTransform.offsetMin = new Vector2(24, 132);
            hint.rectTransform.offsetMax = new Vector2(-24, 166);
            hint.fontSize = 21;
            hint.alignment = TextAnchor.MiddleLeft;
            hint.raycastTarget = false;
            Text mode = safeArea.Find("ModeLabel").GetComponent<Text>();
            PlacementHUD placementHud = GetOrAdd<PlacementHUD>(hud);
            placementHud.Configure(manager, placement, rotate, hint, mode, delete);
            PartPaletteHUD palette = GetOrAdd<PartPaletteHUD>(hud);
            palette.Configure(manager, spawner, new[] { addRamp, addDomino, addSeesaw });
            GetOrAdd<SimulationHUD>(hud).Configure(manager, simulation,
                simulation.transform.Find("Label").GetComponent<Text>());

            Transform clear = hud.transform.Find("ClearText");
            if (clear != null)
            {
                clear.SetParent(safeArea, false);
            }
            Text stageLabel = safeArea.Find("StageLabel").GetComponent<Text>();
            stageLabel.text = "01   /   BUILD YOUR CHAIN";
            EditorUtility.SetDirty(stageLabel);
            EditorUtility.SetDirty(placementHud);
            EditorUtility.SetDirty(palette);
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 6 ready: add a ramp, domino or seesaw; select, drag, rotate and delete in Edit Mode.");
        }

        private static DraggableObject CatalogPart(string name, GameObject source)
        {
            string path = $"{CatalogFolder}/Placeable{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing.GetComponent<DraggableObject>();
            }
            if (source == null || source.GetComponent<DraggableObject>() == null)
            {
                throw new InvalidOperationException($"The Main scene needs a draggable {name} before creating its catalog entry.");
            }

            GameObject copy = Object.Instantiate(source);
            try
            {
                if (PrefabUtility.IsPartOfPrefabInstance(copy))
                {
                    PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }
                copy.name = $"Placeable{name}";
                copy.transform.SetPositionAndRotation(new Vector3(0, source.transform.position.y, 0),
                    source.transform.rotation);
                copy.GetComponent<DraggableObject>().SetSelected(false);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path);
                return saved.GetComponent<DraggableObject>();
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        private static void MoveExistingButton(Transform safeArea, Transform toolbar, string name)
        {
            Transform existing = safeArea.Find(name);
            if (existing != null)
            {
                existing.SetParent(toolbar, false);
            }
        }

        private static Button ConfigureButton(Transform parent, string name, string text, float width,
            Color color, int fontSize)
        {
            RectTransform rect = GetRect(parent, name);
            rect.SetAsLastSibling();
            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = color;
            image.raycastTarget = true;
            Button button = GetOrAdd<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1),
                pressedColor = new Color(0.8f, 0.8f, 0.8f, 1),
                selectedColor = Color.white,
                disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.4f),
                colorMultiplier = 1,
                fadeDuration = 0.1f
            };
            LayoutElement size = GetOrAdd<LayoutElement>(rect.gameObject);
            size.minWidth = size.preferredWidth = width;
            size.flexibleWidth = 0;
            size.minHeight = size.preferredHeight = 100;
            size.flexibleHeight = 0;
            Text label = GetOrAdd<Text>(GetRect(rect, "Label").gameObject);
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(4, 0);
            label.rectTransform.offsetMax = new Vector2(-4, 0);
            return button;
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

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
