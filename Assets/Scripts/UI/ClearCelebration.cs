using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class ClearCelebration : MonoBehaviour
    {
        private const float PopDuration = 0.5f;
        private const float PeakProgress = 0.45f;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private Text clearText;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private AudioManager audioManager;

        private Vector3 baseScale;
        private bool hasBaseScale;
        private bool hasCelebrated;
        private float elapsed;

        public bool IsCelebrating { get; private set; }

        private void OnEnable()
        {
            CaptureBaseScale();
            Subscribe();
            if (gameManager != null)
            {
                HandleStateChanged(gameManager.State);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopCelebration();
        }

        private void Update()
        {
            if (!IsCelebrating)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / PopDuration);
            float scale = progress < PeakProgress
                ? Mathf.Lerp(0.75f, 1.1f, Mathf.SmoothStep(0f, 1f, progress / PeakProgress))
                : Mathf.Lerp(1.1f, 1f, Mathf.SmoothStep(0f, 1f,
                    (progress - PeakProgress) / (1f - PeakProgress)));
            ApplyScale(scale);
            if (progress >= 1f)
            {
                IsCelebrating = false;
                ApplyScale(1f);
            }
        }

        public void Configure(GameManager manager, Text text, ParticleSystem particles, AudioManager audio)
        {
            bool managerChanged = gameManager != manager;
            Unsubscribe();
            StopCelebration();
            gameManager = manager;
            clearText = text;
            confetti = particles;
            audioManager = audio;
            hasBaseScale = false;
            CaptureBaseScale();
            if (managerChanged)
            {
                hasCelebrated = false;
            }
            if (isActiveAndEnabled)
            {
                Subscribe();
                if (gameManager != null)
                {
                    HandleStateChanged(gameManager.State);
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Clear)
            {
                StopCelebration();
                if (state == GameState.Edit)
                {
                    hasCelebrated = false;
                }
                return;
            }
            if (hasCelebrated)
            {
                return;
            }

            hasCelebrated = true;
            elapsed = 0f;
            IsCelebrating = true;
            ApplyScale(0.75f);
            if (confetti != null)
            {
                confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                confetti.Play(true);
            }
            if (audioManager != null)
            {
                audioManager.PlayClear();
            }
        }

        private void StopCelebration()
        {
            IsCelebrating = false;
            elapsed = 0f;
            ApplyScale(1f);
            if (confetti != null)
            {
                confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (audioManager != null)
            {
                audioManager.Stop();
            }
        }

        private void CaptureBaseScale()
        {
            if (!hasBaseScale && clearText != null)
            {
                baseScale = clearText.rectTransform.localScale;
                hasBaseScale = true;
            }
        }

        private void ApplyScale(float multiplier)
        {
            if (hasBaseScale && clearText != null)
            {
                clearText.rectTransform.localScale = baseScale * multiplier;
            }
        }

        private void Subscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += HandleStateChanged;
            }
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
        }
    }
}
