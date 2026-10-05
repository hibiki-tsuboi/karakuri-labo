using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class AttemptMonitor : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BallController ball;
        [SerializeField] private Bounds playableBounds;
        [SerializeField, Min(0.1f)] private float stationarySeconds = 3.5f;
        [SerializeField, Min(0.001f)] private float progressDistance = 0.08f;
        [SerializeField, Min(1f)] private float maximumSeconds = 45f;

        private Vector3 progressPosition;
        private float elapsed;
        private float stationary;

        public Bounds PlayableBounds => playableBounds;

        public void Configure(GameManager manager, BallController stageBall, Bounds bounds)
        {
            if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
            gameManager = manager;
            ball = stageBall;
            playableBounds = bounds;
            if (isActiveAndEnabled && gameManager != null)
                gameManager.StateChanged += HandleStateChanged;
            HandleStateChanged(GameState.Edit);
        }

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StateChanged += HandleStateChanged;
            HandleStateChanged(GameState.Edit);
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            elapsed = stationary = 0f;
            if (ball != null) progressPosition = ball.Body.position;
        }

        private void LateUpdate() => StepAttempt(Time.deltaTime);

        // Also used alongside Physics.Simulate in deterministic stage tests.
        public void StepAttempt(float deltaTime)
        {
            if (gameManager == null || ball == null || gameManager.State != GameState.Playing ||
                deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            Vector3 position = ball.Body.position;
            if (!playableBounds.Contains(position))
            {
                gameManager.FailAttempt(AttemptFailure.Fell);
                return;
            }

            elapsed += deltaTime;
            // World-space progress includes a ball being carried by a lift. Small
            // contact jitter or spinning in place must not postpone failure forever.
            if ((position - progressPosition).sqrMagnitude >= progressDistance * progressDistance)
            {
                progressPosition = position;
                stationary = 0f;
            }
            else
            {
                stationary += deltaTime;
            }

            if (stationary >= stationarySeconds)
                gameManager.FailAttempt(gameManager.HasReachedGoal
                    ? AttemptFailure.GoalRequirement : AttemptFailure.Stopped);
            else if (elapsed >= maximumSeconds)
                gameManager.FailAttempt(AttemptFailure.TimedOut);
        }
    }
}
