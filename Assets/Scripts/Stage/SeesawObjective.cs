using System;
using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class SeesawObjective : StageObjective
    {
        [SerializeField] private Transform spawnedRoot;
        [SerializeField] private BallController ball;
        [SerializeField] private Text progressLabel;
        [SerializeField, Min(0.1f)] private float requiredTilt = 6f;

        private SeesawGoalMember[] attemptMembers = Array.Empty<SeesawGoalMember>();
        private GameManager gameManager;

        public float RequiredTilt => requiredTilt;

        public bool HasRocked
        {
            get
            {
                foreach (SeesawGoalMember member in attemptMembers)
                {
                    if (member != null && member.HasRocked)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override bool IsComplete => HasRocked;

        private void Awake()
        {
            gameManager = GetComponent<GameManager>();
            RefreshLabel();
        }

        private void LateUpdate() => RefreshLabel();

        private void OnDisable() => ResetProgress();

        public void Configure(Transform container, BallController configuredBall, Text label)
        {
            ResetProgress();
            spawnedRoot = container;
            ball = configuredBall;
            progressLabel = label;
            gameManager = GetComponent<GameManager>();
            RefreshLabel();
        }

        public override void BeginAttempt()
        {
            ResetProgress();
            if (spawnedRoot != null && ball != null)
            {
                attemptMembers = spawnedRoot.GetComponentsInChildren<SeesawGoalMember>();
                foreach (SeesawGoalMember member in attemptMembers)
                {
                    member.BeginAttempt(ball, requiredTilt);
                }
            }
            RefreshLabel();
        }

        public override void ResetProgress()
        {
            foreach (SeesawGoalMember member in attemptMembers)
            {
                if (member != null)
                {
                    member.ResetProgress();
                }
            }
            attemptMembers = Array.Empty<SeesawGoalMember>();
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (progressLabel != null)
            {
                progressLabel.text = $"SEESAW {(HasRocked ? 1 : 0)} / 1" +
                    (gameManager != null && gameManager.State == GameState.Clear ? "  /  CLEAR!"
                        : gameManager != null && gameManager.HasReachedGoal ? "  /  BALL IN GOAL" : "");
            }
        }
    }
}
