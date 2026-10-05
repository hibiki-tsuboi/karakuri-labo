using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KarakuriLabo
{
    public sealed class StageSelectHUD : MonoBehaviour
    {
        [SerializeField] private GameManager manager;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject menu;
        [SerializeField] private Button[] stages = Array.Empty<Button>();
        [SerializeField] private string[] scenePaths = Array.Empty<string>();
        private UnityAction[] handlers = Array.Empty<UnityAction>();
        private bool loading;
        private bool restorePlacementOnClose;
        public bool IsOpen => menu != null && menu.activeSelf;
        private bool CanOpen => isActiveAndEnabled && !loading && manager != null &&
            manager.State != GameState.Playing && placement != null && (placement.enabled || IsOpen) &&
            !placement.IsDragging && !placement.IsOrbiting;

        public void Configure(GameManager gameManager, PlacementManager parts, Button open, Button close,
            GameObject panel, Button[] choices, string[] paths)
        {
            Unsubscribe();
            manager = gameManager;
            placement = parts;
            openButton = open;
            closeButton = close;
            menu = panel;
            stages = choices;
            scenePaths = paths;
            Close();
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() { Subscribe(); Close(); }
        private void OnDisable() { Unsubscribe(); Close(); }
        private void LateUpdate()
        {
            if (openButton != null) openButton.interactable = CanOpen;
        }
        private void Open() => TryOpen();
        public bool TryOpen()
        {
            if (!CanOpen || menu == null) return false;
            if (IsOpen) return true;
            menu.SetActive(true);
            menu.transform.SetAsLastSibling();
            restorePlacementOnClose = placement.enabled;
            placement.enabled = false;
            return true;
        }
        public void Close()
        {
            if (menu != null) menu.SetActive(false);
            if (restorePlacementOnClose && placement != null) placement.enabled = true;
            restorePlacementOnClose = false;
        }
        public bool TrySelect(int index)
        {
            if (!CanOpen || !IsOpen || index < 0 || index >= scenePaths.Length ||
                !Application.CanStreamedLevelBeLoaded(scenePaths[index])) return false;
            loading = true;
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(scenePaths[index]);
                if (operation != null) return true;
                loading = false;
            }
            catch (Exception exception)
            {
                loading = false;
                Debug.LogError($"Could not open stage: {exception.Message}", this);
            }
            return false;
        }
        private void Subscribe()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            handlers = new UnityAction[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                int index = i;
                handlers[i] = () => TrySelect(index);
                if (stages[i] != null) stages[i].onClick.AddListener(handlers[i]);
            }
        }
        private void Unsubscribe()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            for (int i = 0; i < stages.Length && i < handlers.Length; i++)
            {
                if (stages[i] != null) stages[i].onClick.RemoveListener(handlers[i]);
            }
        }
    }
}
