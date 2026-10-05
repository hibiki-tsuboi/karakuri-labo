using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    /// <summary>Authors the campaign. Prototype scenes remain editor fixtures.</summary>
    public static class CampaignSceneSetup
    {
        private static readonly string[] Scenes = { "StageOne", "StageThree", "StageFour", "StageFive",
            "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };
        private static readonly string[] Titles = { "FIRST ROLL", "BALANCE ACT", "DEPTH CROSSING", "THE LONG DESCENT",
            "A LITTLE LEAP", "AROUND THE BEND", "CATCH & DROP", "GOING UP", "A GENTLE PUSH" };

        [MenuItem("Karakuri Labo/Set Up Campaign")]
        public static void Apply()
        {
            if (Application.isPlaying || Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => SceneManager.GetSceneAt(i).isDirty))
            {
                throw new InvalidOperationException("Stop Play and save open scenes before updating the campaign.");
            }
            foreach (string name in Scenes)
            {
                if (!File.Exists($"Assets/Scenes/{name}.unity"))
                {
                    throw new InvalidOperationException($"Create {name} before configuring the campaign.");
                }
            }
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            AudioClip music = AtelierMusicComposer.EnsureClip();
            try
            {
                for (int i = 0; i < Scenes.Length; i++)
                {
                    Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{Scenes[i]}.unity");
                    AudioManager audio = Object.FindAnyObjectByType<AudioManager>();
                    audio.ConfigureMusic(music, 0.24f);
                    EditorUtility.SetDirty(audio);
                    Transform hud = GameObject.Find("HUD").transform;
                    Transform safe = hud.Find("SafeArea");
                    Transform toolbar = safe.Find("PartToolbar");
                    Transform obsolete = toolbar.Find("AddDominoButton");
                    if (obsolete != null)
                    {
                        Object.DestroyImmediate(obsolete.gameObject);
                    }
                    safe.Find("StageLabel").GetComponent<Text>().text = $"{i + 1:00}   /   {Titles[i]}";
                    bool hasNext = i + 1 < Scenes.Length;
                    hud.GetComponent<StageManager>().Configure(Object.FindAnyObjectByType<GameManager>(),
                        toolbar.Find("NextButton").GetComponent<Button>(),
                        hasNext ? $"Assets/Scenes/{Scenes[i + 1]}.unity" : string.Empty);
                    hud.GetComponent<PlacementHUD>().ConfigureClearHint(hasNext
                        ? "CLEAR!  /  NEXT TO CONTINUE" : "ALL STAGES CLEAR!  /  RESET TO PLAY AGAIN");
                    StageSelectSetup.Configure(hud, Scenes, Titles);
                    FailureSceneSetup.Configure(hud);
                    JapaneseSceneSetup.Configure(hud);
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)toolbar);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException($"Could not save {scene.path}.");
                    }
                }
                var campaign = Scenes.Select(n => new EditorBuildSettingsScene($"Assets/Scenes/{n}.unity", true)).ToArray();
                EditorBuildSettings.scenes = campaign;
                // Update every saved iOS profile, including an inactive profile with its own scene list.
                foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile"))
                {
                    var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
                    if (profile != null && new SerializedObject(profile).FindProperty("m_BuildTarget").intValue == (int)BuildTarget.iOS)
                    {
                        profile.scenes = campaign;
                        EditorUtility.SetDirty(profile);
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
            Debug.Log("Campaign ready: nine stages, including five new tools. Domino prototypes are excluded from the player.");
        }
    }
}
