using UnityEngine;

namespace KarakuriLabo
{
    public sealed class BallLift : BallMechanism
    {
        [SerializeField] private Rigidbody platform;
        [SerializeField] private Transform gate;
        [SerializeField] private Collider gateCollider;
        [SerializeField] private float travel = 3.2f;
        [SerializeField] private float speed = 1.25f;
        private BallController passenger;
        private float height;
        private float boardingTime;
        private float gateAngle;
        public float Height => height;

        public void Configure(Rigidbody carriage, Transform exitGate)
        {
            platform = carriage;
            gate = exitGate;
            gateCollider = gate.GetComponentInChildren<Collider>();
        }

        protected override void Interact(BallController ball, int channel)
        {
            if (passenger == null && height == 0) passenger = ball;
        }

        protected override void Simulate(float deltaTime)
        {
            if (platform == null || passenger == null) return;
            Vector3 local = platform.transform.InverseTransformPoint(passenger.Body.position);
            if (height == 0 && (Mathf.Abs(local.x) > 0.85f || Mathf.Abs(local.z) > 0.65f || local.y > 1.1f))
            {
                passenger = null;
                boardingTime = 0;
                return;
            }
            boardingTime += deltaTime;
            if (boardingTime < 0.25f) return;
            height = Mathf.MoveTowards(height, travel, deltaTime * speed);
            platform.MovePosition(transform.TransformPoint(new Vector3(0, 0.2f + height, 0)));
            if (height >= travel)
            {
                WasUsed = true;
                gateAngle = Mathf.MoveTowards(gateAngle, 90, deltaTime * 240);
                if (gateCollider != null) gateCollider.enabled = false;
                if (gate != null) gate.localRotation = Quaternion.Euler(0, 0, -gateAngle);
            }
        }

        protected override void ResetMechanism()
        {
            passenger = null;
            height = boardingTime = gateAngle = 0;
            if (platform != null)
            {
                platform.transform.localPosition = new Vector3(0, 0.2f, 0);
                platform.position = platform.transform.position;
            }
            if (gate != null) gate.localRotation = Quaternion.identity;
            if (gateCollider != null) gateCollider.enabled = true;
        }
    }
}
