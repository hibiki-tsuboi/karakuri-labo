using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KarakuriLabo.Tests
{
    public class JapaneseUITests
    {
        private static readonly string[] Scenes = { "StageOne", "StageThree", "StageFour", "StageFive",
            "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };

        [UnityTest]
        public IEnumerator EveryStageUsesBundledJapaneseGlyphs_AndFitsCompactLandscapeInEveryUIState()
        {
            foreach (string scene in Scenes)
            {
                yield return TestSceneLoader.Load(scene);
                yield return null;
                var safe = Object.FindAnyObjectByType<SafeAreaPanel>();
                safe.enabled = false;
                RectTransform rect = safe.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(1000, 540);
                var manager = Object.FindAnyObjectByType<GameManager>();
                var audio = Object.FindAnyObjectByType<AudioManager>();
                var menu = Object.FindAnyObjectByType<StageSelectHUD>();
                yield return null;
                CheckVisibleText(scene);
                Assert.That(menu.TryOpen(), Is.True);
                yield return null;
                CheckVisibleText(scene);
                menu.Close();
                audio.SetMuted(true);
                yield return null;
                CheckVisibleText(scene);
                audio.SetMuted(false);
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                for (int slot = 0; slot < 3; slot++)
                {
                    if (!spawner.CanAddPart(slot)) continue;
                    DraggableObject part = spawner.AddPart(slot);
                    Assert.That(Regex.IsMatch(part.DisplayName, "[A-Za-z]"), Is.False, scene);
                    yield return null;
                    CheckVisibleText(scene);
                }
                foreach (AttemptFailure reason in new[] { AttemptFailure.Stopped, AttemptFailure.Fell,
                    AttemptFailure.GoalRequirement, AttemptFailure.TimedOut })
                {
                    manager.StartSimulation();
                    yield return null;
                    CheckVisibleText(scene);
                    manager.FailAttempt(reason);
                    yield return null;
                    CheckVisibleText(scene);
                    manager.ResetSimulation();
                }
                // The clear label is normally disabled until a real goal event.
                // Existing campaign tests cover natural clears; here check its glyphs and size.
                var clear = Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(t => t.name == "ClearText");
                clear.enabled = true;
                yield return null;
                CheckVisibleText(scene);
                clear.enabled = false;
                // Hidden menu/next/retry labels must also be translated before first display.
                foreach (Text label in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
                    CheckGlyphs(label, scene);
            }
        }

        private static void CheckVisibleText(string scene)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text label in Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude))
            {
                if (!label.enabled || string.IsNullOrEmpty(label.text)) continue;
                CheckGlyphs(label, scene);
                string context = scene + "/" + label.transform.parent.name + "/" + label.name + ": " + label.text;
                Assert.That(label.cachedTextGenerator.characterCountVisible, Is.EqualTo(label.text.Length),
                    context + " must actually render every character, including the title.");
                Assert.That(label.resizeTextForBestFit, Is.False, context);
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(15), context);
                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(label.rectTransform.rect.width + 2), context + " width");
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 0.01f), context + " height");
            }
        }

        private static void CheckGlyphs(Text label, string scene)
        {
            string context = scene + "/" + label.name + ": " + label.text;
            Assert.That(Regex.IsMatch(label.text, "[A-Za-z\\u4e00-\\u9fff]"), Is.False,
                context + " must use kana, familiar punctuation and numerals.");
            Assert.That(label.font, Is.Not.Null, context);
            Assert.That(label.font.name, Does.StartWith("ZenMaruGothic"), context);
            label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
            foreach (char character in label.text.Where(c => !char.IsWhiteSpace(c)).Distinct())
                Assert.That(label.font.HasCharacter(character), Is.True, context + " missing " + character);
        }
    }
}
