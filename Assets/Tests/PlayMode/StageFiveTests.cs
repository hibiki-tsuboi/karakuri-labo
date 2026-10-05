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
    public class StageFiveTests
    {
        private Touchscreen touchscreen;
        private static readonly Vector3[] Solutions =
        {
            new Vector3(-0.368f, 4.36f, 3.768f), new Vector3(-0.368f, 2.52f, -2.336f),
            new Vector3(-0.368f, 0.68f, -8.44f)
        };
        private static readonly string[] Names = { "Upper", "Middle", "Lower" };

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
        public IEnumerator LargeStage_ViewsRevealOffscreenGoal_AndControlsFitCompactSafeArea()
        {
            yield return Load("StageFive");
            var nav = Object.FindAnyObjectByType<StageViewNavigator>();
            var camera = Camera.main;
            var goal = Object.FindAnyObjectByType<GoalController>();
            Assert.That(camera.WorldToViewportPoint(goal.transform.position).y, Is.LessThan(0));
            Assert.That(Object.FindObjectsByType<DraggableObject>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindAnyObjectByType<BallController>().transform.position.y - goal.transform.position.y,
                Is.GreaterThan(6));
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            for (int i = 1; i < 4; i++)
            {
                yield return Tap(800 + i, Button("AreaView" + i));
                Assert.That(nav.CurrentView, Is.EqualTo(i));
            }
            Vector3 point = camera.WorldToViewportPoint(goal.transform.position);
            Assert.That(point.x, Is.InRange(0.1f, 0.9f));
            Assert.That(point.y, Is.InRange(0.18f, 0.9f));
            Assert.That(camera.orthographicSize, Is.GreaterThan(10));
            yield return Tap(804, Button("ResetViewButton"));
            Assert.That(nav.CurrentView, Is.EqualTo(0));
            Assert.That(camera.orthographicSize, Is.EqualTo(4.8f).Within(0.001f));
            AssertCompactLayoutAndRestore();
        }

        [UnityTest]
        public IEnumerator EveryGapIsRequired_AndWrongTurnCannotClear()
        {
            for (int missing = -1; missing < 4; missing++)
            {
                yield return Load("StageFive");
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                for (int i = 0; i < 3; i++)
                {
                    if (missing == -1 || missing == i)
                    {
                        continue;
                    }
                    var part = spawner.AddPart(i);
                    part.transform.SetPositionAndRotation(Solutions[i], Rotation(missing == 3 && i == 1 ? 0 : Yaw(i)));
                }
                var gm = Object.FindAnyObjectByType<GameManager>();
                Vector3 start = Object.FindAnyObjectByType<BallController>().transform.position;
                Simulate(gm, 12);
                Assert.That(gm.State, Is.EqualTo(GameState.Failed), "Missing/wrong part " + missing);
                Assert.That(gm.HasReachedGoal, Is.False);
                gm.ResetSimulation();
                Assert.That(Vector3.Distance(Object.FindAnyObjectByType<BallController>().transform.position, start), Is.LessThan(0.001f));
            }
        }

        [UnityTest]
        public IEnumerator TouchPlacementAcrossThreeViews_ClearsNaturally_FollowsBall_AndRestoresAllParts()
        {
            yield return Load("StageFive");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var nav = Object.FindAnyObjectByType<StageViewNavigator>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var parts = new DraggableObject[3];
            var probes = new BridgeBallContactProbe[3];
            for (int i = 0; i < 3; i++)
            {
                yield return Tap(810 + i * 20, Button("Add" + Names[i] + "RampButton"));
                Assert.That(nav.CurrentView, Is.EqualTo(i), "Adding a part must reveal its altitude.");
                parts[i] = placement.SelectedObject;
                Assert.That(parts[i].transform.position.y, Is.EqualTo(Solutions[i].y).Within(0.001f));
                yield return DragTo(811 + i * 20, parts[i], Solutions[i]);
                for (int step = 0; step < Yaw(i) / 15; step++)
                {
                    yield return Tap(812 + i * 20 + step, Button("RotateButton"));
                }
                Assert.That(Quaternion.Angle(parts[i].transform.rotation, Rotation(Yaw(i))), Is.LessThan(0.01f));
                Assert.That(spawner.AddPart(i), Is.Null);
                probes[i] = parts[i].transform.Find("Rolling surface").gameObject.AddComponent<BridgeBallContactProbe>();
            }
            var gm = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var ballStart = ball.transform.position;
            yield return Tap(878, Button("SimulationButton"));
            Assert.That(nav.IsFollowing, Is.True);
            bool sawMiddle = false;
            float deadline = Time.realtimeSinceStartup + 12;
            while (gm.State == GameState.Playing && Time.realtimeSinceStartup < deadline)
            {
                sawMiddle |= nav.CurrentView == 1;
                yield return null;
            }
            Assert.That(gm.State, Is.EqualTo(GameState.Clear));
            Assert.That(sawMiddle, Is.True);
            Assert.That(nav.CurrentView, Is.EqualTo(2));
            Assert.That(probes.All(p => p.Contacts > 0), Is.True, "The ball must use all three placed ramps.");
            Assert.That(ballStart.z - ball.transform.position.z, Is.GreaterThan(15));
            Assert.That(Button("NextButton").gameObject.activeInHierarchy, Is.True);
            Assert.That(Object.FindAnyObjectByType<StageManager>().CanAdvance, Is.True);
            var view = Camera.main.transform.position;
            yield return Tap(879, Button("SimulationButton"));
            Assert.That(gm.State, Is.EqualTo(GameState.Edit));
            Assert.That(Camera.main.transform.position, Is.EqualTo(view));
            Assert.That(Vector3.Distance(ball.transform.position, ballStart), Is.LessThan(0.001f));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(Vector3.Distance(parts[i].transform.position, Solutions[i]), Is.LessThan(0.005f));
                Assert.That(spawner.GetRemainingCount(i), Is.EqualTo(0));
            }
            // A small placement difference must still be playable, including from a rotated view.
            parts[2].transform.position += new Vector3(0.08f, 0, 0.08f);
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            orbit.BeginOrbit(Vector2.zero);
            orbit.MoveOrbit(new Vector2(Screen.width * 0.25f, 0));
            orbit.CancelOrbit();
            Simulate(gm, 12);
            Assert.That(gm.State, Is.EqualTo(GameState.Clear));
        }

        [UnityTest]
        public IEnumerator NavigationNeverMovesParts_AndIsBlockedDuringDragAndOrbit()
        {
            yield return Load("StageFive");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var nav = Object.FindAnyObjectByType<StageViewNavigator>();
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            var part = spawner.AddPart(1);
            var position = part.transform.position;
            Physics.SyncTransforms();
            Vector2 pointer = Camera.main.WorldToScreenPoint(part.transform.Find("Rolling surface").GetComponent<Collider>().bounds.center);
            Assert.That(placement.BeginPointer(pointer), Is.True);
            var view = Camera.main.transform.position;
            Assert.That(nav.ShowView(0), Is.False);
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(885, Button("AreaView0"));
            Assert.That(Camera.main.transform.position, Is.EqualTo(view));
            Assert.That(Button("AreaView0").interactable, Is.False);
            placement.EndPointer();
            orbit.BeginOrbit(Vector2.zero);
            Assert.That(nav.ShowView(0), Is.False);
            orbit.MoveOrbit(new Vector2(Screen.width * 0.25f, 0));
            orbit.CancelOrbit();
            Quaternion viewRotation = orbit.ViewRotation;
            Assert.That(nav.ShowView(0), Is.True);
            Assert.That(Quaternion.Angle(orbit.ViewRotation, viewRotation), Is.LessThan(0.05f));
            Assert.That(part.transform.position, Is.EqualTo(position));
            Assert.That(nav.ShowView(8), Is.False);
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(spawner.GetRemainingCount(1), Is.EqualTo(1));
            var replacement = spawner.AddPart(1);
            Assert.That(replacement.transform.position, Is.EqualTo(position));
            Assert.That(nav.CurrentView, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ManualViewsOverrideFollowing_UntilFollowButton_AndResetViewReturnsHome()
        {
            yield return Load("StageFive");
            var gm = Object.FindAnyObjectByType<GameManager>();
            var nav = Object.FindAnyObjectByType<StageViewNavigator>();
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            gm.StartSimulation();
            yield return Tap(890, Button("AreaView3"));
            Assert.That(nav.IsFollowing, Is.False);
            Assert.That(nav.CurrentView, Is.EqualTo(3));
            yield return Tap(891, Button("FollowBallButton"));
            Assert.That(nav.IsFollowing, Is.True);
            yield return Tap(892, Button("ResetViewButton"));
            Assert.That(nav.IsFollowing, Is.False);
            Assert.That(nav.CurrentView, Is.EqualTo(0));
            Assert.That(Camera.main.orthographicSize, Is.EqualTo(4.8f).Within(0.001f));
            gm.ResetSimulation();
            yield return null;
            Assert.That(Button("FollowBallButton").interactable, Is.False);
            Assert.That(nav.ShowView(2), Is.True);
        }

        [UnityTest]
        public IEnumerator StageFourNext_LoadsFreshLongStage_AndPreservesMute()
        {
            yield return Load("StageFour");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            spawner.AddPart(0).transform.SetPositionAndRotation(new Vector3(-2.6f, 1.6f, 1.3f), Rotation(30));
            spawner.AddPart(1).transform.SetPositionAndRotation(new Vector3(-0.368f, 0.68f, -0.932f), Rotation(60));
            Object.FindAnyObjectByType<AudioManager>().SetMuted(true);
            Simulate(Object.FindAnyObjectByType<GameManager>(), 8);
            var stages = Object.FindAnyObjectByType<StageManager>();
            Assert.That(stages.CanAdvance, Is.True);
            AssertCompactLayoutAndRestore();
            Assert.That(stages.TryAdvance(), Is.True);
            Assert.That(stages.TryAdvance(), Is.False);
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != "StageFive" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StageFive"));
            Assert.That(Object.FindAnyObjectByType<GameManager>().State, Is.EqualTo(GameState.Edit));
            Assert.That(Object.FindAnyObjectByType<AudioManager>().IsMuted, Is.True);
            Assert.That(Object.FindObjectsByType<DraggableObject>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindAnyObjectByType<StageViewNavigator>().CurrentView, Is.EqualTo(0));
        }

        private static float Yaw(int index) => index == 1 ? 120 : 60;
        private static Quaternion Rotation(float yaw) => Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -16);
        private static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b => b.name == name);

        private static void Simulate(GameManager gm, float seconds)
        {
            var mode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                gm.StartSimulation();
                for (int i = 0; i < seconds / 0.02f && gm.State == GameState.Playing; i++)
                {
                    Physics.Simulate(0.02f);
                    Object.FindAnyObjectByType<AttemptMonitor>()?.StepAttempt(0.02f);
                }
            }
            finally
            {
                Physics.simulationMode = mode;
            }
        }

        private static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            Canvas.ForceUpdateCanvases();
        }
        private IEnumerator DragTo(int id, DraggableObject part, Vector3 destination)
        {
            Physics.SyncTransforms();
            var camera = Camera.main;
            var pointer = (Vector2)camera.WorldToScreenPoint(part.transform.Find("Rolling surface").GetComponent<BoxCollider>().bounds.center);
            var ray = camera.ScreenPointToRay(pointer);
            var plane = new Plane(Vector3.up, part.transform.position);
            Assert.That(plane.Raycast(ray, out float distance), Is.True);
            var target = (Vector2)camera.WorldToScreenPoint(ray.GetPoint(distance) + destination - part.transform.position);
            yield return Touch(id, TouchPhase.Began, pointer);
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(placement.SelectedObject, Is.SameAs(part));
            yield return Touch(id, TouchPhase.Moved, Vector2.Lerp(pointer, target, 0.5f));
            yield return Touch(id, TouchPhase.Moved, target);
            yield return Touch(id, TouchPhase.Ended, target);
            Assert.That(Vector3.Distance(part.transform.position, destination), Is.LessThan(0.005f));
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
