using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class FailureHUD : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private CanvasGroup card;
        [SerializeField] private Text reasonLabel;
        [SerializeField] private Button retryButton;
        private float revealTime;

        public bool IsVisible => card != null && card.gameObject.activeSelf;
        private bool CanRetry => gameManager != null && gameManager.State == GameState.Failed &&
            placement != null && placement.isActiveAndEnabled && !placement.IsDragging && !placement.IsOrbiting;

        public void Configure(GameManager manager, PlacementManager controller, CanvasGroup panel,
            Text reason, Button retry)
        {
            Unsubscribe();
            gameManager = manager;
            placement = controller;
            card = panel;
            reasonLabel = reason;
            retryButton = retry;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (gameManager != null) gameManager.StateChanged += HandleStateChanged;
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
        }

        private void Unsubscribe()
        {
            if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        }

        private void HandleStateChanged(GameState state) => Refresh();

        private void Retry()
        {
            if (CanRetry) gameManager.RetryAttempt();
        }

        private void Refresh()
        {
            bool failed = gameManager != null && gameManager.State == GameState.Failed;
            if (card != null)
            {
                card.gameObject.SetActive(failed);
                card.alpha = 0f;
            }
            revealTime = 0f;
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(failed);
                retryButton.interactable = CanRetry;
            }
            if (reasonLabel == null || !failed) return;
            reasonLabel.text = GameText.Failure(gameManager.FailureReason);
        }

        private void LateUpdate()
        {
            if (retryButton != null) retryButton.interactable = CanRetry;
            if (!IsVisible) return;
            revealTime += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(revealTime / 0.2f));
            card.alpha = progress;
            card.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, progress);
        }
    }
}
