using UnityEngine;

namespace KarakuriLabo
{
    /// <summary>Shared attempt lifecycle for toys placed through the normal part palette.</summary>
    public abstract class BallMechanism : MonoBehaviour
    {
        private GameManager manager;
        public bool WasUsed { get; protected set; }
        protected bool IsRunning => isActiveAndEnabled && manager != null && manager.State == GameState.Playing;

        protected virtual void OnEnable()
        {
            if (!Application.isPlaying) return;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                manager = root.GetComponentInChildren<GameManager>();
                if (manager != null) break;
            }
            if (manager != null) manager.StateChanged += HandleState;
            WasUsed = false;
            ResetMechanism();
        }

        protected virtual void OnDisable()
        {
            if (manager != null) manager.StateChanged -= HandleState;
        }

        private void HandleState(GameState state)
        {
            if (state == GameState.Edit)
            {
                WasUsed = false;
                ResetMechanism();
            }
        }

        public void Receive(Collider collider, int channel)
        {
            if (!IsRunning || collider.attachedRigidbody == null) return;
            BallController ball = collider.attachedRigidbody.GetComponent<BallController>();
            if (ball != null && !ball.Body.isKinematic) Interact(ball, channel);
        }

        private void FixedUpdate() => StepPhysics(Time.fixedDeltaTime);

        // Also used by the editor's deterministic trajectory checks, alongside Physics.Simulate.
        public void StepPhysics(float deltaTime)
        {
            if (IsRunning && deltaTime > 0) Simulate(deltaTime);
        }

        protected virtual void Simulate(float deltaTime) { }
        protected abstract void Interact(BallController ball, int channel);
        protected abstract void ResetMechanism();
    }
}
