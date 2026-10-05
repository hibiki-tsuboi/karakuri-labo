using UnityEngine;

namespace KarakuriLabo
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MechanismSensor : MonoBehaviour
    {
        [SerializeField] private BallMechanism mechanism;
        [SerializeField] private int channel;

        public void Configure(BallMechanism owner, int sensorChannel)
        {
            mechanism = owner;
            channel = sensorChannel;
        }

        private void OnTriggerEnter(Collider other) => Notify(other);
        private void OnTriggerStay(Collider other) => Notify(other);

        private void Notify(Collider other)
        {
            if (mechanism != null) mechanism.Receive(other, channel);
        }
    }
}
