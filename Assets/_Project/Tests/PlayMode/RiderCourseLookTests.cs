using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class RiderCourseLookTests
    {
        [UnityTest] public IEnumerator ActualTurnsKeepTheBarrelInViewWithoutChangingTheRide() => Verify(false);
        [UnityTest] public IEnumerator ReducedMotionKeepsTheSameBarrelInformationWithoutBobOrZoom() => Verify(true);
        private IEnumerator Verify(bool reduced)
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Development/HeroHorse/HeroHorseRaceReview.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("Development view is excluded from mobile builds.");yield break;
#endif
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();
            var presentation=GameObject.Find("Horse proxy").GetComponent<ReinsHorsePresentation>();
            var driver=horse.GetComponent<HeroHorseLocomotion>();var ground=horse.GetComponent<HeroHorseGrounding>();
            var tack=horse.GetComponent<HeroHorseAttachments>();var camera=Camera.main;var rig=camera.GetComponent<RiderCameraRig>();
            var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();
            var skin=horse.GetComponentsInChildren<SkinnedMeshRenderer>();var flags=skin.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
            var behaviours=new Behaviour[]{presentation,driver,ground,tack,rig};var enabled=behaviours.Select(b=>b.enabled).ToArray();
            var bounds=Enumerable.Range(1,3).Select(i=>{
                var renderers=GameObject.Find("Barrel "+i).transform.Find("Detailed painted drum").GetComponentsInChildren<Renderer>();
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;
            }).ToArray();
            string output=Environment.GetEnvironmentVariable("BARREL_COURSE_LOOK_OUTPUT");
            if(!string.IsNullOrEmpty(output)){output=Path.Combine(output,reduced?"comfort":"normal");Directory.CreateDirectory(output);}
            string records=Path.Combine(Application.temporaryCachePath,"course-look-"+Guid.NewGuid().ToString("N"));
            var report=new Review();var captured=new HashSet<string>();
            bool comfort=rig.ReducedMotion;float timeScale=Time.timeScale;
            try
            {
                Time.timeScale=0;foreach(var b in behaviours)b.enabled=false;foreach(var s in skin)s.forceMatrixRecalculationPerRender=true;
                controller.SetRecordDirectory(records);rig.SetReducedMotion(reduced);camera.aspect=16f/9;controller.ResetRun();
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v2.json")));
                Rect Projection(Bounds b,out bool inFront)
                {
                    Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;inFront=true;
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    {
                        var v=camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                        min=Vector2.Min(min,v);max=Vector2.Max(max,v);inFront&=v.z>camera.nearClipPlane;
                    }
                    return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
                }
                bool Visible(Rect b,bool front)=>front && b.xMin>.01f && b.xMax<.99f && b.yMin>.26f && b.yMax<.84f;
                void Capture(string name,int width=1280,int height=720)
                {
                    if(string.IsNullOrEmpty(output) || !captured.Add(name))return;
                    var image=OverlayEvidenceCapture.Render(camera,canvas,width,height,()=>rig.RenderImmediate());
                    try{File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());}finally{Object.DestroyImmediate(image);}
                }
                controller.Begin();float previousLook=0,previousPitch=0;bool comfortChecked=false;
                foreach(var frame in fixture.frames)
                {
                    controller.Step(frame.Input());controller.RefreshPresentation();presentation.ApplyInterpolation(1);
                    driver.Advance(.02f);horse.Animator.Update(.02f);ground.RenderImmediate();tack.RenderImmediate();rig.RenderForCapture(.02f);
                    var run=controller.Run;
                    report.maximumLookSpeed=Mathf.Max(report.maximumLookSpeed,Mathf.Abs(rig.CourseLookYaw-previousLook)/.02f);
                    previousLook=rig.CourseLookYaw;report.maximumLook=Mathf.Max(report.maximumLook,Mathf.Abs(rig.CourseLookYaw));
                    report.maximumPitch=Mathf.Max(report.maximumPitch,rig.CourseLookPitch);
                    report.maximumPitchSpeed=Mathf.Max(report.maximumPitchSpeed,Mathf.Abs(rig.CourseLookPitch-previousPitch)/.02f);previousPitch=rig.CourseLookPitch;
                    if(!comfortChecked && Mathf.Abs(rig.CourseLookYaw)>45)
                    {
                        float before=rig.CourseLookYaw;rig.SetReducedMotion(!reduced);rig.RenderImmediate();
                        Assert.That(rig.CourseLookYaw,Is.EqualTo(before).Within(.0001f),"Comfort toggle must not snap the rider's glance.");
                        rig.SetReducedMotion(reduced);rig.RenderImmediate();comfortChecked=true;
                    }
                    if(run.Phase==ReinsPhase.Racing && run.TurnActive && run.BarrelIndex<3)
                    {
                        int index=run.BarrelIndex;bool middle=run.TurnProgress01>=.10 && run.TurnProgress01<=.85;
                        for(int aspect=0;aspect<2;aspect++)
                        {
                            camera.aspect=aspect==0?16f/9:4f/3;rig.RenderImmediate();
                            int visibleHands=new[]{tack.leftGrip,tack.rightGrip}.Count(g=>{
                                var p=camera.WorldToViewportPoint(g.position);return p.x>.01f && p.x<.99f && p.y>.005f && p.y<.99f && p.z>camera.nearClipPlane;
                            });
                            Assert.That(visibleHands,Is.GreaterThanOrEqualTo(1),"A riding hand must remain visible through the glance.");
                            var projected=Projection(bounds[index],out bool front);bool visible=Visible(projected,front);
                            var rotation=camera.transform.rotation;camera.transform.rotation=presentation.RenderRotation*Quaternion.Euler(25,0,0);
                            var old=Projection(bounds[index],out bool oldFront);bool oldVisible=Visible(old,oldFront);camera.transform.rotation=rotation;
                            var count=report.views[aspect];count.activeSamples++;if(visible)count.framedSamples++;if(oldVisible)count.oldFramedSamples++;
                            if(middle)
                            {
                                count.middleSamples++;if(visible)count.middleFramed++;
                                if(!visible)report.misses.Add(new Miss{tick=run.Tick,barrel=index+1,aspect=aspect,progress=(float)run.TurnProgress01,rect=projected,look=rig.CourseLookYaw});
                            }
                        }
                        camera.aspect=16f/9;rig.RenderImmediate();
                        if(run.TurnProgress01>=.15)Capture("Entry-"+(index+1));
                        if(run.TurnProgress01>=.45)Capture("Middle-"+(index+1));
                        if(run.TurnProgress01>=.75){Capture("Exit-"+(index+1));Capture("Tablet-"+(index+1),1024,768);}
                    }
                    if(run.Phase==ReinsPhase.Approach)Assert.That(rig.CourseLookYaw,Is.EqualTo(0));
                }
                report.finalTimeMs=controller.Run.FinalTimeMs;
                controller.ResetRun();rig.RenderImmediate();Assert.That(rig.CourseLookYaw,Is.EqualTo(0));
                Assert.IsTrue(comfortChecked);Assert.That(report.maximumLookSpeed,Is.LessThanOrEqualTo(110.01));
                Assert.That(report.maximumLook,Is.LessThanOrEqualTo(52.01));
                Assert.That(report.maximumPitch,Is.LessThanOrEqualTo(18.01));Assert.That(report.maximumPitchSpeed,Is.LessThanOrEqualTo(40.01));Assert.That(report.finalTimeMs,Is.EqualTo(V2FixtureExpected.Result.finalTimeMs));
                if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(report,true)+"\n");
                foreach(var v in report.views)
                {
                    Assert.That(v.activeSamples,Is.GreaterThan(600));
                    Assert.That(v.framedSamples,Is.EqualTo(v.activeSamples),"The complete barrel must remain clear throughout each active turn.");
                    Assert.That(v.middleSamples,Is.GreaterThan(300));
                    Assert.That(v.middleFramed,Is.EqualTo(v.middleSamples),"The complete barrel must clear the HUD during the middle of all three turns.");
                    Assert.That(v.framedSamples,Is.GreaterThan(v.oldFramedSamples));
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                controller.CancelRun();rig.SetReducedMotion(comfort);camera.ResetAspect();Time.timeScale=timeScale;
                for(int i=0;i<behaviours.Length;i++)if(behaviours[i])behaviours[i].enabled=enabled[i];
                for(int i=0;i<skin.Length;i++)if(skin[i])skin[i].forceMatrixRecalculationPerRender=flags[i];
                if(Directory.Exists(records))Directory.Delete(records,true);
            }
        }
        [Serializable] sealed class Review
        {
            public string scope="Actual candidate course, projected real drum bounds and real HUD captures. Rectangle checks do not prove pixel occlusion or phone comfort.";
            public float maximumLook,maximumLookSpeed,maximumPitch,maximumPitchSpeed;public long finalTimeMs;
            public View[] views={new View{aspect="16:9"},new View{aspect="4:3"}};public List<Miss> misses=new List<Miss>();
        }
        [Serializable] sealed class View{public string aspect;public int activeSamples,framedSamples,oldFramedSamples,middleSamples,middleFramed;}
        [Serializable] sealed class Miss{public int tick,barrel,aspect;public float progress,look;public Rect rect;}
        [Serializable] sealed class Fixture{public InputFrame[] frames;}
        [Serializable] sealed class InputFrame
        {
            public int leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));
        }
    }
}
