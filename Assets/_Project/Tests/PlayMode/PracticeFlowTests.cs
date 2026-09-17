using System.Collections;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Core;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BarrelRivals.Tests
{
    public sealed class PracticeFlowTests
    {
        [UnityTest]
        public IEnumerator TouchPlaysLaunchDrawTurnExitResultAndRetry()
        {
            yield return SceneManager.LoadSceneAsync("Arena_Practice",LoadSceneMode.Single);
            yield return null;
            var controller=Object.FindFirstObjectByType<PracticeController>();
            Assert.IsTrue(controller.HasBindings);
            var settings=InputSystem.settings;
            var background=settings.backgroundBehavior;
            var routing=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                yield return new WaitForSecondsRealtime(.3f); Canvas.ForceUpdateCanvases(); Capture("M1-Ready.png"); yield return null;
                Vector2 action=ScreenPoint("Practice action",new TracePoint(.5,.5));
                Queue(touch,1,TouchPhase.Began,action); yield return null; yield return null;
                Assert.AreEqual(PracticePhase.Gate,controller.Run.Phase);
                while(controller.Run.NowMs<controller.Run.LaunchCueMs) yield return null;
                Queue(touch,1,TouchPhase.Ended,action); yield return null; yield return null;
                Assert.AreEqual(PracticePhase.Alley,controller.Run.Phase);
                Assert.That(controller.Run.LaunchGrade,Is.Not.EqualTo(SkillGrade.Bad));
                yield return WaitFor(controller,PracticePhase.Preview,8);
                Capture("M1-Preview.png",gold:true); yield return null;
                yield return WaitFor(controller,PracticePhase.Drawing,3);
                var vertices=PatternScoring.Template(controller.Run.Pattern);
                var points=new List<TracePoint>();
                int stride=controller.Run.Pattern==PatternKind.Circle ? 4 : 1;
                for(int i=0;i<vertices.Length;i+=stride) points.Add(vertices[i]);
                Queue(touch,2,TouchPhase.Began,ScreenPoint("Drawing pad",points[0]));
                yield return new WaitForSecondsRealtime(.05f);
                for(int i=1;i<points.Count;i++)
                {
                    Queue(touch,2,TouchPhase.Moved,ScreenPoint("Drawing pad",points[i]));
                    yield return new WaitForSecondsRealtime(.05f);
                }
                Queue(touch,2,TouchPhase.Ended,ScreenPoint("Drawing pad",points[points.Count-1]));
                yield return null; yield return null;
                Assert.IsTrue(controller.Run.TraceSubmitted);
                Assert.That(controller.Run.DrawingGrade.Score,Is.GreaterThanOrEqualTo(90));
                Assert.IsFalse(controller.Run.ExitAccepted,"Drawing release cannot trigger the separate exit skill.");
                yield return WaitFor(controller,PracticePhase.Exit,8);
                while(controller.Run.NowMs<controller.Run.PhaseStartedMs+450) yield return null;
                action=ScreenPoint("Practice action",new TracePoint(.5,.5));
                Queue(touch,3,TouchPhase.Began,action); yield return null; yield return null;
                Queue(touch,3,TouchPhase.Ended,action); yield return null;
                Assert.IsTrue(controller.Run.ExitAccepted);
                Assert.That(controller.Run.ExitGrade,Is.Not.EqualTo(SkillGrade.Bad));
                yield return WaitFor(controller,PracticePhase.Complete,4);
                Assert.AreEqual(0,controller.Run.Result.KnockCount);
                Assert.Greater(controller.Run.Result.RawTimeMs,0);
                Capture("M1-Result.png",mint:true); yield return null;
                uint seed=controller.Run.Seed;
                Vector2 retry=ScreenPoint("Retry",new TracePoint(.5,.5));
                Queue(touch,4,TouchPhase.Began,retry); yield return null; yield return null;
                Queue(touch,4,TouchPhase.Ended,retry); yield return null; yield return null;
                Assert.AreEqual(PracticePhase.Ready,controller.Run.Phase);
                Assert.IsNull(controller.Run.Result); Assert.AreNotEqual(seed,controller.Run.Seed);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(touch);
                settings.backgroundBehavior=background; settings.editorInputBehaviorInPlayMode=routing;
            }
        }

        [UnityTest]
        public IEnumerator PointerOwnershipAndCancellationDoNotLeakIntoANewRun()
        {
            yield return SceneManager.LoadSceneAsync("Arena_Practice",LoadSceneMode.Single);
            yield return null;
            var controller=Object.FindFirstObjectByType<PracticeController>();
            var point=new TracePoint(.5,.5);
            controller.PointerDown(1,true,point); Assert.AreEqual(PracticePhase.Ready,controller.Run.Phase);
            controller.PointerDown(10,false,point); Assert.AreEqual(PracticePhase.Gate,controller.Run.Phase);
            controller.PointerDown(20,false,point); controller.PointerUp(20,point);
            Assert.AreEqual(0,controller.Run.LaunchErrorMs,"A second finger must not release the owned gate hold.");
            controller.PointerUp(10,point); Assert.Less(controller.Run.LaunchErrorMs,0);
            ExecuteEvents.Execute(GameObject.Find("Practice action"),new BaseEventData(EventSystem.current),ExecuteEvents.cancelHandler);
            Assert.AreEqual(PracticePhase.Cancelled,controller.Run.Phase); Assert.IsNull(controller.Run.Result);
            GameObject.Find("Retry").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(PracticePhase.Ready,controller.Run.Phase);
            controller.PointerUp(10,point); Assert.AreEqual(PracticePhase.Ready,controller.Run.Phase);
            controller.PointerDown(30,false,point); Assert.AreEqual(PracticePhase.Gate,controller.Run.Phase);
            controller.CancelPractice();
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator WaitFor(PracticeController controller,PracticePhase phase,double seconds)
        {
            double timeout=Time.realtimeSinceStartupAsDouble+seconds;
            while(controller.Run.Phase!=phase && Time.realtimeSinceStartupAsDouble<timeout) yield return null;
            Assert.AreEqual(phase,controller.Run.Phase);
        }
        private static Vector2 ScreenPoint(string name,TracePoint point)
        {
            var rect=GameObject.Find(name).GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+(float)point.X*rect.rect.width,rect.rect.yMin+(float)point.Y*rect.rect.height)));
        }
        private static void Queue(Touchscreen screen,int id,TouchPhase phase,Vector2 position)
            => InputSystem.QueueStateEvent(screen,new TouchState { touchId=id,phase=phase,position=position,pressure=phase==TouchPhase.Ended?0:1 });

        private static void Capture(string file,bool gold=false,bool mint=false)
        {
            var canvas=GameObject.Find("Skill HUD").GetComponent<Canvas>(); var camera=Camera.main;
            RenderMode mode=canvas.renderMode; Camera previousCamera=canvas.worldCamera;
            float distance=canvas.planeDistance; RenderTexture previousTarget=camera.targetTexture, previousActive=RenderTexture.active;
            var target=new RenderTexture(1280,720,24); var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera; canvas.planeDistance=1; Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                if(gold || mint)
                {
                    int colored=0;
                    for(int y=116;y<460;y++) for(int x=888;x<1230;x++)
                    {
                        Color pixel=image.GetPixel(x,y);
                        if(gold && pixel.r>.75f && pixel.g>.5f && pixel.b<.65f) colored++;
                        if(mint && pixel.r<.6f && pixel.g>.75f && pixel.b>.6f) colored++;
                    }
                    Assert.Greater(colored,200,"The actual drawing pad must visibly render the target/trace.");
                }
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence")); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,file),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=mode; canvas.worldCamera=previousCamera; canvas.planeDistance=distance;
                camera.targetTexture=previousTarget; RenderTexture.active=previousActive; Canvas.ForceUpdateCanvases();
                Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
