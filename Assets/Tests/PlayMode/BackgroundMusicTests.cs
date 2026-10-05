using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KarakuriLabo.Tests
{
    public class BackgroundMusicTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Leave the campaign first to remove its persistent music and isolate mute state.
            yield return TestSceneLoader.Load("Main");
            yield return null;
            yield return SceneManager.LoadSceneAsync("StageOne");
            yield return new WaitForSecondsRealtime(0.8f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return TestSceneLoader.Load("Main");
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoopStreamsQuietly_AndContinuesThroughPlayClearAndReset()
        {
            AudioSource music = MusicSource();
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.loop, Is.True);
            Assert.That(music.playOnAwake, Is.False);
            Assert.That(music.spatialBlend, Is.Zero);
            Assert.That(music.volume, Is.EqualTo(0.24f).Within(0.001f));
            Assert.That(music.clip.length, Is.EqualTo(80f).Within(0.01f));
            Assert.That(music.clip.channels, Is.EqualTo(2));
            Assert.That(music.clip.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(Object.FindAnyObjectByType<GameManager>().State, Is.EqualTo(GameState.Edit));
            float before = music.time;
            yield return ClearCurrentStage();
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.time, Is.GreaterThanOrEqualTo(before));
            var audio = Object.FindAnyObjectByType<AudioManager>();
            Assert.That(audio.GetComponent<AudioSource>().isPlaying, Is.True, "CLEAR still has its separate chime.");
            Object.FindAnyObjectByType<GameManager>().ResetSimulation();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.time, Is.GreaterThan(before));
            Assert.That(audio.GetComponent<AudioSource>().isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator SoundButtonMutesBothVoices_AndUnmuteContinuesWithoutReplayingChime()
        {
            var audio = Object.FindAnyObjectByType<AudioManager>();
            var button = Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b => b.name == "SoundButton");
            var label = button.GetComponentInChildren<Text>();
            AudioSource music = MusicSource();
            audio.PlayClear();
            float before = music.time;
            button.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(music.mute, Is.True);
            Assert.That(label.text, Is.EqualTo("おと：なし"));
            Assert.That(audio.GetComponent<AudioSource>().isPlaying, Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(music.time, Is.GreaterThan(before), "Muting keeps the music timeline running.");
            before = music.time;
            button.onClick.Invoke();
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(music.mute, Is.False);
            Assert.That(label.text, Is.EqualTo("おと：あり"));
            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(music.time, Is.GreaterThan(before));
            Assert.That(music.volume, Is.EqualTo(0.24f).Within(0.001f));
            Assert.That(audio.GetComponent<AudioSource>().isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator NextRetainsOneMusicVoiceAndPosition_AndNeverUnmutesDuringLoad()
        {
            AudioSource music = MusicSource();
            foreach (bool muted in new[] { false, true })
            {
                Object.FindAnyObjectByType<AudioManager>().SetMuted(muted);
                yield return ClearCurrentStage();
                float before = music.time;
                string destination = muted ? "StageFour" : "StageThree";
                Assert.That(Object.FindAnyObjectByType<StageManager>().TryAdvance(), Is.True);
                float deadline = Time.realtimeSinceStartup + 5;
                while (SceneManager.GetActiveScene().name != destination && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(music.mute, Is.EqualTo(muted));
                    yield return null;
                }
                yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(destination));
                Assert.That(MusicSource(), Is.SameAs(music));
                Assert.That(music.isPlaying, Is.True);
                Assert.That(music.time, Is.GreaterThanOrEqualTo(before));
                Assert.That(music.mute, Is.EqualTo(muted));
                Assert.That(Object.FindAnyObjectByType<AudioManager>().IsMuted, Is.EqualTo(muted));
                Assert.That(Object.FindObjectsByType<BackgroundMusicPlayer>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator AppPauseAndDisabledOwnerSuspendMusic_ThenResumeFromItsPosition()
        {
            var player = Object.FindAnyObjectByType<BackgroundMusicPlayer>();
            var audio = Object.FindAnyObjectByType<AudioManager>();
            AudioSource music = MusicSource();
            foreach (bool appPause in new[] { true, false })
            {
                float before = music.time;
                if (appPause)
                {
                    player.SendMessage("OnApplicationPause", true);
                }
                else
                {
                    audio.enabled = false;
                }
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(music.isPlaying, Is.False);
                Assert.That(music.time, Is.EqualTo(before).Within(0.08f));
                if (appPause)
                {
                    player.SendMessage("OnApplicationPause", false);
                }
                else
                {
                    audio.enabled = true;
                }
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(music.isPlaying, Is.True);
                Assert.That(music.time, Is.GreaterThan(before));
            }
        }

        [UnityTest]
        public IEnumerator LoopWrapsWithoutStopping_AndLegacyScenesReleaseTheMusicPlayer()
        {
            AudioSource music = MusicSource();
            music.time = music.clip.length - 0.15f;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.time, Is.InRange(0f, 1f));
            yield return TestSceneLoader.Load("Main");
            yield return null;
            Assert.That(Object.FindAnyObjectByType<BackgroundMusicPlayer>(), Is.Null);
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
        }

        private static AudioSource MusicSource() => Object.FindAnyObjectByType<BackgroundMusicPlayer>().GetComponent<AudioSource>();

        private static IEnumerator ClearCurrentStage()
        {
            var manager = Object.FindAnyObjectByType<GameManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            spawner.AddPart(0).transform.position = SceneManager.GetActiveScene().name == "StageOne"
                ? new Vector3(-4.15f, 1.5f, 0) : new Vector3(-1.4f, 0, 0);
            SimulationMode mode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                manager.StartSimulation();
                for (int i = 0; i < 650 && manager.State != GameState.Clear; i++)
                {
                    Physics.Simulate(0.02f);
                }
            }
            finally
            {
                Physics.simulationMode = mode;
            }
            yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
        }
    }
}
