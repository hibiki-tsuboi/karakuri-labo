using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public sealed class SeesawGoalMember : MonoBehaviour
    {
        private HingeJoint hinge;
        private BallController configuredBall;
        private bool attempting;
        private bool hasBallContact;
        private bool hasRocked;
        private float attemptAngle;
        private float contactAngle;
        private float requiredTilt;

        public bool HasBallContact => hasBallContact;

        public bool HasRocked
        {
            get
            {
                EvaluateTilt();
                return hasRocked;
            }
        }

        private void FixedUpdate() => EvaluateTilt();

        private void OnDisable() => ResetProgress();

        public void BeginAttempt(BallController ball, float minimumTilt = 6f)
        {
            ResetProgress();
            hinge = GetComponent<HingeJoint>();
            configuredBall = ball;
            requiredTilt = Mathf.Max(0.1f, minimumTilt);
            if (hinge != null && configuredBall != null)
            {
                attemptAngle = hinge.angle;
                attempting = true;
            }
        }

        public void ResetProgress()
        {
            attempting = false;
            hasBallContact = false;
            hasRocked = false;
            configuredBall = null;
            attemptAngle = 0f;
            contactAngle = 0f;
        }

        private void OnCollisionEnter(Collision collision) => CheckContact(collision);

        private void OnCollisionStay(Collision collision) => CheckContact(collision);

        private void CheckContact(Collision collision)
        {
            if (!attempting || hasBallContact || hinge == null || configuredBall == null ||
                collision.rigidbody == null || collision.rigidbody != configuredBall.Body)
            {
                return;
            }

            // This component belongs to the board's own Rigidbody, so a ball
            // touching only the stand cannot activate the seesaw objective.
            contactAngle = hinge.angle;
            hasBallContact = true;
        }

        private void EvaluateTilt()
        {
            if (!attempting || !hasBallContact || hasRocked || hinge == null)
            {
                return;
            }

            float angle = hinge.angle;
            // A tilt that happened before the ball arrived earns no credit by
            // itself. Require meaningful new motion after the actual contact,
            // as well as movement away from the attempt's starting orientation.
            if (Mathf.Abs(Mathf.DeltaAngle(contactAngle, angle)) >= requiredTilt &&
                Mathf.Abs(Mathf.DeltaAngle(attemptAngle, angle)) >= requiredTilt)
            {
                hasRocked = true;
            }
        }
    }
}
