using BarrelRivals.Core;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Bounded world-only dirt puffs. Uses movement as presentation input; never changes a run.</summary>
    public sealed class PracticeDust : MonoBehaviour
    {
        [SerializeField] private Transform horse;
        [SerializeField] private ParticleSystem particles;
        private PracticeRun _run;
        private Vector3 _previous;
        private long _previousMs;
        private float _distance;
        private int _side;
        public void Configure(Transform source,ParticleSystem system) { horse=source; particles=system; }
        public void Observe(PracticeRun run)
        {
            if(!horse || !particles || run==null) return;
            if(!ReferenceEquals(_run,run))
            {
                _run=run; _previous=horse.position; _previousMs=run.NowMs; _distance=0; _side=0;
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); return;
            }
            if(run.Phase==PracticePhase.Cancelled || run.Phase==PracticePhase.Complete)
            { particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); return; }
            long elapsed=run.NowMs-_previousMs;
            if(elapsed<=0) return;
            float moved=Vector3.Distance(_previous,horse.position);
            _previous=horse.position; _previousMs=run.NowMs;
            if(elapsed>250 || moved>3 || run.Phase==PracticePhase.Ready || run.Phase==PracticePhase.Gate) { _distance=0; return; }
            _distance+=moved;
            float spacing=run.Phase==PracticePhase.Turn ? .24f : .65f;
            if(_distance<spacing) return;
            _distance=0; _side=1-_side;
            var puff=new ParticleSystem.EmitParams
            {
                position=horse.TransformPoint(new Vector3(_side==0?-.24f:.24f,.06f,-.6f)),
                velocity=-horse.forward*.25f+Vector3.up*.32f,
                startSize=run.Phase==PracticePhase.Turn ? .40f : .24f,
                startLifetime=.85f,
                startColor=new Color(.57f,.45f,.32f,.20f)
            };
            if(!particles.isPlaying) particles.Play();
            particles.Emit(puff,run.Phase==PracticePhase.Turn?2:1);
        }
    }
}
