using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Core;
using BarrelRivals.Core.Reins;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    /// <summary>Offline experimental adapter. Only ReinsRun owns motion, contact, timing and grades.</summary>
    public sealed class ReinsLabController : MonoBehaviour
    {
        public const string SceneName="Arena_ReinsLab";
        public const uint ChallengeSeed=104;
        [SerializeField] private Transform horse;
        [SerializeField] private Transform[] barrels;
        [SerializeField] private Camera rideCamera;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text title,hint,status,feedback,action,results,surfaceText,soundText,cameraText,ghostText;
        [SerializeField] private Image cadence,leftFill,rightFill;
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private ReinsMapGraphic map;
        [SerializeField] private Button begin,retry,surfaceButton,soundButton,cameraButton,classicButton,ghostButton;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Button stableButton;
        public bool CanOpenStable => run!=null && (run.Phase==ReinsPhase.Ready || Terminal(run.Phase));
        public void ConfigureStable(Button button) { stableButton=button; }
        public void OpenStable() { if(CanOpenStable)SceneManager.LoadScene(StableController.SceneName); }
        private readonly Dictionary<ReinsPad,int> owners=new Dictionary<ReinsPad,int>();
        private readonly List<ReinsInput> recorded=new List<ReinsInput>();
        private readonly Dictionary<ReinsSurface,ReinsLabRecord> sessionBests=new Dictionary<ReinsSurface,ReinsLabRecord>();
        private readonly Queue<InputChange> inputChanges=new Queue<InputChange>();
        private readonly PracticeHaptics haptics=new PracticeHaptics();
        private readonly Quaternion[] barrelRest=new Quaternion[3];
        private ReinsRun run,ghostRun;
        private ReinsLabRecord savedBest;
        private Transform ghostHorse;
        private ReinsHorsePresentation horsePresentation,ghostPresentation;
        private RiderCameraRig riderCamera;
        private int ghostFrame;
        private float left,right,sampledLeft,sampledRight;
        private bool sound=true,ghostEnabled=true,recordedFinish,sampledLaunchHeld;
        private int? launchPointer;
        private ReinsSurface surface;
        private double accumulator,lastFrame,rhythmHeld=-1,lastHaptic=-1;
        private int lastFeedback=-1,lastBeat=-1;
        private double launchDspStart;
        private readonly double[] scheduledLaunchTimes=new double[3];
        internal IReadOnlyList<double> ScheduledLaunchTimes => scheduledLaunchTimes;
        private string storageNotice="";
        private string recordDirectory;
        private struct InputChange { public double at;public int kind;public ReinsPad pad;public float value; }
        private AudioSource beatVoice,gradeVoice;
        private readonly AudioSource[] launchVoices=new AudioSource[3];
        private AudioClip beatClip,gradeClip,launchBeepClip,launchGoClip;
        private Text leftPadText,rightPadText;
        private ReinsInputSurface[] pads;
        public ReinsRun Run => run;
        public bool HasBindings => horse && barrels!=null && barrels.Length==3 && rideCamera && safeArea && title && hint && status && feedback && action && results && surfaceText && soundText && cameraText && ghostText && cadence && leftFill && rightFill && resultGroup && map && retry && surfaceButton && soundButton && cameraButton && ghostButton && ghostMaterial;

        public void Configure(Transform horseRoot,Transform[] barrelRoots,Camera camera,RectTransform safe,Text[] labels,Image[] fills,
            CanvasGroup result,ReinsMapGraphic miniMap,Button[] buttons,Material ghostMat)
        {
            horse=horseRoot;barrels=barrelRoots;rideCamera=camera;safeArea=safe;
            title=labels[0];hint=labels[1];status=labels[2];feedback=labels[3];action=labels[4];results=labels[5];surfaceText=labels[6];soundText=labels[7];cameraText=labels[8];ghostText=labels[9];
            cadence=fills[0];leftFill=fills[1];rightFill=fills[2];resultGroup=result;map=miniMap;
            begin=buttons[0];retry=buttons[1];surfaceButton=buttons[2];soundButton=buttons[3];cameraButton=buttons[4];classicButton=buttons[5];ghostButton=buttons[6];ghostMaterial=ghostMat;
        }
        private void Awake()
        {
            if(!HasBindings) { enabled=false;return; }
            pads=FindObjectsByType<ReinsInputSurface>(FindObjectsSortMode.None);
            leftPadText=GameObject.Find("Left label")?.GetComponent<Text>();rightPadText=GameObject.Find("Right label")?.GetComponent<Text>();
            for(int i=0;i<3;i++)barrelRest[i]=barrels[i].rotation;
            sound=PlayerPrefs.GetInt(PracticeFeedback.SoundPreference,1)!=0;
            haptics.Initialize();haptics.SetEnabled(PlayerPrefs.GetInt(PracticeFeedback.HapticsPreference,1)!=0);
            beatVoice=gameObject.AddComponent<AudioSource>();beatVoice.playOnAwake=false;beatVoice.volume=.16f;
            gradeVoice=gameObject.AddComponent<AudioSource>();gradeVoice.playOnAwake=false;gradeVoice.volume=.18f;
            beatClip=Tone("Reins cadence",660,.055f);gradeClip=Tone("Reins result",880,.09f);
            launchBeepClip=Tone("Reins approach beep",660,.075f);launchGoClip=Tone("Reins launch GO",990,.11f);
            for(int i=0;i<launchVoices.Length;i++)
            {launchVoices[i]=gameObject.AddComponent<AudioSource>();launchVoices[i].playOnAwake=false;launchVoices[i].volume=.24f;launchVoices[i].clip=i==2?launchGoClip:launchBeepClip;}
            if(begin)begin.onClick.AddListener(Begin);retry.onClick.AddListener(ResetRun);surfaceButton.onClick.AddListener(ChangeSurface);
            soundButton.onClick.AddListener(ToggleSound);cameraButton.onClick.AddListener(ToggleCamera);ghostButton.onClick.AddListener(ToggleGhost);
            horsePresentation=horse.GetComponent<ReinsHorsePresentation>();
            if(!horsePresentation)horsePresentation=horse.gameObject.AddComponent<ReinsHorsePresentation>();
            horsePresentation.Initialize();
            riderCamera=rideCamera.GetComponent<RiderCameraRig>();
            if(!riderCamera)riderCamera=rideCamera.gameObject.AddComponent<RiderCameraRig>();
            riderCamera.Configure(horsePresentation,horse);
            horse.GetComponentInChildren<ReinsHairMotion>()?.BindComfort(riderCamera);
            horse.GetComponent<StableAppearance>()?.Apply(StableSession.Store.Current);
            // The seated rider occludes the saddle in first person. Keep its shadows; the ghost
            // receives its own visible render state below, and the showroom retains the full tack.
            var saddle=horse.GetComponentsInChildren<Transform>(true);
            foreach(var part in saddle)if(part.name=="Western saddle")
                foreach(var renderer in part.GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            if(stableButton)stableButton.onClick.AddListener(OpenStable);
            CreateGhost();
            ResetRun();
        }
        private void CreateGhost()
        {
            ghostHorse=CreatePresentationGhost(horse,ghostMaterial);
            ghostPresentation=ghostHorse.GetComponent<ReinsHorsePresentation>();ghostPresentation.Initialize();
            ghostHorse.GetComponentInChildren<ReinsHairMotion>(true)?.BindComfort(riderCamera);
        }
        internal static Transform CreatePresentationGhost(Transform sourceHorse,Material material)
        {
            // An inactive staging parent prevents imported play-on-awake effects/scripts from running while cloned.
            var staging=new GameObject("Inactive ghost staging");staging.SetActive(false);
            var ghostHorse=Instantiate(sourceHorse,staging.transform);ghostHorse.name="Own best — Reins recording";
            ghostHorse.gameObject.SetActive(false);
            // Disabling a script does not prevent Awake when the clone becomes active.
            // Remove unknown scripts immediately while the private clone is still inactive.
            try { RemoveGhostBehaviours(ghostHorse); }
            catch { DestroyImmediate(staging);throw; }
            foreach(var collider in ghostHorse.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            foreach(var body in ghostHorse.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
            foreach(var source in ghostHorse.GetComponentsInChildren<AudioSource>(true)){source.playOnAwake=false;source.Stop();source.enabled=false;}
            foreach(var camera in ghostHorse.GetComponentsInChildren<Camera>(true))camera.enabled=false;
            foreach(var listener in ghostHorse.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
            foreach(var light in ghostHorse.GetComponentsInChildren<Light>(true))light.enabled=false;
            foreach(var particles in ghostHorse.GetComponentsInChildren<ParticleSystem>(true))
            {var main=particles.main;main.playOnAwake=false;particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
            foreach(var animator in ghostHorse.GetComponentsInChildren<Animator>(true))animator.applyRootMotion=false;
            try { ApplyGhostMaterials(ghostHorse,material); }
            catch { DestroyImmediate(staging);throw; }
            ghostHorse.SetParent(sourceHorse.parent,true);Destroy(staging);
            return ghostHorse;
        }
        private static void ApplyGhostMaterials(Transform ghost,Material tint)
        {
            ReinsGhostMaterialResources resources=null;
            var hairCopies=new Dictionary<Material,Material>();
            try
            {
                foreach(var renderer in ghost.GetComponentsInChildren<Renderer>(true))
                {
                    if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)){renderer.enabled=false;continue;}
                    var source=renderer.sharedMaterials;
                    var materials=new Material[source.Length];
                    for(int i=0;i<materials.Length;i++)
                    {
                        var original=source[i];
                        bool strandHair=renderer.name=="Horse strand hair";
                        if(!strandHair){materials[i]=tint;continue;}
                        if(!original || !original.HasProperty("_BaseMap") || !original.GetTexture("_BaseMap") || !original.HasProperty("_Cutoff"))
                            throw new InvalidOperationException("Ghost strand hair requires its source alpha atlas and cutoff.");
                        if(!hairCopies.TryGetValue(original,out var copy))
                        {
                            // A regular URP tinted alpha clip multiplies texture alpha by tint alpha first.
                            // The existing ghost opacity (.28) is below the hair cutoff (.36), so it would
                            // erase every strand. This shader clips the source mask before applying opacity.
                            var shader=Resources.Load<Shader>("ReinsGhostHair");
                            if(!shader)throw new InvalidOperationException("Missing Reins ghost strand shader.");
                            if(!resources)
                            {
                                var owner=new GameObject("Own-best ghost material resources");
                                resources=owner.AddComponent<ReinsGhostMaterialResources>();
                                resources.Initialize(ghost);
                            }
                            copy=new Material(shader){name=original.name+" — private own-best ghost",hideFlags=HideFlags.DontSave};
                            resources.Own(copy);
                            copy.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));
                            copy.SetTextureScale("_BaseMap",original.GetTextureScale("_BaseMap"));
                            copy.SetTextureOffset("_BaseMap",original.GetTextureOffset("_BaseMap"));
                            copy.SetFloat("_AlphaClip",1);
                            copy.SetFloat("_Cutoff",original.GetFloat("_Cutoff"));
                            copy.SetFloat("_Cull",original.HasProperty("_Cull")?original.GetFloat("_Cull"):0);
                            copy.SetColor("_BaseColor",tint && tint.HasProperty("_BaseColor")?tint.GetColor("_BaseColor"):Color.white);
                            hairCopies.Add(original,copy);
                        }
                        materials[i]=copy;
                    }
                    // Assign this renderer's slots; never alter the player's shared assets or skeleton.
                    renderer.sharedMaterials=materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
            }
            catch
            {
                if(resources){resources.Release();DestroyImmediate(resources.gameObject);}
                throw;
            }
        }
        private static void RemoveGhostBehaviours(Transform ghost)
        {
            var pending=new List<MonoBehaviour>();
            foreach(var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour && !(behaviour is PracticeHorseVisual) && !(behaviour is ReinsHorsePresentation) && !(behaviour is ReinsRiderTackPresentation) && !(behaviour is ReinsHairMotion))pending.Add(behaviour);
            while(pending.Count>0)
            {
                bool removed=false;
                for(int i=pending.Count-1;i>=0;i--)
                {
                    var candidate=pending[i];
                    if(!candidate){pending.RemoveAt(i);removed=true;continue;}
                    bool required=false;
                    Type candidateType=candidate.GetType();
                    foreach(var dependent in candidate.GetComponents<MonoBehaviour>())
                    {
                        if(!dependent || dependent==candidate)continue;
                        foreach(RequireComponent requirement in dependent.GetType().GetCustomAttributes(typeof(RequireComponent),true))
                            if((requirement.m_Type0!=null && requirement.m_Type0.IsAssignableFrom(candidateType)) ||
                               (requirement.m_Type1!=null && requirement.m_Type1.IsAssignableFrom(candidateType)) ||
                               (requirement.m_Type2!=null && requirement.m_Type2.IsAssignableFrom(candidateType)))required=true;
                    }
                    // Remove dependents before their requirements so Unity does not retain an unsafe script.
                    if(required)continue;
                    DestroyImmediate(candidate);
                    if(candidate)throw new InvalidOperationException("A script could not be removed from the inactive presentation ghost.");
                    pending.RemoveAt(i);removed=true;
                }
                if(!removed)throw new InvalidOperationException("Imported ghost scripts contain dependencies that cannot be safely removed.");
            }
        }
        public void Begin()
        {
            if(run==null || run.Phase!=ReinsPhase.Ready)return;
            // The launch press arms tick 0 immediately; queued release then survives even
            // when down/up happen before the first 20 ms step. Existing side owners persist.
            sampledLaunchHeld=true;rhythmHeld=-1;accumulator=0;
            run.Start();ghostRun?.Start();lastFrame=Time.realtimeSinceStartupAsDouble;
            launchDspStart=AudioSettings.dspTime;ScheduleLaunchAudio();Present();
        }
        public void ResetRun()
        {
            ClearInput();run=new ReinsRun(new ReinsManifest(ChallengeSeed,surface));recorded.Clear();recordedFinish=false;
            accumulator=0;lastFrame=Time.realtimeSinceStartupAsDouble;lastFeedback=-1;lastBeat=-1;
            GetComponent<ReinsFootingVisual>()?.Apply(run.Manifest);
            for(int i=0;i<3;i++)barrels[i].rotation=barrelRest[i];
            StopAudio();launchDspStart=0;storageNotice="";
            savedBest=ReinsLabRecordStore.Load(surface,out storageNotice,recordDirectory);ghostFrame=0;
            if(sessionBests.TryGetValue(surface,out var session) && (savedBest==null || session.finalTimeMs<savedBest.finalTimeMs))savedBest=session;
            ghostRun=savedBest==null?null:new ReinsRun(new ReinsManifest(ChallengeSeed,surface));
            if(ghostHorse)ghostHorse.gameObject.SetActive(false);
            horsePresentation?.ResetFrame(PresentationFrame(run,default));
            if(ghostRun!=null)ghostPresentation?.ResetFrame(PresentationFrame(ghostRun,default));
            riderCamera?.ResetView();
            Present();
        }
        public bool Press(ReinsPad pad,int pointer)
        {
            if(run==null || Terminal(run.Phase) || owners.ContainsKey(pad) || owners.ContainsValue(pointer))return false;
            if(run.Phase==ReinsPhase.Ready && pad==ReinsPad.Rhythm)
            {
                Begin();owners.Add(pad,pointer);launchPointer=pointer;return true;
            }
            owners.Add(pad,pointer);
            // A pre-GO rhythm re-press is owned but silent until it is physically lifted.
            // It cannot rearm launch or become cadence/Wrap when the phase changes.
            if(run.Phase==ReinsPhase.Approach || run.Phase==ReinsPhase.Ready)return true;
            if(run.Phase==ReinsPhase.Drive) { if(pad!=ReinsPad.Rhythm)QueueInput(3,pad); }
            else if(pad==ReinsPad.Rhythm)QueueInput(4,pad);
            return true;
        }
        public void Pull(ReinsPad pad,int pointer,float tension)
        {
            if(!owners.TryGetValue(pad,out var held) || held!=pointer)return;
            if(pad==ReinsPad.Left)left=Mathf.Clamp01(tension);else if(pad==ReinsPad.Right)right=Mathf.Clamp01(tension);
            QueueInput(0,pad,Mathf.Clamp01(tension));
        }
        public void Release(ReinsPad pad,int pointer)
        {
            if(!owners.TryGetValue(pad,out var held) || held!=pointer)return;
            owners.Remove(pad);
            if(pad==ReinsPad.Left)left=0;else if(pad==ReinsPad.Right)right=0;
            if(pad==ReinsPad.Rhythm && launchPointer==pointer)
            {launchPointer=null;QueueInput(2,pad);}
            else QueueInput(1,pad);
        }
        private void ClearInput()
        {
            owners.Clear();launchPointer=null;sampledLaunchHeld=false;
            left=right=sampledLeft=sampledRight=0;inputChanges.Clear();rhythmHeld=-1;
            if(pads!=null)foreach(var pad in pads)if(pad)pad.Clear();
        }
        private void Update()
        {
            if(run==null)return;
            double now=Time.realtimeSinceStartupAsDouble,elapsed=Math.Max(0,now-lastFrame);lastFrame=now;
            if(!Terminal(run.Phase) && run.Phase!=ReinsPhase.Ready)
            {
                // A long suspension is an interrupted practice run, never a free clock extension.
                if(elapsed>.25) { CancelRun();return; }
                accumulator+=elapsed;
                while(accumulator>=.02 && !Terminal(run.Phase))
                {
                    Step(ConsumeInput(now-accumulator+.02));accumulator-=.02;
                }
            }
            Present();
        }
        internal ReinsInput ConsumeInput(double now)
        {
            bool cadenceTap=false;DriveSide drive=DriveSide.None;
            while(inputChanges.Count>0 && inputChanges.Peek().at<=now)
            {
                var change=inputChanges.Dequeue();
                if(change.kind==0){if(change.pad==ReinsPad.Left)sampledLeft=change.value;else if(change.pad==ReinsPad.Right)sampledRight=change.value;}
                else if(change.kind==1){if(change.pad==ReinsPad.Left)sampledLeft=0;else if(change.pad==ReinsPad.Right)sampledRight=0;else rhythmHeld=-1;}
                else if(change.kind==2){sampledLaunchHeld=false;rhythmHeld=-1;}
                else if(change.kind==3)drive=change.pad==ReinsPad.Left?DriveSide.Left:DriveSide.Right;
                else if(change.kind==4){cadenceTap=true;rhythmHeld=change.at;}
            }
            bool wrap=rhythmHeld>=0 && now-rhythmHeld>=.3 && run.DistanceToBarrel<=3.5;
            return new ReinsInput(Mathf.RoundToInt(sampledLeft*1000),Mathf.RoundToInt(sampledRight*1000),cadenceTap,sampledLaunchHeld,wrap,drive);
        }
        private void QueueInput(int kind,ReinsPad pad,float value=0)
        {
            if(inputChanges.Count>=256){CancelRun();return;}
            inputChanges.Enqueue(new InputChange{at=Time.realtimeSinceStartupAsDouble,kind=kind,pad=pad,value=value});
        }
        internal void SetRecordDirectory(string directory){recordDirectory=directory;sessionBests.Clear();ResetRun();}
        internal void RefreshPresentation(){lastFrame=Time.realtimeSinceStartupAsDouble;Present();riderCamera?.RenderImmediate();}
        internal void Step(ReinsInput input)
        {
            if(run==null || run.Phase==ReinsPhase.Ready || Terminal(run.Phase))return;
            run.Step(input);if(recorded.Count<7500)recorded.Add(input);
            horsePresentation?.PushFrame(PresentationFrame(run,input));
            if(ghostRun!=null && ghostFrame<savedBest.frames.Length)
            {
                var ghostInput=savedBest.frames[ghostFrame++].Input();ghostRun.Step(ghostInput);
                ghostPresentation?.PushFrame(PresentationFrame(ghostRun,ghostInput));
            }
            // Phase changes must not release either rein or reinterpret a held rhythm
            // finger. Only a real pointer-up/new down can hand the launch finger to rhythm.
            if(Terminal(run.Phase)){ClearInput();StopAudio();}
            if(run.Phase==ReinsPhase.Complete && !recordedFinish)
            {
                recordedFinish=true;
                if(savedBest==null || run.FinalTimeMs<savedBest.finalTimeMs)
                {var best=ReinsLabRecordStore.Save(surface,recorded,run.FinalTimeMs,out storageNotice,recordDirectory);if(best!=null)sessionBests[surface]=best;}
            }
        }
        private void Present()
        {
            if(run==null || !HasBindings)return;
            if(stableButton)stableButton.gameObject.SetActive(CanOpenStable);
            var screen=Screen.safeArea;
            if(Screen.width>0 && Screen.height>0){safeArea.anchorMin=new Vector2(screen.xMin/Screen.width,screen.yMin/Screen.height);safeArea.anchorMax=new Vector2(screen.xMax/Screen.width,screen.yMax/Screen.height);safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;}
            bool ready=run.Phase==ReinsPhase.Ready,terminal=Terminal(run.Phase);
            foreach(var pad in pads)if(pad.gameObject.activeSelf==terminal)pad.gameObject.SetActive(!terminal);
            feedback.gameObject.SetActive(!terminal);
            if(begin)begin.gameObject.SetActive(false);
            surfaceButton.gameObject.SetActive(ready);soundButton.gameObject.SetActive(ready);
            cameraButton.gameObject.SetActive(ready);ghostButton.gameObject.SetActive(ready);
            if(classicButton)classicButton.gameObject.SetActive(false);
            resultGroup.alpha=terminal?1:0;resultGroup.blocksRaycasts=terminal;resultGroup.interactable=terminal;
            title.text=ready?"REINS RACING · PRACTICE":run.Phase==ReinsPhase.Approach?"WALK TO THE LINE":terminal?(run.Phase==ReinsPhase.Complete?"RUN COMPLETE":"RUN STOPPED"):run.BarrelIndex>=3?"HOME STRETCH":"BARREL "+(run.BarrelIndex+1)+" / 3";
            status.text=$"{run.RaceTimeMs/1000.0:0.00}s  ·  +{run.KnockCount*5}s   |   {run.SpeedMetresPerSecond*3.6:0} km/h";
            hint.text=ready?"Hold the center to walk in. Release on the third beep.":run.Phase==ReinsPhase.Approach?(run.LaunchResolved?"Keep walking. The heartbeat starts after GO.":"Hold through the alley. Release on the third beep."):run.Phase==ReinsPhase.Drive?"Alternate LEFT and RIGHT taps. Aim for a steady finish.":terminal?"Retry the same conditions to improve your line.":run.BarrelIndex>=3?"Run home between the gate posts.":run.WrapActive?"LEG WRAP · Holding the center trades rhythm for a tighter turn.":$"{(run.BarrelIndex==0?"Circle LEFT":"Circle RIGHT")} · {run.DistanceToBarrel:0.0}m to barrel · {run.PocketZone}";
            action.text=ActionLabel();
            cadence.fillAmount=ready?0:run.Phase==ReinsPhase.Approach?(float)run.ElapsedMs/ReinsRun.ApproachMs:run.Phase==ReinsPhase.Drive?(float)run.DriveRemainingMs/4000:(float)run.CadencePhase01;
            cadence.color=run.BlazingHooves?new Color(1,.4f,.15f):run.HotHooves?new Color(1,.68f,.2f):new Color(.35f,.9f,.77f);
            leftFill.fillAmount=left;rightFill.fillAmount=right;
            if(leftPadText)leftPadText.text=run.Phase==ReinsPhase.Drive?"LEFT TAP\nAlternate with right":"LEFT REIN\nDrag down to pull";
            if(rightPadText)rightPadText.text=run.Phase==ReinsPhase.Drive?"RIGHT TAP\nAlternate with left":"RIGHT REIN\nDrag down to pull";
            feedback.text=run.LastFeedback+(run.CadenceStreak>0?"   ·   STREAK "+run.CadenceStreak:"");
            surfaceText.text="DIRT: "+SurfaceName(surface);soundText.text=sound?"SOUND ON":"SOUND OFF";cameraText.text=riderCamera && riderCamera.ReducedMotion?"STEADY VIEW":"RIDER VIEW";
            ghostText.text=savedBest==null?(sessionBests.ContainsKey(surface)?"BEST SAVED · RETRY":"OWN BEST: —"):ghostEnabled?"OWN BEST ON":"OWN BEST OFF";
            if(terminal)results.text=run.Phase==ReinsPhase.Complete?$"{run.FinalTimeMs/1000.0:0.00}s\n{run.RaceTimeMs/1000.0:0.00}s riding + {run.KnockCount*5}s penalties\n{run.StylePoints} style points · {run.LaunchOutcome} launch\n"+(savedBest==null?"First complete run.":$"Previous best: {savedBest.finalTimeMs/1000.0:0.00}s")+"\n"+storageNotice:"Run interrupted or time limit reached.\nNo result was saved.\nFollow the map and circle each barrel in order.\nUse both reins to slow down.";
            horse.SetPositionAndRotation(new Vector3((float)run.X,0,(float)run.Z),Quaternion.Euler(0,(float)(run.HeadingRadians*180/Math.PI),0));
            float interpolation=ready || terminal?1:Mathf.Clamp01((float)(accumulator/.02));
            horsePresentation?.ApplyInterpolation(interpolation);
            for(int i=0;i<3;i++)barrels[i].rotation=barrelRest[i]*(run.IsBarrelKnocked(i)?Quaternion.Euler(0,0,-72):Quaternion.identity);
            map.Show(run);
            if(ghostHorse)
            {
                bool show=ghostEnabled && ghostRun!=null && !ready && run.Phase!=ReinsPhase.Approach && !terminal && !Terminal(ghostRun.Phase);
                if(show)
                {
                    var position=new Vector3((float)ghostRun.X,0,(float)ghostRun.Z);show=(position-horse.position).sqrMagnitude>5;
                    ghostHorse.SetPositionAndRotation(position,Quaternion.Euler(0,(float)(ghostRun.HeadingRadians*180/Math.PI),0));
                    ghostPresentation?.ApplyInterpolation(interpolation);
                }
                ghostHorse.gameObject.SetActive(show);
            }
            ObserveAudio(terminal);
        }
        private string ActionLabel()
        {
            if(run.Phase==ReinsPhase.Ready)return "HOLD TO APPROACH";
            if(run.Phase==ReinsPhase.Approach)
            {
                // The same authoritative boundaries as the scheduled beeps remain playable
                // with sound off. Early releases still see both cues and the fixed GO.
                if(run.ElapsedMs<ReinsRun.FirstLaunchCueMs)return run.LaunchResolved?"WAIT FOR GO":"HOLD · WALK IN";
                bool second=run.ElapsedMs>=ReinsRun.FirstLaunchCueMs+ReinsRun.LaunchCueIntervalMs;
                return (second?"2 · ":"1 · ")+(run.LaunchResolved?"WAIT FOR GO":second?"GET READY":"HOLD");
            }
            if(run.Phase==ReinsPhase.Racing && run.RaceTimeMs<ReinsRun.LaunchDeadlineMs)
                return run.LaunchResolved?"GO · RIDE":"GO · RELEASE";
            return !run.LaunchResolved?"RELEASE TO LAUNCH":launchPointer.HasValue?"LIFT THEN TAP THE BEAT":run.Phase==ReinsPhase.Drive?"LEFT · RIGHT":run.WrapActive?"LEG WRAP":"TAP THE BEAT\nHOLD NEAR BARREL TO WRAP";
        }
        private void ObserveAudio(bool terminal)
        {
            if(terminal){StopAudio();return;}
            // Schedule ahead on DSP time; waiting for the next-beat index to roll over would play 140ms late.
            // Physical audio/input latency still needs device calibration.
            int beat=(int)run.NextCadenceBeatMs;
            if((run.Phase==ReinsPhase.Racing || run.Phase==ReinsPhase.Drive) && beat!=lastBeat)
            {
                double at=launchDspStart+(ReinsRun.ApproachMs+beat)/1000.0;
                if(sound && at>AudioSettings.dspTime){beatVoice.clip=beatClip;beatVoice.PlayScheduled(at);}
                lastBeat=beat;
            }
            if(run.FeedbackTick!=lastFeedback)
            {
                lastFeedback=run.FeedbackTick;
                if(run.RaceTimeMs>=600 && !string.IsNullOrEmpty(run.LastFeedback) && run.LastFeedback.IndexOf("miss",StringComparison.OrdinalIgnoreCase)<0)
                {if(sound){gradeVoice.clip=gradeClip;gradeVoice.Play();}double now=Time.realtimeSinceStartupAsDouble;if(now-lastHaptic>.2){haptics.Pulse(false);lastHaptic=now;}}
            }
        }
        private void ScheduleLaunchAudio()
        {
            if(!sound)return;
            for(int i=0;i<launchVoices.Length;i++)
            {
                double at=launchDspStart+(ReinsRun.FirstLaunchCueMs+i*ReinsRun.LaunchCueIntervalMs)/1000.0;
                if(launchVoices[i] && at>AudioSettings.dspTime)
                {scheduledLaunchTimes[i]=at;launchVoices[i].PlayScheduled(at);}
            }
        }
        private void StopAudio()
        {
            beatVoice?.Stop();gradeVoice?.Stop();
            for(int i=0;i<launchVoices.Length;i++){launchVoices[i]?.Stop();scheduledLaunchTimes[i]=0;}
        }
        private void ChangeSurface(){if(run.Phase!=ReinsPhase.Ready)return;surface=(ReinsSurface)(((int)surface+1)%5);ResetRun();}
        private void ToggleSound(){sound=!sound;PlayerPrefs.SetInt(PracticeFeedback.SoundPreference,sound?1:0);PlayerPrefs.Save();if(!sound)StopAudio();else if(run!=null && run.Phase!=ReinsPhase.Ready && !Terminal(run.Phase)){ScheduleLaunchAudio();lastBeat=-1;}Present();}
        private void ToggleCamera(){if(riderCamera)riderCamera.SetReducedMotion(!riderCamera.ReducedMotion);Present();}
        private void ToggleGhost(){ghostEnabled=!ghostEnabled;Present();}
        public void CancelRun(){if(run==null)return;if(run.Phase!=ReinsPhase.Ready && !Terminal(run.Phase))run.Cancel();ClearInput();accumulator=0;StopAudio();if(ghostHorse)ghostHorse.gameObject.SetActive(false);horsePresentation?.ResetFrame(PresentationFrame(run,default));riderCamera?.ResetView();Present();}
        private void OnApplicationPause(bool paused){if(paused)CancelRun();}
        private void OnApplicationFocus(bool focused){if(!focused)CancelRun();}
        private void OnDestroy(){StopAudio();haptics.Dispose();if(beatClip)Destroy(beatClip);if(gradeClip)Destroy(gradeClip);if(launchBeepClip)Destroy(launchBeepClip);if(launchGoClip)Destroy(launchGoClip);if(ghostHorse)Destroy(ghostHorse.gameObject);}
        public static bool Terminal(ReinsPhase phase)=>phase==ReinsPhase.Complete || phase==ReinsPhase.Cancelled || phase==ReinsPhase.TimedOut;
        private static HorsePresentationFrame PresentationFrame(ReinsRun state,ReinsInput input)
            =>new HorsePresentationFrame(state.Tick,new Vector3((float)state.X,0,(float)state.Z),
                Quaternion.Euler(0,(float)(state.HeadingRadians*180/Math.PI),0),(float)state.SpeedMetresPerSecond,
                (float)state.YawRateRadiansPerSecond,input.LeftPermille/1000f,input.RightPermille/1000f,state.WrapActive,state.Phase==ReinsPhase.Drive);
        private static string SurfaceName(ReinsSurface s)=>s==ReinsSurface.HardPack?"HARD PACK":s==ReinsSurface.LooseSand?"LOOSE SAND":s==ReinsSurface.TackyClay?"TACKY CLAY":s==ReinsSurface.MuddySlop?"MUDDY SLOP":"MIXED";
        private static AudioClip Tone(string name,float hz,float seconds)
        {int n=(int)(22050*seconds);var data=new float[n];for(int i=0;i<n;i++)data[i]=Mathf.Sin(i*2*Mathf.PI*hz/22050)*Mathf.Sin(Mathf.PI*i/n)*.4f;var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(data,0);return clip;}
    }

    [Serializable] public sealed class ReinsLabFrame
    {
        public int tick,leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
        public ReinsInput Input()
        {
            if(drive!="None" && drive!="Left" && drive!="Right")throw new InvalidDataException();
            return new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));
        }
        public ReinsLabFrame(int tick,ReinsInput input){this.tick=tick;leftPermille=input.LeftPermille;rightPermille=input.RightPermille;cadenceTap=input.CadenceTap;launchHeld=input.LaunchHeld;wrap=input.Wrap;drive=input.Drive.ToString();}
    }
    [Serializable] public sealed class ReinsLabRecord
    {public int contractVersion,rulesVersion;public string ruleset,fingerprint;public bool launchInitiallyHeld;public int seed,surface;public long finalTimeMs;public ReinsLabFrame[] frames;}
    internal static class ReinsLabRecordStore
    {
        private static string PathFor(ReinsSurface surface,string directory)=>Path.Combine(directory??Application.persistentDataPath,"reins-v2-"+(int)surface+".json");
        public static ReinsLabRecord Load(ReinsSurface surface,out string notice,string directory=null)
        {
            notice="";string path=PathFor(surface,directory);if(!File.Exists(path))return null;
            try
            {
                if(new FileInfo(path).Length>2*1024*1024)throw new InvalidDataException();
                string json=File.ReadAllText(path);var record=JsonUtility.FromJson<ReinsLabRecord>(json);Validate(record,surface);
                // Device recordings are our own fixed DTO encoding, not a general JSON import.
                // Round-trip shape comparison rejects missing/defaulted, duplicate and v1 keys
                // that JsonUtility otherwise ignores, while allowing insignificant whitespace.
                if(Compact(json)!=JsonUtility.ToJson(record))throw new InvalidDataException();
                return record;
            }
            catch(Exception e) when(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is NullReferenceException)
            {notice="Previous recording could not be loaded.";return null;}
        }
        public static ReinsLabRecord Save(ReinsSurface surface,List<ReinsInput> input,long time,out string notice,string directory=null)
        {
            notice="";ReinsLabRecord record=null;
            try
            {
                var candidate=new ReinsLabRecord{contractVersion=2,rulesVersion=2,ruleset="reins-v2",fingerprint=ReinsRuleFingerprint.Sha256,launchInitiallyHeld=true,seed=(int)ReinsLabController.ChallengeSeed,surface=(int)surface,finalTimeMs=time,frames=new ReinsLabFrame[input.Count]};for(int i=0;i<input.Count;i++)candidate.frames[i]=new ReinsLabFrame(i+1,input[i]);
                Validate(candidate,surface);record=candidate;string path=PathFor(surface,directory),temp=path+".tmp";
                if(File.Exists(path) && Load(surface,out _,directory)==null)
                {notice="Best is session-only; the incompatible recording was preserved.";return record;}
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(temp,JsonUtility.ToJson(record));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);notice="Personal best saved on this device.";
            }
            catch(Exception e) when(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException)
            {notice=record==null?"Recording could not be verified; it was not saved.":"Best is session-only; saving was unavailable.";}
            return record;
        }
        private static string Compact(string json)
        {
            var result=new System.Text.StringBuilder(json.Length);bool quoted=false,escaped=false;
            foreach(char c in json)
            {
                if(quoted){result.Append(c);if(escaped)escaped=false;else if(c=='\\')escaped=true;else if(c=='"')quoted=false;}
                else if(c=='"'){quoted=true;result.Append(c);}else if(!char.IsWhiteSpace(c))result.Append(c);
            }
            return result.ToString();
        }
        internal static void Validate(ReinsLabRecord record,ReinsSurface surface)
        {
            if(record==null || record.contractVersion!=2 || record.rulesVersion!=2 || !record.launchInitiallyHeld || record.ruleset!="reins-v2" || record.fingerprint!=ReinsRuleFingerprint.Sha256 || record.seed!=(int)ReinsLabController.ChallengeSeed || record.surface!=(int)surface || record.frames==null || record.frames.Length<1 || record.frames.Length>7500)throw new InvalidDataException();
            var replay=new ReinsRun(new ReinsManifest(ReinsLabController.ChallengeSeed,surface));replay.Start();
            foreach(var frame in record.frames){if(frame==null || frame.tick!=replay.Tick+1 || ReinsLabController.Terminal(replay.Phase))throw new InvalidDataException();replay.Step(frame.Input());}
            if(replay.Phase!=ReinsPhase.Complete || replay.FinalTimeMs!=record.finalTimeMs)throw new InvalidDataException();
        }
    }
}
