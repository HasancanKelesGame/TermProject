using UnityEngine;

namespace TermProject.AI
{
    [DisallowMultipleComponent]
    public sealed class BotAnimationEventReceiver : MonoBehaviour
    {
        // The imported soldier clips call these events. We handle them here so Unity
        // does not log missing receiver errors during gameplay.
        public void FootStep()
        {
        }

        public void RollSound()
        {
        }

        public void CantRotate()
        {
        }

        public void EndRoll()
        {
        }

        public void EndPickup()
        {
        }
    }
}
