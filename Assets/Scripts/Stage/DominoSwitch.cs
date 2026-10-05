using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DominoSwitch : MonoBehaviour
    {
        [SerializeField] private DominoGate gate;

        public void Configure(DominoGate controlledGate) => gate = controlledGate;

        private void OnTriggerEnter(Collider other) => CheckContact(other);

        private void OnTriggerStay(Collider other) => CheckContact(other);

        private void CheckContact(Collider other)
        {
            // The ball, scenery and unregistered/fallen pieces cannot operate
            // this switch. Its input is a physically contacting domino chain.
            if (gate != null && other.attachedRigidbody != null &&
                other.attachedRigidbody.TryGetComponent(out DominoChainMember member))
            {
                gate.TryPress(member);
            }
        }
    }
}
