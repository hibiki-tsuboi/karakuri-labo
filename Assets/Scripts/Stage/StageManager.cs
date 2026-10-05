using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class StageManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button nextButton;
        [SerializeField] private string nextScenePath = "";

        private bool isLoading;

        public bool CanAdvance => isActiveAndEnabled && !isLoading && gameManager != null &&
            gameManager.State == GameState.Clear && !string.IsNullOrWhiteSpace(nextScenePath) &&
            SceneUtility.GetBuildIndexByScenePath(nextScenePath) >= 0 &&
            Application.CanStreamedLevelBeLoaded(nextScenePath);

        private void OnEnable()
        {
            Subscribe();
            RefreshButton();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
        }

        public void Configure(GameManager manager, Button button, string scenePath)
        {
            Unsubscribe();
            if (nextButton != null && nextButton != button)
            {
                nextButton.gameObject.SetActive(false);
            }
            gameManager = manager;
            nextButton = button;
            nextScenePath = scenePath != null ? scenePath.Trim() : string.Empty;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            RefreshButton();
        }

        public bool TryAdvance()
        {
            if (!CanAdvance)
            {
                return false;
            }

            string destination = nextScenePath;
            AudioManager currentAudio = FindSceneAudio(gameObject.scene);
            bool preserveSound = currentAudio != null;
            bool muted = preserveSound && currentAudio.IsMuted;
            isLoading = true;
            RefreshButton();
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
                if (operation == null)
                {
                    isLoading = false;
                    RefreshButton();
                    return false;
                }
                if (preserveSound)
                {
                    // The source component is destroyed during Single scene loading.
                    // This one-shot completion owns only the destination and setting;
                    // Music keeps its own voice; this restores the destination's clear-effect/UI setting.
                    operation.completed += completed => RestoreSound(destination, muted);
                }
                return true;
            }
            catch (Exception exception)
            {
                isLoading = false;
                RefreshButton();
                Debug.LogError($"Could not load the next stage '{destination}': {exception.Message}", this);
                return false;
            }
        }

        private void Advance()
        {
            TryAdvance();
        }

        private void HandleStateChanged(GameState state) => RefreshButton();

        private void RefreshButton()
        {
            if (nextButton != null)
            {
                bool available = CanAdvance;
                nextButton.interactable = available;
                nextButton.gameObject.SetActive(available);
            }
        }

        private void Subscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += HandleStateChanged;
            }
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(Advance);
            }
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(Advance);
            }
        }

        private static AudioManager FindSceneAudio(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                AudioManager audio = root.GetComponentInChildren<AudioManager>(true);
                if (audio != null)
                {
                    return audio;
                }
            }
            return null;
        }

        private static void RestoreSound(string scenePath, bool muted)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }
            AudioManager audio = FindSceneAudio(scene);
            if (audio != null)
            {
                audio.SetMuted(muted);
            }
        }
    }
}
