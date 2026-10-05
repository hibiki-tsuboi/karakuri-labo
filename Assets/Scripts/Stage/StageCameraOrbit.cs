using System;
using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class StageCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 focusPoint = new Vector3(0, 0.8f, 0);
        [SerializeField] private float degreesPerShortSide = 120f;
        [SerializeField] private float minimumPitch = -75f;
        [SerializeField] private float maximumPitch = 80f;
        [SerializeField] private float minimumZoom = 0.45f;
        [SerializeField] private float maximumZoom = 2.5f;

        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 viewFocus;
        private float homeSize;
        private Camera viewCamera;
        private Vector2 pointerStart;
        private Quaternion orbitRotation = Quaternion.identity;
        private Vector2 previousOrbitPointer;
        private float homeYaw;
        private float homePitch;
        private float yaw;
        private float pitch;
        private Vector3 focusOrigin;
        private float baseSize;
        private Vector2 previousMidpoint;
        private float previousSpan;
        private int gestureWidth;
        private int gestureHeight;
        private bool crossedThreshold;
        private Gesture gesture;

        private enum Gesture { None, Orbit, Pan, TwoFinger }

        public bool IsOrbiting => gesture != Gesture.None;
        public bool IsTwoFinger => gesture == Gesture.TwoFinger;
        public Quaternion ViewRotation => orbitRotation;
        public float ZoomScale { get; private set; } = 1f;
        public Vector3 ViewFocus => viewFocus;
        public Vector3 HomeFocus => focusPoint;
        public event Action ViewReset;
        public event Action ViewManipulated;

        private void Awake() => CaptureHome();
        private void OnDisable() => CancelOrbit();

        public void Configure(Vector3 target)
        {
            CancelOrbit();
            focusPoint = target;
            CaptureHome();
        }

        public bool BeginOrbit(Vector2 position)
        {
            return BeginGesture(position, Gesture.Orbit);
        }

        public bool BeginPan(Vector2 position)
        {
            return BeginGesture(position, Gesture.Pan);
        }

        private bool BeginGesture(Vector2 position, Gesture mode)
        {
            if (!isActiveAndEnabled || IsOrbiting || Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }
            pointerStart = position;
            previousMidpoint = position;
            previousOrbitPointer = position;
            gestureWidth = Screen.width;
            gestureHeight = Screen.height;
            crossedThreshold = false;
            gesture = mode;
            return true;
        }

        public void MoveOrbit(Vector2 position)
        {
            if (!ValidGesture() || gesture == Gesture.TwoFinger)
            {
                return;
            }
            Vector2 fromStart = position - pointerStart;
            float shortSide = Mathf.Min(gestureWidth, gestureHeight);
            // Ignore tap jitter in every direction, then follow the finger directly.
            if (!crossedThreshold && fromStart.magnitude < shortSide * 0.006f)
            {
                return;
            }
            NotifyManipulated();
            if (gesture == Gesture.Pan)
            {
                PanAndZoom(position, 1);
                return;
            }
            // Independent yaw/pitch keep world-up upright even after diagonal drags.
            // Consume each delta at the limits, so reversing a finger responds immediately.
            Vector2 delta = position - previousOrbitPointer;
            previousOrbitPointer = position;
            float sensitivity = degreesPerShortSide / shortSide;
            yaw = Mathf.Repeat(yaw + delta.x * sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minimumPitch, maximumPitch);
            orbitRotation = Quaternion.Euler(pitch, yaw, 0) * Quaternion.Inverse(homeRotation);
            ApplyView();
        }

        public bool BeginTwoFinger(Vector2 first, Vector2 second)
        {
            if (!ValidGesture() || gesture != Gesture.Orbit || Vector2.Distance(first, second) < 8)
            {
                return false;
            }
            gesture = Gesture.TwoFinger;
            previousMidpoint = (first + second) * 0.5f;
            previousSpan = Vector2.Distance(first, second);
            return true;
        }

        public void MoveTwoFinger(Vector2 first, Vector2 second)
        {
            if (!ValidGesture() || gesture != Gesture.TwoFinger)
            {
                return;
            }
            Vector2 midpoint = (first + second) * 0.5f;
            float span = Mathf.Max(8, Vector2.Distance(first, second));
            if (Vector2.Distance(midpoint, previousMidpoint) < 0.5f && Mathf.Abs(span - previousSpan) < 0.5f)
            {
                return;
            }
            NotifyManipulated();
            PanAndZoom(midpoint, previousSpan / span);
            previousSpan = span;
        }

        public void ResumeOneFinger(Vector2 position)
        {
            CancelOrbit();
            BeginOrbit(position);
        }

        public bool Zoom(float factor, Vector2 anchor)
        {
            if (!isActiveAndEnabled || IsOrbiting || !float.IsFinite(factor) || factor <= 0)
            {
                return false;
            }
            previousMidpoint = anchor;
            ViewManipulated?.Invoke();
            PanAndZoom(anchor, factor);
            return true;
        }

        private void PanAndZoom(Vector2 midpoint, float factor)
        {
            Vector3 before = PointOnViewPlane(previousMidpoint);
            ZoomScale = Mathf.Clamp(ZoomScale * factor, minimumZoom, maximumZoom);
            viewCamera.orthographicSize = baseSize * ZoomScale;
            Vector3 after = PointOnViewPlane(midpoint);
            viewFocus = focusOrigin + Vector3.ClampMagnitude(viewFocus + before - after - focusOrigin, baseSize * 3);
            previousMidpoint = midpoint;
            ApplyView();
        }

        private Vector3 PointOnViewPlane(Vector2 screen)
        {
            float depth = viewCamera.WorldToScreenPoint(viewFocus).z;
            return viewCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        }

        private bool ValidGesture()
        {
            if (!IsOrbiting || !isActiveAndEnabled)
            {
                return false;
            }
            if (Screen.width != gestureWidth || Screen.height != gestureHeight)
            {
                CancelOrbit();
                return false;
            }
            return true;
        }

        private void NotifyManipulated()
        {
            if (!crossedThreshold)
            {
                crossedThreshold = true;
                ViewManipulated?.Invoke();
            }
        }

        public bool SetFocus(Vector3 target, float size)
        {
            if (!isActiveAndEnabled || IsOrbiting || size <= 0)
            {
                return false;
            }
            viewFocus = target;
            focusOrigin = target;
            baseSize = size;
            ApplyView();
            return true;
        }

        private void ApplyView()
        {
            viewCamera.orthographicSize = baseSize * ZoomScale;
            transform.SetPositionAndRotation(viewFocus + orbitRotation * (homePosition - focusPoint),
                orbitRotation * homeRotation);
        }

        public void CancelOrbit() => gesture = Gesture.None;

        public void ResetView()
        {
            CancelOrbit();
            orbitRotation = Quaternion.identity;
            yaw = homeYaw;
            pitch = homePitch;
            ZoomScale = 1;
            viewFocus = focusPoint;
            focusOrigin = focusPoint;
            baseSize = homeSize;
            viewCamera.orthographicSize = homeSize;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            ViewReset?.Invoke();
        }

        private void CaptureHome()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            Vector3 forward = homeRotation * Vector3.forward;
            yaw = homeYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = homePitch = Mathf.Asin(Mathf.Clamp(-forward.y, -1, 1)) * Mathf.Rad2Deg;
            viewCamera = GetComponent<Camera>();
            homeSize = viewCamera.orthographicSize;
            baseSize = homeSize;
            viewFocus = focusPoint;
            focusOrigin = focusPoint;
            ZoomScale = 1;
            orbitRotation = Quaternion.identity;
        }
    }
}
