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
    public class StageOneTests
    {
        private static readonly Vector3 SolutionPosition = new Vector3(-4.15f, 1.5f, 0f);
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
        public IEnumerator EmptyStage_OffersExactlyOneRamp_AndTouchUpdatesStockAndButtonState()
        {
            yield return LoadStage();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var button = FindButton("AddRampButton");
            var label = button.GetComponentInChildren<Text>();
            Assert.That(FindParts(), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.CanAddPart(0), Is.True);
            Assert.That(button.interactable, Is.True);
            Assert.That(label.text, Does.Contain("あと 1こ"));
            foreach (var index in new[] { -1, 1, 2 })
            {
                Assert.That(spawner.GetRemainingCount(index), Is.EqualTo(0));
                Assert.That(spawner.CanAddPart(index), Is.False);
                Assert.That(spawner.AddPart(index), Is.Null);
            }
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Where(candidate => candidate.name == "AddDominoButton" || candidate.name == "AddSeesawButton")
                .All(candidate => !candidate.gameObject.activeInHierarchy), Is.True);

            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(161, button);
            var ramp = placement.SelectedObject;
            Assert.That(ramp, Is.Not.Null);
            Assert.That(ramp.DisplayName, Is.EqualTo("さか"));
            Assert.That(FindParts().Length, Is.EqualTo(1));
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(spawner.CanAddParts, Is.False);
            Assert.That(button.interactable, Is.False);
            Assert.That(label.text, Does.Contain("あと 0こ"));
            Assert.That(placement.IsDragging, Is.False);
            yield return Tap(162, button);
            Assert.That(FindParts().Length, Is.EqualTo(1), "Tapping exhausted stock must not create a duplicate.");
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
        }

        [UnityTest]
        public IEnumerator PlayingWithoutRamp_DoesNotClear_AndResetLeavesStockUnused()
        {
            yield return LoadStage();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var position = ball.transform.position;
            manager.StartSimulation();
            Assert.That(ball.Body.isKinematic, Is.False);
            yield return WaitWithoutClear(manager, 6f);
            Assert.That(Vector3.Distance(ball.transform.position, position), Is.GreaterThan(0.2f));
            Assert.That(FindParts(), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.CanAddPart(0), Is.False);
            manager.ResetSimulation();
            yield return null;
            Assert.That(Vector3.Distance(ball.transform.position, position), Is.LessThan(0.001f));
            Assert.That(ball.Body.isKinematic, Is.True);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.CanAddPart(0), Is.True);
            Assert.That(FindButton("AddRampButton").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator WrongPlacementFails_ThenTouchPlacementNaturallyClears_AndResetKeepsSolution()
        {
            yield return LoadStage();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var ballPosition = ball.transform.position;
            var ramp = spawner.AddPart(0);
            var rotation = ramp.transform.rotation;
            Assert.That(Vector3.Distance(ramp.transform.position, new Vector3(0f, 1.5f, -1.4f)),
                Is.LessThan(0.001f));
            manager.StartSimulation();
            yield return WaitWithoutClear(manager, 6f);
            manager.ResetSimulation();
            yield return null;
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));

            touchscreen = InputSystem.AddDevice<Touchscreen>();
            Physics.SyncTransforms();
            var camera = Camera.main;
            var collider = ramp.transform.Find("Rolling surface").GetComponent<Collider>();
            var pointer = (Vector2)camera.WorldToScreenPoint(collider.bounds.center);
            var plane = new Plane(Vector3.up, ramp.transform.position);
            var ray = camera.ScreenPointToRay(pointer);
            Assert.That(plane.Raycast(ray, out var distance), Is.True);
            var destination = (Vector2)camera.WorldToScreenPoint(
                ray.GetPoint(distance) + SolutionPosition - ramp.transform.position);
            yield return Touch(171, TouchPhase.Began, pointer);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            Assert.That(placement.IsDragging, Is.True);
            for (var step = 1; step <= 3; step++)
            {
                yield return Touch(171, TouchPhase.Moved, Vector2.Lerp(pointer, destination, step / 3f));
            }
            yield return Touch(171, TouchPhase.Ended, destination);
            Assert.That(placement.IsDragging, Is.False);
            AssertPose(ramp.transform, SolutionPosition, rotation);

            for (var run = 0; run < 2; run++)
            {
                yield return Tap(172 + run * 2, FindButton("SimulationButton"));
                Assert.That(manager.State, Is.EqualTo(GameState.Playing));
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
                Assert.That(spawner.AddPart(0), Is.Null);
                Assert.That(placement.SelectObject(ramp), Is.False);
                Assert.That(placement.DeleteSelected(), Is.False);
                yield return WaitForNaturalClear(manager);
                Assert.That(FindClearText().enabled, Is.True);
                Assert.That(spawner.CanAddPart(0), Is.False);
                Assert.That(placement.SelectObject(ramp), Is.False);
                Assert.That(placement.RotateSelected(), Is.False);
                Assert.That(placement.DeleteSelected(), Is.False);

                yield return Tap(173 + run * 2, FindButton("SimulationButton"));
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(FindClearText().enabled, Is.False);
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
                Assert.That(spawner.AddPart(0), Is.Null, "RESET must not replenish a ramp still placed in the scene.");
                Assert.That(FindParts().Single(), Is.SameAs(ramp));
                AssertPose(ramp.transform, SolutionPosition, rotation);
                Assert.That(Vector3.Distance(ball.transform.position, ballPosition), Is.LessThan(0.001f));
            }
        }

        [UnityTest]
        public IEnumerator DeleteRefundsImmediately_ReplacementCannotDuplicateStock_AndResetPreservesIt()
        {
            yield return LoadStage();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var original = spawner.AddPart(0);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(original.gameObject.activeSelf, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1),
                "Stock must return before Unity processes deferred destruction.");
            var replacement = spawner.AddPart(0);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(spawner.AddPart(0), Is.Null);
            Assert.That(FindParts().Length, Is.EqualTo(1));

            manager.StartSimulation();
            Assert.That(spawner.AddPart(0), Is.Null);
            Assert.That(placement.DeleteSelected(), Is.False);
            yield return new WaitForFixedUpdate();
            manager.ResetSimulation();
            yield return null;
            Assert.That(original == null, Is.True);
            Assert.That(FindParts().Single(), Is.SameAs(replacement));
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(FindButton("AddRampButton").GetComponentInChildren<Text>().text, Does.Contain("あと 0こ"));
            Assert.That(placement.SelectObject(replacement), Is.True);
            Assert.That(placement.DeleteSelected(), Is.True);
            yield return null;
            Assert.That(FindParts(), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(FindButton("AddRampButton").interactable, Is.True);
            Assert.That(FindButton("AddRampButton").GetComponentInChildren<Text>().text, Does.Contain("あと 1こ"));
        }

        [UnityTest]
        public IEnumerator ModestPlacementOffsets_StillReachTheGoalWithoutMovingTheBall()
        {
            yield return LoadStage();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var ramp = spawner.AddPart(0);
            var ball = Object.FindAnyObjectByType<BallController>();
            var ballPosition = ball.transform.position;
            var positions = new[]
            {
                new Vector3(-4.65f, 1.5f, -0.35f),
                new Vector3(-3.65f, 1.5f, 0.35f)
            };

            foreach (var position in positions)
            {
                ramp.transform.position = position;
                Assert.That(Vector3.Distance(ball.transform.position, ballPosition), Is.LessThan(0.001f));
                manager.StartSimulation();
                yield return WaitForNaturalClear(manager);
                manager.ResetSimulation();
                yield return new WaitForFixedUpdate();
                Assert.That(Vector3.Distance(ramp.transform.position, position), Is.LessThan(0.001f));
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            }
        }

        [UnityTest]
        public IEnumerator ActiveStageButtons_FitCurrentAndCompactSafeAreaWithoutOverlap()
        {
            yield return LoadStage();
            var safeArea = Object.FindAnyObjectByType<SafeAreaPanel>().GetComponent<RectTransform>();
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .Where(button => button.gameObject.activeInHierarchy).ToArray();
            Assert.That(buttons.Any(button => button.name == "AddRampButton"), Is.True);
            Canvas.ForceUpdateCanvases();
            AssertButtonLayout(buttons, Screen.safeArea);
            safeArea.anchorMin = safeArea.anchorMax = new Vector2(0.5f, 0.5f);
            safeArea.anchoredPosition = Vector2.zero;
            safeArea.sizeDelta = new Vector2(1000f, 540f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
            Canvas.ForceUpdateCanvases();
            AssertButtonLayout(buttons, ScreenRect(safeArea));
        }

        [UnityTest]
        public IEnumerator StageOneNext_IsClearOnly_AndTouchLoadsFreshStageThreeOnceWithMutePreserved()
        {
            yield return LoadStage();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var stage = Object.FindAnyObjectByType<StageManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var next = FindButton("NextButton");
            Assert.That(stage.CanAdvance, Is.False);
            Assert.That(stage.TryAdvance(), Is.False);
            Assert.That(next.gameObject.activeInHierarchy, Is.False);
            var ramp = spawner.AddPart(0);
            ramp.transform.position = new Vector3(-4.15f, 1.5f, 0f);
            Object.FindAnyObjectByType<AudioManager>().SetMuted(true);
            manager.StartSimulation();
            Assert.That(stage.CanAdvance, Is.False);
            Assert.That(stage.TryAdvance(), Is.False);
            Assert.That(next.gameObject.activeInHierarchy, Is.False);
            yield return WaitForCampaignClear(manager);
            Assert.That(stage.CanAdvance, Is.True);
            Assert.That(next.gameObject.activeInHierarchy, Is.True);
            Assert.That(next.interactable, Is.True);
            Canvas.ForceUpdateCanvases();

            AssertButtonLayout(Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .Where(button => button.gameObject.activeInHierarchy).ToArray(), Screen.safeArea);

            manager.ResetSimulation();
            Assert.That(stage.CanAdvance, Is.False);
            Assert.That(stage.TryAdvance(), Is.False);
            Assert.That(next.gameObject.activeInHierarchy, Is.False);
            manager.StartSimulation();
            yield return WaitForCampaignClear(manager);
            var duplicateRejected = false;
            var secondRequestObserved = false;
            // The production click listener runs first; another request from that
            // same click must see the transition lock before loading can complete.
            next.onClick.AddListener(() =>
            {
                secondRequestObserved = true;
                duplicateRejected = !stage.TryAdvance();
            });
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(221, next);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "StageThree" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;
            Assert.That(secondRequestObserved, Is.True);
            Assert.That(duplicateRejected, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StageThree"));
            Assert.That(manager == null && stage == null && ramp == null, Is.True);
            Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsInactive.Exclude).Length, Is.EqualTo(1));
            var destinationManager = Object.FindAnyObjectByType<GameManager>();
            var destinationAudio = Object.FindAnyObjectByType<AudioManager>();
            Assert.That(destinationManager.State, Is.EqualTo(GameState.Edit));
            Assert.That(destinationManager.HasReachedGoal, Is.False);
            Assert.That(destinationAudio.IsMuted, Is.True);
            Assert.That(FindButton("SoundButton").GetComponentInChildren<Text>().text, Is.EqualTo("おと：なし"));
            Assert.That(FindParts(), Is.Empty);
            Assert.That(Object.FindAnyObjectByType<PartSpawner>().GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<SeesawObjective>().HasRocked, Is.False);
            Assert.That(FindClearText().enabled, Is.False);
            Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.False);
        }

        private static IEnumerator WaitForCampaignClear(GameManager manager)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            yield return null;
        }

        private static IEnumerator LoadStage()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/StageOne.unity", LoadSceneMode.Single);
            yield return null;
        }

        private static IEnumerator WaitWithoutClear(GameManager manager, float duration)
        {
            var deadline = Time.time + duration;
            var clearText = FindClearText();
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(manager.State, Is.EqualTo(GameState.Playing).Or.EqualTo(GameState.Failed),
                    "The ball must not reach the goal without a correctly placed ramp.");
                Assert.That(clearText.enabled, Is.False);
            }
        }

        private static IEnumerator WaitForNaturalClear(GameManager manager)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            var ball = Object.FindAnyObjectByType<BallController>();
            Assert.That(manager.State, Is.EqualTo(GameState.Clear),
                $"The ball must naturally reach the goal. Final position: {ball.transform.position}");
        }

        private static DraggableObject[] FindParts()
        {
            return Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude);
        }

        private static Button FindButton(string name)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(button => button.name == name);
        }

        private static Text FindClearText()
        {
            return Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(text => text.name == "ClearText");
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.005f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
        }

        private static void AssertButtonLayout(Button[] buttons, Rect bounds)
        {
            var rects = buttons.Select(button => ScreenRect((RectTransform)button.transform)).ToArray();
            for (var index = 0; index < buttons.Length; index++)
            {
                var rect = rects[index];
                Assert.That(rect.width, Is.GreaterThan(0f), buttons[index].name);
                Assert.That(rect.height, Is.GreaterThan(0f), buttons[index].name);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1f), buttons[index].name);
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1f), buttons[index].name);
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1f), buttons[index].name);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1f), buttons[index].name);
                for (var other = index + 1; other < buttons.Length; other++)
                {
                    var width = Mathf.Min(rect.xMax, rects[other].xMax) - Mathf.Max(rect.xMin, rects[other].xMin);
                    var height = Mathf.Min(rect.yMax, rects[other].yMax) - Mathf.Max(rect.yMin, rects[other].yMin);
                    Assert.That(width > 0.5f && height > 0.5f, Is.False,
                        $"{buttons[index].name} overlaps {buttons[other].name}.");
                }
            }
        }

        private static Rect ScreenRect(RectTransform transform)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            var minimum = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var maximum = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private IEnumerator Tap(int id, Button button)
        {
            var point = ScreenRect((RectTransform)button.transform).center;
            yield return Touch(id, TouchPhase.Began, point);
            yield return Touch(id, TouchPhase.Ended, point);
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
