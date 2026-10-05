using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DominoChainMember : MonoBehaviour
    {
        private Rigidbody body;
        private bool attempting;
        private bool activated;
        private bool toppled;

        public bool HasToppled
        {
            get
            {
                if (attempting && activated && body != null &&
                    Vector3.Dot(body.rotation * Vector3.up, Vector3.up) <= 0.5f)
                {
                    toppled = true;
                }
                return toppled;
            }
        }

        public void BeginAttempt()
        {
            ResetProgress();
            body = GetComponent<Rigidbody>();
            attempting = true;
        }

        public void ResetProgress()
        {
            attempting = false;
            activated = false;
            toppled = false;
        }

        private void OnCollisionEnter(Collision collision) => CheckContact(collision);
        private void OnCollisionStay(Collision collision) => CheckContact(collision);

        private void CheckContact(Collision collision)
        {
            if (!attempting || activated || collision.rigidbody == null || body == null ||
                Vector3.Dot(body.rotation * Vector3.up, Vector3.up) <= 0.5f)
            {
                return;
            }
            Rigidbody other = collision.rigidbody;
            // Merely stacking pieces at the spawn point must not satisfy the
            // puzzle. A still-upright piece must be hit by the ball or its chain.
            activated = other.GetComponent<BallController>() != null ||
                (other.TryGetComponent(out DominoChainMember member) && member.activated);
        }
    }
}
