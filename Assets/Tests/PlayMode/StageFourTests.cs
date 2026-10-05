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
    public class StageFourTests
    {
        private Touchscreen touchscreen;
        private static readonly Vector3 HighPosition = new Vector3(-2.6f, 1.6f, 1.3f);
        private static readonly Vector3 LowPosition = new Vector3(-0.368f, 0.68f, -0.932f);

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
        public IEnumerator InitialStage_HasTwoDistinctHeights_AndCompactTouchControls()
        {
            yield return LoadScene("StageFour");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            Assert.That(FindParts(), Is.Empty);
            Assert.That(Object.FindAnyObjectByType<StageObjective>(), Is.Null);
            Assert.That(GameObject.Find("FixedShortRamp"), Is.Null);
            Assert.That(GameObject.Find("Stage/ValleyFloor").GetComponent<Collider>().bounds.max.y,
                Is.LessThan(-2f));
            Assert.That(FindText("StageLabel").text, Does.Contain("DEPTH CROSSING"));
            Assert.That(FindText("GoalHint").text, Does.Contain("BACK TO FRONT"));
            Assert.That(FindButton("AddHighRampButton").GetComponentInChildren<Text>().text, Is.EqualTo("+ HIGH (1)"));
            Assert.That(FindButton("AddLowRampButton").GetComponentInChildren<Text>().text, Is.EqualTo("+ LOW (1)"));
            Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.False);
            AssertCompactLayoutAndRestore();
            var high = spawner.AddPart(0);
            var low = spawner.AddPart(1);
            Assert.That(high.DisplayName, Is.EqualTo("HIGH"));
            Assert.That(low.DisplayName, Is.EqualTo("LOW"));
            Assert.That(high.transform.position.y, Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(low.transform.position.y, Is.EqualTo(0.68f).Within(0.001f));
            foreach (int index in new[] { -1, 0, 1, 2 })
            {
                Assert.That(spawner.AddPart(index), Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator EmptyOrOneRamp_CannotBridgeValley_AndResetRestoresBallAndInventory()
        {
            foreach (int onlyPart in new[] { -1, 0, 1 })
            {
                yield return LoadScene("StageFour");
                var gm = Object.FindAnyObjectByType<GameManager>();
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                var ball = Object.FindAnyObjectByType<BallController>();
                var start = ball.transform.position;
                if (onlyPart >= 0)
                {
                    Place(spawner.AddPart(onlyPart), onlyPart == 0 ? HighPosition : LowPosition,
                        onlyPart == 0 ? 30f : 60f);
                }
                gm.StartSimulation();
                yield return new WaitForSeconds(4f);
                Assert.That(gm.State, Is.EqualTo(GameState.Playing), $"Only part {onlyPart}");
                Assert.That(gm.HasReachedGoal, Is.False);
                gm.ResetSimulation();
                yield return null;
                Assert.That(Vector3.Distance(ball.transform.position, start), Is.LessThan(0.001f));
                Assert.That(ball.Body.isKinematic, Is.True);
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(onlyPart == 0 ? 0 : 1));
                Assert.That(spawner.GetRemainingCount(1), Is.EqualTo(onlyPart == 1 ? 0 : 1));
            }
        }

        [UnityTest]
        public IEnumerator RampsWithoutCorrectDepthOrRotation_DoNotClear()
        {
            for (int layout = 0; layout < 2; layout++)
            {
                yield return LoadScene("StageFour");
                var gm = Object.FindAnyObjectByType<GameManager>();
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                Place(spawner.AddPart(0), HighPosition, layout == 0 ? 0 : 30);
                Place(spawner.AddPart(1), layout == 0 ? LowPosition : new Vector3(LowPosition.x, LowPosition.y, HighPosition.z),
                    layout == 0 ? 0 : 60);
                gm.StartSimulation();
                yield return new WaitForSeconds(4f);
                Assert.That(gm.HasReachedGoal, Is.False, $"Incorrect layout {layout}");
                Assert.That(gm.State, Is.EqualTo(GameState.Playing));
                gm.ResetSimulation();
            }
        }

        [UnityTest]
        public IEnumerator TouchDragAndRotateBothRamps_CrossesDepthNaturally_AndResetsForAnotherLayout()
        {
            yield return LoadScene("StageFour");
            var gm = Object.FindAnyObjectByType<GameManager>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var start = ball.transform.position;
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(401, FindButton("AddHighRampButton"));
            var high = placement.SelectedObject;
            yield return DragTo(402, high, HighPosition);
            yield return Tap(403, FindButton("RotateButton"));
            yield return Tap(404, FindButton("RotateButton"));
            yield return Tap(405, FindButton("AddLowRampButton"));
            var low = placement.SelectedObject;
            yield return DragTo(406, low, LowPosition);
            for (int turn = 0; turn < 4; turn++)
            {
                yield return Tap(407 + turn, FindButton("RotateButton"));
            }
            Assert.That(Quaternion.Angle(high.transform.rotation, Rotation(30)), Is.LessThan(0.05f));
            Assert.That(Quaternion.Angle(low.transform.rotation, Rotation(60)), Is.LessThan(0.05f));
            var highProbe = high.transform.Find("Rolling surface").gameObject.AddComponent<BridgeBallContactProbe>();
            var lowProbe = low.transform.Find("Rolling surface").gameObject.AddComponent<BridgeBallContactProbe>();
            for (int run = 0; run < 2; run++)
            {
                if (run == 1)
                {
                    yield return DragTo(420, low, LowPosition + new Vector3(0.12f, 0, 0.12f));
                }
                var positions = new[] { high.transform.position, low.transform.position };
                highProbe.Contacts = lowProbe.Contacts = 0;
                yield return Tap(421 + run * 2, FindButton("SimulationButton"));
                Assert.That(placement.RotateSelected() || placement.DeleteSelected(), Is.False);
                Assert.That(spawner.AddPart(0), Is.Null);
                yield return WaitForClear(gm);
                Assert.That(highProbe.Contacts, Is.GreaterThan(0));
                Assert.That(lowProbe.Contacts, Is.GreaterThan(0));
                Assert.That(start.z - ball.transform.position.z, Is.GreaterThan(3f));
                Assert.That(FindText("GoalHint").text, Does.Contain("NEXT"));
                Assert.That(Object.FindAnyObjectByType<StageManager>().CanAdvance, Is.True);
                Assert.That(FindButton("NextButton").gameObject.activeInHierarchy, Is.True);
                yield return new WaitForSeconds(2f);
                Assert.That(ball.transform.position.y, Is.GreaterThan(0f), "The goal tray must catch the ball.");
                yield return Tap(422 + run * 2, FindButton("SimulationButton"));
                Assert.That(gm.State, Is.EqualTo(GameState.Edit));
                Assert.That(gm.HasReachedGoal, Is.False);
                Assert.That(Vector3.Distance(ball.transform.position, start), Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(high.transform.position, positions[0]), Is.LessThan(0.005f));
                Assert.That(Vector3.Distance(low.transform.position, positions[1]), Is.LessThan(0.005f));
                Assert.That(Quaternion.Angle(high.transform.rotation, Rotation(30)), Is.LessThan(0.05f));
                Assert.That(Quaternion.Angle(low.transform.rotation, Rotation(60)), Is.LessThan(0.05f));
                Assert.That(spawner.GetRemainingCount(0) + spawner.GetRemainingCount(1), Is.EqualTo(0));
                Assert.That(FindParts().Length, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator DeleteRefundsCorrectHeight_AndDeletedRampsStayDeletedAfterReset()
        {
            yield return LoadScene("StageFour");
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var gm = Object.FindAnyObjectByType<GameManager>();
            var high = spawner.AddPart(0);
            var low = spawner.AddPart(1);
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(0));
            Assert.That(spawner.GetRemainingCount(1), Is.EqualTo(1));
            var replacement = spawner.AddPart(1);
            Assert.That(replacement.DisplayName, Is.EqualTo("LOW"));
            gm.StartSimulation();
            yield return new WaitForFixedUpdate();
            gm.ResetSimulation();
            yield return null;
            Assert.That(low == null, Is.True);
            Assert.That(FindParts(), Is.EquivalentTo(new[] { high, replacement }));
            placement.SelectObject(high);
            Assert.That(placement.DeleteSelected(), Is.True);
            Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(spawner.GetRemainingCount(1), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StageThreeNext_LoadsFreshStageFourOnce_WithMutePreserved()
        {
            yield return LoadScene("StageThree");
            var gm = Object.FindAnyObjectByType<GameManager>();
            var stage = Object.FindAnyObjectByType<StageManager>();
            Object.FindAnyObjectByType<PartSpawner>().AddPart(0).transform.position = new Vector3(-1.4f, 0, 0);
            Object.FindAnyObjectByType<AudioManager>().SetMuted(true);
            Assert.That(stage.TryAdvance(), Is.False);
            gm.StartSimulation();
            yield return WaitForClear(gm);
            AssertCompactLayoutAndRestore();
            var next = FindButton("NextButton");
            bool duplicateRejected = false;
            next.onClick.AddListener(() => duplicateRejected = !stage.TryAdvance());
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Tap(450, next);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "StageFour" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StageFour"));
            Assert.That(duplicateRejected, Is.True);
            Assert.That(gm == null && stage == null, Is.True);
            Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsInactive.Exclude).Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<GameManager>().State, Is.EqualTo(GameState.Edit));
            Assert.That(Object.FindAnyObjectByType<AudioManager>().IsMuted, Is.True);
            Assert.That(FindParts(), Is.Empty);
            Assert.That(Object.FindAnyObjectByType<PartSpawner>().GetRemainingCount(0), Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<PartSpawner>().GetRemainingCount(1), Is.EqualTo(1));
            Assert.That(FindText("ClearText").enabled, Is.False);
        }

        private static IEnumerator LoadScene(string name)
        {
            yield return SceneManager.LoadSceneAsync($"Assets/Scenes/{name}.unity", LoadSceneMode.Single);
            yield return null;
        }

        private static Quaternion Rotation(float yaw) => Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -16);

        private static void Place(DraggableObject part, Vector3 position, float yaw)
        {
            part.transform.SetPositionAndRotation(position, Rotation(yaw));
            Physics.SyncTransforms();
        }

        private static IEnumerator WaitForClear(GameManager gm)
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (gm.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(gm.State, Is.EqualTo(GameState.Clear));
            yield return null;
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

        private static DraggableObject[] FindParts() => Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Exclude);
        private static Button FindButton(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(button => button.name == name);
        private static Text FindText(string name) => Object.FindObjectsByType<Text>(FindObjectsInactive.Include).Single(text => text.name == name);

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

    public sealed class BridgeBallContactProbe : MonoBehaviour
    {
        public int Contacts;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null && collision.rigidbody.GetComponent<BallController>() != null)
            {
                Contacts++;
            }
        }
    }
}
