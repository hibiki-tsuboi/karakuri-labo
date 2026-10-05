using UnityEngine;

namespace KarakuriLabo
{
    public abstract class StageObjective : MonoBehaviour
    {
        public abstract bool IsComplete { get; }
        public abstract void BeginAttempt();
        public abstract void ResetProgress();
    }
}
