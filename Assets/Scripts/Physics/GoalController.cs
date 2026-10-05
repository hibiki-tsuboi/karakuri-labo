using System;
using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GoalController : MonoBehaviour
    {
        public event Action<BallController> BallEntered;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody otherBody = other.attachedRigidbody;
            if (otherBody != null && otherBody.TryGetComponent(out BallController ball))
            {
                BallEntered?.Invoke(ball);
            }
        }
    }
}
