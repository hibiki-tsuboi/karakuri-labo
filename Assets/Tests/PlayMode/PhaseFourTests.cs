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
    public class PhaseFourTests
    {
        private Scene testScene;
        private Touchscreen touchscreen;

        [SetUp]
        public void SetUp()
        {
            testScene = SceneManager.CreateScene("PhaseFourTests",
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
        public void RegisteredDominoes_PlayAndResetRestoreEachLatestPlacementAcrossCycles()
        {
            var ball = CreateBall(new Vector3(-8f, 3f, 0f));
            var manager = CreateGame(ball);
            var dominoes = Enumerable.Range(0, 3)
                .Select(index => CreateDomino(new Vector3(index, 0.6f, 0f))).ToArray();
            var fixedPart = CreateObject("Fixed Part").AddComponent<PhysicsObject>();
            var fixedBody = fixedPart.gameObject.AddComponent<Rigidbody>();
            fixedBody.isKinematic = true;
            manager.ConfigureResetObjects(dominoes.Concat(new[] { fixedPart }).ToArray());

            for (var run = 0; run < 2; run++)
            {
                var positions = new Vector3[dominoes.Length];
                var rotations = new Quaternion[dominoes.Length];
                for (var index = 0; index < dominoes.Length; index++)
                {
                    positions[index] = new Vector3(index + run * 0.25f, 0.6f, run * 0.3f);
                    rotations[index] = Quaternion.Euler(0f, index * 15f + run * 30f, 0f);
                    dominoes[index].transform.SetPositionAndRotation(positions[index], rotations[index]);
                    var body = dominoes[index].GetComponent<Rigidbody>();
                    Assert.That(body.isKinematic, Is.True);
                    body.constraints = RigidbodyConstraints.FreezePositionZ;
                    body.useGravity = false;
                }

                manager.StartSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Playing));
                Assert.That(ball.Body.isKinematic, Is.False);
                Assert.That(fixedBody.isKinematic, Is.True,
                    "Unflagged registered objects must remain fixed during PLAY.");
                foreach (var domino in dominoes)
                {
                    var body = domino.GetComponent<Rigidbody>();
                    Assert.That(body.isKinematic, Is.False);
                    Assert.That(body.useGravity, Is.True);
                    body.position += Vector3.down * 4f;
                    body.rotation = Quaternion.Euler(70f, 80f, 90f);
                    body.linearVelocity = new Vector3(3f, -2f, 0f);
                    body.angularVelocity = Vector3.one;
                    body.constraints = RigidbodyConstraints.None;
                }

                manager.StartSimulation();
                manager.ResetSimulation();
                Assert.That(manager.State, Is.EqualTo(GameState.Edit));
                for (var index = 0; index < dominoes.Length; index++)
                {
                    AssertPose(dominoes[index].transform, positions[index], rotations[index]);
                    var body = dominoes[index].GetComponent<Rigidbody>();
                    Assert.That(body.isKinematic, Is.True);
                    Assert.That(body.useGravity, Is.False);
                    Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezePositionZ));
                    Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                    Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.000001f));
                }
            }
        }

        [Test]
        public void BallContact_TopplesAChainOfDynamicDominoes()
        {
            var floor = CreateObject("Floor");
            floor.transform.position = new Vector3(1f, -0.1f, 0f);
            floor.AddComponent<BoxCollider>().size = new Vector3(10f, 0.2f, 3f);
            var dominoes = Enumerable.Range(0, 4)
                .Select(index => CreateDomino(new Vector3(index * 0.65f, 0.6f, 0f))).ToArray();
            var ball = CreateBall(new Vector3(-1.2f, 0.36f, 0f));
            var manager = CreateGame(ball);
            manager.ConfigureResetObjects(dominoes);
            manager.StartSimulation();
            ball.Body.linearVelocity = Vector3.right * 3.5f;
            Physics.SyncTransforms();
            var physicsScene = testScene.GetPhysicsScene();
            var firstTilted = false;
            var lastTilted = false;
            var lastDirectContact = false;

            for (var step = 0; step < 400 && !lastTilted; step++)
            {
                physicsScene.Simulate(0.01f);
                firstTilted |= IsToppled(dominoes[0].transform);
                lastTilted = IsToppled(dominoes[dominoes.Length - 1].transform);
                lastDirectContact |= ball.Body.position.x >= 1.95f - 0.36f - 0.1f;
            }

            Assert.That(firstTilted, Is.True, "A real ball collision must topple the first domino.");
            Assert.That(lastTilted, Is.True, "The impulse must propagate to the last domino.");
            Assert.That(lastDirectContact, Is.False,
                "The last domino must fall through the chain before the ball can directly strike it.");
        }

        [UnityTest]
        public IEnumerator MainScene_AllDominoesStayFrozenUntilPlay_AndResetRestoresThem()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var dominoes = FindSceneDominoes();
            Assert.That(dominoes.Length, Is.GreaterThanOrEqualTo(3));
            var positions = dominoes.Select(part => part.transform.position).ToArray();
            var rotations = dominoes.Select(part => part.transform.rotation).ToArray();
            for (var step = 0; step < 10; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            for (var index = 0; index < dominoes.Length; index++)
            {
                AssertPose(dominoes[index].transform, positions[index], rotations[index]);
                Assert.That(dominoes[index].GetComponent<Rigidbody>().isKinematic, Is.True);
            }

            manager.StartSimulation();
            foreach (var domino in dominoes)
            {
                var body = domino.GetComponent<Rigidbody>();
                Assert.That(body.isKinematic, Is.False);
                Assert.That(body.useGravity, Is.True);
                // Mutate every registered body to expose missing snapshot references.
                body.position += Vector3.down * 5f;
                body.rotation = Quaternion.Euler(90f, 0f, 0f);
                body.linearVelocity = Vector3.down * 2f;
            }
            manager.ResetSimulation();
            yield return new WaitForFixedUpdate();
            for (var index = 0; index < dominoes.Length; index++)
            {
                AssertPose(dominoes[index].transform, positions[index], rotations[index]);
                var body = dominoes[index].GetComponent<Rigidbody>();
                Assert.That(body.isKinematic, Is.True);
                Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));
            }
        }

        [UnityTest]
        public IEnumerator MainScene_TouchDragAndRotateDomino_OnlyChangesSelectedPiece()
        {
            yield return TestSceneLoader.Load("Main");
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var camera = Camera.main;
            Physics.SyncTransforms();
            var domino = FindSceneDominoes().First(part => IsVisibleToCamera(part, camera));
            var selectedPart = domino.GetComponent<DraggableObject>();
            var otherParts = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude)
                .Where(part => part != selectedPart).ToArray();
            var otherPositions = otherParts.Select(part => part.transform.position).ToArray();
            var otherRotations = otherParts.Select(part => part.transform.rotation).ToArray();
            var initialPosition = domino.transform.position;
            var initialRotation = domino.transform.rotation;
            var center = domino.GetComponent<BoxCollider>().bounds.center;
            var pointer = (Vector2)camera.WorldToScreenPoint(center);
            var destination = (Vector2)camera.WorldToScreenPoint(center + Vector3.left * 0.3f);
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            QueueTouch(81, TouchPhase.Began, pointer);
            yield return null;
            yield return null;
            Assert.That(placement.SelectedObject, Is.SameAs(selectedPart));
            Assert.That(selectedPart.IsSelected, Is.True);
            var label = Object.FindObjectsByType<Text>(FindObjectsInactive.Include)
                .Single(text => text.name == "GoalHint");
            Assert.That(label.text, Does.Contain("ドミノを おしたまま"));
            QueueTouch(81, TouchPhase.Moved, destination);
            yield return null;
            yield return null;
            QueueTouch(81, TouchPhase.Ended, destination);
            yield return null;
            yield return null;
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(Vector3.Distance(domino.transform.position, initialPosition), Is.GreaterThan(0.1f));
            Assert.That(domino.transform.position.y, Is.EqualTo(initialPosition.y).Within(0.001f));

            var rotateButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .Single(button => button.name == "RotateButton");
            Assert.That(rotateButton.interactable, Is.True);
            var rect = (RectTransform)rotateButton.transform;
            var buttonPoint = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            QueueTouch(82, TouchPhase.Began, buttonPoint);
            yield return null;
            yield return null;
            QueueTouch(82, TouchPhase.Ended, buttonPoint);
            yield return null;
            yield return null;
            Assert.That(Quaternion.Angle(domino.transform.rotation,
                Quaternion.AngleAxis(15f, Vector3.up) * initialRotation), Is.LessThan(0.05f));
            Assert.That(domino.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(selectedPart));
            for (var index = 0; index < otherParts.Length; index++)
            {
                AssertPose(otherParts[index].transform, otherPositions[index], otherRotations[index]);
            }
        }

        [UnityTest]
        public IEnumerator MainScene_BallTopplesEveryDomino_AndResetStandsThemBackUp()
        {
            yield return TestSceneLoader.Load("Main");
            var manager = Object.FindAnyObjectByType<GameManager>();
            var dominoes = FindSceneDominoes();
            Assert.That(dominoes.Length, Is.GreaterThanOrEqualTo(3));
            var positions = dominoes.Select(part => part.transform.position).ToArray();
            var rotations = dominoes.Select(part => part.transform.rotation).ToArray();
            manager.StartSimulation();
            var deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline &&
                (manager.State != GameState.Clear || dominoes.Any(part => !IsToppled(part.transform))))
            {
                yield return null;
            }
            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            foreach (var domino in dominoes)
            {
                Assert.That(IsToppled(domino.transform), Is.True,
                    $"{domino.name} must fall in the scene's initial ball-driven chain.");
            }
            manager.ResetSimulation();
            for (var step = 0; step < 5; step++)
            {
                yield return new WaitForFixedUpdate();
            }
            for (var index = 0; index < dominoes.Length; index++)
            {
                AssertPose(dominoes[index].transform, positions[index], rotations[index]);
                Assert.That(dominoes[index].GetComponent<Rigidbody>().isKinematic, Is.True);
            }
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            SceneManager.MoveGameObjectToScene(instance, testScene);
            return instance;
        }

        private PhysicsObject CreateDomino(Vector3 position)
        {
            var instance = CreateObject("Test Domino");
            instance.transform.position = position;
            instance.AddComponent<BoxCollider>().size = new Vector3(0.2f, 1.2f, 0.55f);
            var body = instance.AddComponent<Rigidbody>();
            body.mass = 0.08f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var physicsObject = instance.AddComponent<PhysicsObject>();
            physicsObject.ConfigureSimulation(true);
            return physicsObject;
        }

        private BallController CreateBall(Vector3 position)
        {
            var instance = CreateObject("Test Ball");
            instance.transform.position = position;
            instance.AddComponent<SphereCollider>().radius = 0.36f;
            var body = instance.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            return instance.AddComponent<BallController>();
        }

        private GameManager CreateGame(BallController ball)
        {
            var goal = CreateObject("Test Goal").AddComponent<GoalController>();
            goal.transform.position = Vector3.right * 20f;
            var text = CreateObject("Test Clear Text").AddComponent<Text>();
            var ui = CreateObject("Test UI").AddComponent<UIManager>();
            ui.Configure(text);
            var manager = CreateObject("Test GameManager").AddComponent<GameManager>();
            manager.Configure(ball, goal, ui);
            return manager;
        }

        private static PhysicsObject[] FindSceneDominoes()
        {
            return Object.FindObjectsByType<PhysicsObject>(FindObjectsInactive.Exclude)
                .Where(part => part.name.StartsWith("Domino") && part.GetComponent<DraggableObject>() != null)
                .OrderBy(part => part.name).ToArray();
        }

        private static bool IsVisibleToCamera(PhysicsObject part, Camera camera)
        {
            var point = camera.WorldToScreenPoint(part.GetComponent<BoxCollider>().bounds.center);
            return Physics.Raycast(camera.ScreenPointToRay(point), out var hit, 100f, ~0,
                QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PhysicsObject>() == part;
        }

        private static bool IsToppled(Transform part)
        {
            return Vector3.Angle(part.up, Vector3.up) > 30f;
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
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
