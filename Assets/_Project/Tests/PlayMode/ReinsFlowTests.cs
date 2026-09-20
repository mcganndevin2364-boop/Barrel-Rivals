using System.Collections;
using System.IO;
using System;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace BarrelRivals.Tests
{
    public sealed class ReinsFlowTests
    {
        [UnityTest]
        public IEnumerator MutedLaunchShowsBothCuesAndGoEvenAfterEarlyRelease()
        {
            int soundPreference=PlayerPrefs.GetInt(PracticeFeedback.SoundPreference,1);
            PlayerPrefs.SetInt(PracticeFeedback.SoundPreference,0);
            try
            {
                yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName,LoadSceneMode.Single);yield return null;
                var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
                var label=GameObject.Find("Action").GetComponent<Text>();
                int first=ReinsRun.FirstLaunchCueMs/ReinsRun.StepMs;
                int second=(ReinsRun.FirstLaunchCueMs+ReinsRun.LaunchCueIntervalMs)/ReinsRun.StepMs;
                int go=ReinsRun.ApproachMs/ReinsRun.StepMs,deadline=go+ReinsRun.LaunchDeadlineMs/ReinsRun.StepMs;
                foreach(bool earlyRelease in new[]{false,true})
                {
                    controller.ResetRun();Assert.IsTrue(controller.Press(ReinsPad.Rhythm,71));
                    if(earlyRelease)controller.Release(ReinsPad.Rhythm,71);
                    while(controller.Run.Tick<deadline)
                    {
                        controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
                        int tick=controller.Run.Tick;
                        if(tick==first-1 || tick==first || tick==second-1 || tick==second || tick==go-1 || tick==go || tick==deadline-1 || tick==deadline)
                        {
                            controller.RefreshPresentation();
                            string expected=tick<first?(earlyRelease?"WAIT FOR GO":"HOLD · WALK IN"):
                                tick<second?(earlyRelease?"1 · WAIT FOR GO":"1 · HOLD"):
                                tick<go?(earlyRelease?"2 · WAIT FOR GO":"2 · GET READY"):
                                tick<deadline?(earlyRelease?"GO · RIDE":"GO · RELEASE"):
                                earlyRelease?"TAP THE BEAT\nHOLD NEAR BARREL TO WRAP":"LIFT THEN TAP THE BEAT";
                            Assert.AreEqual(expected,label.text,"Visual launch cue at tick "+tick);
                        }
                    }
                    foreach(double at in controller.ScheduledLaunchTimes)Assert.AreEqual(0,at,"Muted launch must not schedule sound.");
                    Assert.AreEqual(earlyRelease?ReinsLaunchOutcome.Weak:ReinsLaunchOutcome.TimedOut,controller.Run.LaunchOutcome);
                    Assert.AreEqual(0,controller.Run.CadenceAwards);controller.CancelRun();
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{PlayerPrefs.SetInt(PracticeFeedback.SoundPreference,soundPreference);}
        }
        [UnityTest]
        public IEnumerator CenterHoldLaunchPreservesReinsAndHandsOffToFreshCadence()
        {
            int soundPreference=PlayerPrefs.GetInt(PracticeFeedback.SoundPreference,1);
            PlayerPrefs.SetInt(PracticeFeedback.SoundPreference,1);
            try
            {
                yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName,LoadSceneMode.Single);yield return null;
                var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
                Assert.IsTrue(controller.HasBindings);Assert.IsNull(GameObject.Find("Begin"));Assert.IsNull(GameObject.Find("Classic practice"));
                controller.RefreshPresentation();Capture("AlleyV2-Flow-Ready.png");
                var leftPad=GameObject.Find("Left rein");var rightPad=GameObject.Find("Right rein");
                var leftPointer=Drag(leftPad,11,.9f);var rightPointer=Drag(rightPad,22,.6f);
                var rhythm=GameObject.Find("Rhythm and wrap");
                var pointer=new PointerEventData(EventSystem.current){pointerId=71,position=RectTransformUtility.WorldToScreenPoint(null,rhythm.transform.position)};
                ExecuteEvents.Execute(rhythm,pointer,ExecuteEvents.pointerDownHandler);
                Assert.AreEqual(ReinsPhase.Approach,controller.Run.Phase);
                var times=controller.ScheduledLaunchTimes;Assert.Greater(times[0],AudioSettings.dspTime);
                Assert.That(times[1]-times[0],Is.EqualTo(1).Within(.000001));Assert.That(times[2]-times[0],Is.EqualTo(2).Within(.000001));
                while(controller.Run.Tick<199)
                {
                    var input=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);
                    Assert.IsTrue(input.LaunchHeld);Assert.IsFalse(input.CadenceTap);Assert.IsFalse(input.Wrap);controller.Step(input);
                }
                ExecuteEvents.Execute(rhythm,pointer,ExecuteEvents.pointerUpHandler);
                var launch=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);controller.Step(launch);
                Assert.AreEqual(ReinsPhase.Racing,controller.Run.Phase);Assert.AreEqual(ReinsLaunchOutcome.Perfect,controller.Run.LaunchOutcome);
                Assert.AreEqual(0,controller.Run.LaunchReleaseErrorMs);Assert.AreEqual(0,controller.Run.RaceTimeMs);
                Assert.AreEqual(0,controller.Run.X);Assert.AreEqual(0,controller.Run.Z);
                Assert.IsFalse(launch.CadenceTap);Assert.IsFalse(launch.Wrap);
                Assert.IsFalse(controller.Press(ReinsPad.Rhythm,22),"A held side finger cannot own rhythm.");
                controller.Release(ReinsPad.Left,22);
                var two=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);
                Assert.AreEqual(900,two.LeftPermille);Assert.AreEqual(600,two.RightPermille,"Both side owners survive GO.");
                double heading=controller.Run.HeadingRadians;for(int i=0;i<40;i++)controller.Step(two);
                Assert.Less(controller.Run.HeadingRadians,heading);
                ExecuteEvents.Execute(leftPad,leftPointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(rightPad,rightPointer,ExecuteEvents.pointerUpHandler);
                double beforeTap=Time.realtimeSinceStartupAsDouble-.001;
                Assert.IsTrue(controller.Press(ReinsPad.Rhythm,22));
                Assert.IsFalse(controller.ConsumeInput(beforeTap).CadenceTap,"A fresh event cannot be backdated into catch-up.");
                var tap=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);Assert.IsTrue(tap.CadenceTap);
                Assert.IsFalse(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).CadenceTap);
                controller.Release(ReinsPad.Rhythm,22);controller.Step(tap);controller.RefreshPresentation();Capture("AlleyV2-Flow-Riding.png");
                controller.SendMessage("OnApplicationPause",true);
                Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.AreEqual(0,controller.Run.FinalTimeMs);
                foreach(double at in controller.ScheduledLaunchTimes)Assert.AreEqual(0,at);
                controller.ResetRun();Assert.AreEqual(ReinsPhase.Ready,controller.Run.Phase);
                var neutral=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);
                Assert.AreEqual(0,neutral.LeftPermille);Assert.AreEqual(0,neutral.RightPermille);Assert.IsFalse(neutral.CadenceTap);
                LogAssert.NoUnexpectedReceived();
            }
            finally{PlayerPrefs.SetInt(PracticeFeedback.SoundPreference,soundPreference);}
        }
        [UnityTest]
        public IEnumerator SubStepReleaseAndHeldTimeoutRequireFreshRhythmPress()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName,LoadSceneMode.Single);yield return null;
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            Assert.IsTrue(controller.Press(ReinsPad.Rhythm,41));controller.Release(ReinsPad.Rhythm,41);
            controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
            Assert.AreEqual(ReinsLaunchOutcome.Weak,controller.Run.LaunchOutcome);Assert.AreEqual(-3980,controller.Run.LaunchReleaseErrorMs);
            Assert.IsTrue(controller.Press(ReinsPad.Rhythm,41),"A pre-GO re-press is owned but silent.");
            while(controller.Run.Tick<200)controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
            var held=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble+.4);
            Assert.IsFalse(held.CadenceTap);Assert.IsFalse(held.Wrap);Assert.IsFalse(controller.Press(ReinsPad.Left,41));
            controller.Release(ReinsPad.Rhythm,41);controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);
            Assert.IsTrue(controller.Press(ReinsPad.Rhythm,41));Assert.IsTrue(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).CadenceTap);
            controller.CancelRun();controller.ResetRun();Assert.IsTrue(controller.Press(ReinsPad.Rhythm,51));
            while(controller.Run.Tick<220)controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
            Assert.AreEqual(ReinsLaunchOutcome.TimedOut,controller.Run.LaunchOutcome);Assert.IsNull(controller.Run.LaunchReleaseErrorMs);
            Assert.IsFalse(controller.Press(ReinsPad.Right,51),"The timed-out finger remains owned until lifted.");
            held=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble+.4);Assert.IsFalse(held.CadenceTap);Assert.IsFalse(held.Wrap);
            controller.Release(ReinsPad.Rhythm,51);controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
            Assert.AreEqual(ReinsLaunchOutcome.TimedOut,controller.Run.LaunchOutcome);
            Assert.IsTrue(controller.Press(ReinsPad.Rhythm,51));Assert.IsTrue(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).CadenceTap);
            controller.CancelRun();controller.ResetRun();controller.Press(ReinsPad.Rhythm,61);controller.Release(ReinsPad.Rhythm,61);
            controller.CancelRun();controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
            Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.AreEqual(ReinsLaunchOutcome.Pending,controller.Run.LaunchOutcome);
            Assert.IsNull(controller.Run.LaunchReleaseErrorMs,"Cancellation beats a queued but unconsumed release.");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator CancelledTouchAndDisabledPadDoNotGradeLaunch()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName,LoadSceneMode.Single);yield return null;
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var rhythm=GameObject.Find("Rhythm and wrap");var surface=rhythm.GetComponent<ReinsInputSurface>();
            var screen=InputSystem.AddDevice<Touchscreen>();
            try
            {
                InputSystem.QueueStateEvent(screen,new TouchState{touchId=93,phase=UnityEngine.InputSystem.TouchPhase.Began,position=new Vector2(100,100)});
                InputSystem.Update();
                var pointer=new ExtendedPointerEventData(EventSystem.current){pointerId=193,touchId=93,device=screen,position=RectTransformUtility.WorldToScreenPoint(null,rhythm.transform.position)};
                surface.OnPointerDown(pointer);Assert.AreEqual(ReinsPhase.Approach,controller.Run.Phase);
                InputSystem.QueueStateEvent(screen,new TouchState{touchId=93,phase=UnityEngine.InputSystem.TouchPhase.Canceled,position=new Vector2(100,100)});
                InputSystem.Update();surface.OnPointerUp(pointer);
                Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.AreEqual(ReinsLaunchOutcome.Pending,controller.Run.LaunchOutcome);Assert.IsNull(controller.Run.LaunchReleaseErrorMs);
                foreach(double at in controller.ScheduledLaunchTimes)Assert.AreEqual(0,at);
                controller.ResetRun();var manual=new PointerEventData(EventSystem.current){pointerId=71,position=pointer.position};
                surface.OnPointerDown(manual);surface.enabled=false;
                Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.IsNull(controller.Run.LaunchReleaseErrorMs);
                surface.enabled=true;controller.ResetRun();surface.OnPointerDown(manual);
                surface.OnCancel(new BaseEventData(EventSystem.current));
                Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.IsNull(controller.Run.LaunchReleaseErrorMs);
                controller.ResetRun();Assert.IsTrue(controller.Press(ReinsPad.Rhythm,71),"Retry releases stale ownership.");controller.CancelRun();
                LogAssert.NoUnexpectedReceived();
            }
            finally{InputSystem.RemoveDevice(screen);}
        }
        private static PointerEventData Drag(GameObject target,int id,float amount)
        {
            var rect=target.GetComponent<RectTransform>();var local=new Vector2(rect.rect.center.x,rect.rect.yMax-25);
            var pointer=new PointerEventData(EventSystem.current){pointerId=id,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local))};
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);
            local.y-=Mathf.Max(80,rect.rect.height*.50f)*amount;pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.dragHandler);return pointer;
        }

        [UnityTest]
        public IEnumerator FootingPreviewUsesTheSelectedManifestAndSurvivesReset()
        {
            yield return SceneManager.LoadSceneAsync("Arena_ReinsLab",LoadSceneMode.Single);yield return null;
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();
            for(int i=0;i<4;i++)GameObject.Find("Surface").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(ReinsSurface.Mixed,controller.Run.Manifest.Surface);
            var block=new MaterialPropertyBlock();int patches=0;
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(renderer.name.StartsWith("Footing patch"))
            {renderer.GetPropertyBlock(block);Assert.Greater(block.GetColor("_BaseColor").a,.9f);patches++;}
            Assert.AreEqual(6,patches);controller.ResetRun();Assert.AreEqual(ReinsSurface.Mixed,controller.Run.Manifest.Surface);
            controller.RefreshPresentation();Capture("AlleyV2-Flow-Mixed-Footing.png");
            LogAssert.NoUnexpectedReceived();
        }

        [Serializable] private sealed class Fixture { public int contractVersion;public string ruleFingerprint;public bool launchInitiallyHeld;public Manifest manifest;public Frame[] frames; }
        [Serializable] private sealed class Manifest {public int rulesVersion;public string courseId;}
        [Serializable] private sealed class Expected {public Result result;}
        [Serializable] private sealed class Result {public long finalTimeMs;public int stylePoints,knockCount;public string launchOutcome;public int launchReleaseErrorMs;}
        [Serializable] private sealed class Frame
        {public int tick,leftPermille,rightPermille;public bool cadenceTap,launchHeld,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,launchHeld,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));}
        [UnityTest]
        public IEnumerator CanonicalServerRunCompletesInUnityPersistsAndReplays()
        {
            yield return SceneManager.LoadSceneAsync("Arena_ReinsLab",LoadSceneMode.Single);yield return null;
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            string directory=Path.Combine(Application.temporaryCachePath,"reins-proof-"+Guid.NewGuid().ToString("N"));
            controller.SetRecordDirectory(directory);
            try
            {
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v2.json")));
                Assert.AreEqual(2,fixture.contractVersion);Assert.AreEqual(2,fixture.manifest.rulesVersion);Assert.AreEqual("reins-v2",fixture.manifest.courseId);
                Assert.IsTrue(fixture.launchInitiallyHeld);Assert.AreEqual(ReinsRuleFingerprint.Sha256,fixture.ruleFingerprint);
                var expected=JsonUtility.FromJson<Expected>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-response.v2.json"))).result;
                Directory.CreateDirectory(directory);string oldPath=Path.Combine(directory,"reins-lab-v1-0.json");File.WriteAllText(oldPath,"preserve old inputs");
                controller.ResetRun();Assert.IsNull(ReinsLabRecordStore.Load(ReinsSurface.HardPack,out _,directory));
                controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());
                Assert.AreEqual(ReinsPhase.Complete,controller.Run.Phase);Assert.AreEqual(expected.finalTimeMs,controller.Run.FinalTimeMs);Assert.AreEqual(expected.stylePoints,controller.Run.StylePoints);Assert.AreEqual(expected.knockCount,controller.Run.KnockCount);
                Assert.AreEqual(expected.launchOutcome,controller.Run.LaunchOutcome.ToString());Assert.AreEqual(expected.launchReleaseErrorMs,controller.Run.LaunchReleaseErrorMs);
                Assert.AreEqual("preserve old inputs",File.ReadAllText(oldPath));Assert.IsTrue(File.Exists(Path.Combine(directory,"reins-v2-0.json")));
                controller.RefreshPresentation();Capture("AlleyV2-Flow-Result.png");
                var record=ReinsLabRecordStore.Load(ReinsSurface.HardPack,out var notice,directory);Assert.NotNull(record,notice);Assert.AreEqual(ReinsRuleFingerprint.Sha256,record.fingerprint);Assert.IsTrue(record.launchInitiallyHeld);Assert.AreEqual(2,record.contractVersion);
                record.fingerprint="incompatible";Assert.Throws<InvalidDataException>(()=>ReinsLabRecordStore.Validate(record,ReinsSurface.HardPack));
                controller.ResetRun();controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());
                Assert.AreEqual(ReinsPhase.Complete,controller.Run.Phase);Assert.AreEqual(expected.finalTimeMs,controller.Run.FinalTimeMs);
                var ghost=GameObject.Find("Own best — Reins recording");controller.RefreshPresentation();
                Assert.IsFalse(ghost && ghost.activeInHierarchy,"Finished playback is hidden.");
                // An unwritable store path must still preserve a verified session best across Retry.
                controller.SetRecordDirectory(Path.Combine(directory,"blocked"));File.WriteAllText(Path.Combine(directory,"blocked"),"not a directory");
                controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());controller.ResetRun();controller.RefreshPresentation();
                Assert.AreEqual("OWN BEST ON",GameObject.Find("Own ghost").GetComponentInChildren<Text>().text);
                string recordPath=Path.Combine(directory,"reins-v2-0.json");string validJson=File.ReadAllText(recordPath);
                string mixedJson=validJson.Insert(1,"\"gateTap\":true,");File.WriteAllText(recordPath,mixedJson);
                Assert.IsNull(ReinsLabRecordStore.Load(ReinsSurface.HardPack,out _,directory),"A mixed v1 field must not be silently ignored.");
                var inputs=new System.Collections.Generic.List<ReinsInput>();foreach(var frame in fixture.frames)inputs.Add(frame.Input());
                Assert.NotNull(ReinsLabRecordStore.Save(ReinsSurface.HardPack,inputs,expected.finalTimeMs,out notice,directory));
                Assert.That(notice,Does.Contain("session-only"));Assert.AreEqual(mixedJson,File.ReadAllText(recordPath),"Incompatible bytes must be retained.");
                File.WriteAllText(recordPath,validJson.Replace("\"launchInitiallyHeld\":true,",""));
                Assert.IsNull(ReinsLabRecordStore.Load(ReinsSurface.HardPack,out _,directory),"Missing initial arming is not a default launch hold.");
                LogAssert.NoUnexpectedReceived();
            }
            finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        private static void Capture(string name)
        {
            var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();
            var image=OverlayEvidenceCapture.Render(Camera.main,canvas,1280,720);
            try{File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence",name)),image.EncodeToPNG());}
            finally{UnityEngine.Object.Destroy(image);}
        }
    }
}
