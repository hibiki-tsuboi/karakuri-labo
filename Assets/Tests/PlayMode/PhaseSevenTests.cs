using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace KarakuriLabo.Tests
{
    public class PhaseSevenTests
    {
        private Touchscreen touchscreen;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (touchscreen != null && touchscreen.added)
            {
                InputSystem.RemoveDevice(touchscreen);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator NaturalClear_PlaysOneBoundedParticleBurstAndTextPop_ThenSettles()
        {
            yield return LoadMain();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var celebration = Object.FindAnyObjectByType<ClearCelebration>();
            var particles = FindParticles();
            var text = FindClearText();
            var baseScale = text.rectTransform.localScale;
            var scaleAtClear = baseScale;
            var celebratingAtClear = false;
            manager.StateChanged += state =>
            {
                if (state == GameState.Clear)
                {
                    scaleAtClear = text.rectTransform.localScale;
                    celebratingAtClear = celebration.IsCelebrating;
                }
            };
            Assert.That(text.enabled, Is.False);
            Assert.That(particles.IsAlive(true), Is.False);
            manager.StartSimulation();
            var deadline = Time.realtimeSinceStartup + 12f;
            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            Assert.That(text.enabled, Is.True);
            Assert.That(celebratingAtClear, Is.True);
            Assert.That(Vector3.Distance(scaleAtClear, baseScale), Is.GreaterThan(0.05f));
            yield return WaitForParticles(particles);
            Assert.That(particles.particleCount, Is.InRange(1, 96));
            Assert.That(particles.main.maxParticles, Is.LessThanOrEqualTo(96));
            Assert.That(particles.main.loop, Is.False);
            Assert.That(particles.emission.burstCount, Is.EqualTo(1));
            Assert.That(particles.emission.rateOverTime.constantMax, Is.EqualTo(0f));

            yield return WaitUnscaled(0.65f);
            Assert.That(celebration.IsCelebrating, Is.False);
            Assert.That(Vector3.Distance(text.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
            Assert.That(text.enabled, Is.True, "Finishing the pop must retain the CLEAR message.");
        }

        [UnityTest]
        public IEnumerator ResetDuringCelebration_ClearsEveryEffect_AndNextRoundCelebratesAgain()
        {
            yield return LoadMain();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var celebration = Object.FindAnyObjectByType<ClearCelebration>();
            var particles = FindParticles();
            var text = FindClearText();
            var source = FindAudioSource();
            var baseScale = text.rectTransform.localScale;

            for (var run = 0; run < 2; run++)
            {
                yield return EnterGoal(manager);
                yield return WaitForParticles(particles);
                Assert.That(celebration.IsCelebrating, Is.True, $"Round {run + 1} must start a fresh pop.");
                Assert.That(text.enabled, Is.True);
                manager.ResetSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                AssertEffectsStopped(celebration, particles, source, text, baseScale);
                Assert.That(text.enabled, Is.False);
                for (var step = 0; step < 3; step++)
                {
                    yield return new WaitForFixedUpdate();
                }
                AssertEffectsStopped(celebration, particles, source, text, baseScale);
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            }
        }

        [UnityTest]
        public IEnumerator ReenteringGoalWhileAlreadyClear_DoesNotRestartFinishedCelebration()
        {
            yield return LoadMain();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var celebration = Object.FindAnyObjectByType<ClearCelebration>();
            var particles = FindParticles();
            var text = FindClearText();
            var ball = Object.FindAnyObjectByType<BallController>();
            var goal = Object.FindAnyObjectByType<GoalController>();
            var entries = 0;
            goal.BallEntered += _ => entries++;
            var baseScale = text.rectTransform.localScale;
            yield return EnterGoal(manager);
            yield return WaitForParticles(particles);
            var deadline = Time.realtimeSinceStartup + 6f;
            while (particles.IsAlive(true) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(particles.IsAlive(true), Is.False, "A single burst must finish on its own.");
            Assert.That(celebration.IsCelebrating, Is.False);
            var previousEntries = entries;

            ball.Body.position = goal.transform.position + Vector3.up * 5f;
            Physics.SyncTransforms();
            for (var step = 0; step < 3; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            ball.Body.position = goal.GetComponent<BoxCollider>().bounds.center;
            Physics.SyncTransforms();
            for (var step = 0; step < 3; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(entries, Is.GreaterThan(previousEntries), "The ball must actually re-enter the trigger.");
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            Assert.That(celebration.IsCelebrating, Is.False);
            Assert.That(particles.IsAlive(true), Is.False);
            Assert.That(Vector3.Distance(text.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator TouchSoundToggle_PreservesSelectionAndMuteAcrossPlayReset_AndSuppressesAudio()
        {
            yield return LoadMain();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var audio = Object.FindAnyObjectByType<AudioManager>();
            var source = FindAudioSource();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var ramp = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Single(part => part.name == "Ramp");
            var position = ramp.transform.position;
            var rotation = ramp.transform.rotation;
            Assert.That(placement.SelectObject(ramp), Is.True);
            var button = FindSoundButton();
            var label = button.GetComponentInChildren<Text>();
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(label.text, Is.EqualTo("おと：あり"));
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            yield return Touch(141, TouchPhase.Began, ButtonPoint(button));
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            yield return Touch(141, TouchPhase.Ended, ButtonPoint(button));
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(source.mute, Is.True);
            Assert.That(label.text, Is.EqualTo("おと：なし"));
            Assert.That(ramp.transform.position, Is.EqualTo(position));
            Assert.That(Quaternion.Angle(ramp.transform.rotation, rotation), Is.LessThan(0.05f));

            audio.PlayClear();
            Assert.That(source.isPlaying, Is.False);
            yield return EnterGoal(manager);
            yield return WaitForParticles(FindParticles());
            Assert.That(source.isPlaying, Is.False, "Muted CLEAR must still show effects without starting audio.");
            manager.ResetSimulation();
            yield return null;
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(source.mute, Is.True);
            Assert.That(label.text, Is.EqualTo("おと：なし"));
            yield return Touch(142, TouchPhase.Began, ButtonPoint(button));
            yield return Touch(142, TouchPhase.Ended, ButtonPoint(button));
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(source.mute, Is.False);
            Assert.That(label.text, Is.EqualTo("おと：あり"));
            Assert.That(source.isPlaying, Is.False, "Unmuting must not replay a previous round's sound.");
        }

        [UnityTest]
        public IEnumerator DisablingCelebration_StopsImmediately_AndDoesNotReplayUntilAnotherRound()
        {
            yield return LoadMain();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var celebration = Object.FindAnyObjectByType<ClearCelebration>();
            var particles = FindParticles();
            var text = FindClearText();
            var source = FindAudioSource();
            var baseScale = text.rectTransform.localScale;
            yield return EnterGoal(manager);
            yield return WaitForParticles(particles);
            Assert.That(celebration.IsCelebrating, Is.True);
            celebration.enabled = false;
            AssertEffectsStopped(celebration, particles, source, text, baseScale);
            Assert.That(text.enabled, Is.True);
            celebration.enabled = true;
            yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            AssertEffectsStopped(celebration, particles, source, text, baseScale);

            manager.ResetSimulation();
            yield return new WaitForFixedUpdate();
            yield return EnterGoal(manager);
            yield return WaitForParticles(particles);
            Assert.That(celebration.IsCelebrating, Is.True);
        }

        [UnityTest]
        public IEnumerator ClearAudioClip_HasFiniteNonSilentSamples_AndUsesQuietNonLooping2DSource()
        {
            yield return LoadMain();
            var source = FindAudioSource();
            var clip = source.clip;
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.InRange(0.5f, 1.5f));
            Assert.That(clip.frequency, Is.GreaterThanOrEqualTo(22050));
            Assert.That(clip.channels, Is.InRange(1, 2));
            Assert.That(source.spatialBlend, Is.EqualTo(0f));
            Assert.That(source.volume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(source.loop, Is.False);
            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.isPlaying, Is.False);
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True);
            Assert.That(samples.All(value => !float.IsNaN(value) && !float.IsInfinity(value)), Is.True);
            var peak = samples.Max(value => Mathf.Abs(value));
            Assert.That(peak, Is.InRange(0.01f, 1f), "The authored clip must contain audible, valid PCM samples.");
        }

        private static IEnumerator LoadMain()
        {
            yield return TestSceneLoader.Load("Main");
            yield return null;
        }

        private static IEnumerator EnterGoal(GameManager manager)
        {
            manager.StartSimulation();
            var ball = Object.FindAnyObjectByType<BallController>();
            var goal = Object.FindAnyObjectByType<GoalController>();
            ball.Body.useGravity = false;
            ball.Body.linearVelocity = Vector3.zero;
            ball.Body.angularVelocity = Vector3.zero;
            ball.Body.constraints = RigidbodyConstraints.FreezeAll;
            ball.Body.position = goal.GetComponent<BoxCollider>().bounds.center;
            Physics.SyncTransforms();
            for (var step = 0; step < 10 && manager.State != GameState.Clear; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
        }

        private static IEnumerator WaitForParticles(ParticleSystem particles)
        {
            var deadline = Time.realtimeSinceStartup + 0.4f;
            while (particles.particleCount == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(particles.particleCount, Is.GreaterThan(0));
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            var deadline = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static void AssertEffectsStopped(ClearCelebration celebration, ParticleSystem particles,
            AudioSource source, Text text, Vector3 baseScale)
        {
            Assert.That(celebration.IsCelebrating, Is.False);
            Assert.That(particles.particleCount, Is.EqualTo(0));
            Assert.That(particles.IsAlive(true), Is.False);
            Assert.That(source.isPlaying, Is.False);
            Assert.That(Vector3.Distance(text.rectTransform.localScale, baseScale), Is.LessThan(0.001f));
        }

        private static ParticleSystem FindParticles()
        {
            return Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Single();
        }

        private static AudioSource FindAudioSource()
        {
            return Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Single();
        }

        private static Text FindClearText()
        {
            return Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(text => text.name == "ClearText");
        }

        private static Button FindSoundButton()
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(button => button.name == "SoundButton");
        }

        private static Vector2 ButtonPoint(Button button)
        {
            var rect = (RectTransform)button.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private IEnumerator Touch(int id, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = id,
                phase = phase,
                position = position,
                pressure = phase == TouchPhase.Ended ? 0f : 1f
            });
            yield return null;
            yield return null;
        }
    }
}
