using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsAlleyCaptureTests
    {
        [UnityTest] public IEnumerator CaptureActualV2AlleyReleaseAndFirstSteeringWhenRequested()
        {
            string output=Environment.GetEnvironmentVariable("BARREL_ALLEY_CAPTURE_DIRECTORY");
            if(string.IsNullOrEmpty(output)){Assert.Ignore("Set BARREL_ALLEY_CAPTURE_DIRECTORY for controlled moving launch evidence.");yield break;}
            Directory.CreateDirectory(output);Assert.IsEmpty(Directory.GetFiles(output,"frame-*.png"),"Use a fresh output folder.");
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;var source=horse.GetComponent<ReinsHorsePresentation>();
            var animator=horse.GetComponentInChildren<Animator>();var hair=horse.GetComponentInChildren<ReinsHairMotion>();
            var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();var camera=Camera.main;
            var rig=camera.GetComponent<RiderCameraRig>();var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();
            bool sourceEnabled=source.enabled,hairEnabled=hair.enabled,tackEnabled=tack.enabled,rigEnabled=rig.enabled,reduced=rig.ReducedMotion;
            float oldTime=Time.timeScale;var frames=new List<Frame>();
            string records=Path.Combine(Application.temporaryCachePath,"alley-capture-"+Guid.NewGuid().ToString("N"));
            try
            {
                Time.timeScale=0;source.enabled=hair.enabled=tack.enabled=rig.enabled=false;yield return null;
                controller.SetRecordDirectory(records);rig.SetReducedMotion(false);controller.ResetRun();
                void Pose(float dt)
                {
                    controller.RefreshPresentation();source.ApplyInterpolation(1);animator.Update(dt);
                    hair.RenderImmediate();tack.RenderImmediate();rig.RenderForCapture(dt);
                }
                void Capture()
                {
                    var image=OverlayEvidenceCapture.Render(camera,canvas,1280,720);
                    try{File.WriteAllBytes(Path.Combine(output,"frame-"+frames.Count.ToString("D4")+".png"),image.EncodeToPNG());}
                    finally{Object.DestroyImmediate(image);}
                    frames.Add(new Frame{tick=controller.Run.Tick,phase=controller.Run.Phase.ToString(),raceTimeMs=controller.Run.RaceTimeMs,
                        x=controller.Run.X,z=controller.Run.Z,speed=controller.Run.SpeedMetresPerSecond,
                        launchOutcome=controller.Run.LaunchOutcome.ToString(),cameraPosition=camera.transform.position});
                }
                Pose(0);yield return null;Capture();
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v2.json")));
                Assert.GreaterOrEqual(fixture.frames.Length,320);controller.Begin();
                for(int i=0;i<320;i++)
                {
                    controller.Step(fixture.frames[i].Input());Pose(.02f);
                    if(i%2==0)continue;
                    yield return null;Capture();
                    if(controller.Run.Tick==200)
                    {
                        Assert.AreEqual(ReinsPhase.Racing,controller.Run.Phase);
                        Assert.AreEqual(0,controller.Run.RaceTimeMs);Assert.That(controller.Run.Z,Is.EqualTo(0).Within(.000001));
                        Assert.AreEqual(ReinsLaunchOutcome.Perfect,controller.Run.LaunchOutcome);
                    }
                }
                Assert.AreEqual(161,frames.Count);
                Assert.That(frames[50].z,Is.EqualTo(-3).Within(.000001));
                Assert.Greater(controller.Run.SpeedMetresPerSecond,1.5);
                Assert.Less(controller.Run.X,0,"Canonical first steering must actually head toward the left barrel.");
                File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(new CaptureInfo{frameCount=frames.Count,frames=frames.ToArray(),rulesFingerprint=ReinsRuleFingerprint.Sha256},true)+"\n");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                controller.CancelRun();Time.timeScale=oldTime;source.enabled=sourceEnabled;hair.enabled=hairEnabled;tack.enabled=tackEnabled;rig.enabled=rigEnabled;rig.SetReducedMotion(reduced);
                if(Directory.Exists(records))Directory.Delete(records,true);
            }
        }
        [Serializable] private sealed class CaptureInfo
        {
            public string description="Actual saved Reins v2 scene and rider camera: Ready, four-second moving alley, perfect release, first steering and cadence. Controlled 20ms simulation/animation with one player-loop yield per rendered sample, ungraded actual HUD. No audio track; scheduled-beep tests are separate and speaker/input alignment still needs phone playtest. Encoding rate is not measured device FPS.";
            public int framesPerSecond=25,width=1280,height=720,frameCount;public string rulesFingerprint;public Frame[] frames;
        }
        [Serializable] private sealed class Frame{public int tick;public long raceTimeMs;public string phase,launchOutcome;public double x,z,speed;public Vector3 cameraPosition;}
        [Serializable] private sealed class Fixture{public InputFrame[] frames;}
        [Serializable] private sealed class InputFrame
        {
            public int leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));
        }
    }
}
