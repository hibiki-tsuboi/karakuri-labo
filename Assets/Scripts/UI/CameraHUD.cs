using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class CameraHUD : MonoBehaviour
    {
        [SerializeField] private StageCameraOrbit orbit;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private Button resetButton;

        private bool CanReset => orbit != null && orbit.isActiveAndEnabled &&
            !orbit.IsOrbiting && placement != null && placement.isActiveAndEnabled && !placement.IsDragging;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable() => Unsubscribe();
        private void LateUpdate() => Refresh();

        public void Configure(StageCameraOrbit cameraOrbit, PlacementManager placementManager, Button button)
        {
            Unsubscribe();
            orbit = cameraOrbit;
            placement = placementManager;
            resetButton = button;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        private void Subscribe()
        {
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetView);
            }
        }

        private void Unsubscribe()
        {
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetView);
            }
        }

        private void ResetView()
        {
            if (CanReset)
            {
                orbit.ResetView();
            }
        }

        private void Refresh()
        {
            if (resetButton != null)
            {
                resetButton.interactable = CanReset;
            }
        }
    }
}
