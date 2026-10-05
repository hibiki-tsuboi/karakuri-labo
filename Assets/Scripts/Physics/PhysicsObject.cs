using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class PhysicsObject : MonoBehaviour
    {
        [SerializeField] private bool simulateOnPlay;

        private Rigidbody body;
        private bool hasSimulationInterpolation;
        private RigidbodyInterpolation simulationInterpolation;
        private bool hasState;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private Vector3 savedScale;
        private Vector3 savedLinearVelocity;
        private Vector3 savedAngularVelocity;
        private bool savedKinematic;
        private bool savedGravity;
        private bool savedDetectCollisions;
        private bool savedSleeping;
        private RigidbodyConstraints savedConstraints;
        private RigidbodyInterpolation savedInterpolation;
        private CollisionDetectionMode savedCollisionDetection;

        private void Awake()
        {
            if (simulateOnPlay)
            {
                FreezeForEditing();
            }
        }

        public void ConfigureSimulation(bool enabled)
        {
            simulateOnPlay = enabled;
            if (simulateOnPlay)
            {
                FreezeForEditing();
            }
        }

        public void BeginSimulation()
        {
            if (!simulateOnPlay)
            {
                return;
            }

            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                // Placement changes the Transform between physics steps. Start
                // from that exact pose before interpolation takes over again.
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;
                body.position = position;
                body.rotation = rotation;
                body.useGravity = true;
                body.isKinematic = false;
                if (hasSimulationInterpolation)
                {
                    body.interpolation = simulationInterpolation;
                }
                body.WakeUp();
            }
        }

        public void FreezeSimulation()
        {
            body = GetComponent<Rigidbody>();
            if (body == null) return;

            Vector3 position = body.position;
            Quaternion rotation = body.rotation;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void FreezeForEditing()
        {
            body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                if (Application.isPlaying)
                {
                    if (!hasSimulationInterpolation)
                    {
                        simulationInterpolation = body.interpolation;
                        hasSimulationInterpolation = true;
                    }

                    // Interpolation can overwrite a directly edited Transform
                    // with the previous physics pose. Keep authored prefab settings
                    // intact and disable interpolation only during runtime editing.
                    body.interpolation = RigidbodyInterpolation.None;
                }
            }
        }

        public void CaptureState()
        {
            body = GetComponent<Rigidbody>();
            // Interpolation can leave the visible Transform behind the simulated
            // pose. Kinematic and static objects instead follow edit-time placement.
            bool simulated = body != null && !body.isKinematic;
            savedPosition = simulated ? body.position : transform.position;
            savedRotation = simulated ? body.rotation : transform.rotation;
            savedScale = transform.localScale;

            if (body != null)
            {
                savedLinearVelocity = body.linearVelocity;
                savedAngularVelocity = body.angularVelocity;
                savedKinematic = body.isKinematic;
                savedGravity = body.useGravity;
                savedDetectCollisions = body.detectCollisions;
                savedSleeping = body.IsSleeping();
                savedConstraints = body.constraints;
                savedInterpolation = body.interpolation;
                savedCollisionDetection = body.collisionDetectionMode;
            }

            hasState = true;
        }

        public void RestoreState()
        {
            if (!hasState)
            {
                return;
            }

            transform.SetPositionAndRotation(savedPosition, savedRotation);
            transform.localScale = savedScale;

            if (body == null)
            {
                return;
            }

            // A kinematic body rejects velocity assignments. No physics step occurs
            // while restoring, so temporarily release it before reinstating its mode.
            body.isKinematic = false;
            body.position = savedPosition;
            body.rotation = savedRotation;
            body.useGravity = savedGravity;
            body.constraints = savedConstraints;
            body.detectCollisions = savedDetectCollisions;
            body.linearVelocity = savedLinearVelocity;
            body.angularVelocity = savedAngularVelocity;
            body.isKinematic = savedKinematic;
            body.interpolation = savedInterpolation;
            body.collisionDetectionMode = savedCollisionDetection;

            if (!savedKinematic)
            {
                if (savedSleeping)
                {
                    body.Sleep();
                }
                else
                {
                    body.WakeUp();
                }
            }
        }
    }
}
