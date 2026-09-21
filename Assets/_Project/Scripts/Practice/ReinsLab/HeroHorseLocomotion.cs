using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Speed-driven presentation study. Never moves the actor or writes race state.</summary>
    [DisallowMultipleComponent]
    public sealed class HeroHorseLocomotion : MonoBehaviour
    {
        public HorseRigBindings horse;
        public HeroHorseAttachments attachments;
        [Range(0,14)] public float targetSpeed=1.5f;
        public float SmoothedSpeed { get; private set; }
        static readonly int Speed=Animator.StringToHash("Speed");
        static readonly int Rate=Animator.StringToHash("StrideRate");

        void OnEnable() { SmoothedSpeed=Sanitize(targetSpeed);Apply(); }
        void Update()=>Advance(Time.deltaTime);

        // Explicit timestep supports the same live driver in controlled Editor capture.
        public void Advance(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;
            SmoothedSpeed=Mathf.Lerp(SmoothedSpeed,Sanitize(targetSpeed),1-Mathf.Exp(-seconds/.25f));
            Apply();
        }
        static float Sanitize(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp(value,0,14);
        void Apply()
        {
            if(horse && horse.Animator && horse.Animator.runtimeAnimatorController)
            {
                horse.Animator.SetFloat(Speed,SmoothedSpeed);
                horse.Animator.SetFloat(Rate,Mathf.Max(1,SmoothedSpeed/12));
            }
            if(attachments)attachments.reviewSpeed=SmoothedSpeed;
        }
    }
}
