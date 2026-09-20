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

namespace BarrelRivals.Tests
{
    public sealed class ReinsFlowTests
    {
        [UnityTest]
        public IEnumerator ClassicNavigationAndTwoOwnedReinsReachTheNewRace()
        {
            yield return SceneManager.LoadSceneAsync("Arena_Practice",LoadSceneMode.Single);yield return null;
            var link=GameObject.Find("Practice mode navigation");Assert.NotNull(link);
            link.GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("Arena_ReinsLab",SceneManager.GetActiveScene().name);
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();Assert.IsTrue(controller.HasBindings);
            yield return null;controller.RefreshPresentation();Capture("Reins-Ready.png");
            GameObject.Find("Begin").GetComponent<Button>().onClick.Invoke();controller.enabled=false;
            while(controller.Run.Phase==ReinsPhase.Preview)controller.Step(default);
            Assert.AreEqual(ReinsPhase.Gate,controller.Run.Phase);
            var rhythm=GameObject.Find("Rhythm and wrap");var pointer=new PointerEventData(EventSystem.current){pointerId=71,position=RectTransformUtility.WorldToScreenPoint(null,rhythm.transform.position)};
            while(controller.Run.Phase==ReinsPhase.Gate)
            {
                bool peak=controller.Run.PhaseElapsedMs%1000==480;
                if(peak)ExecuteEvents.Execute(rhythm,pointer,ExecuteEvents.pointerDownHandler);
                controller.Step(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble));
                if(peak)ExecuteEvents.Execute(rhythm,pointer,ExecuteEvents.pointerUpHandler);
            }
            Assert.AreEqual(3,controller.Run.GatePeaksHit);Assert.IsFalse(controller.Run.FalseBreak);
            var leftPad=GameObject.Find("Left rein");var rightPad=GameObject.Find("Right rein");
            var leftPointer=Drag(leftPad,11,.9f);var rightPointer=Drag(rightPad,22,.6f);
            Assert.IsFalse(controller.Press(ReinsPad.Rhythm,22),"A finger already holding a rein cannot own a second action.");
            controller.Release(ReinsPad.Left,22);
            var two=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);Assert.AreEqual(900,two.LeftPermille);Assert.AreEqual(600,two.RightPermille);
            var fill=GameObject.Find("Left tension").GetComponent<Image>();Assert.NotNull(fill.sprite,"Image.Filled ignores fillAmount without a sprite.");
            fill.fillAmount=.25f;Canvas.ForceUpdateCanvases();var mesh=fill.canvasRenderer.GetMesh();Assert.NotNull(mesh);Assert.Greater(mesh.vertexCount,0);Assert.Less(mesh.bounds.size.x,fill.rectTransform.rect.width*.4f);
            double heading=controller.Run.HeadingRadians;for(int i=0;i<40;i++)controller.Step(two);
            Assert.Less(controller.Run.HeadingRadians,heading,"Differential reins must turn the actual horse left.");
            ExecuteEvents.Execute(leftPad,leftPointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(rightPad,rightPointer,ExecuteEvents.pointerUpHandler);
            double beforeTap=Time.realtimeSinceStartupAsDouble-.001;
            Assert.IsTrue(controller.Press(ReinsPad.Rhythm,22));Assert.IsFalse(controller.ConsumeInput(beforeTap).CadenceTap,"A fresh input must not be applied to an earlier catch-up tick.");var tap=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);
            Assert.IsTrue(tap.CadenceTap);Assert.IsFalse(controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).CadenceTap,"A held touch cannot synthesize repeated beats.");
            controller.Release(ReinsPad.Rhythm,22);controller.Step(tap);controller.RefreshPresentation();Capture("Reins-Riding.png");
            Assert.That(controller.Run.SpeedMetresPerSecond,Is.GreaterThan(0));
            controller.SendMessage("OnApplicationPause",true);
            Assert.AreEqual(ReinsPhase.Cancelled,controller.Run.Phase);Assert.AreEqual(0,controller.Run.FinalTimeMs);
            controller.ResetRun();Assert.AreEqual(ReinsPhase.Ready,controller.Run.Phase);
            var neutral=controller.ConsumeInput(Time.realtimeSinceStartupAsDouble);Assert.AreEqual(0,neutral.LeftPermille);Assert.AreEqual(0,neutral.RightPermille);Assert.IsFalse(neutral.CadenceTap);
            GameObject.Find("Classic practice").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("Arena_Practice",SceneManager.GetActiveScene().name);
            LogAssert.NoUnexpectedReceived();
        }
        private static PointerEventData Drag(GameObject target,int id,float amount)
        {
            var rect=target.GetComponent<RectTransform>();var local=new Vector2(rect.rect.center.x,rect.rect.yMax-25);
            var pointer=new PointerEventData(EventSystem.current){pointerId=id,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local))};
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);
            local.y-=rect.rect.height*.65f*amount;pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));
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
            controller.RefreshPresentation();Capture("Reins-Mixed-Footing.png");
            LogAssert.NoUnexpectedReceived();
        }

        [Serializable] private sealed class Fixture { public Frame[] frames; }
        [Serializable] private sealed class Frame
        {public int tick,leftPermille,rightPermille;public bool cadenceTap,gateTap,wrap;public string drive;
            public ReinsInput Input()=>new ReinsInput(leftPermille,rightPermille,cadenceTap,gateTap,wrap,(DriveSide)Enum.Parse(typeof(DriveSide),drive));}
        [UnityTest]
        public IEnumerator CanonicalServerRunCompletesInUnityPersistsAndReplays()
        {
            yield return SceneManager.LoadSceneAsync("Arena_ReinsLab",LoadSceneMode.Single);yield return null;
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            string directory=Path.Combine(Application.temporaryCachePath,"reins-proof-"+Guid.NewGuid().ToString("N"));
            controller.SetRecordDirectory(directory);
            try
            {
                var fixture=JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,"../Contracts/Reins/complete-request.v1.json")));
                controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());
                Assert.AreEqual(ReinsPhase.Complete,controller.Run.Phase);Assert.AreEqual(35120,controller.Run.FinalTimeMs);Assert.AreEqual(300,controller.Run.StylePoints);Assert.AreEqual(0,controller.Run.KnockCount);
                controller.RefreshPresentation();Capture("Reins-Result.png");
                var record=ReinsLabRecordStore.Load(ReinsSurface.HardPack,out var notice,directory);Assert.NotNull(record,notice);Assert.AreEqual(ReinsRuleFingerprint.Sha256,record.fingerprint);
                record.fingerprint="incompatible";Assert.Throws<InvalidDataException>(()=>ReinsLabRecordStore.Validate(record,ReinsSurface.HardPack));
                controller.ResetRun();controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());
                Assert.AreEqual(ReinsPhase.Complete,controller.Run.Phase);Assert.AreEqual(35120,controller.Run.FinalTimeMs);
                var ghost=GameObject.Find("Own best — Reins recording");controller.RefreshPresentation();
                Assert.IsFalse(ghost && ghost.activeInHierarchy,"Finished playback is hidden.");
                // An unwritable store path must still preserve a verified session best across Retry.
                controller.SetRecordDirectory(Path.Combine(directory,"blocked"));File.WriteAllText(Path.Combine(directory,"blocked"),"not a directory");
                controller.Begin();foreach(var frame in fixture.frames)controller.Step(frame.Input());controller.ResetRun();controller.RefreshPresentation();
                Assert.AreEqual("OWN BEST ON",GameObject.Find("Own ghost").GetComponentInChildren<Text>().text);
                LogAssert.NoUnexpectedReceived();
            }
            finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        private static void Capture(string name)
        {
            var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();var camera=Camera.main;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var distance=canvas.planeDistance;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.01f;Canvas.ForceUpdateCanvases();
                foreach(var label in canvas.GetComponentsInChildren<Text>(true)){label.cachedTextGenerator.Invalidate();label.SetAllDirty();}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence",name));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;camera.targetTexture=oldTarget;RenderTexture.active=active;UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);Canvas.ForceUpdateCanvases();}
        }
    }
}
