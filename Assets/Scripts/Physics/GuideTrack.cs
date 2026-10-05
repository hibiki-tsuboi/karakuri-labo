using UnityEngine;

namespace KarakuriLabo
{
    /// <summary>Curve and funnel motion comes entirely from their hollow collision geometry.</summary>
    public sealed class GuideTrack : BallMechanism
    {
        private BallController enteredBall;

        protected override void Interact(BallController ball, int channel)
        {
            if (channel == 0) enteredBall = ball;
            else if (enteredBall == ball) WasUsed = true;
        }

        protected override void ResetMechanism() => enteredBall = null;
    }
}
