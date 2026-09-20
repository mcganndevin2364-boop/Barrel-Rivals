using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsMotionTests
    {
        [UnityTest] public IEnumerator RenderedGaitDrivesCameraAndHandsWithoutMovingTheRaceRoot()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;
            var source=horse.GetComponent<ReinsHorsePresentation>();var animator=horse.GetComponentInChildren<Animator>();
            var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();var camera=Camera.main;var rig=camera.GetComponent<RiderCameraRig>();
            Vector3 rootPosition=horse.position;Quaternion rootRotation=horse.rotation;bool reduced=rig.ReducedMotion;
            try {
                Assert.IsFalse(animator.applyRootMotion);
                Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate,animator.cullingMode);
                source.ResetFrame(Sample(0,rootPosition,rootRotation,8));animator.Update(0);
                rig.SetReducedMotion(false);
                var left=Find(horse,"Left rider grip");var rein=Find(horse,"Left braided rein").GetComponent<MeshFilter>();
                var grip=Find(horse,"Closed left grip");var bit=Find(horse,"Left bit anchor");
                Assert.IsNotNull(bit);
                animator.Play(0,0,.125f);animator.Update(0);tack.RenderImmediate();rig.RenderImmediate();
                Assert.That(source.GaitPhaseRadians,Is.EqualTo(Mathf.PI*.25f).Within(.002f));
                var frontLeg=Find(horse,"Bone_L.001");var neck=Find(horse,"Bone.001");
                Quaternion legAtFirstPhase=frontLeg.localRotation,neckAtFirstPhase=neck.localRotation;
                Vector3 cameraHigh=camera.transform.position,handHigh=left.localPosition,torsoHigh=source.TorsoMotion;
                AssertReinEndpoint(rein,0,grip.position);
                AssertReinEndpoint(rein,ReinsRiderTackPresentation.Segments*8,bit.position);
                animator.Play(0,0,.625f);animator.Update(0);tack.RenderImmediate();rig.RenderImmediate();
                Assert.That(source.GaitPhaseRadians,Is.EqualTo(Mathf.PI*1.25f).Within(.002f));
                Assert.That(Quaternion.Angle(legAtFirstPhase,frontLeg.localRotation),Is.GreaterThan(10),"An advancing phase is insufficient: the actual leg must animate.");
                Assert.That(Quaternion.Angle(neckAtFirstPhase,neck.localRotation),Is.GreaterThan(1),"The visible neck/tack skeleton must animate.");
                float torsoTravel=torsoHigh.y-source.TorsoMotion.y;
                Assert.That(Mathf.Abs(torsoTravel),Is.InRange(.010f,.035f),"Imported torso must compress and recover.");
                Assert.That(cameraHigh.y-camera.transform.position.y,Is.EqualTo(torsoTravel).Within(.001f));
                Assert.That(handHigh.y-left.localPosition.y-torsoTravel,Is.EqualTo(Mathf.Sqrt(2)*.008f).Within(.001f));
                AssertReinEndpoint(rein,0,grip.position);
                rig.SetReducedMotion(true);Vector3 reducedPosition=camera.transform.position;
                animator.Play(0,0,.125f);animator.Update(0);tack.RenderImmediate();rig.RenderImmediate();reducedPosition=camera.transform.position;
                animator.Play(0,0,.625f);animator.Update(0);tack.RenderImmediate();rig.RenderImmediate();
                Assert.That(Vector3.Distance(reducedPosition,camera.transform.position),Is.LessThan(.0001f));
                float reducedFov=camera.fieldOfView;
                foreach(float speed in new[]{1.5f,8f,14f,30f}) {
                    source.ResetFrame(Sample(0,rootPosition,rootRotation,speed));animator.Update(0);rig.RenderImmediate();
                    Assert.That(animator.speed,Is.InRange(1f,1.75f));
                    Assert.That(animator.speed,Is.EqualTo(speed<=8?1:1.75f).Within(.0001f));
                    Assert.That(camera.fieldOfView,Is.EqualTo(reducedFov).Within(.0001f));
                }
                // Fixed-step capture must advance the same smoothing as runtime;
                // repeated paused RenderImmediate calls would freeze both envelopes at idle.
                source.ResetFrame(Sample(0,rootPosition,rootRotation,0));animator.Update(0);rig.SetReducedMotion(false);rig.RenderImmediate();
                float idleHeight=camera.transform.position.y,idleFov=camera.fieldOfView;
                source.PushFrame(Sample(1,rootPosition,rootRotation,8));source.ApplyInterpolation(1);
                animator.Play(0,0,.125f);animator.Update(0);rig.RenderForCapture(.04f);
                Assert.That(camera.transform.position.y-idleHeight,Is.InRange(-.125f,-.100f));
                Assert.That(camera.fieldOfView,Is.GreaterThan(idleFov+.1f));
                Assert.That(Vector3.Distance(rootPosition,horse.position),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(rootRotation,horse.rotation),Is.LessThan(.0001f));
                Assert.AreEqual(ReinsPhase.Ready,controller.Run.Phase);
                Assert.AreEqual(0,controller.Run.Tick);LogAssert.NoUnexpectedReceived();
            } finally {rig.SetReducedMotion(reduced);}
        }

        // Opt in when a person will review movement, rather than generating hundreds
        // of PNGs on every regression run. Root tooling encodes these at exactly 25fps.
        [UnityTest] public IEnumerator CaptureBoundedActualMotionStudyWhenRequested()
        {
            string output=Environment.GetEnvironmentVariable("BARREL_MOTION_CAPTURE_DIRECTORY");
            if(string.IsNullOrEmpty(output)){Assert.Ignore("Set BARREL_MOTION_CAPTURE_DIRECTORY to an empty output directory for moving evidence.");yield break;}
            Directory.CreateDirectory(output);Assert.IsEmpty(Directory.GetFiles(output,"frame-*.png"),"Use a fresh directory; never mix motion runs.");
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType);
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;var source=horse.GetComponent<ReinsHorsePresentation>();
            var animator=horse.GetComponentInChildren<Animator>();var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();
            var camera=Camera.main;var rig=camera.GetComponent<RiderCameraRig>();bool reduced=rig.ReducedMotion;
            float timeScale=Time.timeScale;bool sourceEnabled=source.enabled,tackEnabled=tack.enabled,rigEnabled=rig.enabled;
            var side=new GameObject("Motion diagnostic side camera").AddComponent<Camera>();side.CopyFrom(camera);side.enabled=false;
            var frameInfo=new List<MotionFrame>();int frameIndex=0;
            Color32[] firstWalkSilhouette=null;int walkSilhouetteChangedPixels=0;
            var body=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("HorseBody",StringComparison.Ordinal));
            string replayDirectory=Path.Combine(Application.temporaryCachePath,"motion-"+Guid.NewGuid().ToString("N"));
            var target=new RenderTexture(640,360,24);var image=new Texture2D(1280,360,TextureFormat.RGB24,false);
            try {
                // Unity caches skinned render data within a player-loop frame. Give
                // every captured pose its own frame, but advance only our explicit
                // .02/.04 second steps, never wall-clock camera or Animator updates.
                Assert.AreEqual(AnimatorUpdateMode.Normal,animator.updateMode);
                Time.timeScale=0;source.enabled=false;tack.enabled=false;rig.enabled=false;
                // The current frame already computed a nonzero deltaTime before
                // the pause. Flush it before resetting or sampling any clip.
                yield return null;
                Assert.That(Time.deltaTime,Is.EqualTo(0),"Controlled sampling must start in a fully paused frame.");
                controller.SetRecordDirectory(replayDirectory);rig.SetReducedMotion(false);
                // The actual v2 alley is captured separately. Label these two short
                // rig samples honestly; the remaining sections use the canonical replay.
                foreach(float speed in new[]{0f,1.5f}) {
                    source.ResetFrame(Sample(0,horse.position,horse.rotation,speed));animator.Update(0);
                    for(int i=0;i<30;i++) {
                        source.PushFrame(Sample((i+1)*2,horse.position,horse.rotation,speed));source.ApplyInterpolation(1);
                        animator.Update(.04f);horse.GetComponentInChildren<ReinsHairMotion>().RenderImmediate();tack.RenderImmediate();rig.RenderForCapture(.04f);
                        float phase=source.GaitPhaseRadians;
                        yield return null;
                        Assert.That(Mathf.Abs(Mathf.DeltaAngle(phase*Mathf.Rad2Deg,source.GaitPhaseRadians*Mathf.Rad2Deg)),Is.LessThan(.001f),"Yielding must not advance the controlled animation clock.");
                        Capture(output,frameIndex++,speed==0?"Idle rig study":"Walk rig study (isolated pose)",false,source,controller,camera,side,target,image,frameInfo,false);
                        if(speed>0 && (i==5 || i==18)) {
                            var silhouette=CaptureBodySilhouette(output,frameIndex-1,body,side,target);
                            if(firstWalkSilhouette==null)firstWalkSilhouette=silhouette;
                            else {
                                // The fixed camera, root and white unlit body exclude
                                // lighting, ground, reins and hair from the evidence.
                                walkSilhouetteChangedPixels=ChangedLegSilhouettePixels(firstWalkSilhouette,silhouette);
                                Assert.That(walkSilhouetteChangedPixels,Is.GreaterThan(100),"Actual rendered lower-leg silhouettes must differ at the two captured walk phases.");
                            }
                        }
                    }
                }
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v2.json")));
                for(int pass=0;pass<2;pass++) {
                    controller.ResetRun();rig.SetReducedMotion(pass==1);controller.Begin();int turnFrames=0;
                    foreach(var frame in fixture.frames) {
                        controller.Step(frame.Input());controller.RefreshPresentation();source.ApplyInterpolation(1);
                        animator.Update(.02f);horse.GetComponentInChildren<ReinsHairMotion>().RenderImmediate();tack.RenderImmediate();rig.RenderForCapture(.02f);
                        var run=controller.Run;if(run.Tick%2!=0)continue;
                        string section=null;
                        if(pass==0 && run.Phase==ReinsPhase.Racing && run.RaceTimeMs<1600)section="Canonical race acceleration";
                        else if(pass==0 && run.Phase==ReinsPhase.Racing && run.BarrelIndex==0 && run.TurnActive && turnFrames<40){section="Canonical first-barrel turn";turnFrames++;}
                        else if(run.Phase==ReinsPhase.Drive && run.PhaseElapsedMs<1600)section=pass==0?"Canonical Drive":"Canonical Drive / reduced motion";
                        if(section!=null) {
                            float phase=source.GaitPhaseRadians;
                            yield return null;
                            Assert.That(Mathf.Abs(Mathf.DeltaAngle(phase*Mathf.Rad2Deg,source.GaitPhaseRadians*Mathf.Rad2Deg)),Is.LessThan(.001f),"Yielding must not advance the controlled animation clock.");
                            Capture(output,frameIndex++,section,true,source,controller,camera,side,target,image,frameInfo,pass==1);
                        }
                    }
                    Assert.AreEqual(ReinsPhase.Complete,controller.Run.Phase);Assert.AreEqual(V2FixtureExpected.Result.finalTimeMs,controller.Run.FinalTimeMs);
                    Assert.AreEqual(0,controller.Run.KnockCount);Assert.AreEqual(300,controller.Run.StylePoints);
                }
                Assert.Greater(frameIndex,150);Assert.LessOrEqual(frameIndex,220);
                var drive=frameInfo.Where(f=>f.section=="Canonical Drive").ToArray();Assert.Greater(drive.Length,20);
                float legMovement=drive.Max(f=>Quaternion.Angle(drive[0].frontLegRotation,f.frontLegRotation));
                float neckMovement=drive.Max(f=>Quaternion.Angle(drive[0].neckRotation,f.neckRotation));
                Assert.That(legMovement,Is.GreaterThan(10),"Captured Drive must show real leg motion, not only an advancing phase.");
                Assert.That(neckMovement,Is.GreaterThan(1),"Captured Drive must show real neck motion.");
                var evidence=new MotionEvidence {
                    unityVersion=Application.unityVersion,graphicsDevice=SystemInfo.graphicsDeviceName,frames=frameInfo.ToArray(),
                    description="Actual Unity motion study, one player-loop frame per manually stepped sample with scaled time paused. Left: rider camera; right: diagnostic side camera with rider-only saddle visibility lifted for inspection. HUD omitted. Idle/walk are explicit rig samples; other segments replay accepted canonical v2 input. Two fixed-camera body-only silhouettes verify rendered lower-leg changes. Not phone performance or foot-planting acceptance.",
                    rulesFingerprint=ReinsRuleFingerprint.Sha256,framesPerSecond=25,width=1280,height=360,frameCount=frameIndex,
                    canonicalFinalTimeMs=V2FixtureExpected.Result.finalTimeMs,devicePerformanceMeasured=false,footPlantingAccepted=false,
                    capturedDriveLegRangeDegrees=legMovement,capturedDriveNeckRangeDegrees=neckMovement,
                    walkSilhouetteChangedPixels=walkSilhouetteChangedPixels
                };
                File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(evidence,true)+"\n");LogAssert.NoUnexpectedReceived();
            } finally {
                Time.timeScale=timeScale;source.enabled=sourceEnabled;tack.enabled=tackEnabled;rig.enabled=rigEnabled;
                rig.SetReducedMotion(reduced);Object.Destroy(side.gameObject);Object.Destroy(target);Object.Destroy(image);
                if(Directory.Exists(replayDirectory))Directory.Delete(replayDirectory,true);
            }
        }
        private static HorsePresentationFrame Sample(int tick,Vector3 position,Quaternion rotation,float speed)
            =>new HorsePresentationFrame(tick,position,rotation,speed,0,0,0,false,false);
        private static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        private static void AssertReinEndpoint(MeshFilter rein,int vertexOffset,Vector3 point)
        {
            var vertices=rein.sharedMesh.vertices;Vector3 center=Vector3.zero;
            for(int i=0;i<8;i++)center+=vertices[vertexOffset+i];center=rein.transform.TransformPoint(center/8);
            Assert.That(Vector3.Distance(center,point),Is.LessThan(.001f),"Rendered rein must meet its moving grip/bit.");
        }
        private static void Capture(string output,int index,string section,bool replay,ReinsHorsePresentation source,ReinsLabController controller,
            Camera camera,Camera side,RenderTexture target,Texture2D image,List<MotionFrame> frames,bool reduced)
        {
            var active=RenderTexture.active;var prior=camera.targetTexture;
            var saddle=source.GetComponentsInChildren<Renderer>(true).Where(r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly).ToArray();
            try {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),0,0);
                side.transform.position=source.RenderPosition+source.RenderRotation*new Vector3(4.2f,2.5f,.3f);
                side.transform.LookAt(source.RenderPosition+Vector3.up*1.2f);side.fieldOfView=50;side.targetTexture=target;
                foreach(var renderer in saddle)renderer.shadowCastingMode=ShadowCastingMode.On;
                side.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,360),640,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,"frame-"+index.ToString("D4")+".png"),image.EncodeToPNG());
                frames.Add(new MotionFrame {index=index,section=section,canonicalReplay=replay,tick=controller.Run.Tick,
                    speed=source.Speed,gaitPhase=source.GaitPhaseRadians,reducedMotion=reduced,cameraPosition=camera.transform.position,
                    frontLegRotation=Find(source.transform,"Bone_L.001").localRotation,neckRotation=Find(source.transform,"Bone.001").localRotation,
                    cameraFieldOfView=camera.fieldOfView});
            } finally {
                foreach(var renderer in saddle)renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
                camera.targetTexture=prior;side.targetTexture=null;RenderTexture.active=active;
            }
        }
        private static Color32[] CaptureBodySilhouette(string output,int index,SkinnedMeshRenderer body,Camera side,RenderTexture target)
        {
            const int silhouetteLayer=30;
            Assert.IsFalse(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>r!=body && r.gameObject.layer==silhouetteLayer),"The isolated silhouette layer must be empty.");
            var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.IsNotNull(shader);
            var material=new Material(shader);material.SetColor("_BaseColor",Color.white);
            var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
            int layer=body.gameObject.layer,mask=side.cullingMask;var materials=body.sharedMaterials;
            var clear=side.clearFlags;Color background=side.backgroundColor;var active=RenderTexture.active;var prior=side.targetTexture;
            try {
                body.gameObject.layer=silhouetteLayer;body.sharedMaterials=Enumerable.Repeat(material,materials.Length).ToArray();
                side.cullingMask=1<<silhouetteLayer;side.clearFlags=CameraClearFlags.SolidColor;side.backgroundColor=Color.black;
                side.targetTexture=target;side.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,640,360),0,0);texture.Apply();
                var pixels=texture.GetPixels32();Assert.Greater(pixels.Count(p=>p.r>127),500,"The actual skinned body must be visible in the silhouette render.");
                File.WriteAllBytes(Path.Combine(output,"silhouette-"+index.ToString("D4")+".png"),texture.EncodeToPNG());return pixels;
            } finally {
                body.gameObject.layer=layer;body.sharedMaterials=materials;side.cullingMask=mask;side.clearFlags=clear;side.backgroundColor=background;
                side.targetTexture=prior;RenderTexture.active=active;Object.Destroy(material);Object.Destroy(texture);
            }
        }
        private static int ChangedLegSilhouettePixels(Color32[] a,Color32[] b)
        {
            int changed=0,visible=0;
            // Bottom-origin pixels below the belly in this fixed 640x360 side view.
            for(int y=55;y<165;y++)for(int x=210;x<460;x++) {
                int pixel=y*640+x;bool before=a[pixel].r>127,after=b[pixel].r>127;
                if(before)visible++;if(before!=after)changed++;
            }
            Assert.Greater(visible,100,"The evidence crop must include the horse's lower legs.");return changed;
        }
        [Serializable] private sealed class MotionEvidence {
            public string unityVersion,graphicsDevice,description,rulesFingerprint;public int framesPerSecond,width,height,frameCount;public long canonicalFinalTimeMs;
            public float capturedDriveLegRangeDegrees,capturedDriveNeckRangeDegrees;public int walkSilhouetteChangedPixels;
            public bool devicePerformanceMeasured,footPlantingAccepted;public MotionFrame[] frames;
        }
        [Serializable] private sealed class MotionFrame {
            public int index,tick;public string section;public bool canonicalReplay,reducedMotion;public float speed,gaitPhase,cameraFieldOfView;public Vector3 cameraPosition;
            public Quaternion frontLegRotation,neckRotation;
        }
        [Serializable] private sealed class Fixture {public Frame[] frames;}
        [Serializable] private sealed class Frame {
            public int leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));
        }
    }
}
