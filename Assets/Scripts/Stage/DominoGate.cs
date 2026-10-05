using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class DominoGate : MonoBehaviour
    {
        [SerializeField] private Rigidbody barrier;
        [SerializeField] private Transform buttonTop;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Vector3 closedPosition;
        [SerializeField] private Vector3 releasedButtonPosition;
        [SerializeField, Min(0.1f)] private float liftHeight = 1.4f;
        [SerializeField, Min(0.1f)] private float liftSpeed = 3.5f;

        private DominoObjective objective;
        private float lift;
        private MaterialPropertyBlock indicatorProperties;

        public bool IsPressed { get; private set; }
        public bool IsOpen => IsPressed && lift >= liftHeight;

        private void Awake() => ResetMechanism();

        private void OnDisable() => ResetMechanism();

        private void FixedUpdate()
        {
            if (!IsPressed || barrier == null || lift >= liftHeight)
            {
                return;
            }
            lift = Mathf.MoveTowards(lift, liftHeight, liftSpeed * Time.fixedDeltaTime);
            barrier.MovePosition(barrier.transform.parent.TransformPoint(closedPosition + Vector3.up * lift));
        }

        public void Configure(Rigidbody gateBarrier, Transform switchTop, Renderer statusIndicator)
        {
            barrier = gateBarrier;
            buttonTop = switchTop;
            indicator = statusIndicator;
            closedPosition = barrier.transform.localPosition;
            releasedButtonPosition = buttonTop.localPosition;
            ResetMechanism();
        }

        public void BeginAttempt(DominoObjective stageObjective)
        {
            ResetMechanism();
            objective = stageObjective;
        }

        internal void TryPress(DominoChainMember member)
        {
            if (IsPressed || objective == null || !objective.OwnsToppledMember(member) ||
                objective.ToppledCount < objective.RequiredCount)
            {
                return;
            }
            IsPressed = true;
            if (buttonTop != null)
            {
                buttonTop.localPosition = releasedButtonPosition + Vector3.down * 0.06f;
            }
            SetIndicator(new Color32(65, 188, 121, 255));
        }

        public void ResetMechanism()
        {
            objective = null;
            IsPressed = false;
            lift = 0f;
            if (barrier != null)
            {
                barrier.transform.localPosition = closedPosition;
                barrier.position = barrier.transform.position;
            }
            if (buttonTop != null)
            {
                buttonTop.localPosition = releasedButtonPosition;
            }
            SetIndicator(new Color32(229, 139, 59, 255));
        }

        private void SetIndicator(Color color)
        {
            if (indicator == null)
            {
                return;
            }
            if (indicatorProperties == null)
            {
                indicatorProperties = new MaterialPropertyBlock();
            }
            indicator.GetPropertyBlock(indicatorProperties);
            indicatorProperties.SetColor("_BaseColor", color);
            indicator.SetPropertyBlock(indicatorProperties);
        }
    }
}
