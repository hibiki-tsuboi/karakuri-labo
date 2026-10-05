using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class SimulationHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private Color playColor = new Color32(37, 112, 88, 255);
        [SerializeField] private Color resetColor = new Color32(52, 76, 112, 255);

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(GameManager manager, Button button, Text label)
        {
            Unsubscribe();
            gameManager = manager;
            simulationButton = button;
            buttonLabel = label;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        private void Subscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += HandleStateChanged;
            }
            if (simulationButton != null)
            {
                simulationButton.onClick.AddListener(HandleClick);
            }
        }

        public void ConfigureColors(Color play, Color reset)
        {
            playColor = play;
            resetColor = reset;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
            if (simulationButton != null)
            {
                simulationButton.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleStateChanged(GameState state) => Refresh();

        private void HandleClick()
        {
            if (gameManager == null)
            {
                return;
            }
            if (gameManager.State == GameState.Edit)
            {
                gameManager.StartSimulation();
            }
            else
            {
                gameManager.ResetSimulation();
            }
        }

        private void Refresh()
        {
            bool editing = gameManager != null && gameManager.State == GameState.Edit;
            if (buttonLabel != null)
            {
                buttonLabel.text = editing ? "PLAY" : "RESET";
            }
            if (simulationButton != null)
            {
                simulationButton.interactable = gameManager != null;
                if (simulationButton.targetGraphic != null)
                {
                    simulationButton.targetGraphic.color = editing ? playColor : resetColor;
                }
            }
        }
    }
}
