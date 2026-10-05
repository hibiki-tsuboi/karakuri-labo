using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KarakuriLabo.Tests
{
    public class PhaseOneTests
    {
        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            // Component tests use their own physics world, independent of the open stage.
            testScene = SceneManager.CreateScene("PhaseOneTests",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (testScene.IsValid() && testScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(testScene);
            }
        }

        [Test]
        public void StartSimulation_ReleasesBallFromEditState()
        {
            var ball = CreateBall(new Vector3(6f, 0f, 0f));
            var manager = CreateGame(ball, out _, out var clearText);

            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(ball.Body.isKinematic, Is.True);
            Assert.That(clearText.enabled, Is.False);

            manager.StartSimulation();

            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(ball.Body.isKinematic, Is.False);
            Assert.That(clearText.enabled, Is.False);
        }

        [Test]
        public void OrdinaryRigidbodyNamedBall_EnteringGoalDoesNotClear()
        {
            var ball = CreateBall(new Vector3(6f, 0f, 0f));
            var manager = CreateGame(ball, out var goal, out var clearText);
            var notifications = 0;
            goal.BallEntered += _ => notifications++;
            var impostor = CreateObject("Ball");
            impostor.AddComponent<SphereCollider>();
            impostor.AddComponent<Rigidbody>().useGravity = false;
            manager.StartSimulation();

            SimulatePhysics();

            Assert.That(notifications, Is.Zero);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(clearText.enabled, Is.False);
        }

        [Test]
        public void DifferentBall_EnteringGoalDoesNotClearConfiguredStage()
        {
            var ball = CreateBall(new Vector3(6f, 0f, 0f));
            var manager = CreateGame(ball, out var goal, out var clearText);
            var otherBall = CreateBall(Vector3.zero);
            BallController enteredBall = null;
            goal.BallEntered += entered => enteredBall = entered;
            manager.StartSimulation();
            otherBall.BeginSimulation();

            SimulatePhysics();

            Assert.That(enteredBall, Is.SameAs(otherBall), "The trigger must actually fire.");
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(clearText.enabled, Is.False);
        }

        [Test]
        public void ConfiguredBallChildCollider_EnteringGoalClearsAndShowsMessage()
        {
            var ball = CreateBall(Vector3.zero);
            ball.GetComponent<SphereCollider>().enabled = false;
            var child = CreateObject("Ball collider");
            child.transform.SetParent(ball.transform, false);
            child.AddComponent<SphereCollider>();
            var manager = CreateGame(ball, out _, out var clearText);
            manager.StartSimulation();

            SimulatePhysics();

            Assert.That(manager.State, Is.EqualTo(GameState.Clear));
            Assert.That(clearText.gameObject.activeInHierarchy && clearText.enabled, Is.True);
            Assert.That(clearText.text, Is.EqualTo("CLEAR!"));

            manager.StartSimulation();
            Assert.That(manager.State, Is.EqualTo(GameState.Clear),
                "Starting again must not undo a completed stage.");
        }

        [UnityTest]
        public IEnumerator MainScene_BallRollsToGoalAndDisplaysClear()
        {
            yield return TestSceneLoader.Load("Main");

            var scene = SceneManager.GetActiveScene();
            var manager = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var clearText = Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(text =>
                text.gameObject.scene == scene && text.name == "ClearText");

            Assert.That(manager, Is.Not.Null);
            Assert.That(ball, Is.Not.Null);
            Assert.That(manager.State, Is.EqualTo(GameState.Edit));
            Assert.That(ball.Body.isKinematic, Is.True);
            var startPosition = ball.transform.position;
            manager.StartSimulation();
            var deadline = Time.realtimeSinceStartup + 12f;

            while (manager.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(manager.State, Is.EqualTo(GameState.Clear),
                $"Ball did not reach Goal within 12 seconds. Last position: {ball.transform.position}");
            Assert.That(Vector3.Distance(startPosition, ball.transform.position), Is.GreaterThan(0.5f));
            Assert.That(clearText.gameObject.activeInHierarchy && clearText.enabled, Is.True);
            Assert.That(clearText.text, Is.EqualTo("CLEAR!"));
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

            clearText = CreateObject("ClearText").AddComponent<Text>();
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
    }
}
