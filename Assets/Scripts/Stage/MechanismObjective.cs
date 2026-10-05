using UnityEngine;

namespace KarakuriLabo
{
    public sealed class MechanismObjective : StageObjective
    {
        [SerializeField] private PartSpawner spawner;
        public override bool IsComplete
        {
            get
            {
                if (spawner == null) return false;
                BallMechanism[] mechanisms = spawner.GetComponentsInChildren<BallMechanism>();
                if (mechanisms.Length == 0) return false;
                foreach (BallMechanism mechanism in mechanisms)
                {
                    if (!mechanism.WasUsed) return false;
                }
                return true;
            }
        }
        public void Configure(PartSpawner parts) => spawner = parts;
        public override void BeginAttempt() { }
        public override void ResetProgress() { }
    }
}
