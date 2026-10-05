using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class PartPaletteHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PartSpawner spawner;
        [SerializeField] private Button[] addButtons = Array.Empty<Button>();
        [SerializeField] private string[] buttonLabels = Array.Empty<string>();

        private UnityAction[] clickHandlers = Array.Empty<UnityAction>();
        private Text[] labelTexts = Array.Empty<Text>();

        private void OnEnable()
        {
            CacheLabels(false);
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            // Drag ownership can change independently of game state or selection.
            Refresh();
        }

        public void Configure(GameManager manager, PartSpawner partSpawner, Button[] buttons)
        {
            Unsubscribe();
            RestoreLabels();
            gameManager = manager;
            spawner = partSpawner;
            addButtons = buttons != null ? (Button[])buttons.Clone() : Array.Empty<Button>();
            CacheLabels(true);
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
            clickHandlers = new UnityAction[addButtons.Length];
            for (int i = 0; i < addButtons.Length; i++)
            {
                int index = i;
                clickHandlers[i] = () => AddPart(index);
                if (addButtons[i] != null)
                {
                    addButtons[i].onClick.AddListener(clickHandlers[i]);
                }
            }
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
            for (int i = 0; i < addButtons.Length && i < clickHandlers.Length; i++)
            {
                if (addButtons[i] != null && clickHandlers[i] != null)
                {
                    addButtons[i].onClick.RemoveListener(clickHandlers[i]);
                }
            }
            clickHandlers = Array.Empty<UnityAction>();
        }

        private void HandleStateChanged(GameState state) => Refresh();

        private void AddPart(int index)
        {
            if (spawner != null)
            {
                spawner.AddPart(index);
            }
            Refresh();
        }

        private void Refresh()
        {
            bool editing = gameManager != null && gameManager.State == GameState.Edit;
            for (int index = 0; index < addButtons.Length; index++)
            {
                Button button = addButtons[index];
                if (button != null)
                {
                    button.gameObject.SetActive(gameManager == null || gameManager.State != GameState.Clear);
                    button.interactable = editing && spawner != null && spawner.CanAddPart(index);
                }
                if (index < labelTexts.Length && labelTexts[index] != null)
                {
                    int remaining = spawner != null ? spawner.GetRemainingCount(index) : -1;
                    labelTexts[index].text = remaining < 0
                        ? buttonLabels[index]
                        : buttonLabels[index] + " (" + remaining + ")";
                }
            }
        }

        private void CacheLabels(bool replace)
        {
            if (replace || buttonLabels == null || buttonLabels.Length != addButtons.Length)
            {
                buttonLabels = new string[addButtons.Length];
            }
            labelTexts = new Text[addButtons.Length];
            for (int index = 0; index < addButtons.Length; index++)
            {
                if (addButtons[index] != null)
                {
                    labelTexts[index] = addButtons[index].GetComponentInChildren<Text>(true);
                }
                if (buttonLabels[index] == null)
                {
                    buttonLabels[index] = labelTexts[index] != null ? labelTexts[index].text : string.Empty;
                }
            }
        }

        private void RestoreLabels()
        {
            for (int index = 0; index < addButtons.Length && index < buttonLabels.Length; index++)
            {
                if (addButtons[index] != null && buttonLabels[index] != null)
                {
                    Text label = addButtons[index].GetComponentInChildren<Text>(true);
                    if (label != null)
                    {
                        label.text = buttonLabels[index];
                    }
                }
            }
        }
    }
}
