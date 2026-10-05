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
    public class PhaseTwoTests
    {
        private static readonly Vector3 StageOrigin = new Vector3(1000f, 0f, 1000f);
        private Scene testScene;
        private Camera stageCamera;
        private GameManager game;
        private BallController ball;
        private GoalController goal;
        private PlacementManager placement;
        private DraggableObject ramp;
        private BoxCollider rampCollider;
        private GameObject selectionVisual;
        private Touchscreen touchscreen;
        private Mouse mouse;

        [SetUp]
        public void SetUp()
        {
            // Global raycasts need the default physics world. Keep fixtures far from Main.
            testScene = SceneManager.CreateScene("PhaseTwoTests");
            stageCamera = CreateObject("Test Camera").AddComponent<Camera>();
            stageCamera.enabled = false;
            stageCamera.orthographic = true;
            stageCamera.orthographicSize = 8f;
            stageCamera.transform.position = StageOrigin + new Vector3(0f, 12f, -12f);
            stageCamera.transform.LookAt(StageOrigin);

            var ballObject = CreateObject("Test Ball");
            ballObject.transform.position = StageOrigin + Vector3.right * 8f;
            ballObject.AddComponent<SphereCollider>();
            ballObject.AddComponent<Rigidbody>();
            ball = ballObject.AddComponent<BallController>();

            var goalObject = CreateObject("Test Goal");
            goalObject.transform.position = StageOrigin + Vector3.right * 12f;
            var goalTrigger = goalObject.AddComponent<BoxCollider>();
            goalTrigger.isTrigger = true;
            goalTrigger.size = Vector3.one * 3f;
            goal = goalObject.AddComponent<GoalController>();

            var clearText = CreateObject("Test ClearText").AddComponent<Text>();
            var ui = CreateObject("Test UI").AddComponent<UIManager>();
            ui.Configure(clearText);
            game = CreateObject("Test GameManager").AddComponent<GameManager>();
            game.Configure(ball, goal, ui);

            var rampObject = CreateObject("Test Ramp");
            rampObject.transform.position = StageOrigin + Vector3.up;
            rampObject.transform.rotation = Quaternion.Euler(0f, 23f, -18f);
            var colliderObject = CreateObject("Ramp Child Collider");
            colliderObject.transform.SetParent(rampObject.transform, false);
            rampCollider = colliderObject.AddComponent<BoxCollider>();
            rampCollider.size = new Vector3(3f, 0.3f, 2f);
            selectionVisual = CreateObject("Selection Visual");
            selectionVisual.transform.SetParent(rampObject.transform, false);
            ramp = rampObject.AddComponent<DraggableObject>();
            ramp.Configure(selectionVisual);

            placement = CreateObject("Test PlacementManager").AddComponent<PlacementManager>();
            placement.Configure(game, stageCamera,
                new Vector2(StageOrigin.x - 4f, StageOrigin.z - 4f),
                new Vector2(StageOrigin.x + 4f, StageOrigin.z + 4f));
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (touchscreen != null && touchscreen.added)
            {
                InputSystem.RemoveDevice(touchscreen);
            }

            if (mouse != null && mouse.added)
            {
                InputSystem.RemoveDevice(mouse);
            }

            if (testScene.IsValid() && testScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(testScene);
            }
        }

        [UnityTest]
        public IEnumerator MainScene_StartsInEditAndKeepsBallFrozen()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var stageBall = Object.FindAnyObjectByType<BallController>();
            var stagePlacement = Object.FindAnyObjectByType<PlacementManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(stageBall, Is.Not.Null);
            Assert.That(stagePlacement, Is.Not.Null);
            var initialPosition = stageBall.transform.position;

            for (var frame = 0; frame < 5; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(stageBall.Body.isKinematic, Is.True);
            Assert.That(Vector3.Distance(stageBall.transform.position, initialPosition), Is.LessThan(0.001f));
            Assert.That(stagePlacement.SelectedObject, Is.Null);
            var rotateButton = FindRotateButton();
            Assert.That(rotateButton.interactable, Is.False);
        }

        [Test]
        public void DraggingChildCollider_PreservesPointerOffsetAndHeight()
        {
            var start = ramp.transform.position;
            var pointer = ScreenPoint(ramp.transform.TransformPoint(new Vector3(0.6f, 0.15f, 0f)));
            var selectedEvents = 0;
            DraggableObject notifiedSelection = null;
            placement.SelectionChanged += selected =>
            {
                notifiedSelection = selected;
                selectedEvents++;
            };

            Assert.That(placement.BeginPointer(pointer, 7), Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            Assert.That(ramp.IsSelected && selectionVisual.activeSelf, Is.True);
            Assert.That(selectedEvents, Is.EqualTo(1));
            Assert.That(notifiedSelection, Is.SameAs(ramp));
            placement.MovePointer(pointer, 7);
            Assert.That(Vector3.Distance(ramp.transform.position, start), Is.LessThan(0.001f),
                "Touching away from the pivot must not snap the ramp to the finger.");

            var movement = new Vector3(1.2f, 0f, 0.8f);
            var startOnPlane = PointOnPlane(pointer, start.y);
            placement.MovePointer(ScreenPoint(startOnPlane + movement), 7);
            Assert.That(Vector3.Distance(ramp.transform.position, start + movement), Is.LessThan(0.005f));
            Assert.That(ramp.transform.position.y, Is.EqualTo(start.y).Within(0.001f));
            placement.EndPointer(7);
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
        }

        [Test]
        public void DraggingBeyondStage_ClampsRampPivotToBounds()
        {
            var start = ramp.transform.position;
            var pointer = ScreenPoint(rampCollider.bounds.center);
            Assert.That(placement.BeginPointer(pointer), Is.True);
            var startOnPlane = PointOnPlane(pointer, start.y);
            placement.MovePointer(ScreenPoint(startOnPlane + new Vector3(20f, 0f, -20f)));

            Assert.That(ramp.transform.position.x, Is.EqualTo(StageOrigin.x + 4f).Within(0.005f));
            Assert.That(ramp.transform.position.z, Is.EqualTo(StageOrigin.z - 4f).Within(0.005f));
            Assert.That(ramp.transform.position.y, Is.EqualTo(start.y).Within(0.001f));
        }

        [Test]
        public void RotateSelected_UsesWorldYawAndPreservesSlopeOverFullTurn()
        {
            Assert.That(placement.RotateSelected(), Is.False);
            Assert.That(placement.BeginPointer(ScreenPoint(rampCollider.bounds.center)), Is.True);
            placement.EndPointer();
            var initialRotation = ramp.transform.rotation;
            var initialPosition = ramp.transform.position;
            var initialSlope = Vector3.Dot(ramp.transform.right, Vector3.up);

            Assert.That(placement.RotateSelected(), Is.True);
            Assert.That(Quaternion.Angle(ramp.transform.rotation,
                Quaternion.AngleAxis(15f, Vector3.up) * initialRotation), Is.LessThan(0.05f));
            Assert.That(Vector3.Dot(ramp.transform.right, Vector3.up), Is.EqualTo(initialSlope).Within(0.0001f));
            for (var click = 1; click < 24; click++)
            {
                Assert.That(placement.RotateSelected(), Is.True);
            }

            Assert.That(Quaternion.Angle(ramp.transform.rotation, initialRotation), Is.LessThan(0.05f));
            Assert.That(ramp.transform.position, Is.EqualTo(initialPosition));
        }

        [Test]
        public void SecondPointer_CannotHijackOrEndDrag_AndCancelReleasesOwner()
        {
            var pointer = ScreenPoint(rampCollider.bounds.center);
            var start = ramp.transform.position;
            var initialRotation = ramp.transform.rotation;
            Assert.That(placement.BeginPointer(pointer, 11), Is.True);
            Assert.That(placement.BeginPointer(pointer, 22), Is.False);
            placement.MovePointer(pointer + Vector2.right * 100f, 22);
            placement.EndPointer(22);
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(ramp.transform.position, Is.EqualTo(start));
            Assert.That(placement.RotateSelected(), Is.False,
                "A second finger must not rotate the ramp during an owned drag.");
            Assert.That(ramp.transform.rotation, Is.EqualTo(initialRotation));

            placement.CancelDrag();
            Assert.That(placement.IsDragging, Is.False);
            placement.MovePointer(pointer + Vector2.right * 100f, 11);
            Assert.That(ramp.transform.position, Is.EqualTo(start));
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            Assert.That(placement.BeginPointer(pointer, 22), Is.True,
                "A canceled pointer must not prevent the next gesture.");
        }

        [Test]
        public void OccludingNonDraggableCollider_PreventsSelectingRampBehindIt()
        {
            var pointer = ScreenPoint(rampCollider.bounds.center);
            var blocker = CreateObject("Opaque Blocker");
            var ray = stageCamera.ScreenPointToRay(pointer);
            blocker.transform.position = ray.GetPoint(3f);
            blocker.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            Physics.SyncTransforms();

            Assert.That(placement.BeginPointer(pointer), Is.False);
            Assert.That(placement.SelectedObject, Is.Null);
            Assert.That(placement.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator LeavingEdit_CancelsSelectionAndBlocksMovementInPlayingAndClear()
        {
            var pointer = ScreenPoint(rampCollider.bounds.center);
            Assert.That(placement.BeginPointer(pointer), Is.True);
            var initialPosition = ramp.transform.position;
            var initialRotation = ramp.transform.rotation;
            game.StartSimulation();

            Assert.That(game.State, Is.EqualTo(GameState.Playing));
            AssertEditingBlocked(pointer, initialPosition, initialRotation);
            ball.Body.useGravity = false;
            ball.Body.position = goal.transform.position;
            Physics.SyncTransforms();
            for (var frame = 0; frame < 3; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(game.State, Is.EqualTo(GameState.Clear), "The actual goal trigger should complete the fixture.");
            AssertEditingBlocked(pointer, initialPosition, initialRotation);
        }

        [UnityTest]
        public IEnumerator MainScene_TouchDragThenRotateButton_KeepsSelectionWithoutDragging()
        {
            yield return TestSceneLoader.Load("Main");
            var stagePlacement = Object.FindAnyObjectByType<PlacementManager>();
            var stageRamp = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Single(part => part.name == "Ramp");
            var camera = Camera.main;
            var button = FindRotateButton();
            Assert.That(stagePlacement, Is.Not.Null);
            Assert.That(stageRamp, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var position = stageRamp.transform.position;
            var pointer = (Vector2)camera.WorldToScreenPoint(stageRamp.GetComponentInChildren<Collider>().bounds.center);
            QueueTouch(41, TouchPhase.Began, pointer);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.True);

            var movedPointer = pointer + new Vector2(Screen.width * 0.04f, Screen.height * 0.03f);
            QueueTouch(41, TouchPhase.Moved, movedPointer);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(stageRamp.transform.position, position), Is.GreaterThan(0.1f));
            Assert.That(stageRamp.transform.position.y, Is.EqualTo(position.y).Within(0.001f));
            var beforeRelease = stageRamp.transform.position;
            var releasePointer = movedPointer - new Vector2(Screen.width * 0.01f, Screen.height * 0.0075f);
            var expectedReleasePosition = beforeRelease +
                PointOnPlane(camera, releasePointer, position.y) - PointOnPlane(camera, movedPointer, position.y);
            QueueTouch(41, TouchPhase.Ended, releasePointer);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.IsDragging, Is.False);
            Assert.That(button.interactable, Is.True);
            Assert.That(Vector3.Distance(stageRamp.transform.position, beforeRelease), Is.GreaterThan(0.02f));
            Assert.That(Vector3.Distance(stageRamp.transform.position, expectedReleasePosition), Is.LessThan(0.005f),
                "The release event must apply its final coordinates even without a preceding move event.");

            var afterDrag = stageRamp.transform.position;
            var beforeRotation = stageRamp.transform.rotation;
            var buttonRect = (RectTransform)button.transform;
            var buttonPoint = RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center));
            QueueTouch(42, TouchPhase.Began, buttonPoint);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.False);
            QueueTouch(42, TouchPhase.Ended, buttonPoint);
            yield return null;
            yield return null;

            Assert.That(Quaternion.Angle(stageRamp.transform.rotation,
                Quaternion.AngleAxis(15f, Vector3.up) * beforeRotation), Is.LessThan(0.05f));
            Assert.That(stageRamp.transform.position, Is.EqualTo(afterDrag));
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.False);

            Physics.SyncTransforms();
            pointer = camera.WorldToScreenPoint(stageRamp.GetComponentInChildren<Collider>().bounds.center);
            QueueTouch(43, TouchPhase.Began, pointer);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.IsDragging, Is.True);
            var beforeCancellation = stageRamp.transform.position;
            QueueTouch(43, TouchPhase.Canceled, pointer + new Vector2(Screen.width * 0.04f, 0f));
            yield return null;
            yield return null;
            Assert.That(stagePlacement.IsDragging, Is.False);
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stageRamp.transform.position, Is.EqualTo(beforeCancellation),
                "Canceled touches must release ownership without applying their final coordinates.");
        }

        [UnityTest]
        public IEnumerator MainScene_MouseDragAndRotateButton_UseThePollingAndUiInputPaths()
        {
            yield return TestSceneLoader.Load("Main");
            var stagePlacement = Object.FindAnyObjectByType<PlacementManager>();
            var stageRamp = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Single(part => part.name == "Ramp");
            var camera = Camera.main;
            var button = FindRotateButton();
            Assert.That(stagePlacement, Is.Not.Null);
            Assert.That(stageRamp, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            mouse = InputSystem.AddDevice<Mouse>();
            var initialPosition = stageRamp.transform.position;
            var pointer = (Vector2)camera.WorldToScreenPoint(stageRamp.GetComponentInChildren<Collider>().bounds.center);
            QueueMouse(pointer, true);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.True);

            var movedPointer = pointer + new Vector2(Screen.width * 0.04f, Screen.height * 0.03f);
            QueueMouse(movedPointer, true);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(stageRamp.transform.position, initialPosition), Is.GreaterThan(0.1f));
            var beforeRelease = stageRamp.transform.position;
            var releasePointer = movedPointer - new Vector2(Screen.width * 0.01f, Screen.height * 0.0075f);
            var expectedReleasePosition = beforeRelease +
                PointOnPlane(camera, releasePointer, initialPosition.y) -
                PointOnPlane(camera, movedPointer, initialPosition.y);
            QueueMouse(releasePointer, false);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.IsDragging, Is.False);
            Assert.That(Vector3.Distance(stageRamp.transform.position, expectedReleasePosition), Is.LessThan(0.005f));
            Assert.That(stageRamp.transform.position.y, Is.EqualTo(initialPosition.y).Within(0.001f));
            Assert.That(button.interactable, Is.True);

            var afterDrag = stageRamp.transform.position;
            var beforeRotation = stageRamp.transform.rotation;
            var buttonRect = (RectTransform)button.transform;
            var buttonPoint = RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center));
            QueueMouse(buttonPoint, true);
            yield return null;
            yield return null;
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.False);
            QueueMouse(buttonPoint, false);
            yield return null;
            yield return null;

            Assert.That(Quaternion.Angle(stageRamp.transform.rotation,
                Quaternion.AngleAxis(15f, Vector3.up) * beforeRotation), Is.LessThan(0.05f));
            Assert.That(stageRamp.transform.position, Is.EqualTo(afterDrag));
            Assert.That(stagePlacement.SelectedObject, Is.SameAs(stageRamp));
            Assert.That(stagePlacement.IsDragging, Is.False);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            SceneManager.MoveGameObjectToScene(instance, testScene);
            return instance;
        }

        private Vector2 ScreenPoint(Vector3 worldPosition)
        {
            return stageCamera.WorldToScreenPoint(worldPosition);
        }

        private Vector3 PointOnPlane(Vector2 screenPosition, float height)
        {
            return PointOnPlane(stageCamera, screenPosition, height);
        }

        private static Vector3 PointOnPlane(Camera camera, Vector2 screenPosition, float height)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            Assert.That(plane.Raycast(ray, out var distance), Is.True);
            return ray.GetPoint(distance);
        }

        private void AssertEditingBlocked(Vector2 pointer, Vector3 position, Quaternion rotation)
        {
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(placement.SelectedObject, Is.Null);
            Assert.That(ramp.IsSelected || selectionVisual.activeSelf, Is.False);
            Assert.That(placement.BeginPointer(pointer), Is.False);
            placement.MovePointer(pointer + Vector2.right * 100f);
            Assert.That(placement.RotateSelected(), Is.False);
            Assert.That(ramp.transform.position, Is.EqualTo(position));
            Assert.That(ramp.transform.rotation, Is.EqualTo(rotation));
        }

        private static Button FindRotateButton()
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Single(button => button.name == "RotateButton");
        }

        private void QueueTouch(int touchId, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = touchId,
                phase = phase,
                position = position,
                pressure = phase == TouchPhase.Ended || phase == TouchPhase.Canceled ? 0f : 1f
            });
        }

        private void QueueMouse(Vector2 position, bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState
            {
                position = position,
                buttons = (ushort)(pressed ? 1 : 0)
            });
        }
    }
}
