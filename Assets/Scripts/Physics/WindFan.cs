using UnityEngine;

namespace KarakuriLabo
{
    public sealed class WindFan : BallMechanism
    {
        [SerializeField] private Transform rotor;
        [SerializeField] private Transform[] streamers;
        [SerializeField] private float reach = 7;
        [SerializeField] private float acceleration = 8;
        private float phase;

        public void Configure(Transform blades, Transform[] ribbons)
        {
            rotor = blades;
            streamers = ribbons;
        }

        protected override void Interact(BallController ball, int channel)
        {
            Vector3 local = transform.InverseTransformPoint(ball.Body.position) - new Vector3(0, 0.8f, 0);
            if (local.x < 0.55f || local.x > reach || new Vector2(local.y, local.z).magnitude > 1.05f) return;
            Vector3 origin = transform.TransformPoint(new Vector3(0.6f, 0.8f, 0));
            Vector3 direction = ball.Body.position - origin;
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.rigidbody != ball.Body) return;
            float along = Vector3.Dot(ball.Body.linearVelocity, transform.right);
            if (along < 5.5f)
            {
                ball.Body.AddForce(transform.right * (acceleration * (1 - 0.45f * local.x / reach)), ForceMode.Acceleration);
                ball.Body.WakeUp();
                WasUsed = true;
            }
        }

        protected override void Simulate(float deltaTime)
        {
            phase += deltaTime;
            if (rotor != null) rotor.localRotation = Quaternion.Euler(phase * 620, 0, 0);
            if (streamers == null) return;
            for (int i = 0; i < streamers.Length; i++)
            {
                if (streamers[i] != null) streamers[i].localRotation = Quaternion.Euler(0, Mathf.Sin(phase * 9 + i) * 6, Mathf.Sin(phase * 7 + i) * 5);
            }
        }

        protected override void ResetMechanism()
        {
            phase = 0;
            if (rotor != null) rotor.localRotation = Quaternion.identity;
            if (streamers == null) return;
            foreach (Transform ribbon in streamers)
            {
                if (ribbon != null) ribbon.localRotation = Quaternion.identity;
            }
        }
    }
}
