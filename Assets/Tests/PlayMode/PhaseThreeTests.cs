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
    public class PhaseThreeTests
    {
        private Scene testScene;
        private Touchscreen touchscreen;

        [SetUp]
        public void SetUp()
        {
            testScene = SceneManager.CreateScene("PhaseThreeTests",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (touchscreen != null && touchscreen.added)
            {
                InputSystem.RemoveDevice(touchscreen);
            }

            if (testScene.IsValid() && testScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(testScene);
            }
        }

        [Test]
        public void PhysicsObject_RestoresDynamicBodyMotionAndSimulationSettings()
        {
            var instance = CreateObject("Snapshot Body");
            instance.AddComponent<BoxCollider>();
            var position = new Vector3(2f, 3f, 4f);
            var rotation = Quaternion.Euler(12f, 23f, 34f);
            var scale = new Vector3(1f, 2f, 3f);
            var velocity = new Vector3(2f, 3f, 0f);
            // Rotation constraints use the body's local axes, so use an allowed
            // angular velocity in that frame and initialize a coherent pose.
            var angularVelocity = rotation * new Vector3(0f, 1f, 2f);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            var body = instance.AddComponent<Rigidbody>();
            var physicsObject = instance.AddComponent<PhysicsObject>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Physics.SyncTransforms();
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
            Assert.That(Vector3.Distance(body.position, position), Is.LessThan(0.001f),
                "The fixture must retain the requested pose before capturing it.");
            Assert.That(Quaternion.Angle(body.rotation, rotation), Is.LessThan(0.05f));
            Assert.That(Vector3.Distance(body.linearVelocity, velocity), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(body.angularVelocity, angularVelocity), Is.LessThan(0.001f));

            physicsObject.CaptureState();

            body.position = Vector3.one * 10f;
            body.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.detectCollisions = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = true;
            physicsObject.RestoreState();

            AssertPose(body.transform, position, rotation);
            Assert.That(Vector3.Distance(body.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(body.rotation, rotation), Is.LessThan(0.05f));
            Assert.That(instance.transform.localScale, Is.EqualTo(scale));
            Assert.That(Vector3.Distance(body.linearVelocity, velocity), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(body.angularVelocity, angularVelocity), Is.LessThan(0.001f));
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.constraints,
                Is.EqualTo(RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX));
            Assert.That(body.detectCollisions, Is.True);
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
        }

        [Test]
        public void PhysicsObject_CapturesUnrenderedDynamicPose()
        {
            var instance = CreateObject("Unrendered Body");
            instance.AddComponent<BoxCollider>();
            var body = instance.AddComponent<Rigidbody>();
            var physicsObject = instance.AddComponent<PhysicsObject>();
            var position = new Vector3(2f, 3f, 4f);
            var rotation = Quaternion.Euler(12f, 23f, 34f);
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.position = position;
            body.rotation = rotation;

            // Capture before a physics or rendering update copies the dynamic
            // body's authoritative pose to its visible Transform.
            Assert.That(Vector3.Distance(body.position, position), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(instance.transform.position, position), Is.GreaterThan(0.1f));
            physicsObject.CaptureState();
            body.position = Vector3.one * 10f;
            body.rotation = Quaternion.identity;
            physicsObject.RestoreState();

            AssertPose(instance.transform, position, rotation);
            Assert.That(Vector3.Distance(body.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(body.rotation, rotation), Is.LessThan(0.05f));
        }

        [Test]
        public void Reset_RestoresLatestPlayPlacement_AndRepeatedPlayCannotOverwriteSnapshot()
        {
            var ball = CreateBall(new Vector3(8f, 3f, 0f));
            var manager = CreateGame(ball, out _, out _);
            var ramp = CreateObject("Snapshot Ramp").AddComponent<PhysicsObject>();
            var support = CreateObject("Ramp Support");
            support.transform.SetParent(ramp.transform, false);
            support.transform.localPosition = new Vector3(1f, -1f, 0f);
            manager.ConfigureResetObjects(ramp);
            var ballPosition = ball.transform.position;
            var ballRotation = ball.transform.rotation;

            for (var run = 0; run < 2; run++)
            {
                var position = new Vector3(1f + run, 2f, 3f - run);
                var rotation = Quaternion.Euler(0f, 15f + run * 30f, -16f);
                ramp.transform.SetPositionAndRotation(position, rotation);
                var supportPosition = support.transform.position;
                manager.ResetSimulation();
                AssertPose(ramp.transform, position, rotation);
                manager.StartSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Playing));

                ramp.transform.SetPositionAndRotation(Vector3.one * 20f, Quaternion.identity);
                ball.Body.position = new Vector3(20f, -30f, 10f);
                ball.Body.rotation = Quaternion.Euler(50f, 60f, 70f);
                ball.Body.linearVelocity = new Vector3(2f, -4f, 1f);
                ball.Body.angularVelocity = Vector3.one;
                manager.StartSimulation();
                manager.ResetSimulation();

                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                AssertPose(ramp.transform, position, rotation);
                Assert.That(Vector3.Distance(support.transform.position, supportPosition), Is.LessThan(0.001f));
                AssertPose(ball.transform, ballPosition, ballRotation);
                Assert.That(ball.Body.isKinematic, Is.True);
                Assert.That(ball.Body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                Assert.That(ball.Body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                Assert.That(ball.Body.useGravity, Is.False,
                    "RESET must restore the saved setting, even though PLAY enabled gravity.");
            }
        }

        [Test]
        public void ResetAfterClear_HidesMessageAndAllowsTheGoalToFireAgain()
        {
            var ball = CreateBall(Vector3.right * 8f);
            var manager = CreateGame(ball, out var goal, out var clearText);
            var goalEntries = 0;
            goal.BallEntered += _ => goalEntries++;

            for (var run = 0; run < 2; run++)
            {
                manager.StartSimulation();
                ball.Body.useGravity = false;
                ball.Body.position = goal.transform.position;
                SimulatePhysics();
                Assert.That(manager.State, Is.EqualTo(GameState.Clear));
                Assert.That(clearText.enabled, Is.True);
                Assert.That(goalEntries, Is.EqualTo(run + 1));

                manager.ResetSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(clearText.enabled, Is.False);
                SimulatePhysics();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit),
                    "Physics callbacks after RESET must not bring back CLEAR.");
                Assert.That(ball.Body.isKinematic, Is.True);
                Assert.That(Vector3.Distance(ball.transform.position, Vector3.right * 8f), Is.LessThan(0.001f));
            }
        }

        [UnityTest]
        public IEnumerator MainScene_TouchPlayAndReset_RestoresBallAndEnablesEditing()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var ramp = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Single(part => part.name == "Ramp");
            var button = FindButton("SimulationButton");
            var label = button.GetComponentInChildren<Text>();
            var rotateButton = FindButton("RotateButton");
            var initialPosition = ball.transform.position;
            var initialRotation = ball.transform.rotation;
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            var rampPoint = (Vector2)Camera.main.WorldToScreenPoint(ramp.GetComponentInChildren<Collider>().bounds.center);
            yield return Tap(61, rampPoint);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            Assert.That(rotateButton.interactable, Is.True);
            yield return Tap(62, ButtonPoint(rotateButton));
            var rampPosition = ramp.transform.position;
            var rampRotation = ramp.transform.rotation;
            Assert.That(label.text, Is.EqualTo("スタート"));

            yield return Tap(63, ButtonPoint(button));
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(label.text, Is.EqualTo("やりなおす"));
            Assert.That(placement.SelectedObject, Is.Null);
            Assert.That(rotateButton.interactable, Is.False);
            for (var step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(Vector3.Distance(ball.transform.position, initialPosition), Is.GreaterThan(0.02f));

            // Exercise the scene's registered ramp snapshot as well as the moving ball.
            ramp.transform.SetPositionAndRotation(rampPosition + Vector3.right, Quaternion.identity);
            yield return Tap(64, ButtonPoint(button));
            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(label.text, Is.EqualTo("スタート"));
            Assert.That(ball.Body.isKinematic, Is.True);
            Assert.That(ball.Body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(ball.Body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            AssertPose(ball.transform, initialPosition, initialRotation);
            AssertPose(ramp.transform, rampPosition, rampRotation);
            for (var step = 0; step < 5; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            AssertPose(ball.transform, initialPosition, initialRotation);

            Physics.SyncTransforms();
            rampPoint = Camera.main.WorldToScreenPoint(ramp.GetComponentInChildren<Collider>().bounds.center);
            yield return Tap(65, rampPoint);
            Assert.That(placement.SelectedObject, Is.SameAs(ramp));
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(rotateButton.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator MainScene_ResetAfterClear_CanCompleteTheSameStageAgain()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var clearText = Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .Single(text => text.name == "ClearText");
            var label = FindButton("SimulationButton").GetComponentInChildren<Text>();
            var position = ball.transform.position;
            var rotation = ball.transform.rotation;

            for (var run = 0; run < 2; run++)
            {
                manager.StartSimulation();
                var deadline = Time.realtimeSinceStartup + 12f;
                while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(manager.State, Is.EqualTo(GameState.Clear),
                    $"Run {run + 1} did not reach Goal. Ball position: {ball.transform.position}");
                Assert.That(clearText.enabled, Is.True);
                Assert.That(label.text, Is.EqualTo("やりなおす"));

                manager.ResetSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                Assert.That(clearText.enabled, Is.False);
                Assert.That(label.text, Is.EqualTo("スタート"));
                AssertPose(ball.transform, position, rotation);
                Assert.That(ball.Body.isKinematic, Is.True);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
            }
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            SceneManager.MoveGameObjectToScene(instance, testScene);
            return instance;
        }

        private BallController CreateBall(Vector3 position)
        {
            var instance = CreateObject("Test Ball");
            instance.transform.position = position;
            instance.AddComponent<SphereCollider>();
            instance.AddComponent<Rigidbody>().useGravity = false;
            return instance.AddComponent<BallController>();
        }

        private GameManager CreateGame(BallController ball, out GoalController goal, out Text clearText)
        {
            var goalObject = CreateObject("Test Goal");
            var trigger = goalObject.AddComponent<BoxCollider>();
            trigger.size = Vector3.one * 3f;
            trigger.isTrigger = true;
            goal = goalObject.AddComponent<GoalController>();
            clearText = CreateObject("Test ClearText").AddComponent<Text>();
            var ui = CreateObject("Test UI").AddComponent<UIManager>();
            ui.Configure(clearText);
            var manager = CreateObject("Test GameManager").AddComponent<GameManager>();
            manager.Configure(ball, goal, ui);
            return manager;
        }

        private void SimulatePhysics()
        {
            Physics.SyncTransforms();
            var physicsScene = testScene.GetPhysicsScene();
            for (var step = 0; step < 3; step++)
            {
                physicsScene.Simulate(Time.fixedDeltaTime);
            }
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
        }

        private static Button FindButton(string name)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Single(button => button.name == name);
        }

        private static Vector2 ButtonPoint(Button button)
        {
            var rect = (RectTransform)button.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private IEnumerator Tap(int touchId, Vector2 point)
        {
            QueueTouch(touchId, TouchPhase.Began, point);
            yield return null;
            yield return null;
            QueueTouch(touchId, TouchPhase.Ended, point);
            yield return null;
            yield return null;
        }

        private void QueueTouch(int touchId, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = touchId,
                phase = phase,
                position = position,
                pressure = phase == TouchPhase.Ended ? 0f : 1f
            });
        }
    }
}
