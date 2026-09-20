using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class StableFlowTests
    {
        private string directory;
        [SetUp] public void SetUp()
        {
            directory=Path.Combine(Path.GetTempPath(),"BarrelStableFlow-"+Guid.NewGuid().ToString("N"));
            StableSession.UseForTests(new StableProfileStore(directory));
        }
        [TearDown] public void TearDown()
        {
            StableSession.UseForTests(null);
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator GearPreviewEquipRestartAndRacePreserveChoiceWithoutChangingRules()
        {
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
            var stable=Object.FindFirstObjectByType<StableController>();Assert.IsNotNull(stable);
            var appearance=GameObject.Find("Copper").GetComponent<StableAppearance>();
            Assert.AreEqual("pad-desert",stable.Equipped.padId);
            Click("Gear tab");yield return null;Assert.IsTrue(stable.IsGearOpen);
            Click("Select pad-turquoise");yield return null;
            Assert.AreEqual("pad-turquoise",appearance.Applied.padId);Assert.AreEqual("pad-desert",stable.Equipped.padId);
            Click("MyStable tab");Assert.AreEqual("pad-desert",appearance.Applied.padId);
            Click("Gear tab");Click("Select pad-turquoise");Click("Equip selected");
            Click("Select reins-crimson");Click("Equip selected");
            Assert.AreEqual("pad-turquoise",stable.Equipped.padId);Assert.AreEqual("reins-crimson",stable.Equipped.reinsId);
            // Simulate a process restart: the next scene must resolve disk state, not a surviving object.
            StableSession.UseForTests(new StableProfileStore(directory));
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
            stable=Object.FindFirstObjectByType<StableController>();Assert.AreEqual("pad-turquoise",stable.Equipped.padId);
            stable.Race();yield return null;yield return null;
            var race=Object.FindFirstObjectByType<ReinsLabController>();Assert.IsNotNull(race);race.enabled=false;
            var horse=GameObject.Find("Horse proxy");appearance=horse.GetComponent<StableAppearance>();
            Assert.AreEqual("reins-crimson",appearance.Applied.reinsId);Assert.AreEqual("pad-turquoise",appearance.Applied.padId);
            var rein=horse.GetComponentsInChildren<Renderer>().First(r=>r.name=="Left braided rein");
            Assert.AreEqual("Crimson rein braid",rein.sharedMaterials[1].name);
            Assert.AreEqual(500,race.Run.Manifest.Horse.FirePermille);
            var playerSaddle=horse.GetComponentsInChildren<Renderer>().First(r=>r.name=="Contoured western leather");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly,playerSaddle.shadowCastingMode,"Own saddle must not obstruct the seated rider camera.");
            var ghost=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Own best — Reins recording");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,ghost.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Contoured western leather").shadowCastingMode,"Ghost retains visible tack independently of the rider camera.");
            Assert.IsTrue(race.CanOpenStable);race.Begin();Assert.IsFalse(race.CanOpenStable);race.OpenStable();
            Assert.AreEqual(ReinsLabController.SceneName,SceneManager.GetActiveScene().name,"Stable navigation must not abandon an active attempt.");
            race.ResetRun();race.OpenStable();yield return null;yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<StableController>());LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator StableAndGearRenderActualSavedSceneAndInputRemainsReachable()
        {
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
            var stable=Object.FindFirstObjectByType<StableController>();var horse=GameObject.Find("Copper");
            Assert.IsNotNull(stable);Assert.IsNotNull(horse.GetComponentInChildren<Animator>());
            Assert.IsEmpty(horse.GetComponentsInChildren<Collider>(true));
            var savedRotation=horse.transform.rotation;
            var orbit=GameObject.Find("Drag horse to rotate");
            ExecuteEvents.Execute<IDragHandler>(orbit,new PointerEventData(EventSystem.current){delta=new Vector2(60,0)},ExecuteEvents.dragHandler);
            Assert.Greater(Quaternion.Angle(savedRotation,horse.transform.rotation),1);stable.ResetView();
            yield return null;Capture("StableGear-MyStable");
            Click("Gear tab");yield return null;
            Click("Select reins-crimson");yield return null;
            // Verify the transparent orbit region does not steal touches intended for gear cards.
            var card=GameObject.Find("Select reins-crimson").GetComponent<RectTransform>();
            var results=new List<RaycastResult>();var point=RectTransformUtility.WorldToScreenPoint(null,card.TransformPoint(card.rect.center));
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
            Assert.IsNotEmpty(results);Assert.AreEqual(card.GetComponent<Button>(),results[0].gameObject.GetComponentInParent<Button>());
            Capture("StableGear-GearPreview");Click("Equip selected");Capture("StableGear-GearEquipped");
            foreach(var renderer in horse.GetComponentsInChildren<Renderer>(true))foreach(var m in renderer.sharedMaterials){Assert.IsNotNull(m);Assert.IsTrue(m.shader.isSupported);}
            LogAssert.NoUnexpectedReceived();
        }
        private static void Click(string name)
        {var go=GameObject.Find(name);Assert.IsNotNull(go,name);var button=go.GetComponent<Button>();Assert.IsTrue(button.interactable,name);button.onClick.Invoke();}
        private static void Capture(string name)
        {
            var camera=Camera.main;var canvas=GameObject.Find("Stable HUD").GetComponent<Canvas>();
            var mode=canvas.renderMode;var priorCamera=canvas.worldCamera;float distance=canvas.planeDistance;var priorTarget=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.01f;
                Canvas.ForceUpdateCanvases();foreach(var label in canvas.GetComponentsInChildren<Text>(true)){label.cachedTextGenerator.Invalidate();label.SetAllDirty();}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence/"+name+".png"));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {canvas.renderMode=mode;canvas.worldCamera=priorCamera;canvas.planeDistance=distance;camera.targetTexture=priorTarget;RenderTexture.active=active;Object.Destroy(target);Object.Destroy(image);Canvas.ForceUpdateCanvases();}
        }
    }
}
