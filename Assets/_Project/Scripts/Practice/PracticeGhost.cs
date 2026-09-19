using BarrelRivals.Core;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BarrelRivals.PlayMode.Tests")]

namespace BarrelRivals.Practice
{
    /// <summary>Visual-only playback of an equivalent local personal best. Never an online opponent.</summary>
    public sealed class PracticeGhost : MonoBehaviour
    {
        [SerializeField] private Transform horse;
        [SerializeField] private Material ghostMaterial;
        private GameObject _visual;
        private Renderer[] _renderers;
        private bool[] _originallyEnabled;
        private PracticeReplayPlayer _player;
        private long _holdStartedMs;
        private bool _started;
        public bool Available => _player != null;
        public bool VisibleEnabled { get; set; } = true;

        public void Configure(Transform source, Material material) { horse=source; ghostMaterial=material; }
        public void SetBest(PracticeReplay replay)
        {
            _player = replay?.CreatePlayer(); _started=false;
            // Cache the original neutral rig before the first race, even when no PB exists yet.
            if (!_visual && horse && ghostMaterial)
            {
                _visual=Instantiate(horse.gameObject,horse.position,horse.rotation);
                _visual.name="Your personal-best ghost";
                foreach(var particles in _visual.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                foreach (var collider in _visual.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
                _renderers=_visual.GetComponentsInChildren<Renderer>(true);
                _originallyEnabled=new bool[_renderers.Length];
                for (int i=0;i<_renderers.Length;i++)
                {
                    var renderer=_renderers[i]; _originallyEnabled[i]=renderer.enabled;
                    var materials=renderer.sharedMaterials;
                    for (int m=0;m<materials.Length;m++) materials[m]=ghostMaterial;
                    renderer.sharedMaterials=materials;
                    renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                }
            }
            SetVisible(false);
        }
        public void StartAt(long holdStartedMs) { _holdStartedMs=holdStartedMs; _started=true; }
        public void Observe(PracticeRun run)
        {
            if (!_started || _player==null || !_visual || run.Phase==PracticePhase.Cancelled || run.Phase==PracticePhase.Complete)
            { SetVisible(false); return; }
            _player.AdvanceTo(System.Math.Max(0,run.NowMs-_holdStartedMs));
            var pose=PracticePath.Sample(_player.Run);
            _visual.transform.SetPositionAndRotation(new Vector3((float)pose.X,0,(float)pose.Z),Quaternion.Euler(0,(float)pose.HeadingDegrees,0));
            // An overlapping replay would obscure the body camera and current horse.
            SetVisible(VisibleEnabled && run.Phase!=PracticePhase.Ready && run.Phase!=PracticePhase.Gate &&
                (_visual.transform.position-horse.position).sqrMagnitude>5);
        }
        private void SetVisible(bool visible)
        {
            if (_renderers==null) return;
            for (int i=0;i<_renderers.Length;i++) if (_renderers[i]) _renderers[i].enabled=visible && _originallyEnabled[i];
        }
        private void OnDestroy() { if (_visual) Destroy(_visual); }
    }
}
