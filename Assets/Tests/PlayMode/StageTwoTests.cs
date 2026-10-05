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
    [Category("LegacyPrototype")]
    public class StageTwoTests
    {
        private static readonly Vector3[] SolutionPositions =
        {
            new Vector3(0.15f, 0.455f, 0.7f),
            new Vector3(0.73f, 0.455f, 0.7f),
            new Vector3(1.31f, 0.455f, 0.7f)
        };
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
        public IEnumerator InitialStage_HasFixedRampAndThreeDominoStock_WithCompactAccessibleButtons()
        {
            yield return LoadScene("StageTwo");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var stage = Object.FindAnyObjectByType<StageManager>();
            var ramp = GameObject.Find("FixedRamp");
            Assert.That(ramp, Is.Not.Null);
            Assert.That(ramp.GetComponentsInChildren<DraggableObject>(true), Is.Empty);
            Assert.That(FindParts(), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(3));
            Assert.That(spawner.CanAddPart(0), Is.True);
            Assert.That(spawner.CanAddPart(1), Is.False);
            Assert.That(spawner.AddPart(1), Is.Null);
            Assert.That(objective.RequiredCount, Is.EqualTo(3));
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsPressed, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsOpen, Is.False);
            Assert.That(GameObject.Find("GateBarrier").GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(FindText("DominoProgress").text, Does.Contain("0 / 3"));
            Assert.That(FindButton("AddDominoButton").GetComponentInChildren<Text>().text, Does.Contain("(3)"));
            Assert.That(stage.CanAdvance, Is.False);
            Assert.That(stage.TryAdvance(), Is.False);
            Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.False);
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Where(button => button.name == "AddRampButton" || button.name == "AddSeesawButton")
                .All(button => !button.gameObject.activeInHierarchy), Is.True);

            var buttons = ActiveButtons();
            Canvas.ForceUpdateCanvases();
            AssertButtonLayout(buttons, Screen.safeArea);
            var safeArea = Object.FindAnyObjectByType<SafeAreaPanel>().GetComponent<RectTransform>();
            safeArea.anchorMin = safeArea.anchorMax = new Vector2(0.5f, 0.5f);
            safeArea.anchoredPosition = Vector2.zero;
            safeArea.sizeDelta = new Vector2(1000f, 540f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
            Canvas.ForceUpdateCanvases();
            AssertButtonLayout(buttons, ScreenRect(safeArea));
        }

        [UnityTest]
        public IEnumerator ClosedGate_HoldsBallWithoutDominoes_AndResetRestoresTheMechanism()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            manager.StartSimulation();
            yield return WaitSimulation(7f);
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsPressed, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(FindText("ClearText").enabled, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(3));
            var ball = Object.FindAnyObjectByType<BallController>();
            Assert.That(ball.Body.position.x, Is.InRange(1.8f, 2.25f),
                "The closed physical gate should hold the ball before the goal.");
            manager.ResetSimulation();
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(spawner.CanAddPart(0), Is.True);
        }

        [UnityTest]
        public IEnumerator DominoesToppledWithoutBallContact_DoNotOpenGate()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var dominoes = new DraggableObject[3];
            for (var index = 0; index < dominoes.Length; index++)
            {
                dominoes[index] = spawner.AddPart(0);
                dominoes[index].transform.position = SolutionPositions[index] + Vector3.forward * 0.95f;
            }
            manager.StartSimulation();
            // Deliberately topple pieces outside the ball's lane with unrelated forces.
            // Rotation alone must not count as a chain initiated by the ball.
            foreach (var domino in dominoes)
            {
                domino.GetComponent<Rigidbody>().AddTorque(Vector3.back * 0.03f, ForceMode.Impulse);
            }
            yield return WaitSimulation(7f);
            Assert.That(dominoes.All(part => Vector3.Angle(part.transform.up, Vector3.up) >= 60f), Is.True,
                "The negative fixture must physically topple all three pieces.");
            Assert.That(dominoes.All(part => !part.GetComponent<DominoChainMember>().HasToppled), Is.True);
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsPressed, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(FindText("ClearText").enabled, Is.False);
            manager.ResetSimulation();
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(dominoes.All(part => !part.GetComponent<DominoChainMember>().HasToppled), Is.True);
        }

        [UnityTest]
        public IEnumerator BallContactWithAnAlreadyFallenDomino_DoesNotRetroactivelyCountIt()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var domino = Object.FindAnyObjectByType<PartSpawner>().AddPart(0);
            domino.transform.SetPositionAndRotation(new Vector3(0.8f, 0.28f, -0.6f),
                Quaternion.Euler(0f, 0f, 90f));
            var body = domino.GetComponent<Rigidbody>();
            // Keep the pre-fallen piece flat so contact cannot stand it up and
            // legitimately start a new fall. Translation and ball physics remain free.
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var contacts = domino.gameObject.AddComponent<DominoBallContactProbe>();
            var ball = Object.FindAnyObjectByType<BallController>();
            ball.transform.position = new Vector3(0.8f, 1.1f, -0.6f);
            ball.Body.position = ball.transform.position;
            Physics.SyncTransforms();
            manager.StartSimulation();
            yield return WaitSimulation(2f);
            yield return new WaitForFixedUpdate();
            Assert.That(contacts.BallContacts, Is.GreaterThan(0),
                "The ball must actually collide with the flat domino.");
            Assert.That(Vector3.Dot(body.rotation * Vector3.up, Vector3.up), Is.LessThan(0.5f));
            Assert.That(domino.GetComponent<DominoChainMember>().HasToppled, Is.False);
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsPressed, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
        }

        [UnityTest]
        public IEnumerator TouchPlacedThreeDominoChain_ClearsNaturally_AndResetSupportsAnotherRound()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var stage = Object.FindAnyObjectByType<StageManager>();
            var gate = Object.FindAnyObjectByType<DominoGate>();
            var barrier = GameObject.Find("GateBarrier").transform;
            var buttonTop = GameObject.Find("SwitchButton").transform;
            var barrierPosition = barrier.position;
            var buttonPosition = buttonTop.position;
            var dominoes = new DraggableObject[3];
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            for (var index = 0; index < dominoes.Length; index++)
            {
                yield return Tap(201 + index * 2, FindButton("AddDominoButton"));
                dominoes[index] = placement.SelectedObject;
                Assert.That(dominoes[index], Is.Not.Null);
                Assert.That(dominoes[index].DisplayName, Is.EqualTo("DOMINO"));
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(2 - index));
                yield return DragTo(202 + index * 2, dominoes[index], SolutionPositions[index]);
                dominoes[index].gameObject.AddComponent<DominoBallContactProbe>();
            }
            Assert.That(spawner.AddPart(0), Is.Null);
            Assert.That(FindButton("AddDominoButton").interactable, Is.False);
            Assert.That(FindButton("AddDominoButton").GetComponentInChildren<Text>().text, Does.Contain("(0)"));
            var rotations = dominoes.Select(part => part.transform.rotation).ToArray();
            var expectedPositions = SolutionPositions.ToArray();

            for (var run = 0; run < 2; run++)
            {
                if (run == 1)
                {
                    // A second valid placement must be captured afresh, not restored
                    // to the first round's guide positions.
                    for (var index = 0; index < dominoes.Length; index++)
                    {
                        expectedPositions[index] += new Vector3(0.08f, 0f, -0.05f);
                        dominoes[index].transform.position = expectedPositions[index];
                    }
                }
                yield return Tap(211 + run * 2, FindButton("SimulationButton"));
                Assert.That(spawner.CanAddPart(0), Is.False);
                Assert.That(placement.DeleteSelected(), Is.False);
                yield return WaitForClear(manager);
                Assert.That(manager.HasReachedGoal, Is.True);
                Assert.That(objective.ToppledCount, Is.EqualTo(3));
                Assert.That(objective.IsComplete, Is.True);
                Assert.That(gate.IsPressed && gate.IsOpen, Is.True);
                Assert.That(barrier.position.y, Is.GreaterThan(barrierPosition.y + 1f));
                Assert.That(buttonTop.position.y, Is.LessThan(buttonPosition.y));
                if (run == 0)
                {
                    Assert.That(dominoes[0].GetComponent<DominoBallContactProbe>().BallContacts, Is.GreaterThan(0));
                    Assert.That(dominoes[1].GetComponent<DominoBallContactProbe>().BallContacts, Is.Zero,
                        "The ball must use its own lane, not ride over the chain.");
                    Assert.That(dominoes[2].GetComponent<DominoBallContactProbe>().BallContacts, Is.Zero);
                }
                Assert.That(dominoes.All(part => part.GetComponent<DominoChainMember>().HasToppled), Is.True);
                Assert.That(FindText("DominoProgress").text, Does.Contain("3 / 3"));
                Assert.That(FindText("DominoProgress").text, Does.EndWith("CLEAR!"));
                Assert.That(FindText("GoalHint").text, Does.Contain("NEXT TO CONTINUE"));
                Assert.That(FindText("ClearText").enabled, Is.True);
                Assert.That(stage.CanAdvance, Is.True);
                Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.True);
                Assert.That(FindButton("NextButton").interactable, Is.True);

                yield return Tap(212 + run * 2, FindButton("SimulationButton"));
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(manager.HasReachedGoal, Is.False);
                Assert.That(objective.ToppledCount, Is.EqualTo(0));
                Assert.That(objective.IsComplete, Is.False);
                Assert.That(gate.IsPressed || gate.IsOpen, Is.False);
                Assert.That(Vector3.Distance(barrier.position, barrierPosition), Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(buttonTop.position, buttonPosition), Is.LessThan(0.001f));
                Assert.That(FindText("DominoProgress").text, Does.Contain("0 / 3"));
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
                Assert.That(FindParts().Length, Is.EqualTo(3));
                for (var index = 0; index < dominoes.Length; index++)
                {
                    AssertPose(dominoes[index].transform, expectedPositions[index], rotations[index]);
                    Assert.That(dominoes[index].GetComponent<Rigidbody>().isKinematic, Is.True);
                    Assert.That(dominoes[index].GetComponent<DominoChainMember>().HasToppled, Is.False);
                }
            }
        }

        [UnityTest]
        public IEnumerator TwoDominoesAreInsufficient_AndDeleteRefundsOnlyTheirUsedStock()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var dominoes = new DraggableObject[2];
            for (var index = 0; index < dominoes.Length; index++)
            {
                dominoes[index] = spawner.AddPart(0);
                dominoes[index].transform.position = SolutionPositions[index];
            }
            manager.StartSimulation();
            yield return WaitSimulation(7f);
            Assert.That(objective.ToppledCount, Is.EqualTo(2));
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoGate>().IsPressed, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.AddPart(0), Is.Null);
            manager.ResetSimulation();
            Assert.That(objective.ToppledCount, Is.EqualTo(0));
            Assert.That(placement.SelectObject(dominoes[0]), Is.True);
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(2));
            var replacement = spawner.AddPart(0);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.AddPart(0), Is.Not.Null);
            Assert.That(spawner.AddPart(0), Is.Null);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            yield return null;
            Assert.That(dominoes[0] == null, Is.True);
            Assert.That(FindParts().Length, Is.EqualTo(3));
        }

        private static IEnumerator LoadScene(string name)
        {
            yield return TestSceneLoader.Load(name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompleteChainWithoutSwitchContact_KeepsGateClosed()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<DominoObjective>();
            var gate = Object.FindAnyObjectByType<DominoGate>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            foreach (var position in SolutionPositions)
            {
                spawner.AddPart(0).transform.position = position;
            }
            // Move the receiving switch out of reach; the full natural chain
            // still falls, but a counter alone cannot operate the gate.
            Object.FindAnyObjectByType<DominoSwitch>().transform.position += Vector3.forward * 1.5f;
            manager.StartSimulation();
            yield return WaitSimulation(7f);
            Assert.That(objective.ToppledCount, Is.EqualTo(3));
            Assert.That(gate.IsPressed || gate.IsOpen || objective.IsComplete, Is.False);
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
        }

        [UnityTest]
        public IEnumerator BallTouchingSwitchDirectly_CannotOpenTheGate()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var gate = Object.FindAnyObjectByType<DominoGate>();
            var sensor = Object.FindAnyObjectByType<DominoSwitch>();
            var probe = sensor.gameObject.AddComponent<SwitchBallContactProbe>();
            var ball = Object.FindAnyObjectByType<BallController>();
            ball.transform.position = sensor.transform.position + Vector3.up * 0.75f;
            ball.Body.position = ball.transform.position;
            Physics.SyncTransforms();
            manager.StartSimulation();
            yield return WaitSimulation(2f);
            Assert.That(probe.BallContacts, Is.GreaterThan(0), "The ball must really enter the switch sensor.");
            Assert.That(gate.IsPressed || gate.IsOpen, Is.False);
            Assert.That(Object.FindAnyObjectByType<DominoObjective>().IsComplete, Is.False);
        }

        [UnityTest]
        public IEnumerator ResetWhileGateIsOpening_ClosesGateAndReleasesSwitch_ThenCanClearAgain()
        {
            yield return LoadScene("StageTwo");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var gate = Object.FindAnyObjectByType<DominoGate>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var barrier = GameObject.Find("GateBarrier").transform;
            var switchTop = GameObject.Find("SwitchButton").transform;
            var barrierPosition = barrier.position;
            var buttonPosition = switchTop.position;
            foreach (var position in SolutionPositions)
            {
                spawner.AddPart(0).transform.position = position;
            }
            manager.StartSimulation();
            var deadline = Time.realtimeSinceStartup + 12f;
            while (!gate.IsPressed && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(gate.IsPressed, Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(gate.IsOpen, Is.False);
            Assert.That(barrier.position.y, Is.GreaterThan(barrierPosition.y));
            manager.ResetSimulation();
            yield return WaitSimulation(0.6f);
            Assert.That(gate.IsPressed || gate.IsOpen, Is.False);
            Assert.That(Vector3.Distance(barrier.position, barrierPosition), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(switchTop.position, buttonPosition), Is.LessThan(0.001f));
            Assert.That(Object.FindAnyObjectByType<DominoObjective>().ToppledCount, Is.Zero);
            Assert.That(manager.HasReachedGoal, Is.False);
            manager.StartSimulation();
            yield return WaitForClear(manager);
            Assert.That(gate.IsOpen, Is.True);
        }

        private static IEnumerator WaitForClear(GameManager manager)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            yield return null;
        }

        private static IEnumerator WaitSimulation(float duration)
        {
            var deadline = Time.time + duration;
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator DragTo(int id, DraggableObject part, Vector3 destination)
        {
            Physics.SyncTransforms();
            var camera = Camera.main;
            var collider = part.GetComponent<BoxCollider>();
            var pointer = (Vector2)camera.WorldToScreenPoint(collider.bounds.center);
            var plane = new Plane(Vector3.up, part.transform.position);
            var ray = camera.ScreenPointToRay(pointer);
            Assert.That(plane.Raycast(ray, out var distance), Is.True);
            var target = (Vector2)camera.WorldToScreenPoint(ray.GetPoint(distance) + destination - part.transform.position);
            var rotation = part.transform.rotation;
            yield return Touch(id, TouchPhase.Began, pointer);
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(part));
            yield return Touch(id, TouchPhase.Moved, Vector2.Lerp(pointer, target, 0.5f));
            yield return Touch(id, TouchPhase.Moved, target);
            yield return Touch(id, TouchPhase.Ended, target);
            Assert.That(placement.IsDragging, Is.False);
            AssertPose(part.transform, destination, rotation);
        }

        private static DraggableObject[] FindParts()
        {
            return Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude);
        }

        private static Button FindButton(string name)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(button => button.name == name);
        }

        private static Text FindText(string name)
        {
            return Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(text => text.name == name);
        }

        private static Button[] ActiveButtons()
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .Where(button => button.gameObject.activeInHierarchy).ToArray();
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

        private static void AssertCompactLayoutAndRestore()
        {
            var safeArea = Object.FindAnyObjectByType<SafeAreaPanel>().GetComponent<RectTransform>();
            var anchorMin = safeArea.anchorMin;
            var anchorMax = safeArea.anchorMax;
            var position = safeArea.anchoredPosition;
            var size = safeArea.sizeDelta;
            try
            {
                safeArea.anchorMin = safeArea.anchorMax = new Vector2(0.5f, 0.5f);
                safeArea.anchoredPosition = Vector2.zero;
                safeArea.sizeDelta = new Vector2(1000f, 540f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
                Canvas.ForceUpdateCanvases();
                AssertButtonLayout(ActiveButtons(), ScreenRect(safeArea));
            }
            finally
            {
                safeArea.anchorMin = anchorMin;
                safeArea.anchorMax = anchorMax;
                safeArea.anchoredPosition = position;
                safeArea.sizeDelta = size;
                LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
                Canvas.ForceUpdateCanvases();
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

    public sealed class DominoBallContactProbe : MonoBehaviour
    {
        public int BallContacts { get; private set; }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody.GetComponent<BallController>() != null)
            {
                BallContacts++;
            }
        }
    }

    public sealed class SwitchBallContactProbe : MonoBehaviour
    {
        public int BallContacts { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<BallController>() != null)
            {
                BallContacts++;
            }
        }
    }
}
