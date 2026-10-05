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
    public class PhaseSixTests
    {
        private static readonly string[] AddButtonNames =
            { "AddRampButton", "AddDominoButton", "AddSeesawButton" };
        private static readonly string[] PartNames = { "さか", "ドミノ", "シーソー" };
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
        public IEnumerator TouchAddAndDelete_AllThreeKindsSelectWithoutMovingExistingParts()
        {
            yield return LoadMain();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var originals = FindParts();
            var positions = originals.Select(part => part.transform.position).ToArray();
            var rotations = originals.Select(part => part.transform.rotation).ToArray();
            var ball = Object.FindAnyObjectByType<BallController>();
            var goal = Object.FindAnyObjectByType<GoalController>();
            var ballPosition = ball.transform.position;
            var goalPosition = goal.transform.position;
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            for (var index = 0; index < AddButtonNames.Length; index++)
            {
                var addButton = FindButton(AddButtonNames[index]);
                yield return Touch(101 + index * 2, TouchPhase.Began, ButtonPoint(addButton));
                Assert.That(placement.IsDragging, Is.False, "Toolbar presses must not reach stage colliders.");
                yield return Touch(101 + index * 2, TouchPhase.Ended, ButtonPoint(addButton));
                Assert.That(FindParts().Length, Is.EqualTo(originals.Length + 1),
                    "A single tap must create exactly one part.");
                var added = placement.SelectedObject;
                Assert.That(added, Is.Not.Null);
                Assert.That(added.DisplayName, Is.EqualTo(PartNames[index]));
                Assert.That(added.IsSelected, Is.True);
                Assert.That(placement.IsDragging, Is.False);
                Assert.That(added.transform.position.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(added.transform.position.z, Is.EqualTo(-1.4f).Within(0.001f));
                var authored = originals.First(part => part.DisplayName == PartNames[index]);
                Assert.That(added.transform.position.y, Is.EqualTo(authored.transform.position.y).Within(0.001f));
                Assert.That(Quaternion.Angle(added.transform.rotation, authored.transform.rotation),
                    Is.LessThan(0.05f), "Spawning must preserve the ramp's authored slope.");
                Assert.That(FindButton("DeleteButton").interactable, Is.True);
                Assert.That(FindButton("RotateButton").interactable, Is.True);
                var hint = Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                    .Single(text => text.name == "GoalHint");
                Assert.That(hint.text, Does.Contain(PartNames[index] + "を おしたまま"));

                var descendants = added.GetComponentsInChildren<Transform>(true);
                yield return Tap(102 + index * 2, FindButton("DeleteButton"));
                Assert.That(added == null, Is.True);
                Assert.That(descendants.All(child => child == null), Is.True);
                Assert.That(placement.SelectedObject, Is.Null);
                Assert.That(FindParts().Length, Is.EqualTo(originals.Length));
                Assert.That(FindButton("DeleteButton").interactable, Is.False);
            }

            for (var index = 0; index < originals.Length; index++)
            {
                AssertPose(originals[index].transform, positions[index], rotations[index]);
            }
            Assert.That(ball, Is.SameAs(Object.FindAnyObjectByType<BallController>()));
            Assert.That(goal, Is.SameAs(Object.FindAnyObjectByType<GoalController>()));
            Assert.That(ball.transform.position, Is.EqualTo(ballPosition));
            Assert.That(goal.transform.position, Is.EqualTo(goalPosition));
        }

        [UnityTest]
        public IEnumerator NewlyAddedParts_PlayAndResetRestoreLatestAssemblyPosesAcrossCycles()
        {
            yield return LoadMain();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var added = Enumerable.Range(0, 3).Select(spawner.AddPart).ToArray();
            Assert.That(added.All(part => part != null), Is.True);
            var domino = added[1].GetComponent<Rigidbody>();
            var board = added[2].GetComponentInChildren<HingeJoint>().GetComponent<Rigidbody>();
            var stand = added[2].GetComponent<Rigidbody>();
            var snapshots = added.SelectMany(part => part.GetComponentsInChildren<PhysicsObject>(true)).ToArray();
            var partCount = FindParts().Length;

            for (var run = 0; run < 2; run++)
            {
                for (var index = 0; index < added.Length; index++)
                {
                    var position = new Vector3(-3f + index * 3f, added[index].transform.position.y,
                        -1.4f + run * 0.1f);
                    if (index == 1)
                    {
                        position.y = 1.8f + run * 0.2f;
                    }
                    added[index].transform.SetPositionAndRotation(position,
                        Quaternion.AngleAxis(run * 15f, Vector3.up) * added[index].transform.rotation);
                }
                var positions = snapshots.Select(part => part.transform.position).ToArray();
                var rotations = snapshots.Select(part => part.transform.rotation).ToArray();
                var dominoHeight = domino.transform.position.y;
                manager.StartSimulation();
                Assert.That(domino.isKinematic, Is.False);
                Assert.That(board.isKinematic, Is.False);
                Assert.That(stand.isKinematic, Is.True);
                board.AddTorque(board.transform.forward * 0.15f, ForceMode.Impulse);
                for (var step = 0; step < 8; step++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(domino.position.y, Is.LessThan(dominoHeight - 0.03f),
                    "Newly registered dominoes must actually enter the physics simulation.");

                // Disturb every new snapshot, including the static ramp and both seesaw bodies.
                foreach (var part in snapshots)
                {
                    part.transform.position += Vector3.up * 2f;
                    part.transform.rotation = Quaternion.Euler(20f, 30f, 40f);
                    var body = part.GetComponent<Rigidbody>();
                    if (body != null && !body.isKinematic)
                    {
                        body.linearVelocity = Vector3.one;
                        body.angularVelocity = Vector3.one;
                    }
                }
                manager.StartSimulation();
                manager.ResetSimulation();
                yield return new WaitForFixedUpdate();
                Assert.That(FindParts().Length, Is.EqualTo(partCount));
                for (var index = 0; index < snapshots.Length; index++)
                {
                    AssertPose(snapshots[index].transform, positions[index], rotations[index]);
                    var body = snapshots[index].GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        Assert.That(body.isKinematic, Is.True);
                        Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                        Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator DeleteSeesawThenImmediatePlay_RemovesJointAndNeverRestoresDeletedParts()
        {
            yield return LoadMain();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var initialCount = FindParts().Length;
            var seesaw = spawner.AddPart(2);
            var board = seesaw.GetComponentInChildren<HingeJoint>().GetComponent<Rigidbody>();
            var descendants = seesaw.GetComponentsInChildren<Transform>(true);
            manager.StartSimulation();
            yield return new WaitForFixedUpdate();
            manager.ResetSimulation();
            Assert.That(placement.SelectObject(seesaw), Is.True);
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(seesaw.gameObject.activeSelf, Is.False);
            Assert.That(placement.SelectedObject, Is.Null);

            // Destroy is deferred. Starting immediately must not release an unregistered child body.
            manager.StartSimulation();
            Assert.That(board.isKinematic, Is.True);
            manager.ResetSimulation();
            yield return null;
            Assert.That(seesaw == null, Is.True);
            Assert.That(descendants.All(child => child == null), Is.True);
            Assert.That(FindParts().Length, Is.EqualTo(initialCount));

            var originalDomino = FindParts().First(part => part.DisplayName == "ドミノ");
            Assert.That(placement.SelectObject(originalDomino), Is.True);
            Assert.That(placement.DeleteSelected(), Is.True);
            for (var run = 0; run < 2; run++)
            {
                manager.StartSimulation();
                yield return new WaitForFixedUpdate();
                manager.ResetSimulation();
            }
            Assert.That(originalDomino == null, Is.True);
            Assert.That(FindParts().Length, Is.EqualTo(initialCount - 1));
            Assert.That(Object.FindObjectsByType<BallController>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<GoalController>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayingAndClear_BlockAddDeleteSelectionAndRegistryChanges()
        {
            yield return LoadMain();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var ramp = FindParts().Single(part => part.name == "Ramp");
            var rampPhysics = ramp.GetComponent<PhysicsObject>();
            var count = FindParts().Length;
            Assert.That(placement.SelectObject(ramp), Is.True);
            manager.StartSimulation();
            AssertEditingBlocked(spawner, placement, manager, ramp, rampPhysics);

            var deadline = Time.realtimeSinceStartup + 12f;
            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            AssertEditingBlocked(spawner, placement, manager, ramp, rampPhysics);
            Assert.That(FindParts().Length, Is.EqualTo(count));
            manager.ResetSimulation();
            yield return null;
            Assert.That(spawner.CanAddParts, Is.True);
            foreach (var name in AddButtonNames)
            {
                Assert.That(FindButton(name).interactable, Is.True);
            }
            Assert.That(FindButton("DeleteButton").interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator SecondFingerOnToolbar_CannotAddDeleteOrRotateDuringDrag()
        {
            yield return LoadMain();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var domino = spawner.AddPart(1);
            var count = FindParts().Length;
            var position = domino.transform.position;
            var rotation = domino.transform.rotation;
            Physics.SyncTransforms();
            var pointer = (Vector2)Camera.main.WorldToScreenPoint(domino.GetComponent<BoxCollider>().bounds.center);
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Touch(121, TouchPhase.Began, pointer);
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(domino));

            var names = new[] { "AddDominoButton", "DeleteButton", "RotateButton" };
            for (var index = 0; index < names.Length; index++)
            {
                yield return Tap(122 + index, FindButton(names[index]));
                Assert.That(placement.IsDragging, Is.True);
                Assert.That(placement.SelectedObject, Is.SameAs(domino));
                Assert.That(FindParts().Length, Is.EqualTo(count));
                AssertPose(domino.transform, position, rotation);
                Assert.That(spawner.CanAddParts, Is.False);
                Assert.That(spawner.AddPart(0), Is.Null);
                Assert.That(placement.DeleteSelected(), Is.False);
                Assert.That(placement.RotateSelected(), Is.False);
            }
            yield return Touch(121, TouchPhase.Ended, pointer);
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(spawner.CanAddParts, Is.True);
            Assert.That(FindButton("DeleteButton").interactable, Is.True);
            Assert.That(FindButton("RotateButton").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator ToolbarButtons_FitSafeAreaWithoutOverlap_AtCurrentAndCompactWidths()
        {
            yield return LoadMain();
            var safeArea = Object.FindAnyObjectByType<SafeAreaPanel>().GetComponent<RectTransform>();
            var buttons = AddButtonNames.Concat(new[] { "RotateButton", "DeleteButton", "SimulationButton" })
                .Select(FindButton).ToArray();
            Canvas.ForceUpdateCanvases();
            AssertToolbarLayout(buttons, Screen.safeArea);

            // A compact landscape layout with side insets leaves about 1000 reference pixels.
            safeArea.anchorMin = safeArea.anchorMax = new Vector2(0.5f, 0.5f);
            safeArea.anchoredPosition = Vector2.zero;
            safeArea.sizeDelta = new Vector2(1000f, 540f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
            Canvas.ForceUpdateCanvases();
            AssertToolbarLayout(buttons, ScreenRect(safeArea));
        }

        private static void AssertEditingBlocked(PartSpawner spawner, PlacementManager placement,
            GameManager manager, DraggableObject ramp, PhysicsObject rampPhysics)
        {
            Assert.That(spawner.CanAddParts, Is.False);
            for (var index = 0; index < AddButtonNames.Length; index++)
            {
                Assert.That(spawner.AddPart(index), Is.Null);
                Assert.That(FindButton(AddButtonNames[index]).interactable, Is.False);
            }
            Assert.That(placement.SelectObject(ramp), Is.False);
            Assert.That(placement.SelectedObject, Is.Null);
            Assert.That(placement.DeleteSelected(), Is.False);
            Assert.That(placement.RotateSelected(), Is.False);
            Assert.That(manager.RegisterResetObjects(rampPhysics), Is.False);
            Assert.That(manager.UnregisterResetObjects(rampPhysics), Is.False);
            Assert.That(FindButton("DeleteButton").interactable, Is.False);
            Assert.That(FindButton("RotateButton").interactable, Is.False);
        }

        private static void AssertToolbarLayout(Button[] buttons, Rect bounds)
        {
            var rects = buttons.Select(button => ScreenRect((RectTransform)button.transform)).ToArray();
            for (var index = 0; index < buttons.Length; index++)
            {
                var rect = rects[index];
                Assert.That(rect.width, Is.GreaterThan(0f));
                Assert.That(rect.height, Is.GreaterThan(0f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1f), buttons[index].name);
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1f), buttons[index].name);
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1f), buttons[index].name);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1f), buttons[index].name);
                for (var other = index + 1; other < buttons.Length; other++)
                {
                    var overlapWidth = Mathf.Min(rect.xMax, rects[other].xMax) - Mathf.Max(rect.xMin, rects[other].xMin);
                    var overlapHeight = Mathf.Min(rect.yMax, rects[other].yMax) - Mathf.Max(rect.yMin, rects[other].yMin);
                    Assert.That(overlapWidth > 0.5f && overlapHeight > 0.5f, Is.False,
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

        private static DraggableObject[] FindParts()
        {
            return Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude);
        }

        private static Button FindButton(string name)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Single(button => button.name == name);
        }

        private static Vector2 ButtonPoint(Button button)
        {
            return ScreenRect((RectTransform)button.transform).center;
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
        }

        private static IEnumerator LoadMain()
        {
            yield return TestSceneLoader.Load("Main");
            yield return null;
        }

        private IEnumerator Tap(int id, Button button)
        {
            var point = ButtonPoint(button);
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
