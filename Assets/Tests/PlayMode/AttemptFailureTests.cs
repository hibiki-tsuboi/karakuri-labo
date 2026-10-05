using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KarakuriLabo.Tests
{
    public class AttemptFailureTests
    {
        private static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();
        private static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
            .Single(b => b.name == name);

        [UnityTest]
        public IEnumerator MissingRampAutomaticallyFails_ThenRetryAndAdjustPreserveLayoutAndCamera()
        {
            yield return TestSceneLoader.Load("StageOne");
            yield return null;
            var manager = Find<GameManager>();
            var ball = Find<BallController>();
            var spawner = Find<PartSpawner>();
            var ramp = spawner.AddPart(0); // Deliberately leave it on the storage position.
            Vector3 rampPosition = ramp.transform.position;
            Vector3 start = ball.Body.position;
            var orbit = Find<StageCameraOrbit>();
            orbit.BeginOrbit(new Vector2(500, 400));
            orbit.MoveOrbit(new Vector2(610, 450));
            orbit.CancelOrbit();
            Quaternion rotation = orbit.ViewRotation;
            int failures = 0;
            manager.StateChanged += state => { if (state == GameState.Failed) failures++; };
            manager.StartSimulation();
            float deadline = Time.realtimeSinceStartup + 12;
            while (manager.State == GameState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.Failed));
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.Stopped));
            Assert.That(Find<FailureHUD>().IsVisible, Is.True);
            Assert.That(Button("SimulationButton").GetComponentInChildren<Text>().text, Is.EqualTo("なおす"));
            Assert.That(Button("RetryAttemptButton").interactable, Is.True);
            Assert.That(Button("NextButton").gameObject.activeSelf, Is.False);
            Assert.That(Button("AddRampButton").gameObject.activeSelf, Is.False);
            Assert.That(ball.Body.isKinematic, Is.True);
            Vector3 failedPosition = ball.Body.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(ball.Body.position, failedPosition), Is.LessThan(0.001f));
            Assert.That(manager.FailAttempt(AttemptFailure.Fell), Is.False);
            Assert.That(failures, Is.EqualTo(1));

            Button("RetryAttemptButton").onClick.Invoke();
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(manager.RetryAttempt(), Is.False, "A second tap must not recapture a moving layout.");
            Assert.That(Vector3.Distance(ball.Body.position, start), Is.LessThan(0.001f));
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.None));
            Assert.That(Find<FailureHUD>().IsVisible, Is.False);
            Assert.That(ball.Body.isKinematic, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.Zero);
            manager.FailAttempt(AttemptFailure.Stopped);
            Button("SimulationButton").onClick.Invoke();
            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(Vector3.Distance(ramp.transform.position, rampPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(orbit.ViewRotation, rotation), Is.LessThan(0.01f));
            Assert.That(Object.FindObjectsByType<DraggableObject>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            ramp.transform.position = new Vector3(-4.15f, 1.5f, 0);
            ToyStageTests.Simulate(manager, 12);
            yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            Assert.That(Find<FailureHUD>().IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator FallingEndsAttemptOnce_AndLateGoalCannotClearIt()
        {
            yield return TestSceneLoader.Load("StageFour");
            yield return null;
            var manager = Find<GameManager>();
            var ball = Find<BallController>();
            var monitor = Find<AttemptMonitor>();
            Vector3 start = ball.Body.position;
            manager.StartSimulation();
            ball.Body.position = new Vector3(0, monitor.PlayableBounds.min.y - 0.1f, 0);
            ball.Body.linearVelocity = Vector3.down * 8;
            monitor.StepAttempt(0.02f);
            Assert.That(manager.State, Is.EqualTo(GameState.Failed));
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.Fell));
            ball.Body.position = Find<GoalController>().transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(manager.State, Is.EqualTo(GameState.Failed));
            Assert.That(Find<StageManager>().CanAdvance, Is.False);
            manager.ResetSimulation();
            Assert.That(Vector3.Distance(ball.Body.position, start), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator ProgressTimersIgnorePauseAndAllowCarriedMovement_ButCatchJitterAndEndlessLoops()
        {
            yield return TestSceneLoader.Load("StageOne");
            yield return null;
            var manager = Find<GameManager>();
            var monitor = Find<AttemptMonitor>();
            var ball = Find<BallController>();
            manager.StartSimulation();
            ball.Body.isKinematic = true;
            Vector3 start = ball.Body.position;
            for (int i = 0; i < 100; i++) monitor.StepAttempt(0f);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            for (int i = 0; i < 12; i++)
            {
                ball.Body.position = start + Vector3.up * (i + 1) * 0.1f;
                monitor.StepAttempt(0.5f);
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Playing), "A lift must count as world-space progress.");
            Vector3 carried = ball.Body.position;
            for (int i = 0; i < 8; i++)
            {
                ball.Body.position = carried + Vector3.right * (i % 2 == 0 ? 0.01f : -0.01f);
                monitor.StepAttempt(0.5f);
            }
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.Stopped));
            Assert.That(manager.RetryAttempt(), Is.True);
            ball.Body.isKinematic = true;
            for (int i = 0; i < 100 && manager.State == GameState.Playing; i++)
            {
                ball.Body.position = start + Vector3.right * (i % 2 == 0 ? 0.2f : 0f);
                monitor.StepAttempt(0.5f);
            }
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.TimedOut));
        }

        [UnityTest]
        public IEnumerator IncompleteGoalExplainsMissingMechanism_AndClearTakesPriorityOverFailure()
        {
            yield return TestSceneLoader.Load("StageThree");
            yield return null;
            var manager = Find<GameManager>();
            ToyStageTests.Simulate(manager, 16);
            yield return null;
            Assert.That(manager.HasReachedGoal, Is.True);
            Assert.That(manager.FailureReason, Is.EqualTo(AttemptFailure.GoalRequirement));
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .Single(t => t.name == "FailureReason").text, Does.Contain("まだ どうぐを つかっていないよ"));

            manager.ResetSimulation();
            Find<PartSpawner>().AddPart(0).transform.position = new Vector3(-1.4f, 0, 0);
            ToyStageTests.Simulate(manager, 16);
            yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            Assert.That(manager.FailAttempt(AttemptFailure.TimedOut), Is.False);
            Assert.That(Find<FailureHUD>().IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator FailureFreezesLiftInPlace_AndAdjustRestoresItsCarriageAndGate()
        {
            yield return TestSceneLoader.Load("StageLift");
            yield return null;
            var part = ToyStageTests.PlaceSolution(3);
            var lift = part.GetComponent<BallLift>();
            var manager = Find<GameManager>();
            Physics.SyncTransforms();
            manager.StartSimulation();
            float deadline = Time.realtimeSinceStartup + 6;
            while (lift.Height < 0.9f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(lift.Height, Is.InRange(0.9f, 3.19f));
            manager.FailAttempt(AttemptFailure.TimedOut);
            float height = lift.Height;
            Vector3 carriage = part.transform.Find("Carriage").position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(lift.Height, Is.EqualTo(height));
            Assert.That(Vector3.Distance(carriage, part.transform.Find("Carriage").position), Is.LessThan(0.001f));
            manager.ResetSimulation();
            yield return new WaitForFixedUpdate();
            Assert.That(lift.Height, Is.Zero);
            Assert.That(part.transform.Find("Carriage").localPosition.y, Is.EqualTo(0.2f).Within(0.002f));
            Assert.That(part.transform.Find("Carriage/Exit gate").GetComponentInChildren<Collider>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator EveryStageHasCompactFailureUI_AndCameraAndMenuDoNotTriggerRetry()
        {
            foreach (string scene in new[] { "StageOne", "StageThree", "StageFour", "StageFive",
                "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" })
            {
                yield return TestSceneLoader.Load(scene);
                yield return null;
                Assert.That(Find<AttemptMonitor>(), Is.Not.Null, scene);
                var manager = Find<GameManager>();
                manager.StartSimulation();
                manager.FailAttempt(AttemptFailure.Stopped);
                yield return null;
                var safe = Find<SafeAreaPanel>();
                safe.enabled = false;
                var rect = safe.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
                rect.sizeDelta = new Vector2(1000, 540);
                rect.anchoredPosition = Vector2.zero;
                Canvas.ForceUpdateCanvases();
                RectTransform card = (RectTransform)rect.Find("FailureCard");
                var buttons = rect.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy).ToArray();
                foreach (Button button in buttons)
                {
                    Assert.That(WorldRect(rect).Contains(WorldRect((RectTransform)button.transform).center), Is.True, scene + button.name);
                    Assert.That(WorldRect(card).Overlaps(WorldRect((RectTransform)button.transform)), Is.False, scene + button.name);
                }
                for (int a = 0; a < buttons.Length; a++)
                    for (int b = a + 1; b < buttons.Length; b++)
                        Assert.That(WorldRect((RectTransform)buttons[a].transform).Overlaps(WorldRect((RectTransform)buttons[b].transform)),
                            Is.False, scene + ": " + buttons[a].name + " / " + buttons[b].name);
                var orbit = Find<StageCameraOrbit>();
                Assert.That(orbit.BeginOrbit(new Vector2(500, 350)), Is.True);
                orbit.MoveOrbit(new Vector2(550, 390));
                yield return null;
                Assert.That(Button("RetryAttemptButton").interactable, Is.False);
                Button("RetryAttemptButton").onClick.Invoke();
                Assert.That(manager.State, Is.EqualTo(GameState.Failed));
                orbit.CancelOrbit();
                var menu = Find<StageSelectHUD>();
                Assert.That(menu.TryOpen(), Is.True);
                Button("RetryAttemptButton").onClick.Invoke();
                Button("SimulationButton").onClick.Invoke();
                Assert.That(manager.State, Is.EqualTo(GameState.Failed));
            }
        }

        private static Rect WorldRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
