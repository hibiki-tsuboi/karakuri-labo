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
    public class CameraOrbitTests
    {
        private Touchscreen touchscreen;
        private Mouse mouse;

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
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryStage_HasOrbitControlsThatFitCompactSafeArea()
        {
            foreach (string scene in new[] { "Main", "StageOne", "StageTwo", "StageThree", "StageFour", "StageFive",
                "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" })
            {
                yield return Load(scene);
                var camera = Camera.main;
                var orbit = camera.GetComponent<StageCameraOrbit>();
                Assert.That(orbit, Is.Not.Null, scene);
                AssertRotation(orbit.ViewRotation, Quaternion.identity);
                Assert.That(FindButton("ResetViewButton").interactable, Is.True);
                Assert.That(Vector3.Distance(camera.transform.position, orbit.ViewFocus), Is.GreaterThan(5));
                AssertCompactLayoutAndRestore();
                Vector3 homePosition = camera.transform.position;
                Quaternion homeRotation = camera.transform.rotation;
                orbit.BeginOrbit(Vector2.zero);
                orbit.MoveOrbit(new Vector2(ShortSide * 0.3f, -ShortSide * 0.1f));
                orbit.CancelOrbit();
                AssertUpright(camera.transform);
                orbit.ResetView();
                AssertPose(camera.transform, homePosition, homeRotation);
            }
        }

        [UnityTest]
        public IEnumerator EmptyTouchSwipes_RotateBothWaysAroundStage_AndFullTurnRetainsFraming()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            Vector3 start = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Vector3 pivot = new Vector3(-0.5f, 0.2f, -0.7f);
            float distance = Vector3.Distance(start, pivot);
            float zoom = camera.orthographicSize;
            var ball = Object.FindAnyObjectByType<BallController>();
            Vector3 ballPosition = ball.transform.position;
            // The world can rotate underneath an owned background gesture.
            Vector2 startPointer = Point(0.15f, 0.6f);
            for (int index = 0; index < 6; index++)
            {
                yield return Swipe(601 + index, startPointer, startPointer + Vector2.right * ShortSide * 0.5f);
                Assert.That(Vector3.Distance(camera.transform.position, pivot), Is.EqualTo(distance).Within(0.001f));
                Assert.That(camera.orthographicSize, Is.EqualTo(zoom));
                Assert.That(camera.transform.position.y, Is.EqualTo(start.y).Within(0.002f));
                AssertUpright(camera.transform);
                AssertRotation(orbit.ViewRotation, Quaternion.AngleAxis(60 * (index + 1), Vector3.up));
            }
            AssertPose(camera.transform, start, rotation);
            yield return Swipe(608, startPointer + Vector2.right * ShortSide * 0.25f, startPointer);
            AssertRotation(orbit.ViewRotation, Quaternion.AngleAxis(-30, Vector3.up));
            yield return Tap(609, FindButton("ResetViewButton"));
            AssertPose(camera.transform, start, rotation);
            Assert.That(ball.transform.position, Is.EqualTo(ballPosition));
            Assert.That(ball.Body.isKinematic, Is.True);
        }

        [UnityTest]
        public IEnumerator TapsIgnoreJitter_AndVerticalAndDiagonalSwipesRotate()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            Object.FindAnyObjectByType<PartSpawner>().AddPart(0);
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            Vector2 point = Point(0.15f, 0.6f);
            yield return Touch(610, TouchPhase.Began, point);
            Assert.That(placement.SelectedObject, Is.Null);
            yield return Touch(610, TouchPhase.Ended, point + new Vector2(Screen.width * 0.002f, 0));
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
            Vector3 cameraStart = Camera.main.transform.position;
            yield return Swipe(611, point, point + new Vector2(0, Screen.height * 0.2f));
            Assert.That(Quaternion.Angle(orbit.ViewRotation, Quaternion.identity), Is.GreaterThan(20));
            Assert.That(Mathf.Abs(Camera.main.transform.position.y - cameraStart.y), Is.GreaterThan(1));
            var verticalView = orbit.ViewRotation;
            yield return Swipe(612, point, point + new Vector2(ShortSide * 0.15f, ShortSide * 0.15f));
            Assert.That(Quaternion.Angle(orbit.ViewRotation, verticalView), Is.GreaterThan(20));
            Assert.That(placement.CanModifyParts, Is.True);
        }

        [UnityTest]
        public IEnumerator DraggingPartFromRotatedView_PreservesHeight_AndBlocksSecondFingerCamera()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            yield return Swipe(620, Point(0.15f, 0.6f), Point(0.3f, 0.52f));
            var camera = Camera.main;
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var part = Object.FindAnyObjectByType<PartSpawner>().AddPart(0);
            Physics.SyncTransforms();
            var before = part.transform.position;
            var cameraPosition = camera.transform.position;
            var cameraRotation = camera.transform.rotation;
            Vector2 pointer = camera.WorldToScreenPoint(part.transform.Find("Rolling surface").GetComponent<Collider>().bounds.center);
            Vector2 end = pointer + new Vector2(Screen.width * 0.025f, Screen.height * 0.02f);
            var plane = new Plane(Vector3.up, before);
            var ray = camera.ScreenPointToRay(pointer);
            Assert.That(plane.Raycast(ray, out float distance), Is.True);
            var offset = before - ray.GetPoint(distance);
            ray = camera.ScreenPointToRay(end);
            Assert.That(plane.Raycast(ray, out distance), Is.True);
            var expected = ray.GetPoint(distance) + offset;
            yield return Touch(621, TouchPhase.Began, pointer);
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(FindButton("ResetViewButton").interactable, Is.False);
            yield return Swipe(622, Point(0.15f, 0.6f), Point(0.3f, 0.6f));
            Assert.That(placement.IsDragging, Is.True);
            AssertPose(camera.transform, cameraPosition, cameraRotation);
            yield return Touch(621, TouchPhase.Ended, end);
            Assert.That(Vector3.Distance(part.transform.position, expected), Is.LessThan(0.005f));
            Assert.That(part.transform.position.y, Is.EqualTo(before.y).Within(0.001f));
            Assert.That(placement.SelectedObject, Is.SameAs(part));
            AssertPose(camera.transform, cameraPosition, cameraRotation);
        }

        [UnityTest]
        public IEnumerator OrbitGesture_BlocksSecondFingerPartsPaletteAndViewReset_UntilRelease()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            var part = spawner.AddPart(0);
            var position = part.transform.position;
            var rotation = part.transform.rotation;
            yield return Touch(630, TouchPhase.Began, Point(0.15f, 0.6f));
            yield return Touch(630, TouchPhase.Moved, Point(0.25f, 0.6f));
            Quaternion ownedView = Camera.main.GetComponent<StageCameraOrbit>().ViewRotation;
            Assert.That(placement.IsOrbiting, Is.True);
            Assert.That(placement.CanModifyParts, Is.False);
            Assert.That(placement.SelectObject(part), Is.False);
            Assert.That(placement.RotateSelected() || placement.DeleteSelected(), Is.False);
            Assert.That(spawner.AddPart(1), Is.Null);
            Assert.That(FindButton("AddLowRampButton").interactable, Is.False);
            Assert.That(FindButton("ResetViewButton").interactable, Is.False);
            yield return Tap(631, FindButton("AddLowRampButton"));
            yield return Tap(632, FindButton("ResetViewButton"));
            Physics.SyncTransforms();
            Vector2 pointer = Camera.main.WorldToScreenPoint(part.transform.Find("Rolling surface").GetComponent<Collider>().bounds.center);
            yield return Swipe(633, pointer, pointer + new Vector2(50, 0));
            Assert.That(placement.IsOrbiting, Is.True);
            AssertRotation(Camera.main.GetComponent<StageCameraOrbit>().ViewRotation, ownedView);
            AssertPose(part.transform, position, rotation);
            Assert.That(spawner.GetRemainingCount(1), Is.EqualTo(1));
            yield return Touch(630, TouchPhase.Ended, Point(0.3f, 0.6f));
            Assert.That(Quaternion.Angle(Camera.main.GetComponent<StageCameraOrbit>().ViewRotation, ownedView), Is.GreaterThan(5));
            Assert.That(placement.CanModifyParts, Is.True);
            Assert.That(spawner.AddPart(1), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator UiOriginCancelFocusPauseAndDeviceLoss_DoNotLeaveCameraGestureActive()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            var button = FindButton("ResetViewButton");
            yield return Swipe(640, ScreenRect((RectTransform)button.transform).center, Point(0.15f, 0.6f));
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
            yield return Touch(641, TouchPhase.Began, Point(0.15f, 0.6f));
            yield return Touch(641, TouchPhase.Canceled, Point(0.4f, 0.6f));
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
            Assert.That(placement.IsOrbiting, Is.False);
            foreach (bool pause in new[] { false, true })
            {
                int id = pause ? 643 : 642;
                yield return Touch(id, TouchPhase.Began, Point(0.15f, 0.6f));
                Assert.That(placement.IsOrbiting, Is.True);
                placement.SendMessage(pause ? "OnApplicationPause" : "OnApplicationFocus", pause);
                yield return Touch(id, TouchPhase.Moved, Point(0.4f, 0.6f));
                AssertRotation(orbit.ViewRotation, Quaternion.identity);
                Assert.That(placement.IsOrbiting, Is.False);
                yield return Touch(id, TouchPhase.Ended, Point(0.4f, 0.6f));
                placement.SendMessage(pause ? "OnApplicationPause" : "OnApplicationFocus", !pause);
            }
            yield return Touch(644, TouchPhase.Began, Point(0.15f, 0.6f));
            InputSystem.RemoveDevice(touchscreen);
            yield return null;
            yield return null;
            Assert.That(placement.IsOrbiting, Is.False);
            Assert.That(placement.CanModifyParts, Is.True);
        }

        [UnityTest]
        public IEnumerator OrbitDuringPlayAndClear_DoesNotChangeSolution_AndSimulationResetKeepsView()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var spawner = Object.FindAnyObjectByType<PartSpawner>();
            spawner.AddPart(0).transform.SetPositionAndRotation(new Vector3(-2.6f, 1.6f, 1.3f),
                Quaternion.Euler(0, 30, 0) * Quaternion.Euler(0, 0, -16));
            spawner.AddPart(1).transform.SetPositionAndRotation(new Vector3(-0.368f, 0.68f, -0.932f),
                Quaternion.Euler(0, 60, 0) * Quaternion.Euler(0, 0, -16));
            var gm = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var ballPosition = ball.transform.position;
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            Physics.SyncTransforms();
            gm.StartSimulation();
            yield return Swipe(650, Point(0.15f, 0.6f), Point(0.4f, 0.6f));
            Assert.That(Quaternion.Angle(orbit.ViewRotation, Quaternion.identity), Is.GreaterThan(25));
            Quaternion playingView = orbit.ViewRotation;
            float deadline = Time.realtimeSinceStartup + 10;
            while (gm.State != GameState.Clear && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(gm.State, Is.EqualTo(GameState.Clear));
            yield return Swipe(651, Point(0.15f, 0.6f), Point(0.4f, 0.6f));
            Assert.That(Quaternion.Angle(orbit.ViewRotation, playingView), Is.GreaterThan(25));
            var cameraPosition = Camera.main.transform.position;
            var cameraRotation = Camera.main.transform.rotation;
            yield return Tap(652, FindButton("SimulationButton"));
            Assert.That(gm.State, Is.EqualTo(GameState.Edit));
            AssertPose(Camera.main.transform, cameraPosition, cameraRotation);
            Assert.That(Vector3.Distance(ball.transform.position, ballPosition), Is.LessThan(0.001f));
            yield return Tap(653, FindButton("ResetViewButton"));
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
            Assert.That(spawner.GetRemainingCount(0) + spawner.GetRemainingCount(1), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MouseBackgroundDrag_UsesReleaseCoordinates_AndViewButtonRestoresHome()
        {
            yield return Load("StageFour");
            mouse = InputSystem.AddDevice<Mouse>();
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            yield return MouseAt(Point(0.15f, 0.6f), true);
            yield return MouseAt(Point(0.35f, 0.6f), true);
            Assert.That(Quaternion.Angle(orbit.ViewRotation, Quaternion.identity), Is.EqualTo(120 * Screen.width * 0.2f / ShortSide).Within(0.05f));
            yield return MouseAt(Point(0.4f, 0.6f), false);
            Assert.That(Quaternion.Angle(orbit.ViewRotation, Quaternion.identity), Is.EqualTo(120 * Screen.width * 0.25f / ShortSide).Within(0.05f));
            Assert.That(orbit.IsOrbiting, Is.False);
            Vector2 button = ScreenRect((RectTransform)FindButton("ResetViewButton").transform).center;
            yield return MouseAt(button, true);
            yield return MouseAt(button, false);
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator PitchLimits_PreventInversion_AndReverseImmediatelyAfterOvershoot()
        {
            yield return Load("StageFour");
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            Vector3 homePosition = camera.transform.position;
            Quaternion homeRotation = camera.transform.rotation;
            float radius = Vector3.Distance(homePosition, orbit.ViewFocus);
            foreach (float direction in new[] { -1f, 1f })
            {
                orbit.ResetView();
                Assert.That(orbit.BeginOrbit(Vector2.zero), Is.True);
                orbit.MoveOrbit(Vector2.up * ShortSide * direction * 4);
                float limit = direction < 0 ? 80 : -75;
                Assert.That(CameraPitch(camera), Is.EqualTo(limit).Within(0.01f));
                AssertUpright(camera.transform);
                // Continuing past a limit must not accumulate hidden input.
                orbit.MoveOrbit(Vector2.up * ShortSide * direction * 5);
                Assert.That(CameraPitch(camera), Is.EqualTo(limit).Within(0.01f));
                Vector2 reversed = Vector2.up * ShortSide * direction * 4.95f;
                orbit.MoveOrbit(reversed);
                Assert.That(CameraPitch(camera), Is.EqualTo(limit + direction * 6).Within(0.02f));
                float height = camera.transform.position.y;
                orbit.MoveOrbit(reversed + Vector2.right * ShortSide * 0.25f);
                Assert.That(camera.transform.position.y, Is.EqualTo(height).Within(0.002f));
                Assert.That(Vector3.Distance(camera.transform.position, orbit.ViewFocus), Is.EqualTo(radius).Within(0.002f));
                AssertUpright(camera.transform);
                orbit.CancelOrbit();
            }
            orbit.ResetView();
            AssertPose(camera.transform, homePosition, homeRotation);
        }

        [UnityTest]
        public IEnumerator MixedSwipesAndClosedLoops_KeepWorldUp_AndDoNotAccumulateRoll()
        {
            yield return Load("StageFour");
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            Vector3 homePosition = camera.transform.position;
            Quaternion homeRotation = camera.transform.rotation;
            var deltas = new[] { new Vector2(0.3f, -0.12f), new Vector2(-0.12f, -0.10f),
                new Vector2(-0.25f, 0.07f), new Vector2(0.07f, 0.15f) };
            // Releasing/re-grabbing at each corner must behave like one continuous drag.
            foreach (bool releaseBetweenSegments in new[] { false, true })
            {
                orbit.ResetView();
                Vector2 pointer = Vector2.zero;
                if (!releaseBetweenSegments) orbit.BeginOrbit(pointer);
                for (int loop = 0; loop < 5; loop++)
                {
                    foreach (Vector2 delta in deltas)
                    {
                        if (releaseBetweenSegments) orbit.BeginOrbit(pointer);
                        Vector2 begin = pointer;
                        for (int step = 1; step <= 8; step++)
                        {
                            pointer = begin + delta * ShortSide * step / 8;
                            orbit.MoveOrbit(pointer);
                            AssertUpright(camera.transform);
                        }
                        if (releaseBetweenSegments) orbit.CancelOrbit();
                    }
                    AssertPose(camera.transform, homePosition, homeRotation);
                }
                orbit.CancelOrbit();
            }
        }

        [UnityTest]
        public IEnumerator DiagonalSwipe_MatchesSeparateHorizontalAndVerticalSwipes()
        {
            yield return Load("StageFour");
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            Vector2 delta = new Vector2(0.3f, -0.2f) * ShortSide;
            orbit.BeginOrbit(Vector2.zero);
            orbit.MoveOrbit(delta);
            orbit.CancelOrbit();
            Vector3 diagonalPosition = camera.transform.position;
            Quaternion diagonalRotation = camera.transform.rotation;
            orbit.ResetView();
            orbit.BeginOrbit(Vector2.zero);
            orbit.MoveOrbit(new Vector2(delta.x, 0));
            orbit.CancelOrbit();
            orbit.BeginOrbit(Vector2.zero);
            orbit.MoveOrbit(new Vector2(0, delta.y));
            orbit.CancelOrbit();
            AssertPose(camera.transform, diagonalPosition, diagonalRotation);
            AssertUpright(camera.transform);
        }

        [UnityTest]
        public IEnumerator TwoFingerPinchAndPan_KeepAnchorUnderFingers_AndEitherFingerCanContinueOrbit()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            Vector3 homePosition = camera.transform.position;
            Quaternion homeRotation = camera.transform.rotation;
            for (int releaseFirst = 0; releaseFirst < 2; releaseFirst++)
            {
                orbit.ResetView();
                int firstId = 670 + releaseFirst * 10;
                int secondId = firstId + 1;
                Vector2 first = Point(0.2f, 0.55f);
                Vector2 second = Point(0.65f, 0.55f);
                Vector2 midpoint = (first + second) * 0.5f;
                float depth = camera.WorldToScreenPoint(orbit.ViewFocus).z;
                Vector3 anchor = camera.ScreenToWorldPoint(new Vector3(midpoint.x, midpoint.y, depth));
                yield return Touch(firstId, TouchPhase.Began, first);
                yield return Touch(secondId, TouchPhase.Began, second);
                Assert.That(orbit.IsTwoFinger, Is.True);
                Assert.That(placement.CanModifyParts, Is.False);
                AssertPose(camera.transform, homePosition, homeRotation);
                first = Point(0.15f, 0.62f);
                second = Point(0.7f, 0.62f);
                yield return TouchPair(firstId, first, secondId, second);
                Assert.That(orbit.ZoomScale, Is.EqualTo(0.45f / 0.55f).Within(0.001f));
                Assert.That(Vector2.Distance(camera.WorldToScreenPoint(anchor), (first + second) * 0.5f), Is.LessThan(1));
                AssertRotation(camera.transform.rotation, homeRotation);
                first += Point(0.03f, 0.04f);
                second += Point(0.03f, 0.04f);
                yield return TouchPair(firstId, first, secondId, second);
                Assert.That(Vector2.Distance(camera.WorldToScreenPoint(anchor), (first + second) * 0.5f), Is.LessThan(1));
                Assert.That(orbit.ZoomScale, Is.EqualTo(0.45f / 0.55f).Within(0.001f));
                Vector3 beforeRelease = camera.transform.position;
                // A third finger does not steal or reset the two-finger gesture.
                yield return Swipe(firstId + 2, Point(0.2f, 0.5f), Point(0.3f, 0.5f));
                AssertPose(camera.transform, beforeRelease, homeRotation);
                int released = releaseFirst == 0 ? firstId : secondId;
                int remaining = releaseFirst == 0 ? secondId : firstId;
                Vector2 remainingPoint = releaseFirst == 0 ? second : first;
                yield return Touch(released, TouchPhase.Ended, releaseFirst == 0 ? first : second);
                Assert.That(orbit.IsTwoFinger, Is.False);
                Assert.That(orbit.IsOrbiting, Is.True);
                AssertPose(camera.transform, beforeRelease, homeRotation);
                remainingPoint += new Vector2(ShortSide * 0.05f, ShortSide * 0.05f);
                yield return Touch(remaining, TouchPhase.Moved, remainingPoint);
                Assert.That(Quaternion.Angle(camera.transform.rotation, homeRotation), Is.GreaterThan(5));
                yield return Touch(remaining, TouchPhase.Ended, remainingPoint);
                Assert.That(placement.CanModifyParts, Is.True);
                yield return Tap(firstId + 3, FindButton("ResetViewButton"));
                Assert.That(orbit.ZoomScale, Is.EqualTo(1));
                Assert.That(orbit.ViewFocus, Is.EqualTo(orbit.HomeFocus));
                AssertPose(camera.transform, homePosition, homeRotation);
            }
        }

        [UnityTest]
        public IEnumerator TwoFingerCancellationAndDeviceLoss_ReleaseAllGestureLocks()
        {
            yield return Load("StageFour");
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            var placement = Object.FindAnyObjectByType<PlacementManager>();
            yield return Touch(710, TouchPhase.Began, Point(0.2f, 0.55f));
            yield return Touch(711, TouchPhase.Began, Point(0.65f, 0.55f));
            Assert.That(orbit.IsTwoFinger, Is.True);
            yield return Touch(711, TouchPhase.Canceled, Point(0.65f, 0.55f));
            Assert.That(placement.CanModifyParts, Is.True);
            yield return Touch(710, TouchPhase.Moved, Point(0.3f, 0.65f));
            AssertRotation(orbit.ViewRotation, Quaternion.identity);
            yield return Touch(710, TouchPhase.Ended, Point(0.3f, 0.65f));
            yield return Touch(712, TouchPhase.Began, Point(0.2f, 0.55f));
            yield return Touch(713, TouchPhase.Began, Point(0.65f, 0.55f));
            Assert.That(orbit.IsTwoFinger, Is.True);
            InputSystem.RemoveDevice(touchscreen);
            yield return null;
            yield return null;
            Assert.That(placement.CanModifyParts, Is.True);
            Assert.That(FindButton("ResetViewButton").interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator MousePanAndWheelZoom_RespectUiAndLimits_AndResetRestoresAllAxes()
        {
            yield return Load("StageFour");
            mouse = InputSystem.AddDevice<Mouse>();
            var camera = Camera.main;
            var orbit = camera.GetComponent<StageCameraOrbit>();
            Vector3 home = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Vector2 point = Point(0.2f, 0.55f);
            yield return MouseStateAt(new MouseState { position = point, buttons = 2 });
            yield return MouseStateAt(new MouseState { position = point + Point(0.07f, 0.04f), buttons = 2 });
            yield return MouseStateAt(new MouseState { position = point + Point(0.07f, 0.04f) });
            Assert.That(Vector3.Distance(home, camera.transform.position), Is.GreaterThan(0.5f));
            AssertRotation(camera.transform.rotation, rotation);
            yield return MouseStateAt(new MouseState { position = point, scroll = new Vector2(0, 120) });
            Assert.That(orbit.ZoomScale, Is.LessThan(1));
            float zoom = orbit.ZoomScale;
            Vector2 ui = ScreenRect((RectTransform)FindButton("ResetViewButton").transform).center;
            yield return MouseStateAt(new MouseState { position = ui, scroll = new Vector2(0, 120) });
            Assert.That(orbit.ZoomScale, Is.EqualTo(zoom));
            orbit.Zoom(0.0001f, point);
            Assert.That(orbit.ZoomScale, Is.EqualTo(0.45f));
            orbit.Zoom(10000f, point);
            Assert.That(orbit.ZoomScale, Is.EqualTo(2.5f));
            Assert.That(orbit.Zoom(float.NaN, point), Is.False);
            orbit.ResetView();
            AssertPose(camera.transform, home, rotation);
            Assert.That(orbit.ZoomScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ManualCameraStopsBallFollow_AndAreaChangesPreserveRotationAndZoom()
        {
            yield return Load("StageFive");
            var gm = Object.FindAnyObjectByType<GameManager>();
            var nav = Object.FindAnyObjectByType<StageViewNavigator>();
            var orbit = Camera.main.GetComponent<StageCameraOrbit>();
            gm.StartSimulation();
            Assert.That(nav.IsFollowing, Is.True);
            Assert.That(orbit.BeginOrbit(Point(0.3f, 0.5f)), Is.True);
            orbit.MoveOrbit(Point(0.4f, 0.65f));
            orbit.CancelOrbit();
            Assert.That(nav.IsFollowing, Is.False);
            Quaternion rotation = orbit.ViewRotation;
            orbit.Zoom(0.7f, Point(0.5f, 0.5f));
            Assert.That(nav.ShowView(2), Is.True);
            AssertRotation(orbit.ViewRotation, rotation);
            Assert.That(orbit.ZoomScale, Is.EqualTo(0.7f).Within(0.001f));
            yield return null;
            Assert.That(nav.IsFollowing, Is.False);
            orbit.ResetView();
            Assert.That(nav.CurrentView, Is.EqualTo(0));
            Assert.That(orbit.ZoomScale, Is.EqualTo(1));
            gm.ResetSimulation();
        }

        private IEnumerator TouchPair(int firstId, Vector2 first, int secondId, Vector2 second)
        {
            InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = firstId, phase = TouchPhase.Moved, position = first, pressure = 1 });
            InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = secondId, phase = TouchPhase.Moved, position = second, pressure = 1 });
            yield return null;
            yield return null;
        }

        private IEnumerator MouseStateAt(MouseState state)
        {
            InputSystem.QueueStateEvent(mouse, state);
            yield return null;
            yield return null;
        }

        private static float CameraPitch(Camera camera) =>
            Mathf.Asin(Mathf.Clamp(-camera.transform.forward.y, -1, 1)) * Mathf.Rad2Deg;

        private static void AssertUpright(Transform camera)
        {
            Assert.That(Mathf.Abs(Vector3.Dot(camera.right, Vector3.up)), Is.LessThan(0.00001f), "Horizontal swipe must not roll the view.");
            Assert.That(Vector3.Dot(camera.up, Vector3.up), Is.GreaterThan(0.1f), "The camera must remain upright at both pitch limits.");
        }

        private static float ShortSide => Mathf.Min(Screen.width, Screen.height);

        private static void AssertRotation(Quaternion actual, Quaternion expected) =>
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.08f));

        private static IEnumerator Load(string name)
        {
            yield return TestSceneLoader.Load(name);
            yield return null;
        }

        private static Vector2 Point(float x, float y) => new Vector2(Screen.width * x, Screen.height * y);
        private static Button FindButton(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Single(b => b.name == name);

        private IEnumerator Swipe(int id, Vector2 start, Vector2 end)
        {
            yield return Touch(id, TouchPhase.Began, start);
            yield return Touch(id, TouchPhase.Moved, end);
            yield return Touch(id, TouchPhase.Ended, end);
        }

        private IEnumerator MouseAt(Vector2 position, bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = (ushort)(pressed ? 1 : 0) });
            yield return null;
            yield return null;
        }

        private static void AssertPose(Transform target, Vector3 position, Quaternion rotation)
        {
            Assert.That(Vector3.Distance(target.position, position), Is.LessThan(0.005f));
            Assert.That(Quaternion.Angle(target.rotation, rotation), Is.LessThan(0.05f));
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
