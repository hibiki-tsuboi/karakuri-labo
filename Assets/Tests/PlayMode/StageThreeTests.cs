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
    public class StageThreeTests
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
        public IEnumerator InitialStage_OffersOneSeesawAndFixedRamp_WithoutExtraPartTypes()
        {
            yield return LoadScene("StageThree");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var objective = Object.FindAnyObjectByType<SeesawObjective>();
            var ramp = GameObject.Find("FixedShortRamp");
            Assert.That(ramp, Is.Not.Null);
            Assert.That(ramp.GetComponentsInChildren<DraggableObject>(true), Is.Empty);
            Assert.That(FindParts(), Is.Empty);
            Assert.That(Object.FindObjectsByType<DominoChainMember>(FindObjectsInactive.Include), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.CanAddPart(0), Is.True);
            foreach (var index in new[] { -1, 1, 2 })
            {
                Assert.That(spawner.GetRemainingCount(index), Is.EqualTo(0));
                Assert.That(spawner.AddPart(index), Is.Null);
            }
            Assert.That(FindButton("AddSeesawButton").GetComponentInChildren<Text>().text, Does.Contain("(1)"));
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Where(button => button.name == "AddRampButton" || button.name == "AddDominoButton")
                .All(button => !button.gameObject.activeInHierarchy), Is.True);
            Assert.That(objective.RequiredTilt, Is.EqualTo(6f));
            Assert.That(objective.HasRocked, Is.False);
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(FindText("SeesawProgress").text, Does.Contain("0 / 1"));
            Assert.That(Object.FindAnyObjectByType<StageManager>().CanAdvance, Is.False);
            Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.False);
            AssertCompactLayoutAndRestore();
        }

        [UnityTest]
        public IEnumerator GoalWithoutSeesaw_DoesNotClear_AndResetRestoresUnusedInventory()
        {
            yield return LoadScene("StageThree");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<SeesawObjective>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var position = ball.transform.position;
            manager.StartSimulation();
            yield return WaitForGoal(manager);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(objective.HasRocked, Is.False);
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(FindText("ClearText").enabled, Is.False);
            Assert.That(FindText("SeesawProgress").text, Does.Contain("BALL IN GOAL"));
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.AddPart(0), Is.Null);
            manager.ResetSimulation();
            yield return null;
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(objective.HasRocked, Is.False);
            Assert.That(Vector3.Distance(ball.transform.position, position), Is.LessThan(0.001f));
            Assert.That(ball.Body.isKinematic, Is.True);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(FindButton("AddSeesawButton").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator OffLaneSeesawTiltWithoutBallContact_DoesNotCountEvenAfterGoal()
        {
            yield return LoadScene("StageThree");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var objective = Object.FindAnyObjectByType<SeesawObjective>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var seesaw = spawner.AddPart(0);
            Assert.That(Vector3.Distance(seesaw.transform.position, new Vector3(0f, 0f, -1.4f)),
                Is.LessThan(0.001f));
            var member = seesaw.GetComponentInChildren<SeesawGoalMember>();
            var hinge = member.GetComponent<HingeJoint>();
            var body = member.GetComponent<Rigidbody>();
            var rootPosition = seesaw.transform.position;
            var boardPosition = body.transform.position;
            var boardRotation = body.transform.rotation;
            manager.StartSimulation();
            // A real tilt caused by an unrelated force must not satisfy a ball-driven objective.
            body.AddTorque(body.transform.forward * 0.2f, ForceMode.Impulse);
            var maximumTilt = 0f;
            var deadline = Time.realtimeSinceStartup + 12f;
            while ((!manager.HasReachedGoal || maximumTilt < objective.RequiredTilt) &&
                Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                maximumTilt = Mathf.Max(maximumTilt, Mathf.Abs(hinge.angle));
            }
            Assert.That(maximumTilt, Is.GreaterThanOrEqualTo(objective.RequiredTilt));
            Assert.That(manager.HasReachedGoal, Is.True);
            Assert.That(member.HasBallContact, Is.False);
            Assert.That(member.HasRocked, Is.False);
            Assert.That(objective.IsComplete, Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            manager.ResetSimulation();
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(seesaw.transform.position, rootPosition), Is.LessThan(0.001f));
            AssertPose(body.transform, boardPosition, boardRotation);
            Assert.That(member.HasBallContact || member.HasRocked, Is.False);
            Assert.That(manager.HasReachedGoal, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TouchPlacedSeesaw_ClearsNaturally_AndResetRestoresAssemblyForAnotherPlacement()
        {
            yield return LoadScene("StageThree");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var objective = Object.FindAnyObjectByType<SeesawObjective>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var goal = Object.FindAnyObjectByType<GoalController>();
            var fixedRamp = GameObject.Find("FixedShortRamp").transform;
            var fixedTargets = new[] { ball.transform, goal.transform, fixedRamp };
            var fixedPositions = fixedTargets.Select(target => target.position).ToArray();
            var fixedRotations = fixedTargets.Select(target => target.rotation).ToArray();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(241, FindButton("AddSeesawButton"));
            var seesaw = placement.SelectedObject;
            Assert.That(seesaw, Is.Not.Null);
            Assert.That(seesaw.DisplayName, Is.EqualTo("SEESAW"));
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(FindButton("AddSeesawButton").interactable, Is.False);
            var member = seesaw.GetComponentInChildren<SeesawGoalMember>();
            var board = member.GetComponent<Rigidbody>();
            var hinge = member.GetComponent<HingeJoint>();
            var stand = seesaw.GetComponent<Rigidbody>();
            var placements = new[] { new Vector3(-1.4f, 0f, 0f), new Vector3(-1.6f, 0f, 0.2f) };

            for (var run = 0; run < placements.Length; run++)
            {
                yield return DragBoardTo(242 + run * 3, seesaw, member, placements[run]);
                var rootPosition = seesaw.transform.position;
                var rootRotation = seesaw.transform.rotation;
                var boardPosition = board.transform.position;
                var boardRotation = board.transform.rotation;
                Assert.That(stand.isKinematic && board.isKinematic, Is.True);
                yield return Tap(243 + run * 3, FindButton("SimulationButton"));
                Assert.That(stand.isKinematic, Is.True);
                Assert.That(board.isKinematic, Is.False);
                Assert.That(spawner.AddPart(0), Is.Null);
                Assert.That(placement.DeleteSelected(), Is.False);
                yield return WaitForClear(manager);
                Assert.That(manager.HasReachedGoal, Is.True);
                Assert.That(member.HasBallContact, Is.True);
                Assert.That(member.HasRocked, Is.True);
                Assert.That(objective.IsComplete, Is.True);
                Assert.That(FindText("SeesawProgress").text, Does.Contain("1 / 1"));
                Assert.That(FindText("GoalHint").text, Does.Contain("NEXT TO CONTINUE"));
                Assert.That(Object.FindAnyObjectByType<StageManager>().CanAdvance, Is.True);
                Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.True);

                yield return Tap(244 + run * 3, FindButton("SimulationButton"));
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(manager.HasReachedGoal, Is.False);
                Assert.That(member.HasBallContact || member.HasRocked || objective.HasRocked, Is.False);
                Assert.That(FindText("SeesawProgress").text, Does.Contain("0 / 1"));
                Assert.That(FindText("ClearText").enabled, Is.False);
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
                Assert.That(spawner.AddPart(0), Is.Null);
                Assert.That(FindParts().Single(), Is.SameAs(seesaw));
                AssertPose(seesaw.transform, rootPosition, rootRotation);
                AssertPose(board.transform, boardPosition, boardRotation);
                Assert.That(stand.isKinematic && board.isKinematic, Is.True);
                Assert.That(board.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                Assert.That(board.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                Assert.That(Vector3.Distance(board.position + board.rotation * hinge.anchor,
                    stand.position + stand.rotation * hinge.connectedAnchor), Is.LessThan(0.001f));
                for (var index = 0; index < fixedTargets.Length; index++)
                {
                    AssertPose(fixedTargets[index], fixedPositions[index], fixedRotations[index]);
                }
            }
        }

        [UnityTest]
        public IEnumerator DeleteRefundsWholeSeesawImmediately_AndReplacementKeepsOnePieceLimit()
        {
            yield return LoadScene("StageThree");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var original = spawner.AddPart(0);
            var originalMember = original.GetComponentInChildren<SeesawGoalMember>();
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(original.gameObject.activeSelf, Is.False);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            var replacement = spawner.AddPart(0);
            Assert.That(replacement, Is.Not.Null);
            Assert.That(spawner.AddPart(0), Is.Null);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            manager.StartSimulation();
            yield return new WaitForFixedUpdate();
            manager.ResetSimulation();
            yield return null;
            Assert.That(original == null && originalMember == null, Is.True);
            Assert.That(FindParts().Single(), Is.SameAs(replacement));
            Assert.That(Object.FindObjectsByType<SeesawGoalMember>(FindObjectsInactive.Exclude).Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<SeesawObjective>().HasRocked, Is.False);
            Assert.That(placement.SelectObject(replacement), Is.True);
            Assert.That(placement.DeleteSelected(), Is.True);
            yield return null;
            Assert.That(FindParts(), Is.Empty);
            Assert.That(Object.FindObjectsByType<SeesawGoalMember>(FindObjectsInactive.Exclude), Is.Empty);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(FindButton("AddSeesawButton").interactable, Is.True);
            Assert.That(FindButton("AddSeesawButton").GetComponentInChildren<Text>().text, Does.Contain("(1)"));
        }

        private static IEnumerator LoadScene(string name)
        {
            yield return SceneManager.LoadSceneAsync($"Assets/Scenes/{name}.unity", LoadSceneMode.Single);
            yield return null;
        }

        private static IEnumerator WaitForGoal(GameManager manager)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while (!manager.HasReachedGoal && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(manager.HasReachedGoal, Is.True, "The unchanged ball must naturally reach the goal.");
            yield return null;
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

        private IEnumerator DragBoardTo(int id, DraggableObject seesaw, SeesawGoalMember member, Vector3 destination)
        {
            Physics.SyncTransforms();
            var camera = Camera.main;
            var pointer = (Vector2)camera.WorldToScreenPoint(member.GetComponent<BoxCollider>().bounds.center);
            var ray = camera.ScreenPointToRay(pointer);
            var plane = new Plane(Vector3.up, seesaw.transform.position);
            Assert.That(plane.Raycast(ray, out var distance), Is.True);
            var target = (Vector2)camera.WorldToScreenPoint(ray.GetPoint(distance) + destination - seesaw.transform.position);
            yield return Touch(id, TouchPhase.Began, pointer);
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(seesaw));
            yield return Touch(id, TouchPhase.Moved, Vector2.Lerp(pointer, target, 0.5f));
            yield return Touch(id, TouchPhase.Moved, target);
            yield return Touch(id, TouchPhase.Ended, target);
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(Vector3.Distance(seesaw.transform.position, destination), Is.LessThan(0.005f));
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

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.005f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
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
                Canvas.ForceUpdateCanvases();
                AssertButtonLayout(Screen.safeArea);
                safeArea.anchorMin = safeArea.anchorMax = new Vector2(0.5f, 0.5f);
                safeArea.anchoredPosition = Vector2.zero;
                safeArea.sizeDelta = new Vector2(1000f, 540f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
                Canvas.ForceUpdateCanvases();
                AssertButtonLayout(ScreenRect(safeArea));
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

        private static void AssertButtonLayout(Rect bounds)
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .Where(button => button.gameObject.activeInHierarchy).ToArray();
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
