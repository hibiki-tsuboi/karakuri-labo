using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class JapaneseSceneSetup
    {
        private const string FontRoot = "Assets/Art/Fonts/ZenMaruGothic/";
        private static readonly string[] Campaign = { "StageOne", "StageThree", "StageFour", "StageFive",
            "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };

        [MenuItem("Karakuri Labo/Apply Japanese UI")]
        public static void Apply()
        {
            if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Stop Play and save scenes before applying Japanese UI.");

            AssetDatabase.Refresh();
            // Include font data in the player; never rely on fonts installed on the Mac.
            foreach (string weight in new[] { "Medium", "Bold" })
            {
                var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(FontRoot + $"ZenMaruGothic-{weight}.ttf");
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string name in Campaign.Concat(new[] { "Main", "StageTwo" }))
                {
                    string path = $"Assets/Scenes/{name}.unity";
                    if (!File.Exists(path)) continue;
                    Scene scene = EditorSceneManager.OpenScene(path);
                    Configure(GameObject.Find("HUD").transform);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException($"Could not save {path}.");
                }
                PlayerSettings.productName = "からくりラボ";
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        internal static void Configure(Transform hud)
        {
            Font regular = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "ZenMaruGothic-Medium.ttf");
            Font bold = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "ZenMaruGothic-Bold.ttf");
            if (regular == null || bold == null) throw new InvalidOperationException("Japanese fonts are missing.");
            string scene = hud.gameObject.scene.name;
            int stage = Array.IndexOf(Campaign, scene);
            Transform safe = hud.Find("SafeArea");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var placement = hud.GetComponent<PlacementHUD>();
            placement.ConfigureHints(GameText.StageHint(scene, false), scene == "Main" ? "" : GameText.StageHint(scene, true));
            placement.ConfigureClearHint(scene == "StageFan" ? GameText.AllClearHint : GameText.ClearHint);
            EditorUtility.SetDirty(placement);

            foreach (Text text in hud.GetComponentsInChildren<Text>(true))
            {
                string parent = text.transform.parent.name;
                bool button = text.GetComponentInParent<Button>(true) != null;
                text.font = button || text.name == "Title" || text.name == "ClearText" || text.name == "FailureTitle" ? bold : regular;
                text.fontStyle = FontStyle.Normal;
                text.resizeTextForBestFit = false;
                text.lineSpacing = 1.1f;
                if (button)
                {
                    text.fontSize = parent.StartsWith("Add") ? (scene == "StageFive" ? 18 : 20) : 20;
                    if (parent == "SimulationButton" || parent == "NextButton" || parent == "RetryAttemptButton") text.fontSize = 26;
                    text.text = ButtonText(parent, scene, text.text);
                }
                else
                {
                    switch (text.name)
                    {
                        case "Title":
                            text.text = "からくりラボ";
                            text.fontSize = 30;
                            text.rectTransform.anchoredPosition = new Vector2(82, -20);
                            text.rectTransform.sizeDelta = new Vector2(500, 48);
                            break;
                        case "K": text.text = "か"; text.font = bold; text.fontSize = 24; break;
                        case "StageLabel":
                            text.text = stage < 0 ? GameText.StageTitle(scene) : $"ステージ {stage + 1}　{GameText.StageTitle(scene)}";
                            text.fontSize = 18;
                            text.rectTransform.anchoredPosition = new Vector2(83, -72);
                            text.color = new Color32(60, 98, 101, 255);
                            break;
                        case "ModeLabel": text.text = GameText.EditMode; text.fontSize = 18; break;
                        case "ClearText": text.text = GameText.Clear; text.fontSize = 68; break;
                        case "GoalHint": text.text = GameText.StageHint(scene, false); text.fontSize = 19; break;
                        case "FailureTitle":
                            text.text = GameText.TryAgain;
                            text.fontSize = 32;
                            text.rectTransform.sizeDelta = new Vector2(376, 56);
                            break;
                        case "FailureReason": text.text = ""; text.fontSize = 20; break;
                        case "StageMenuTitle": text.text = "あそぶ ステージを えらぼう"; text.fontSize = 24; break;
                        case "SeesawProgress": text.text = "シーソー 0 / 1"; text.fontSize = 18; break;
                        case "DominoProgress": text.text = "ドミノ 0 / 3 ／ とびら"; text.fontSize = 18; break;
                        case "CameraHint":
                            text.text = GameText.CameraHint + (scene == "StageFive" ? "\nばしょの ボタンで いどう" : "");
                            text.fontSize = 15;
                            text.color = new Color32(60, 98, 101, 255);
                            text.rectTransform.sizeDelta = new Vector2(240, scene == "StageFive" ? 104 : 80);
                            break;
                    }
                }
                EditorUtility.SetDirty(text);
            }

            // Translate the serialized base labels as well as Text. The palette
            // regenerates its inventory labels on every frame and after resets.
            var palette = hud.GetComponent<PartPaletteHUD>();
            if (palette != null)
            {
                var data = new SerializedObject(palette);
                var buttons = data.FindProperty("addButtons");
                var labels = data.FindProperty("buttonLabels");
                labels.arraySize = buttons.arraySize;
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                for (int i = 0; i < buttons.arraySize; i++)
                {
                    var button = (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                    string label = ButtonText(button.name, scene, "どうぐ");
                    labels.GetArrayElementAtIndex(i).stringValue = label;
                    int remaining = spawner.GetRemainingCount(i);
                    button.GetComponentInChildren<Text>(true).text = label + (remaining < 0 ? "" : $"\nあと {remaining}こ");
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(palette);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)safe);
        }

        private static string ButtonText(string name, string scene, string fallback)
        {
            if (name.StartsWith("StageChoice") && int.TryParse(name.Substring(11), out int index) && index < Campaign.Length)
                return $"{index + 1}\n{GameText.StageTitle(Campaign[index])}";
            switch (name)
            {
                case "SimulationButton": return GameText.Play;
                case "RetryAttemptButton": return GameText.Retry;
                case "NextButton": return "つぎへ";
                case "RotateButton": return "まわす";
                case "DeleteButton": return "しまう";
                case "ResetViewButton": return "みかたを もどす";
                case "SoundButton": return GameText.SoundOn;
                case "StagesButton": return "ステージ";
                case "CloseStagesButton": return "とじる";
                case "FollowBallButton": return "ボールを みる";
                case "AreaView0": return "うえ";
                case "AreaView1": return "なか";
                case "AreaView2": return "した";
                case "AreaView3": return "ぜんたい";
                case "AddRampButton": return "さか";
                case "AddSeesawButton": return "シーソー";
                case "AddDominoButton": return "ドミノ";
                case "AddHighRampButton": return "たかい さか";
                case "AddLowRampButton": return "ひくい さか";
                case "AddUpperRampButton": return "うえの さか";
                case "AddMiddleRampButton": return "なかの さか";
                case "AddLowerRampButton": return "したの さか";
                case "AddToyButton":
                    switch (scene)
                    {
                        case "StageSpring": return "ばね";
                        case "StageCurve": return "カーブ";
                        case "StageFunnel": return "じょうご";
                        case "StageLift": return "リフト";
                        case "StageFan": return "せんぷうき";
                    }
                    break;
            }
            return fallback;
        }
    }
}
