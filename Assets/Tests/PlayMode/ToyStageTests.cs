using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Tests
{
    public class ToyStageTests
    {
        internal static readonly string[] Scenes = { "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" };
        internal static readonly Vector3[] Solutions = { new Vector3(-2, 0.2f, 1.3f), new Vector3(0.1f, 0.55f, -1.3f),
            new Vector3(-0.7f, 0.55f, 0), new Vector3(-0.5f, 0, 0), new Vector3(-4.1f, 0, 1.2f) };

        [UnityTest] public IEnumerator Spring_JumpsTheGap_AndResets() => VerifySolution(0);
        [UnityTest] public IEnumerator Curve_TurnsIntoTheDepthLane_AndResets() => VerifySolution(1);
        [UnityTest] public IEnumerator Funnel_CatchesThenDropsThroughItsOpening_AndResets() => VerifySolution(2);
        [UnityTest] public IEnumerator Lift_CarriesTheBallUpAndReleasesIt_AndResets() => VerifySolution(3);
        [UnityTest] public IEnumerator Fan_PushesTheStationaryBallUphill_AndResets() => VerifySolution(4);

        [UnityTest]
        public IEnumerator AllFiveToolsWorkWithTheLivePhysicsLoop_AndAreAccessibleFromStageSelection()
        {
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 3;
                yield return SceneManager.LoadSceneAsync("StageOne");
                yield return null;
                Object.FindAnyObjectByType<AudioManager>().SetMuted(true);
                for (int i = 0; i < Scenes.Length; i++)
                {
                    StageSelectHUD menu = Object.FindAnyObjectByType<StageSelectHUD>();
                    Assert.That(menu.TryOpen(), Is.True);
                    Assert.That(menu.TrySelect(i + 4), Is.True);
                    Assert.That(menu.TrySelect(i + 4), Is.False, "Only one load can be started.");
                    float deadline = Time.realtimeSinceStartup + 5;
                    while (SceneManager.GetActiveScene().name != Scenes[i] && Time.realtimeSinceStartup < deadline) yield return null;
                    yield return null;
                    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(Scenes[i]));
                    Assert.That(Object.FindAnyObjectByType<AudioManager>().IsMuted, Is.True);
                    DraggableObject part = PlaceSolution(i);
                    Physics.SyncTransforms();
                    GameManager gm = Object.FindAnyObjectByType<GameManager>();
                    gm.StartSimulation();
                    Assert.That(Object.FindAnyObjectByType<StageSelectHUD>().TryOpen(), Is.False, "A running attempt must be reset before changing stages.");
                    deadline = Time.realtimeSinceStartup + 7;
                    while (gm.State != GameState.Clear && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(gm.State, Is.EqualTo(GameState.Clear), Scenes[i] + ": " + Object.FindAnyObjectByType<BallController>().Body.position);
                    Assert.That(part.GetComponent<BallMechanism>().WasUsed, Is.True);
                    Assert.That(Object.FindAnyObjectByType<StageSelectHUD>().TryOpen(), Is.True);
                }
            }
            finally
            {
                Time.timeScale = previousScale;
            }
        }

        [UnityTest]
        public IEnumerator DirectionMattersForSpringAndFan_AndSolidWallsBlockTheWind()
        {
            foreach (int i in new[] { 0, 4 })
            {
                yield return SceneManager.LoadSceneAsync(Scenes[i]);
                yield return null;
                DraggableObject part = PlaceSolution(i);
                part.transform.Rotate(0, 180, 0, Space.World);
                var gm = Object.FindAnyObjectByType<GameManager>();
                Simulate(gm, 14);
                yield return null;
                Assert.That(gm.State, Is.EqualTo(GameState.Playing), Scenes[i] + " facing backwards");
            }
            yield return SceneManager.LoadSceneAsync("StageFan");
            yield return null;
            DraggableObject fan = PlaceSolution(4);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetPositionAndRotation(fan.transform.TransformPoint(new Vector3(0.9f, 0.8f, 0)), fan.transform.rotation);
            wall.transform.localScale = new Vector3(0.2f, 2, 2);
            Simulate(Object.FindAnyObjectByType<GameManager>(), 8);
            Assert.That(fan.GetComponent<WindFan>().WasUsed, Is.False, "A wall between the fan and ball must block its force.");
        }

        [UnityTest]
        public IEnumerator ResetDuringLiftTravelRestoresTheCarriageAndClosedGate()
        {
            yield return SceneManager.LoadSceneAsync("StageLift");
            yield return null;
            DraggableObject part = PlaceSolution(3);
            var lift = part.GetComponent<BallLift>();
            var gm = Object.FindAnyObjectByType<GameManager>();
            Physics.SyncTransforms();
            gm.StartSimulation();
            float deadline = Time.realtimeSinceStartup + 6;
            while (lift.Height < 0.9f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(lift.Height, Is.InRange(0.9f, 3.19f));
            gm.ResetSimulation();
            yield return new WaitForFixedUpdate();
            Assert.That(lift.Height, Is.Zero);
            Assert.That(lift.WasUsed, Is.False);
            Assert.That(part.transform.Find("Carriage").localPosition.y, Is.EqualTo(0.2f).Within(0.002f));
            Assert.That(part.transform.Find("Carriage/Exit gate").GetComponentInChildren<Collider>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator MissingOrMisplacedToolsCannotClear_AndInventoryReturnsAfterDeletion()
        {
            for (int index = 0; index < Scenes.Length; index++)
            {
                yield return SceneManager.LoadSceneAsync(Scenes[index]);
                yield return null;
                var gm = Object.FindAnyObjectByType<GameManager>();
                Simulate(gm, 14);
                yield return null;
                Assert.That(gm.State, Is.EqualTo(GameState.Playing), Scenes[index] + " without a tool");
                gm.ResetSimulation();
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                var part = spawner.AddPart(0);
                Assert.That(part, Is.Not.Null);
                Assert.That(spawner.GetRemainingCount(0), Is.Zero);
                Assert.That(spawner.AddPart(0), Is.Null);
                Assert.That(part.GetComponentsInChildren<Collider>().Any(c => !c.isTrigger), Is.True, "Every tool must be touch-selectable.");
                Simulate(gm, 14);
                yield return null;
                Assert.That(gm.State, Is.EqualTo(GameState.Playing), Scenes[index] + " with the tool on its storage position");
                gm.ResetSimulation();
                var placement = Object.FindAnyObjectByType<PlacementManager>();
                Assert.That(placement.SelectObject(part), Is.True);
                Assert.That(placement.DeleteSelected(), Is.True);
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
                Assert.That(spawner.AddPart(0), Is.Not.Null);
            }
        }

        internal static DraggableObject PlaceSolution(int index)
        {
            DraggableObject part = Object.FindAnyObjectByType<PartSpawner>().AddPart(0);
            part.transform.SetPositionAndRotation(Solutions[index], Quaternion.Euler(0, index == 0 || index == 4 ? 30 : 0, 0));
            return part;
        }

        private static IEnumerator VerifySolution(int index)
        {
            yield return SceneManager.LoadSceneAsync(Scenes[index]);
            yield return null;
            DraggableObject part = PlaceSolution(index);
            BallMechanism mechanism = part.GetComponent<BallMechanism>();
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            BallController ball = Object.FindAnyObjectByType<BallController>();
            Vector3 ballStart = ball.transform.position;
            var trace = new List<string>();
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.That(mechanism.WasUsed, Is.False);
                Assert.That(gm.State, Is.EqualTo(GameState.Edit));
                Simulate(gm, 16, trace);
                yield return null;
                Assert.That(gm.State, Is.EqualTo(GameState.Clear), Scenes[index] + " attempt " + attempt + "\n" + string.Join("\n", trace));
                Assert.That(mechanism.WasUsed, Is.True);
                Assert.That(Object.FindAnyObjectByType<PartSpawner>().CanAddParts, Is.False);
                if (index == 3) Assert.That(part.GetComponent<BallLift>().Height, Is.EqualTo(3.2f).Within(0.001f));
                gm.ResetSimulation();
                yield return new WaitForFixedUpdate();
                Assert.That(mechanism.WasUsed, Is.False);
                Assert.That(Vector3.Distance(ball.transform.position, ballStart), Is.LessThan(0.002f));
                Assert.That(Vector3.Distance(part.transform.position, Solutions[index]), Is.LessThan(0.002f));
                if (index == 3) Assert.That(part.GetComponent<BallLift>().Height, Is.Zero);
            }
        }

        internal static void Simulate(GameManager manager, float seconds, List<string> trace = null)
        {
            SimulationMode previous = Physics.simulationMode;
            BallMechanism[] mechanisms = Object.FindObjectsByType<BallMechanism>(FindObjectsSortMode.None);
            BallController ball = Object.FindAnyObjectByType<BallController>();
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                manager.StartSimulation();
                for (int step = 0; step < seconds / 0.02f && manager.State != GameState.Clear; step++)
                {
                    foreach (BallMechanism mechanism in mechanisms) mechanism.StepPhysics(0.02f);
                    Physics.Simulate(0.02f);
                    if (step % 25 == 0) trace?.Add($"{step * 0.02f:F1}s: {ball.Body.position:F2}, velocity {ball.Body.linearVelocity:F2}, used={mechanisms.Any(m => m.WasUsed)}, goal={manager.HasReachedGoal}");
                    if (manager.HasReachedGoal && mechanisms.Length > 0 && mechanisms.All(m => m.WasUsed)) break;
                }
            }
            finally
            {
                Physics.simulationMode = previous;
            }
        }
    }
}
