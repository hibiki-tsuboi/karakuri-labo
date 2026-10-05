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
    public class ToyInteractionTests
    {
        private Touchscreen touchscreen;
        [SetUp] public void SetUp() => touchscreen = InputSystem.AddDevice<Touchscreen>();
        [TearDown] public void TearDown()
        {
            if (touchscreen != null && touchscreen.added) InputSystem.RemoveDevice(touchscreen);
        }

        [UnityTest]
        public IEnumerator EveryNewToolCanBeAddedDraggedRotatedAndDeletedWithTouch()
        {
            for (int i = 0; i < ToyStageTests.Scenes.Length; i++)
            {
                yield return SceneManager.LoadSceneAsync(ToyStageTests.Scenes[i]);
                yield return null;
                yield return Tap(9000 + i * 10, Button("AddToyButton"));
                var placement = Object.FindAnyObjectByType<PlacementManager>();
                var spawner = Object.FindAnyObjectByType<PartSpawner>();
                var part = placement.SelectedObject;
                Assert.That(part, Is.Not.Null);
                Physics.SyncTransforms();
                Vector3 start = part.transform.position;
                Vector2 pointer = PickVisibleSurface(part);
                Plane plane = new Plane(Vector3.up, start);
                Ray ray = Camera.main.ScreenPointToRay(pointer);
                Assert.That(plane.Raycast(ray, out float distance), Is.True);
                Vector3 shift = new Vector3(0.3f, 0, 0.3f);
                Vector2 target = Camera.main.WorldToScreenPoint(ray.GetPoint(distance) + shift);
                yield return Touch(9001 + i * 10, TouchPhase.Began, pointer);
                Assert.That(placement.IsDragging, Is.True, ToyStageTests.Scenes[i]);
                Assert.That(Object.FindAnyObjectByType<StageSelectHUD>().TryOpen(), Is.False);
                yield return Touch(9001 + i * 10, TouchPhase.Moved, target);
                yield return Touch(9001 + i * 10, TouchPhase.Ended, target);
                Assert.That(Vector3.Distance(part.transform.position, start + shift), Is.LessThan(0.006f));
                Assert.That(part.transform.position.y, Is.EqualTo(start.y).Within(0.001f));
                Quaternion rotation = part.transform.rotation;
                yield return Tap(9002 + i * 10, Button("RotateButton"));
                Assert.That(Quaternion.Angle(rotation, part.transform.rotation), Is.EqualTo(15).Within(0.01f));
                yield return Tap(9003 + i * 10, Button("DeleteButton"));
                Assert.That(placement.SelectedObject, Is.Null);
                Assert.That(spawner.GetRemainingCount(0), Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<BallMechanism>(FindObjectsSortMode.None), Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator StagePickerFitsCompactSafeArea_BlocksSceneGestures_AndLoadsOnTap()
        {
            yield return SceneManager.LoadSceneAsync("StageOne");
            yield return null;
            yield return Tap(9100, Button("StagesButton"));
            var picker = Object.FindAnyObjectByType<StageSelectHUD>();
            Assert.That(picker.IsOpen, Is.True);
            var safe = Object.FindAnyObjectByType<SafeAreaPanel>().GetComponent<RectTransform>();
            Vector2 min = safe.anchorMin, max = safe.anchorMax, size = safe.sizeDelta, position = safe.anchoredPosition;
            try
            {
                safe.anchorMin = safe.anchorMax = Vector2.one * 0.5f;
                safe.anchoredPosition = Vector2.zero;
                safe.sizeDelta = new Vector2(1000, 540);
                LayoutRebuilder.ForceRebuildLayoutImmediate(safe);
                Canvas.ForceUpdateCanvases();
                Rect bounds = ScreenRect(safe);
                var buttons = safe.Find("StageMenu").GetComponentsInChildren<Button>();
                Assert.That(buttons.Length, Is.EqualTo(10));
                foreach (Button button in buttons)
                {
                    Rect rect = ScreenRect(button.GetComponent<RectTransform>());
                    Assert.That(bounds.Contains(rect.min) && bounds.Contains(rect.max), Is.True, button.name);
                    Assert.That(rect.width, Is.GreaterThan(44));
                    Assert.That(rect.height, Is.GreaterThan(44));
                }
                Vector2 covered = Camera.main.WorldToScreenPoint(Object.FindAnyObjectByType<BallController>().transform.position);
                var placement = Object.FindAnyObjectByType<PlacementManager>();
                Assert.That(placement.BeginPointer(covered, 9101), Is.False);
            }
            finally
            {
                safe.anchorMin = min; safe.anchorMax = max; safe.sizeDelta = size; safe.anchoredPosition = position;
                LayoutRebuilder.ForceRebuildLayoutImmediate(safe);
                Canvas.ForceUpdateCanvases();
            }
            yield return Tap(9102, Button("CloseStagesButton"));
            Assert.That(picker.IsOpen, Is.False);
            yield return Tap(9103, Button("StagesButton"));
            yield return Tap(9104, Button("StageChoice8"));
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != "StageFan" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("StageFan"));
        }

        private static Vector2 PickVisibleSurface(DraggableObject part)
        {
            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>())
            {
                Vector2 screen = Camera.main.WorldToScreenPoint(renderer.bounds.center);
                if (screen.y < Screen.height * 0.24f || screen.y > Screen.height * 0.80f) continue;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(screen), out RaycastHit hit, 100,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<DraggableObject>() == part)
                    return screen;
            }
            Assert.Fail("No visible touch surface on " + part.name);
            return Vector2.zero;
        }

        private static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b => b.name == name);
        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        private IEnumerator Tap(int id, Button button)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            yield return Touch(id, TouchPhase.Began, point);
            yield return Touch(id, TouchPhase.Ended, point);
        }
        private static IEnumerator Touch(int id, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(Touchscreen.current, new TouchState
            {
                touchId = id, phase = phase, position = position, pressure = phase == TouchPhase.Ended ? 0 : 1
            });
            yield return null;
            yield return null;
        }
    }
}
