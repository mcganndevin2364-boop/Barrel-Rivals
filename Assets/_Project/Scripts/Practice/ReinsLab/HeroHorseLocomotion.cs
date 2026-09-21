using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Speed-driven presentation study. Never moves the actor or writes race state.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class HeroHorseLocomotion : MonoBehaviour
    {
        public HorseRigBindings horse;
        public HeroHorseAttachments attachments;
        public ReinsHorsePresentation source;
        [Range(0,14)] public float targetSpeed=1.5f;
        public float SmoothedSpeed { get; private set; }
        static readonly int Speed=Animator.StringToHash("Speed");
        static readonly int Rate=Animator.StringToHash("StrideRate");
        int lastTick=-1;

        void OnEnable() { SmoothedSpeed=Sanitize(source ? source.Speed : targetSpeed);lastTick=-1;Apply(); }
        void Update()=>Advance(Time.deltaTime);

        // Explicit timestep supports the same live driver in controlled Editor capture.
        public void Advance(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)return;
            if(source)
            {
                targetSpeed=source.Speed;
                if(source.Tick<lastTick || lastTick<0)SmoothedSpeed=Sanitize(targetSpeed);
                lastTick=source.Tick;
            }
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
