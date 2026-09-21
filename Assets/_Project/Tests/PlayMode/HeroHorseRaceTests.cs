using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Reins;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace BarrelRivals.Tests
{
    public sealed class HeroHorseRaceTests
    {
        const string Scene="Assets/_Project/Development/HeroHorse/HeroHorseRaceReview.unity";
        static IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("Development integration scene is deliberately outside mobile builds.");yield break;
#endif
        }

        [UnityTest]
        public IEnumerator FullCourseUsesAcceptedSpeedReinsAndCameraWithoutChangingTheResult()
        {
            yield return Load();
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var actor=GameObject.Find("Horse proxy");var presentation=actor.GetComponent<ReinsHorsePresentation>();
            var horse=actor.GetComponentInChildren<HorseRigBindings>();Assert.IsNotNull(horse);
            var driver=horse.GetComponent<HeroHorseLocomotion>();var tack=horse.GetComponent<HeroHorseAttachments>();
            var ground=horse.GetComponent<HeroHorseGrounding>();var camera=Camera.main;var rig=camera.GetComponent<RiderCameraRig>();
            Assert.AreSame(presentation,driver.source);Assert.AreSame(presentation,tack.source);
            Assert.IsEmpty(horse.GetComponentsInChildren<Collider>(true));
            var output=Environment.GetEnvironmentVariable("BARREL_HORSE_RACE_OUTPUT");
            bool movie=Environment.GetEnvironmentVariable("BARREL_HORSE_RACE_MOVIE")=="1";
            if(!string.IsNullOrEmpty(output))Directory.CreateDirectory(output);
            var skins=horse.GetComponentsInChildren<SkinnedMeshRenderer>();
            // The preserved FBX calls its Walk take "Scene"; compare the actual
            // imported subasset, not a display label or a newly renamed clip.
            AnimationClip walk=null;
#if UNITY_EDITOR
            walk=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Development/HeroHorse/HeroHorse.fbx")
                .OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
#endif
            var flags=skins.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
            var report=new Review();var captures=new HashSet<string>();var gaits=new HashSet<string>();
            var frames=new List<Frame>();var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();
            var behaviours=new Behaviour[]{presentation,driver,ground,tack,rig};var enabled=behaviours.Select(b=>b.enabled).ToArray();
            float timeScale=Time.timeScale;bool comfort=rig.ReducedMotion;
            string records=Path.Combine(Application.temporaryCachePath,"hero-race-"+Guid.NewGuid().ToString("N"));
            try
            {
                Time.timeScale=0;foreach(var b in behaviours)b.enabled=false;
                foreach(var skin in skins)skin.forceMatrixRecalculationPerRender=true;
                controller.SetRecordDirectory(records);rig.SetReducedMotion(false);controller.ResetRun();
                void Pose(float dt)
                {
                    controller.RefreshPresentation();presentation.ApplyInterpolation(1);driver.Advance(dt);
                    horse.Animator.Update(dt);ground.RenderImmediate();tack.RenderImmediate();rig.RenderForCapture(dt);
                    Assert.That(horse.Animator.applyRootMotion,Is.False);
                    report.maximumActorError=Mathf.Max(report.maximumActorError,Vector3.Distance(actor.transform.position,
                        new Vector3((float)controller.Run.X,0,(float)controller.Run.Z)));
                    report.maximumReachError=Mathf.Max(report.maximumReachError,tack.rider.MaximumReachError);
                    report.minimumHoofHeight=Mathf.Min(report.minimumHoofHeight,ground.MinimumAfter);
                    int visibleGrips=0;
                    foreach(var grip in new[]{tack.leftGrip,tack.rightGrip})
                    {
                        var p=camera.WorldToViewportPoint(grip.position);
                        report.minimumGripX=Mathf.Min(report.minimumGripX,p.x);report.maximumGripX=Mathf.Max(report.maximumGripX,p.x);
                        report.minimumGripY=Mathf.Min(report.minimumGripY,p.y);report.minimumGripDepth=Mathf.Min(report.minimumGripDepth,p.z);
                        if(p.x>.01f && p.x<.99f && p.y>.005f && p.y<.99f && p.z>camera.nearClipPlane)visibleGrips++;
                        if(Mathf.Abs(rig.CourseLookYaw)<1)
                            Assert.IsTrue(p.x>.01f && p.x<.99f && p.y>.005f && p.z>camera.nearClipPlane,"Forward view must retain both grips.");
                    }
                    report.minimumVisibleGrips=Mathf.Min(report.minimumVisibleGrips,visibleGrips);
                    foreach(var clip in horse.Animator.GetCurrentAnimatorClipInfo(0))if(clip.weight>.15f)gaits.Add(clip.clip==walk?"Walk":clip.clip.name);
                    Assert.That(tack.leftPull,Is.EqualTo(presentation.LeftRein));Assert.That(tack.rightPull,Is.EqualTo(presentation.RightRein));
                    Assert.That(driver.targetSpeed,Is.EqualTo(presentation.Speed));
                }
                void Capture(string name)
                {
                    if(string.IsNullOrEmpty(output))return;
                    var image=OverlayEvidenceCapture.Render(camera,canvas,1280,720,()=>rig.RenderImmediate());
                    try{File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());}
                    finally{Object.DestroyImmediate(image);}
                }
                void Stage(string name){if(captures.Add(name))Capture(name);}
                void MovieFrame()
                {
                    if(!movie)return;Capture("frame-"+frames.Count.ToString("D4"));
                    frames.Add(new Frame{tick=controller.Run.Tick,phase=controller.Run.Phase.ToString(),camera=camera.transform.position,
                        speed=(float)controller.Run.SpeedMetresPerSecond,x=controller.Run.X,z=controller.Run.Z});
                }
                Pose(0);yield return null;Stage("Ready");MovieFrame();
                var neutralEye=camera.transform.position;
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v2.json")));
                controller.Begin();
                for(int i=0;i<fixture.frames.Length;i++)
                {
                    controller.Step(fixture.frames[i].Input());Pose(.02f);
                    var run=controller.Run;
                    if(run.Phase==ReinsPhase.Approach && run.Tick==100)Stage("Alley");
                    if(run.Phase==ReinsPhase.Racing && run.BarrelIndex<3)
                    {
                        if(run.DistanceToBarrel<=12)Stage("Approach-"+(run.BarrelIndex+1));
                        if(run.TurnActive && run.DistanceToBarrel<=3.5 && run.TurnProgress01>=.20)Stage("Turn-"+(run.BarrelIndex+1));
                    }
                    if(run.Phase==ReinsPhase.Drive && run.PhaseElapsedMs>=500)Stage("Drive");
                    if(movie && i%2==1){yield return null;MovieFrame();}
                }
                Stage("Finish");
                if(movie && (frames.Count==0 || frames[frames.Count-1].tick!=controller.Run.Tick)){yield return null;MovieFrame();}
                Assert.That(controller.Run.Phase,Is.EqualTo(ReinsPhase.Complete));
                Assert.That(controller.Run.FinalTimeMs,Is.EqualTo(V2FixtureExpected.Result.finalTimeMs));
                Assert.That(controller.Run.KnockCount,Is.EqualTo(0));Assert.That(controller.Run.StylePoints,Is.EqualTo(300));
                Assert.That(report.maximumActorError,Is.LessThan(1e-6));
                Assert.That(report.maximumReachError,Is.LessThan(.01));Assert.That(report.minimumHoofHeight,Is.GreaterThan(-.002));
                // A natural side glance may carry the outer hand off screen, while
                // the inner riding hand and unchanged rein HUD remain available.
                Assert.That(report.minimumVisibleGrips,Is.GreaterThanOrEqualTo(1));
                Assert.That(gaits,Does.Contain("Walk"));Assert.That(gaits,Does.Contain("Sprint"));
                Assert.That(captures,Does.Contain("Turn-1"));Assert.That(captures,Does.Contain("Turn-2"));Assert.That(captures,Does.Contain("Turn-3"));
                report.finalTimeMs=controller.Run.FinalTimeMs;report.gaits=gaits.OrderBy(s=>s).ToArray();report.frames=frames.ToArray();
                report.stages=captures.ToArray();report.fingerprint=ReinsRuleFingerprint.Sha256;
                controller.ResetRun();Pose(0);Assert.That(driver.SmoothedSpeed,Is.EqualTo(0).Within(.00001));
                Assert.That(Vector3.Distance(camera.transform.position,neutralEye),Is.LessThan(.002),"Retry must return to its actual ready view.");
                rig.SetReducedMotion(true);controller.Begin();
                for(int i=0;i<240;i++)
                {
                    controller.Step(fixture.frames[i].Input());Pose(.02f);
                    Assert.That(camera.transform.position.y,Is.EqualTo(neutralEye.y).Within(.002),"Comfort mode must remove stride camera motion.");
                }
                if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"full-course.json"),JsonUtility.ToJson(report,true)+"\n");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                controller.CancelRun();Time.timeScale=timeScale;rig.SetReducedMotion(comfort);
                for(int i=0;i<behaviours.Length;i++)if(behaviours[i])behaviours[i].enabled=enabled[i];
                for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].forceMatrixRecalculationPerRender=flags[i];
                if(Directory.Exists(records))Directory.Delete(records,true);
            }
        }

        [UnityTest]
        public IEnumerator LiveCandidateGhostUsesItsOwnRigReinsAndHairResources()
        {
            yield return Load();
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var actor=GameObject.Find("Horse proxy").transform;var source=actor.GetComponentInChildren<HorseRigBindings>();
            var original=source.GetComponent<HeroHorseAttachments>();original.RenderImmediate();
            var tint=(Material)typeof(ReinsLabController).GetField("ghostMaterial",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(controller);
            Transform ghost=null;Mesh ownedRein=null;Material[] ownedHair=null;
            try
            {
                ghost=ReinsLabController.CreatePresentationGhost(actor,tint);
                var horse=ghost.GetComponentInChildren<HorseRigBindings>(true);Assert.IsNotNull(horse);
                var p=ghost.GetComponent<ReinsHorsePresentation>();var driver=horse.GetComponent<HeroHorseLocomotion>();
                var tack=horse.GetComponent<HeroHorseAttachments>();Assert.IsNotNull(driver);Assert.IsNotNull(tack);
                Assert.AreSame(p,driver.source);Assert.AreSame(p,tack.source);Assert.AreNotSame(source,horse);
                Assert.IsNull(ghost.GetComponent<StableAppearance>());
                ownedHair=horse.Groom.sharedMaterials;
                Assert.That(ownedHair.All(m=>m.shader.name=="Barrel Rivals/Reins Ghost Hair"),Is.True);
                Assert.AreNotSame(source.Groom.sharedMaterial,ownedHair[0]);
                var actorPosition=actor.position;ghost.position=new Vector3(6,0,4);ghost.gameObject.SetActive(true);
                var hoof=horse.GetComponentsInChildren<Transform>().Single(t=>t.name=="ForeHoof.L");var start=hoof.position;
                float travel=0;var previous=start;
                for(int i=0;i<50;i++)
                {
                    p.PushFrame(new HorsePresentationFrame(i+1,ghost.position,Quaternion.identity,12,.3f,.7f,.1f,false,false));p.ApplyInterpolation(1);
                    yield return null;
                    travel+=Vector3.Distance(previous,hoof.position);previous=hoof.position;
                    Assert.That(Vector3.Distance(actor.position,actorPosition),Is.LessThan(.00001));
                    Assert.That(tack.rider.MaximumReachError,Is.LessThan(.01));
                }
                Assert.That(travel,Is.GreaterThan(.15));Assert.That(driver.SmoothedSpeed,Is.GreaterThan(5));
                ownedRein=tack.leftRein.sharedMesh;Assert.AreNotSame(original.leftRein.sharedMesh,ownedRein);
                Assert.That(tack.leftPull,Is.EqualTo(.7f));Assert.That(tack.rightPull,Is.EqualTo(.1f));
                Object.Destroy(ghost.gameObject);yield return null;yield return null;
                Assert.IsTrue(ownedRein==null);Assert.IsTrue(ownedHair.All(m=>m==null));
                Assert.IsTrue(original.leftRein.sharedMesh);Assert.IsTrue(source.Groom.sharedMaterial);
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(ghost)Object.Destroy(ghost.gameObject);}
        }

        [UnityTest]
        public IEnumerator SavedStableChoicesBindAllTwentyStylesToTheNewRigWithoutWritingTheProfile()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"hero-stable-"+Guid.NewGuid().ToString("N"));
            StableSession.UseForTests(new StableProfileStore(directory));
            try
            {
                yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
                var stable=Object.FindFirstObjectByType<StableController>();
                foreach(var item in StableCatalog.Gear)
                {
                    stable.ShowSlot(item.Slot);stable.Preview(item.Id);stable.EquipSelected();
                    Assert.That(stable.Equipped.Equipped(item.Slot),Is.EqualTo(item.Id));
                }
                string saved=File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName));
                yield return Load();
                var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
                var actor=GameObject.Find("Horse proxy");var appearance=actor.GetComponent<StableAppearance>();
                foreach(StableSlot slot in Enum.GetValues(typeof(StableSlot)))
                    Assert.That(appearance.Applied.Equipped(slot),Is.EqualTo(StableSession.Store.Current.Equipped(slot)));
                var names=new Dictionary<StableSlot,string[]>{
                    {StableSlot.Saddle,new[]{"Western saddle"}},{StableSlot.Pad,new[]{"Woven saddle pad"}},
                    {StableSlot.Reins,new[]{"Fitted left rein","Fitted right rein"}},
                    {StableSlot.Headstall,new[]{"Fitted leather headstall"}},{StableSlot.Gloves,new[]{"Glove shell","Glove shell"}}};
                var palettes=(StableAppearance.Palette[])typeof(StableAppearance).GetField("palettes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(appearance);
                foreach(var item in StableCatalog.Gear)
                {
                    var profile=StableProfile.Starter();Assert.IsTrue(profile.TryEquip(item.Slot,item.Id));appearance.Apply(profile);
                    var targets=actor.GetComponentsInChildren<MeshRenderer>(true).Where(r=>names[item.Slot].Contains(r.name)).ToArray();
                    Assert.That(targets.Length,Is.EqualTo(names[item.Slot].Length));
                    foreach(var target in targets)Assert.AreSame(palettes.Single(p=>p.gearId==item.Id).material,
                        target.sharedMaterials[item.Slot==StableSlot.Reins?1:0],item.Id+" on "+target.name);
                }
                Assert.That(File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName)),Is.EqualTo(saved));
                Assert.That(controller.Run.Tick,Is.EqualTo(0));
                LogAssert.NoUnexpectedReceived();
            }
            finally{StableSession.UseForTests(null);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        [Serializable] sealed class Review
        {
            public string scope="Actual full-course development scene. No phone FPS, world stance-locking, photographic quality, or player adoption acceptance.";
            public string fingerprint;public long finalTimeMs;public string[] stages,gaits;public Frame[] frames;
            public float maximumActorError,maximumReachError,minimumHoofHeight=float.PositiveInfinity;
            public int minimumVisibleGrips=2;
            public float minimumGripX=1,maximumGripX,minimumGripY=1,minimumGripDepth=float.PositiveInfinity;
        }
        [Serializable] sealed class Frame{public int tick;public string phase;public Vector3 camera;public float speed;public double x,z;}
        [Serializable] sealed class Fixture{public InputFrame[] frames;}
        [Serializable] sealed class InputFrame
        {
            public int leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));
        }
    }
}
