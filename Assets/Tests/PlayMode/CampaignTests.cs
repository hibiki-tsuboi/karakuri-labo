using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KarakuriLabo.Tests
{
    public class CampaignTests
    {
        private static readonly string[] Scenes = { "StageOne", "StageThree", "StageFour", "StageFive",
            "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };

        [UnityTest]
        public IEnumerator PlayerIncludesNineLevels_WithoutDominoControlsOrCatalogEntries()
        {
            // Unity Test Runner adds a temporary harness scene to the live build list.
            string[] campaign = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(SceneUtility.GetScenePathByBuildIndex).Where(path => path.StartsWith("Assets/Scenes/")).ToArray();
            Assert.That(campaign.Length, Is.EqualTo(9));
            Assert.That(Application.CanStreamedLevelBeLoaded("Assets/Scenes/StageTwo.unity"), Is.False);
            Assert.That(Application.CanStreamedLevelBeLoaded("Assets/Scenes/Main.unity"), Is.False);
            for (int i = 0; i < Scenes.Length; i++)
            {
                string path = $"Assets/Scenes/{Scenes[i]}.unity";
                Assert.That(campaign[i], Is.EqualTo(path));
                yield return SceneManager.LoadSceneAsync(path);
                yield return null;
                Assert.That(Label("StageLabel").text, Does.StartWith($"{i + 1:00}   /"));
                var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
                Assert.That(transforms.Any(t => t.name.ToLowerInvariant().Contains("domino")), Is.False, path);
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                    .Any(t => t.text.ToUpperInvariant().Contains("DOMINO")), Is.False, path);
                Assert.That(Object.FindAnyObjectByType<DominoObjective>(), Is.Null);
                Assert.That(Object.FindAnyObjectByType<DominoGate>(), Is.Null);
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                int count = i == 2 ? 2 : i == 3 ? 3 : 1;
                for (int slot = 0; slot < count; slot++)
                {
                    var part = spawner.AddPart(slot);
                    Assert.That(part, Is.Not.Null);
                    Assert.That(part.DisplayName, Does.Not.Contain("DOMINO"));
                    Assert.That(part.GetComponentInChildren<DominoChainMember>(true), Is.Null);
                }
                Assert.That(spawner.CanAddPart(count), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator NaturalClearsAndNextButtons_ReachEveryLevel_ThenStopAtTheWindStage()
        {
            yield return SceneManager.LoadSceneAsync("StageOne");
            yield return null;
            Object.FindAnyObjectByType<AudioManager>().SetMuted(true);
            for (int i = 0; i < Scenes.Length; i++)
            {
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(Scenes[i]));
                Assert.That(Object.FindAnyObjectByType<AudioManager>().IsMuted, Is.True);
                var manager = Object.FindAnyObjectByType<GameManager>();
                var progression = Object.FindAnyObjectByType<StageManager>();
                var next = Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b => b.name == "NextButton");
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(next.gameObject.activeInHierarchy, Is.False);
                PlaceSolution(i);
                ToyStageTests.Simulate(manager, 16);
                // Seesaw objectives poll their actual contacts/rotation in Update.
                yield return null;
                Assert.That(manager.State, Is.EqualTo(GameState.Clear), Scenes[i]);
                bool hasNext = i + 1 < Scenes.Length;
                Assert.That(progression.CanAdvance, Is.EqualTo(hasNext));
                Assert.That(next.gameObject.activeInHierarchy, Is.EqualTo(hasNext));
                if (hasNext)
                {
                    next.onClick.Invoke();
                    float deadline = Time.realtimeSinceStartup + 5;
                    while (SceneManager.GetActiveScene().name != Scenes[i + 1] && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }
                    yield return null;
                }
                else
                {
                    Assert.That(Label("GoalHint").text, Does.Contain("ALL STAGES CLEAR"));
                    Assert.That(progression.TryAdvance(), Is.False);
                }
            }
        }

        private static Text Label(string name) => Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
            .Single(t => t.name == name);

        private static void PlaceSolution(int stage)
        {
            if (stage >= 4)
            {
                ToyStageTests.PlaceSolution(stage - 4);
                return;
            }
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            if (stage < 2)
            {
                spawner.AddPart(0).transform.position = stage == 0 ? new Vector3(-4.15f, 1.5f, 0) : new Vector3(-1.4f, 0, 0);
                return;
            }
            Vector3[] positions = stage == 2
                ? new[] { new Vector3(-2.6f, 1.6f, 1.3f), new Vector3(-0.368f, 0.68f, -0.932f) }
                : new[] { new Vector3(-0.368f, 4.36f, 3.768f), new Vector3(-0.368f, 2.52f, -2.336f),
                    new Vector3(-0.368f, 0.68f, -8.44f) };
            for (int i = 0; i < positions.Length; i++)
            {
                float yaw = stage == 2 ? (i == 0 ? 30 : 60) : (i == 1 ? 120 : 60);
                spawner.AddPart(i).transform.SetPositionAndRotation(positions[i],
                    Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -16));
            }
        }
    }
}
