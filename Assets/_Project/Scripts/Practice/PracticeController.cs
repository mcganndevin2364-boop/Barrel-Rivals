using System;
using BarrelRivals.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    /// <summary>Unity presentation/input adapter for the offline shared practice rules.</summary>
    public sealed class PracticeController : MonoBehaviour
    {
        [SerializeField] private Transform horse, barrel;
        [SerializeField] private Camera rideCamera;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text phaseLabel, instruction, timer, actionLabel, shapeLabel, resultLabel;
        [SerializeField] private CanvasGroup actionGroup, drawingGroup, resultGroup;
        [SerializeField] private Image progress, drawingProgress;
        [SerializeField] private PatternGraphic pattern;
        [SerializeField] private Button retry;
        private PracticeRun _run;
        private double _origin;
        private uint _seed=104;
        private int? _pointer;
        private PracticePhase _pointerPhase, _shownPhase=(PracticePhase)(-1);
        private long _lastTextAt=-1000;
        private Vector3 _cameraStart;
        private Quaternion _cameraRotation, _barrelRotation;
        private AudioSource[] _beeps;
        private AudioClip[] _tones;
        public PracticeRun Run => _run;
        public bool HasBindings => horse && barrel && rideCamera && safeArea && phaseLabel && instruction && timer && actionLabel && shapeLabel && resultLabel && actionGroup && drawingGroup && resultGroup && progress && drawingProgress && pattern && retry;
        private long ClockMs => Math.Max(_run?.NowMs??0,(long)((Time.realtimeSinceStartupAsDouble-_origin)*1000));

        public void Configure(Transform horseRoot,Transform firstBarrel,Camera camera,RectTransform safe,
            Text phase,Text hint,Text clock,Text action,Text shape,Text result,CanvasGroup actionPanel,
            CanvasGroup drawingPanel,CanvasGroup resultPanel,Image bar,Image drawBar,PatternGraphic graphic,Button reset)
        {
            horse=horseRoot; barrel=firstBarrel; rideCamera=camera; safeArea=safe;
            phaseLabel=phase; instruction=hint; timer=clock; actionLabel=action; shapeLabel=shape; resultLabel=result;
            actionGroup=actionPanel; drawingGroup=drawingPanel; resultGroup=resultPanel;
            progress=bar; drawingProgress=drawBar; pattern=graphic; retry=reset;
        }
        private void Awake()
        {
            if(!HasBindings) { Debug.LogError("Practice scene has missing bindings.",this); enabled=false; return; }
            _cameraStart=rideCamera.transform.position; _cameraRotation=rideCamera.transform.rotation; _barrelRotation=barrel.rotation;
            _beeps=new AudioSource[3]; _tones=new AudioClip[3];
            for(int i=0;i<3;i++)
            {
                _beeps[i]=gameObject.AddComponent<AudioSource>(); _beeps[i].playOnAwake=false; _beeps[i].spatialBlend=0;
                _tones[i]=Tone(i==2?1100:650); _beeps[i].clip=_tones[i];
            }
            retry.onClick.AddListener(Retry); ResetRun();
        }
        public void Retry() { _seed++; ResetRun(); }
        private void ResetRun()
        {
            foreach(var source in _beeps) source.Stop();
            _origin=Time.realtimeSinceStartupAsDouble; _run=new PracticeRun(_seed); _pointer=null;
            _shownPhase=(PracticePhase)(-1); _lastTextAt=-1000;
            barrel.rotation=_barrelRotation; rideCamera.transform.SetPositionAndRotation(_cameraStart,_cameraRotation);
            pattern.Configure(_run.Pattern,_run.Trace); Present();
        }
        private void Update()
        {
            _run.AdvanceTo(ClockMs); Present();
            if(Screen.width>0 && Screen.height>0)
            {
                Rect r=Screen.safeArea; safeArea.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);
                safeArea.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);
            }
        }
        public void PointerDown(int id,bool drawing,TracePoint point)
        {
            if(_pointer.HasValue) return;
            long now=ClockMs; _run.AdvanceTo(now); bool accepted=false;
            if(drawing && _run.Phase==PracticePhase.Drawing) accepted=_run.BeginTrace(now,point);
            else if(!drawing && _run.Phase==PracticePhase.Ready)
            {
                accepted=_run.StartHold(now);
                if(accepted)
                {
                    long[] cues={_run.FirstBeepMs,_run.SecondBeepMs,_run.LaunchCueMs};
                    for(int i=0;i<3;i++) _beeps[i].PlayScheduled(AudioSettings.dspTime+(cues[i]-now)/1000.0);
                }
            }
            else if(!drawing && _run.Phase==PracticePhase.Exit) accepted=_run.TapExit(now);
            if(accepted) { _pointer=id; _pointerPhase=_run.Phase; }
            pattern.Refresh(_run.Phase==PracticePhase.Preview); Present();
        }
        public void PointerMove(int id,TracePoint point)
        {
            if(_pointer!=id || _pointerPhase!=PracticePhase.Drawing) return;
            _run.AddTrace(ClockMs,point); pattern.Refresh(_run.Phase==PracticePhase.Complete);
        }
        public void PointerUp(int id,TracePoint point)
        {
            if(_pointer!=id) return;
            long now=ClockMs;
            if(_pointerPhase==PracticePhase.Gate) _run.ReleaseHold(now);
            if(_pointerPhase==PracticePhase.Drawing) { _run.AddTrace(now,point); _run.SubmitTrace(now); pattern.Refresh(_run.Phase==PracticePhase.Complete); }
            _pointer=null; Present();
        }
        public void CancelPractice()
        {
            if(_run==null || !_run.Cancel(ClockMs)) return;
            _pointer=null; foreach(var source in _beeps) source.Stop(); Present();
        }
        private void OnApplicationPause(bool paused) { if(paused) CancelPractice(); }
        private void OnApplicationFocus(bool focused) { if(!focused) CancelPractice(); }
        private void OnDestroy()
        {
            if(retry) retry.onClick.RemoveListener(Retry);
            if(_tones!=null) foreach(var clip in _tones) if(clip) Destroy(clip);
        }
        private void Present()
        {
            if(_run.Phase!=PracticePhase.Cancelled)
            {
                var pose=PracticePath.Sample(_run);
                horse.SetPositionAndRotation(new Vector3((float)pose.X,0,(float)pose.Z),Quaternion.Euler(0,(float)pose.HeadingDegrees,0));
                if(_run.Phase!=PracticePhase.Ready)
                {
                    float blend=_run.Phase==PracticePhase.Gate ? Mathf.SmoothStep(0,1,(_run.NowMs-_run.PhaseStartedMs)/1000f) : 1;
                    rideCamera.transform.SetPositionAndRotation(Vector3.Lerp(_cameraStart,horse.TransformPoint(new Vector3(0,2.7f,-.35f)),blend),
                        Quaternion.Slerp(_cameraRotation,horse.rotation*Quaternion.Euler(7,0,0),blend));
                }
            }
            if(_run.KnockCount>0) barrel.rotation=_barrelRotation*Quaternion.Euler(0,0,-70);
            if(_shownPhase!=_run.Phase)
            {
                _shownPhase=_run.Phase; _lastTextAt=-1000;
                Show(actionGroup,_run.Phase==PracticePhase.Ready || _run.Phase==PracticePhase.Gate || _run.Phase==PracticePhase.Exit);
                Show(drawingGroup,_run.Phase==PracticePhase.Preview || _run.Phase==PracticePhase.Drawing || _run.Phase==PracticePhase.Complete);
                Show(resultGroup,_run.Phase==PracticePhase.Complete || _run.Phase==PracticePhase.Cancelled);
                pattern.Refresh(_run.Phase==PracticePhase.Preview || _run.Phase==PracticePhase.Complete);
                phaseLabel.text=PhaseTitle(_run.Phase);
                if(_run.Phase==PracticePhase.Complete)
                    resultLabel.text=$"{_run.Result.FinalTimeMs/1000.0:0.00}s\nRaw {_run.Result.RawTimeMs/1000.0:0.00}s  +  {_run.KnockCount*5}s penalty\n\nLaunch: {_run.LaunchGrade}\nDrawing: {_run.DrawingGrade.Score}/100 — {_run.DrawingGrade.Grade}\nExit: {_run.ExitGrade}\n\nOne barrel complete. Try again to improve.";
                if(_run.Phase==PracticePhase.Cancelled) resultLabel.text="Practice interrupted\n\nRetry when you are ready.\nNo result was recorded.";
            }
            progress.fillAmount=_run.Phase==PracticePhase.Exit ? (float)_run.PhaseProgress : 1-(float)_run.PhaseProgress;
            // Size the solid image directly; Image.fillAmount requires an assigned sprite.
            progress.rectTransform.anchorMax=new Vector2(progress.fillAmount,1);
            progress.color=_run.Phase==PracticePhase.Exit && Math.Abs(_run.PhaseProgress-.5)<=.1 ? new Color(.3f,1,.7f) : new Color(1,.73f,.32f);
            drawingProgress.rectTransform.anchorMax=new Vector2(_run.Phase==PracticePhase.Complete ? 0 : 1-(float)_run.PhaseProgress,1);
            if(_run.NowMs-_lastTextAt<50) return;
            _lastTextAt=_run.NowMs; timer.text=$"{_run.RawTimeMs/1000.0:0.00}s  ·  +{_run.KnockCount*5}s";
            shapeLabel.text=_run.Phase==PracticePhase.Preview ? "MEMORIZE · "+_run.Pattern.ToString().ToUpperInvariant() : _run.Phase==PracticePhase.Complete ? "GOLD: TARGET   MINT: YOUR TRACE" : $"{(_run.TraceSubmitted?"TRACE LOCKED":"DRAW FROM MEMORY")} · {Math.Max(0,_run.DrawingClosesMs-_run.NowMs)/1000.0:0.0}s";
            switch(_run.Phase)
            {
                case PracticePhase.Ready: instruction.text="One barrel. Three skills. Hold through the beeps, then release on GO."; actionLabel.text="HOLD TO BEGIN"; break;
                case PracticePhase.Gate:
                    instruction.text=_run.LaunchErrorMs<0 ? "Early release — wait for your launch." : "Keep holding. Release on the third beep.";
                    actionLabel.text=_run.NowMs>=_run.LaunchCueMs ? "GO! RELEASE" : _run.NowMs>=_run.SecondBeepMs ? "2 · HOLD" : _run.NowMs>=_run.FirstBeepMs ? "1 · HOLD" : "READY… HOLD"; break;
                case PracticePhase.Alley: instruction.text=_run.AutoLaunched ? "Auto-launch. Lift your finger, then get ready to draw." : "Launch: "+_run.LaunchGrade+". Your horse follows the course."; break;
                case PracticePhase.Preview: instruction.text="Remember the gold shape. It will disappear."; break;
                case PracticePhase.Drawing: instruction.text=_pointer.HasValue && _pointerPhase==PracticePhase.Gate ? "Lift your finger, then start a fresh drawing touch." : _run.TraceSubmitted ? "Trace received. The turn window stays the same length." : "Draw one closed shape in the pad. Lift when finished."; break;
                case PracticePhase.Turn: instruction.text="Drawing: "+_run.DrawingGrade.Grade+". "+TraceHint(_run.DrawingGrade.Reason)+(_run.DrawingGrade.Quality<35 ? " Barrel contact: +5s." : " Get ready for the exit cue."); break;
                case PracticePhase.Exit: instruction.text="Use a NEW tap when the bar reaches the center mark."; actionLabel.text=_run.ExitAccepted ? "EXIT: "+_run.ExitGrade.ToString().ToUpperInvariant() : "TAP AT CENTER"; break;
                case PracticePhase.RunOut: instruction.text="Exit: "+_run.ExitGrade+". Finishing the practice section…"; break;
                case PracticePhase.Complete: instruction.text="Compare your trace with the target. Retry brings a new challenge."; break;
                case PracticePhase.Cancelled: instruction.text="Select Retry to start a fresh practice run."; break;
            }
        }
        private static void Show(CanvasGroup group,bool visible) { group.alpha=visible?1:0; group.blocksRaycasts=visible; group.interactable=visible; }
        private static string TraceHint(TraceReason reason)
        {
            switch(reason)
            {
                case TraceReason.TooSmall: return "Draw a larger outline.";
                case TraceReason.OpenShape: return "Close the shape.";
                case TraceReason.ExcessLength: return "Use one outline without extra loops.";
                case TraceReason.Invalid: return "Keep a single stroke inside the pad.";
                case TraceReason.Missing: return "Draw a complete shape before time ends.";
                case TraceReason.LowAccuracy: return "Follow the remembered outline more closely.";
                default: return "Shape accepted.";
            }
        }
        private static string PhaseTitle(PracticePhase phase)
        {
            switch(phase)
            {
                case PracticePhase.Ready: return "FIRST BARREL · PRACTICE";
                case PracticePhase.Gate: return "01 / LAUNCH";
                case PracticePhase.Alley: return "RIDE THE ALLEY";
                case PracticePhase.Preview: return "02 / REMEMBER";
                case PracticePhase.Drawing: return "02 / DRAW";
                case PracticePhase.Turn: return "ROUND THE BARREL";
                case PracticePhase.Exit: return "03 / EXIT TIMING";
                case PracticePhase.RunOut: return "FINISH STRONG";
                case PracticePhase.Complete: return "FIRST BARREL COMPLETE";
                default: return "PRACTICE PAUSED";
            }
        }
        private static AudioClip Tone(float frequency)
        {
            const int rate=22050, count=2205; var samples=new float[count];
            for(int i=0;i<count;i++) samples[i]=Mathf.Sin(2*Mathf.PI*frequency*i/rate)*.16f*Mathf.Min(i/110f,(count-i)/220f,1);
            var clip=AudioClip.Create("Practice cue",count,1,rate,false); clip.SetData(samples,0); return clip;
        }
    }
}
