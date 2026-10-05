using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(PhysicsObject))]
    public sealed class BallController : MonoBehaviour
    {
        private Rigidbody body;

        public Rigidbody Body
        {
            get
            {
                if (body == null)
                {
                    body = GetComponent<Rigidbody>();
                }

                return body;
            }
        }

        private void Awake()
        {
            Body.isKinematic = true;
        }

        public void BeginSimulation()
        {
            Body.useGravity = true;
            Body.isKinematic = false;
            Body.WakeUp();
        }
    }
}
