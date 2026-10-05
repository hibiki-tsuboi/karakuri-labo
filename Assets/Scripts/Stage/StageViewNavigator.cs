using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KarakuriLabo
{
    /// <summary>Named views for a large course, with optional ball following during a run.</summary>
    [DisallowMultipleComponent]
    public sealed class StageViewNavigator : MonoBehaviour
    {
        [SerializeField] private StageCameraOrbit orbit;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private PartSpawner spawner;
        [SerializeField] private GameManager manager;
        [SerializeField] private BallController ball;
        [SerializeField] private Vector3[] centers = Array.Empty<Vector3>();
        [SerializeField] private float[] sizes = Array.Empty<float>();
        [SerializeField] private Button[] viewButtons = Array.Empty<Button>();
        [SerializeField] private Button followButton;

        private UnityAction[] handlers = Array.Empty<UnityAction>();
        public int CurrentView { get; private set; }
        public bool IsFollowing { get; private set; }
        private bool CanNavigate => isActiveAndEnabled && orbit != null && orbit.isActiveAndEnabled &&
            placement != null && placement.isActiveAndEnabled && !placement.IsDragging && !placement.IsOrbiting;

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        public void Configure(StageCameraOrbit cameraOrbit, PlacementManager placementManager,
            PartSpawner partSpawner, GameManager gameManager, BallController stageBall,
            Vector3[] viewCenters, float[] viewSizes, Button[] buttons, Button follow)
        {
            Unsubscribe();
            orbit = cameraOrbit;
            placement = placementManager;
            spawner = partSpawner;
            manager = gameManager;
            ball = stageBall;
            centers = (Vector3[])viewCenters.Clone();
            sizes = (float[])viewSizes.Clone();
            viewButtons = (Button[])buttons.Clone();
            followButton = follow;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        public bool ShowView(int index)
        {
            if (!CanNavigate || !MoveTo(index))
            {
                return false;
            }
            IsFollowing = false;
            Refresh();
            return true;
        }

        public void FollowBall()
        {
            if (!CanNavigate || manager == null || manager.State != GameState.Playing)
            {
                return;
            }
            IsFollowing = true;
            UpdateFollowing();
            Refresh();
        }

        private bool MoveTo(int index)
        {
            if (index < 0 || index >= centers.Length || index >= sizes.Length ||
                !orbit.SetFocus(centers[index], sizes[index]))
            {
                return false;
            }
            CurrentView = index;
            return true;
        }

        private void LateUpdate()
        {
            if (IsFollowing && CanNavigate)
            {
                UpdateFollowing();
            }
            Refresh();
        }

        private void UpdateFollowing()
        {
            if (ball == null || centers.Length < 3)
            {
                return;
            }
            // The descending course has three ordered altitude bands. Camera movement
            // never moves, accelerates or teleports the ball.
            float y = ball.transform.position.y;
            int index = y > (centers[0].y + centers[1].y) * 0.5f ? 0 :
                y > (centers[1].y + centers[2].y) * 0.5f ? 1 : 2;
            if (index != CurrentView)
            {
                MoveTo(index);
            }
        }

        private void HandleState(GameState state)
        {
            IsFollowing = state == GameState.Playing;
            if (IsFollowing && CanNavigate)
            {
                MoveTo(0);
            }
            Refresh();
        }

        private void HandleAdded(int index) => ShowView(index);
        private void HandleResetView()
        {
            IsFollowing = false;
            CurrentView = 0;
            Refresh();
        }

        private void HandleManualView()
        {
            IsFollowing = false;
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < viewButtons.Length; i++)
            {
                if (viewButtons[i] == null)
                {
                    continue;
                }
                viewButtons[i].interactable = CanNavigate;
                viewButtons[i].targetGraphic.color = i == CurrentView
                    ? new Color32(171, 206, 197, 255) : new Color32(249, 242, 228, 255);
            }
            if (followButton != null)
            {
                followButton.interactable = CanNavigate && manager != null && manager.State == GameState.Playing;
                followButton.GetComponentInChildren<Text>().text = IsFollowing ? "FOLLOWING" : "FOLLOW BALL";
            }
        }

        private void Subscribe()
        {
            if (manager != null)
            {
                manager.StateChanged += HandleState;
            }
            if (spawner != null)
            {
                spawner.PartAdded += HandleAdded;
            }
            if (orbit != null)
            {
                orbit.ViewReset += HandleResetView;
                orbit.ViewManipulated += HandleManualView;
            }
            handlers = new UnityAction[viewButtons.Length];
            for (int i = 0; i < viewButtons.Length; i++)
            {
                int index = i;
                handlers[i] = () => ShowView(index);
                if (viewButtons[i] != null)
                {
                    viewButtons[i].onClick.AddListener(handlers[i]);
                }
            }
            if (followButton != null)
            {
                followButton.onClick.AddListener(FollowBall);
            }
        }

        private void Unsubscribe()
        {
            if (manager != null)
            {
                manager.StateChanged -= HandleState;
            }
            if (spawner != null)
            {
                spawner.PartAdded -= HandleAdded;
            }
            if (orbit != null)
            {
                orbit.ViewReset -= HandleResetView;
                orbit.ViewManipulated -= HandleManualView;
            }
            for (int i = 0; i < viewButtons.Length && i < handlers.Length; i++)
            {
                if (viewButtons[i] != null)
                {
                    viewButtons[i].onClick.RemoveListener(handlers[i]);
                }
            }
            if (followButton != null)
            {
                followButton.onClick.RemoveListener(FollowBall);
            }
            handlers = Array.Empty<UnityAction>();
        }
    }
}
