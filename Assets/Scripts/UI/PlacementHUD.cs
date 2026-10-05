using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class PlacementHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Text selectionLabel;
        [SerializeField] private Text modeLabel;
        [SerializeField] private string editHint = "ADD A PART  /  TAP TO SELECT";
        [SerializeField] private string selectedHint = "";
        [SerializeField] private string clearHint = "CLEAR!  /  RESET TO TRY AGAIN";

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            RefreshActions();
        }

        public void Configure(GameManager manager, PlacementManager placementManager,
            Button rotate, Text selection, Text mode, Button delete = null)
        {
            Unsubscribe();
            gameManager = manager;
            placement = placementManager;
            rotateButton = rotate;
            deleteButton = delete;
            selectionLabel = selection;
            modeLabel = mode;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        public void ConfigureHints(string editing, string selected)
        {
            editHint = editing;
            selectedHint = selected;
            Refresh();
        }

        public void ConfigureClearHint(string cleared)
        {
            clearHint = cleared;
            Refresh();
        }

        private void Subscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += HandleStateChanged;
            }
            if (placement != null)
            {
                placement.SelectionChanged += HandleSelectionChanged;
            }
            if (rotateButton != null)
            {
                rotateButton.onClick.AddListener(Rotate);
            }
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(Delete);
            }
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
            if (placement != null)
            {
                placement.SelectionChanged -= HandleSelectionChanged;
            }
            if (rotateButton != null)
            {
                rotateButton.onClick.RemoveListener(Rotate);
            }
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(Delete);
            }
        }

        private void HandleStateChanged(GameState state) => Refresh();
        private void HandleSelectionChanged(DraggableObject selected) => Refresh();

        private void Rotate()
        {
            if (placement != null)
            {
                placement.RotateSelected();
            }
        }

        private void Delete()
        {
            if (placement != null)
            {
                placement.DeleteSelected();
            }
        }

        private void RefreshActions()
        {
            bool canEditSelection = gameManager != null && gameManager.State == GameState.Edit &&
                placement != null && placement.SelectedObject != null &&
                placement.SelectedObject.isActiveAndEnabled && placement.CanModifyParts;
            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(gameManager == null || gameManager.State != GameState.Clear);
                rotateButton.interactable = canEditSelection;
            }
            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(gameManager == null || gameManager.State != GameState.Clear);
                deleteButton.interactable = canEditSelection;
            }
        }

        private void Refresh()
        {
            bool editing = gameManager != null && gameManager.State == GameState.Edit;
            bool selected = placement != null && placement.SelectedObject != null;
            RefreshActions();
            if (selectionLabel != null)
            {
                selectionLabel.text = editing
                    ? (selected ? (!string.IsNullOrEmpty(selectedHint) ? selectedHint
                            : placement.SelectedObject.DisplayName + " SELECTED  /  DRAG TO MOVE")
                        : editHint)
                    : gameManager != null && gameManager.State == GameState.Clear
                        ? clearHint
                        : "RESET TO EDIT THE LAYOUT";
            }
            if (modeLabel != null)
            {
                modeLabel.text = editing ? "EDIT MODE" :
                    gameManager != null && gameManager.State == GameState.Clear ? "CLEAR" : "PLAYING";
            }
        }
    }
}
