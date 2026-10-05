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
    public class PhaseFiveTests
    {
        private Scene testScene;
        private Touchscreen touchscreen;

        [SetUp]
        public void SetUp()
        {
            testScene = SceneManager.CreateScene("PhaseFiveTests",
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
        public void OffCenterBallContact_RotatesBoardAroundConnectedAnchorWithinLimits()
        {
            var root = CreateSeesaw(out var board, out var hinge);
            var ball = CreateBall(new Vector3(0.85f, 2f, 0f));
            var manager = CreateGame(ball, root, board);
            var contacts = board.gameObject.AddComponent<SeesawContactProbe>();
            manager.StartSimulation();
            Physics.SyncTransforms();
            var maximumAngle = SimulateContact(hinge, 200);

            Assert.That(contacts.BallContacts, Is.GreaterThan(0),
                "Board motion must follow an actual ball collision.");
            Assert.That(maximumAngle, Is.GreaterThan(5f));
            Assert.That(maximumAngle, Is.LessThanOrEqualTo(22f),
                "Allow a small solver tolerance beyond the authored 18-degree stop.");
            Assert.That(root.GetComponent<Rigidbody>().isKinematic, Is.True);
            AssertPose(root.transform, Vector3.zero, Quaternion.identity);
        }

        [Test]
        public void TranslatedAndYawedAssembly_ResetRestoresBothBodies_AndContactWorksAgain()
        {
            var root = CreateSeesaw(out var board, out var hinge);
            var ball = CreateBall(Vector3.zero);
            var manager = CreateGame(ball, root, board);
            var contacts = board.gameObject.AddComponent<SeesawContactProbe>();
            var boardBody = board.GetComponent<Rigidbody>();

            for (var run = 0; run < 2; run++)
            {
                var rootPosition = new Vector3(run * 1.1f, 0f, run * 0.4f);
                var rootRotation = Quaternion.Euler(0f, run * 30f, 0f);
                root.transform.SetPositionAndRotation(rootPosition, rootRotation);
                ball.transform.SetPositionAndRotation(
                    root.transform.TransformPoint(new Vector3(0.85f, 2f, 0f)), rootRotation);
                var boardPosition = board.transform.position;
                var boardRotation = board.transform.rotation;
                var contactsBeforePlay = contacts.BallContacts;
                Assert.That(boardBody.isKinematic, Is.True);
                Assert.That(boardBody.interpolation, Is.EqualTo(RigidbodyInterpolation.None));

                manager.StartSimulation();
                Assert.That(root.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(boardBody.isKinematic, Is.False);
                Assert.That(boardBody.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
                // Local PhysicsScenes are stepped explicitly and require transform synchronization.
                Physics.SyncTransforms();
                var maximumAngle = SimulateContact(hinge, 180);
                Assert.That(contacts.BallContacts, Is.GreaterThan(contactsBeforePlay));
                Assert.That(maximumAngle, Is.GreaterThan(5f), $"Run {run + 1} did not tilt.");
                Assert.That(maximumAngle, Is.LessThanOrEqualTo(22f));

                manager.ResetSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                AssertPose(root.transform, rootPosition, rootRotation);
                AssertPose(board.transform, boardPosition, boardRotation);
                Assert.That(boardBody.isKinematic, Is.True);
                Assert.That(boardBody.interpolation, Is.EqualTo(RigidbodyInterpolation.None));
                Assert.That(boardBody.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                Assert.That(boardBody.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                AssertAnchorConnected(hinge, 0.001f);
                testScene.GetPhysicsScene().Simulate(0.02f);
                AssertPose(board.transform, boardPosition, boardRotation);
            }
        }

        [UnityTest]
        public IEnumerator MainScene_EditAssemblyIsFrozen_ImmediatePlayAndResetKeepHingeConnected()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var root = FindSeesaw();
            var hinge = root.GetComponentInChildren<HingeJoint>();
            var board = hinge.GetComponent<Rigidbody>();
            var stand = root.GetComponent<Rigidbody>();
            var rootPosition = root.transform.position;
            var rootRotation = root.transform.rotation;
            var boardPosition = board.transform.position;
            var boardRotation = board.transform.rotation;
            Assert.That(hinge.connectedBody, Is.SameAs(stand));

            for (var step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            AssertPose(root.transform, rootPosition, rootRotation);
            AssertPose(board.transform, boardPosition, boardRotation);
            Assert.That(stand.isKinematic && board.isKinematic, Is.True);
            Assert.That(board.interpolation, Is.EqualTo(RigidbodyInterpolation.None));

            root.transform.SetPositionAndRotation(rootPosition + new Vector3(0.2f, 0f, -0.2f),
                Quaternion.AngleAxis(15f, Vector3.up) * rootRotation);
            rootPosition = root.transform.position;
            rootRotation = root.transform.rotation;
            boardPosition = board.transform.position;
            boardRotation = board.transform.rotation;
            // Exercise immediate PLAY after a transform edit, without a SyncTransforms
            // call or a FixedUpdate allowing the stand's Rigidbody to catch up first.
            manager.StartSimulation();
            Assert.That(stand.isKinematic, Is.True);
            Assert.That(board.isKinematic, Is.False);
            for (var step = 0; step < 3; step++)
            {
                yield return new WaitForFixedUpdate();
                AssertAnchorConnected(hinge, 0.03f);
            }
            AssertPose(root.transform, rootPosition, rootRotation);

            // Moving both bodies exposes missing reset references and child-before-parent restoration.
            root.transform.position += Vector3.forward;
            board.position += Vector3.down * 2f;
            manager.ResetSimulation();
            yield return new WaitForFixedUpdate();
            AssertPose(root.transform, rootPosition, rootRotation);
            AssertPose(board.transform, boardPosition, boardRotation);
            Assert.That(stand.isKinematic && board.isKinematic, Is.True);
            Assert.That(board.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(board.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            AssertAnchorConnected(hinge, 0.001f);
        }

        [UnityTest]
        public IEnumerator MainScene_TouchBoardDragsAndRotatesWholeSeesaw_WithoutMovingOtherParts()
        {
            yield return TestSceneLoader.Load("Main");
            var root = FindSeesaw();
            var hinge = root.GetComponentInChildren<HingeJoint>();
            var board = hinge.transform;
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var camera = Camera.main;
            var otherParts = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Where(part => part != root).ToArray();
            var otherPositions = otherParts.Select(part => part.transform.position).ToArray();
            var otherRotations = otherParts.Select(part => part.transform.rotation).ToArray();
            var initialPosition = root.transform.position;
            var initialRotation = root.transform.rotation;
            var boardLocalPosition = board.localPosition;
            var boardLocalRotation = board.localRotation;
            Physics.SyncTransforms();
            var pointer = VisibleBoardPoint(hinge.GetComponent<BoxCollider>(), camera, root);
            var delta = (Vector2)(camera.WorldToScreenPoint(root.transform.position + Vector3.left * 0.3f)
                - camera.WorldToScreenPoint(root.transform.position));
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            yield return Touch(91, TouchPhase.Began, pointer);
            Assert.That(placement.SelectedObject, Is.SameAs(root));
            Assert.That(root.IsSelected, Is.True);
            var label = Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .Single(text => text.name == "GoalHint");
            Assert.That(label.text, Does.Contain("シーソーを おしたまま"));
            yield return Touch(91, TouchPhase.Moved, pointer + delta);
            yield return Touch(91, TouchPhase.Ended, pointer + delta);
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(Vector3.Distance(root.transform.position, initialPosition), Is.GreaterThan(0.1f));
            Assert.That(root.transform.position.y, Is.EqualTo(initialPosition.y).Within(0.001f));

            var rotateButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Single(button => button.name == "RotateButton");
            var rect = (RectTransform)rotateButton.transform;
            var buttonPoint = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            yield return Touch(92, TouchPhase.Began, buttonPoint);
            yield return Touch(92, TouchPhase.Ended, buttonPoint);
            Assert.That(Quaternion.Angle(root.transform.rotation,
                Quaternion.AngleAxis(15f, Vector3.up) * initialRotation), Is.LessThan(0.05f));
            Assert.That(Vector3.Distance(board.localPosition, boardLocalPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(board.localRotation, boardLocalRotation), Is.LessThan(0.05f));
            Assert.That(hinge.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(root));
            for (var index = 0; index < otherParts.Length; index++)
            {
                AssertPose(otherParts[index].transform, otherPositions[index], otherRotations[index]);
            }
        }

        [UnityTest]
        public IEnumerator MainScene_NaturalBallContactTiltsSeesaw_AndStageStillClears()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var hinge = FindSeesaw().GetComponentInChildren<HingeJoint>();
            var contacts = hinge.gameObject.AddComponent<SeesawContactProbe>();
            var maximumAngle = 0f;
            manager.StartSimulation();
            var deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline &&
                (manager.State != GameState.Clear || contacts.BallContacts == 0 || maximumAngle < 2f))
            {
                yield return new WaitForFixedUpdate();
                maximumAngle = Mathf.Max(maximumAngle, Mathf.Abs(hinge.angle));
                AssertAnchorConnected(hinge, 0.03f);
                Assert.That(hinge.angle, Is.InRange(hinge.limits.min - 4f, hinge.limits.max + 4f));
            }
            Assert.That(contacts.BallContacts, Is.GreaterThan(0),
                "The initial layout must route the ball onto the seesaw.");
            Assert.That(maximumAngle, Is.GreaterThanOrEqualTo(2f));
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
        }

        private PhysicsObject CreateSeesaw(out PhysicsObject board, out HingeJoint hinge)
        {
            var root = CreateObject("Test Seesaw");
            var standBody = root.AddComponent<Rigidbody>();
            standBody.isKinematic = true;
            standBody.useGravity = false;
            var rootPhysics = root.AddComponent<PhysicsObject>();
            var plank = CreateObject("Board");
            plank.transform.SetParent(root.transform, false);
            plank.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            plank.AddComponent<BoxCollider>().size = new Vector3(2.6f, 0.16f, 1f);
            var boardBody = plank.AddComponent<Rigidbody>();
            boardBody.mass = 0.3f;
            boardBody.interpolation = RigidbodyInterpolation.Interpolate;
            boardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            boardBody.solverIterations = 12;
            boardBody.solverVelocityIterations = 4;
            board = plank.AddComponent<PhysicsObject>();
            board.ConfigureSimulation(true);
            hinge = plank.AddComponent<HingeJoint>();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedBody = standBody;
            hinge.anchor = Vector3.zero;
            hinge.connectedAnchor = new Vector3(0f, 0.65f, 0f);
            hinge.axis = Vector3.forward;
            hinge.limits = new JointLimits { min = -18f, max = 18f };
            hinge.useLimits = true;
            return rootPhysics;
        }

        private BallController CreateBall(Vector3 position)
        {
            var instance = CreateObject("Test Ball");
            instance.transform.position = position;
            instance.AddComponent<SphereCollider>().radius = 0.3f;
            var body = instance.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return instance.AddComponent<BallController>();
        }

        private GameManager CreateGame(BallController ball, params PhysicsObject[] parts)
        {
            var goal = CreateObject("Test Goal").AddComponent<GoalController>();
            goal.transform.position = Vector3.right * 20f;
            var text = CreateObject("Test Clear Text").AddComponent<Text>();
            var ui = CreateObject("Test UI").AddComponent<UIManager>();
            ui.Configure(text);
            var manager = CreateObject("Test GameManager").AddComponent<GameManager>();
            manager.Configure(ball, goal, ui);
            manager.ConfigureResetObjects(parts);
            return manager;
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            SceneManager.MoveGameObjectToScene(instance, testScene);
            return instance;
        }

        private float SimulateContact(HingeJoint hinge, int steps)
        {
            var maximumAngle = 0f;
            for (var step = 0; step < steps; step++)
            {
                testScene.GetPhysicsScene().Simulate(0.01f);
                maximumAngle = Mathf.Max(maximumAngle, Mathf.Abs(hinge.angle));
                AssertAnchorConnected(hinge, 0.03f);
            }
            return maximumAngle;
        }

        private static DraggableObject FindSeesaw()
        {
            return Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Single(part => part.name == "Seesaw");
        }

        private static Vector2 VisibleBoardPoint(BoxCollider collider, Camera camera, DraggableObject root)
        {
            foreach (var fraction in new[] { -0.35f, 0f, 0.35f })
            {
                var local = collider.center + new Vector3(collider.size.x * fraction, collider.size.y * 0.5f, 0f);
                var screen = camera.WorldToScreenPoint(collider.transform.TransformPoint(local));
                if (Physics.Raycast(camera.ScreenPointToRay(screen), out var hit, 100f, ~0,
                    QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<DraggableObject>() == root)
                {
                    return screen;
                }
            }
            Assert.Fail("The seesaw board must have an unobstructed touch target in Main.");
            return Vector2.zero;
        }

        private static void AssertAnchorConnected(HingeJoint hinge, float tolerance)
        {
            var boardBody = hinge.GetComponent<Rigidbody>();
            var boardAnchor = boardBody.position + boardBody.rotation * hinge.anchor;
            var standBody = hinge.connectedBody;
            var standAnchor = standBody.position + standBody.rotation * hinge.connectedAnchor;
            Assert.That(Vector3.Distance(boardAnchor, standAnchor), Is.LessThan(tolerance),
                "The board must stay attached to the stand at its configured pivot.");
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
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

    public sealed class SeesawContactProbe : MonoBehaviour
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
}
