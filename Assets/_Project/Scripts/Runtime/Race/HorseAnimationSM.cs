using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class HorseAnimationSM : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int TurnHash = Animator.StringToHash("TurnAngle");

        public void UpdateAnimation(float speed, float steer)
        {
            if (_animator == null) return;
            _animator.SetFloat(SpeedHash, speed);
            _animator.SetFloat(TurnHash, steer);
        }
    }
}
