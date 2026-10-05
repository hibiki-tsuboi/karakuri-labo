using UnityEngine;

namespace KarakuriLabo
{
    public sealed class SpringPad : BallMechanism
    {
        [SerializeField] private Transform plate;
        [SerializeField] private Transform coil;
        [SerializeField] private float forwardSpeed = 5.2f;
        [SerializeField] private float upwardSpeed = 7.2f;
        private float cooldown;

        public void Configure(Transform movingPlate, Transform spring)
        {
            plate = movingPlate;
            coil = spring;
        }

        protected override void Interact(BallController ball, int channel)
        {
            Vector3 local = transform.InverseTransformPoint(ball.Body.position);
            if (cooldown > 0 || ball.Body.linearVelocity.y > 0.5f || local.y < 0.3f ||
                Mathf.Abs(local.x) > 0.85f || Mathf.Abs(local.z) > 0.85f) return;
            ball.Body.linearVelocity = transform.right * forwardSpeed + Vector3.up * upwardSpeed;
            ball.Body.WakeUp();
            cooldown = 0.65f;
            WasUsed = true;
        }

        protected override void Simulate(float deltaTime)
        {
            cooldown = Mathf.Max(0, cooldown - deltaTime);
            float age = 0.65f - cooldown;
            float compression = cooldown > 0 ? Mathf.Sin(Mathf.Clamp01(age / 0.22f) * Mathf.PI) * 0.16f : 0;
            if (plate != null) plate.localPosition = new Vector3(0, 0.34f - compression, 0);
            if (coil != null) coil.localScale = new Vector3(1, 1 - compression * 2.6f, 1);
        }

        protected override void ResetMechanism()
        {
            cooldown = 0;
            if (plate != null) plate.localPosition = new Vector3(0, 0.34f, 0);
            if (coil != null) coil.localScale = Vector3.one;
        }
    }
}
