using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class PlacementManager : MonoBehaviour
    {
        private enum PointerSource
        {
            None,
            Mouse,
            Touch
        }

        [SerializeField] private GameManager gameManager;
        [SerializeField] private Camera placementCamera;
        [SerializeField] private StageCameraOrbit cameraOrbit;
        [SerializeField] private Vector2 minimumXZ = new Vector2(-6f, -3f);
        [SerializeField] private Vector2 maximumXZ = new Vector2(6f, 3f);

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private GameManager subscribedManager;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private float dragHeight;
        private int activePointerId;
        private int secondPointerId = -1;
        private int mouseButton;
        private PointerSource pointerSource;

        public DraggableObject SelectedObject { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsOrbiting => cameraOrbit != null && cameraOrbit.IsOrbiting;
        public bool CanModifyParts => CanEdit && !HasPointerGesture;
        public event Action<DraggableObject> SelectionChanged;

        private bool CanEdit => isActiveAndEnabled && gameManager != null &&
            gameManager.State == GameState.Edit && placementCamera != null;
        private bool HasPointerGesture => IsDragging || IsOrbiting;

        private void OnEnable()
        {
            SubscribeToState();
        }

        private void OnDisable()
        {
            UnsubscribeFromState();
            CancelDrag();
            Select(null);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelDrag();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelDrag();
            }
        }

        public void Configure(GameManager manager, Camera camera, Vector2 minXZ, Vector2 maxXZ)
        {
            UnsubscribeFromState();
            CancelDrag();
            Select(null);
            gameManager = manager;
            placementCamera = camera;
            minimumXZ = Vector2.Min(minXZ, maxXZ);
            maximumXZ = Vector2.Max(minXZ, maxXZ);
            if (isActiveAndEnabled)
            {
                SubscribeToState();
            }
        }

        public bool BeginPointer(Vector2 screenPosition, int pointerId = -1)
        {
            if (!CanModifyParts || IsOverUi(screenPosition, pointerId))
            {
                return false;
            }

            Ray ray = placementCamera.ScreenPointToRay(screenPosition);
            if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
                    UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Select(null);
                return false;
            }

            DraggableObject draggable = hit.collider.GetComponentInParent<DraggableObject>();
            if (draggable == null || !draggable.isActiveAndEnabled)
            {
                Select(null);
                return false;
            }

            Vector3 position = draggable.transform.position;
            dragPlane = new Plane(Vector3.up, position);
            if (Mathf.Abs(Vector3.Dot(ray.direction, Vector3.up)) < 0.08f || !dragPlane.Raycast(ray, out float distance))
            {
                Select(draggable);
                return false;
            }

            Select(draggable);
            dragOffset = position - ray.GetPoint(distance);
            dragHeight = position.y;
            activePointerId = pointerId;
            pointerSource = PointerSource.None;
            IsDragging = true;
            return true;
        }

        public void MovePointer(Vector2 screenPosition, int pointerId = -1)
        {
            if (!IsDragging || pointerId != activePointerId)
            {
                return;
            }

            if (!CanEdit || SelectedObject == null || !SelectedObject.isActiveAndEnabled)
            {
                CancelDrag();
                Select(null);
                return;
            }

            Ray ray = placementCamera.ScreenPointToRay(screenPosition);
            if (!dragPlane.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 position = ray.GetPoint(distance) + dragOffset;
            position.x = Mathf.Clamp(position.x, minimumXZ.x, maximumXZ.x);
            position.y = dragHeight;
            position.z = Mathf.Clamp(position.z, minimumXZ.y, maximumXZ.y);
            SelectedObject.transform.position = position;
        }

        public void EndPointer(int pointerId = -1)
        {
            if (pointerId == activePointerId)
            {
                CancelDrag();
            }
        }

        public void CancelDrag()
        {
            IsDragging = false;
            secondPointerId = -1;
            pointerSource = PointerSource.None;
            if (cameraOrbit != null)
            {
                cameraOrbit.CancelOrbit();
            }
        }

        public void ConfigureOrbit(StageCameraOrbit orbit)
        {
            CancelDrag();
            cameraOrbit = orbit;
        }

        public bool SelectObject(DraggableObject draggable)
        {
            if (!CanModifyParts || (draggable != null && !draggable.isActiveAndEnabled))
            {
                return false;
            }

            Select(draggable);
            return true;
        }

        public bool DeleteSelected()
        {
            if (!CanModifyParts || SelectedObject == null || !SelectedObject.isActiveAndEnabled)
            {
                return false;
            }

            GameObject selectedRoot = SelectedObject.gameObject;
            if (!gameManager.UnregisterResetObjects(selectedRoot.GetComponentsInChildren<PhysicsObject>(true)))
            {
                return false;
            }

            // Destroy is deferred until the end of the frame. Remove every
            // collider immediately so a following PLAY cannot hit a deleted part.
            selectedRoot.SetActive(false);
            Select(null);
            Destroy(selectedRoot);
            return true;
        }

        public bool RotateSelected()
        {
            if (!CanModifyParts || SelectedObject == null || !SelectedObject.isActiveAndEnabled)
            {
                return false;
            }

            // Pre-multiply the world yaw to retain the ramp's existing slope.
            SelectedObject.transform.rotation = Quaternion.AngleAxis(15f, Vector3.up) *
                SelectedObject.transform.rotation;
            return true;
        }

        private void Update()
        {
            if (!CanEdit)
            {
                if (IsDragging)
                {
                    CancelDrag();
                }
                Select(null);
            }

            if (SelectedObject != null && !SelectedObject.isActiveAndEnabled)
            {
                CancelDrag();
                Select(null);
            }

            if (PollTouches())
            {
                if (pointerSource == PointerSource.Mouse)
                {
                    CancelDrag();
                }

                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                if (pointerSource == PointerSource.Mouse)
                {
                    CancelDrag();
                }

                return;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && BeginInputPointer(position))
            {
                pointerSource = PointerSource.Mouse;
                mouseButton = 0;
            }
            else if ((mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame) &&
                CanStartCamera(position) && cameraOrbit != null && cameraOrbit.BeginPan(position))
            {
                pointerSource = PointerSource.Mouse;
                activePointerId = -1;
                mouseButton = mouse.rightButton.wasPressedThisFrame ? 1 : 2;
            }

            if (pointerSource == PointerSource.Mouse)
            {
                ButtonControl button = mouseButton == 1 ? mouse.rightButton : mouseButton == 2 ? mouse.middleButton : mouse.leftButton;
                if (button.wasReleasedThisFrame || !button.isPressed)
                {
                    MoveInputPointer(position);
                    EndPointer();
                }
                else
                {
                    MoveInputPointer(position);
                }
            }
            else if (CanStartCamera(position) && cameraOrbit != null && Mathf.Abs(mouse.scroll.y.ReadValue()) > 0.01f)
            {
                cameraOrbit.Zoom(Mathf.Exp(-mouse.scroll.y.ReadValue() * 0.0015f), position);
            }
        }

        private bool PollTouches()
        {
            bool touchActivity = false;
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (!HasTouchState(touch))
                    {
                        continue;
                    }

                    touchActivity = true;
                    int pointerId = touch.touchId.ReadValue();
                    Vector2 position = touch.position.ReadValue();
                    if (touch.press.wasPressedThisFrame && BeginInputPointer(position, pointerId))
                    {
                        pointerSource = PointerSource.Touch;
                    }
                    else if (touch.press.wasPressedThisFrame && pointerSource == PointerSource.Touch &&
                        IsOrbiting && secondPointerId == -1 && pointerId != activePointerId &&
                        !IsOverUi(position, pointerId) && HitPart(position) == null)
                    {
                        TouchControl first = FindTouch(screen, activePointerId);
                        if (first != null && first.press.isPressed && cameraOrbit.BeginTwoFinger(first.position.ReadValue(), position))
                        {
                            secondPointerId = pointerId;
                        }
                    }
                }
            }

            if (pointerSource != PointerSource.Touch)
            {
                return touchActivity;
            }
            TouchControl primary = FindTouch(screen, activePointerId);
            TouchControl secondary = secondPointerId != -1 ? FindTouch(screen, secondPointerId) : null;
            if (primary == null || primary.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled ||
                (secondPointerId != -1 && (secondary == null || secondary.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)))
            {
                CancelDrag();
                return touchActivity;
            }
            if (secondPointerId != -1)
            {
                cameraOrbit.MoveTwoFinger(primary.position.ReadValue(), secondary.position.ReadValue());
                if (!primary.press.isPressed || !secondary.press.isPressed)
                {
                    TouchControl remaining = primary.press.isPressed ? primary : secondary.press.isPressed ? secondary : null;
                    if (remaining == null)
                    {
                        CancelDrag();
                    }
                    else
                    {
                        activePointerId = remaining.touchId.ReadValue();
                        secondPointerId = -1;
                        cameraOrbit.ResumeOneFinger(remaining.position.ReadValue());
                    }
                }
            }
            else
            {
                MoveInputPointer(primary.position.ReadValue(), activePointerId);
                if (!primary.press.isPressed)
                {
                    EndPointer(activePointerId);
                }
            }
            return touchActivity;
        }

        private static bool HasTouchState(TouchControl touch) => touch.press.isPressed ||
            touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame;

        private static TouchControl FindTouch(Touchscreen screen, int id)
        {
            if (screen == null)
            {
                return null;
            }
            foreach (TouchControl touch in screen.touches)
            {
                if (touch.touchId.ReadValue() == id && HasTouchState(touch))
                {
                    return touch;
                }
            }
            return null;
        }

        private bool CanStartCamera(Vector2 position, int pointerId = -1) => isActiveAndEnabled &&
            gameManager != null && placementCamera != null && !HasPointerGesture && !IsOverUi(position, pointerId);

        private DraggableObject HitPart(Vector2 position)
        {
            Ray ray = placementCamera.ScreenPointToRay(position);
            return UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponentInParent<DraggableObject>() : null;
        }

        private bool BeginInputPointer(Vector2 position, int pointerId = -1)
        {
            // One owner routes each gesture. A swipe that starts on UI or a part
            // can never turn into a camera gesture when the finger moves away.
            if (!CanStartCamera(position, pointerId))
            {
                return false;
            }
            if (CanEdit && HitPart(position) != null)
            {
                return BeginPointer(position, pointerId);
            }
            if (CanEdit) Select(null);
            if (cameraOrbit == null || !cameraOrbit.BeginOrbit(position))
            {
                return false;
            }
            activePointerId = pointerId;
            return true;
        }

        private void MoveInputPointer(Vector2 position, int pointerId = -1)
        {
            if (pointerId != activePointerId)
            {
                return;
            }
            if (IsOrbiting)
            {
                cameraOrbit.MoveOrbit(position);
            }
            else
            {
                MovePointer(position, pointerId);
            }
        }

        private bool IsOverUi(Vector2 position, int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            // Query current coordinates directly, independently of EventSystem.Update order.
            PointerEventData pointer = new PointerEventData(eventSystem)
            {
                position = position,
                pointerId = pointerId
            };
            uiHits.Clear();
            eventSystem.RaycastAll(pointer, uiHits);
            foreach (RaycastResult hit in uiHits)
            {
                if (hit.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        private void Select(DraggableObject draggable)
        {
            if (SelectedObject == draggable)
            {
                return;
            }

            if (SelectedObject != null)
            {
                SelectedObject.SetSelected(false);
            }

            SelectedObject = draggable;
            if (SelectedObject != null)
            {
                SelectedObject.SetSelected(true);
            }

            SelectionChanged?.Invoke(SelectedObject);
        }

        private void SubscribeToState()
        {
            UnsubscribeFromState();
            subscribedManager = gameManager;
            if (subscribedManager != null)
            {
                subscribedManager.StateChanged += HandleStateChanged;
                HandleStateChanged(subscribedManager.State);
            }
        }

        private void UnsubscribeFromState()
        {
            if (subscribedManager != null)
            {
                subscribedManager.StateChanged -= HandleStateChanged;
            }

            subscribedManager = null;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Edit)
            {
                CancelDrag();
                Select(null);
            }
        }
    }
}
