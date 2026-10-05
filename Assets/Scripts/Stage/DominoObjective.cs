using System;
using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class DominoObjective : StageObjective
    {
        [SerializeField] private Transform dominoContainer;
        [SerializeField, Min(1)] private int requiredCount = 3;
        [SerializeField] private Text progressLabel;
        [SerializeField] private DominoGate gate;

        private DominoChainMember[] attemptMembers = Array.Empty<DominoChainMember>();
        private GameManager gameManager;

        public int RequiredCount => requiredCount;
        public int ToppledCount
        {
            get
            {
                int count = 0;
                foreach (DominoChainMember member in attemptMembers)
                {
                    if (member != null && member.HasToppled)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
        public override bool IsComplete => ToppledCount >= requiredCount && (gate == null || gate.IsOpen);

        public bool OwnsToppledMember(DominoChainMember member)
        {
            return member != null && Array.IndexOf(attemptMembers, member) >= 0 && member.HasToppled;
        }

        private void Awake()
        {
            gameManager = GetComponent<GameManager>();
            RefreshLabel();
        }

        private void LateUpdate() => RefreshLabel();

        private void OnDisable() => ResetProgress();

        public void Configure(Transform container, int count, Text label, DominoGate stageGate = null)
        {
            ResetProgress();
            dominoContainer = container;
            requiredCount = Mathf.Max(1, count);
            progressLabel = label;
            gate = stageGate;
            gameManager = GetComponent<GameManager>();
            RefreshLabel();
        }

        public override void BeginAttempt()
        {
            ResetProgress();
            if (dominoContainer != null)
            {
                attemptMembers = dominoContainer.GetComponentsInChildren<DominoChainMember>();
                foreach (DominoChainMember member in attemptMembers)
                {
                    member.BeginAttempt();
                }
            }
            if (gate != null)
            {
                gate.BeginAttempt(this);
            }
            RefreshLabel();
        }

        public override void ResetProgress()
        {
            if (gate != null)
            {
                gate.ResetMechanism();
            }
            foreach (DominoChainMember member in attemptMembers)
            {
                if (member != null)
                {
                    member.ResetProgress();
                }
            }
            attemptMembers = Array.Empty<DominoChainMember>();
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (progressLabel != null)
            {
                progressLabel.text = $"ドミノ {ToppledCount} / {requiredCount}" +
                    (gameManager != null && gameManager.State == GameState.Clear ? " ／ できた！"
                        : gate != null ? (gate.IsOpen ? " ／ あいたよ" : gate.IsPressed
                            ? " ／ あくよ" : " ／ とびら")
                        : gameManager != null && gameManager.HasReachedGoal ? " ／ ゴール！" : "");
            }
        }
    }
}
