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
            Assert.IsNull(GameObject.Find("Select pad-turquoise"),"Other slot cards must stay filtered out.");
            Click("Filter Pad");
            Click("Select pad-turquoise");yield return null;
            Assert.AreEqual("pad-turquoise",appearance.Applied.padId);Assert.AreEqual("pad-desert",stable.Equipped.padId);
            Click("MyStable tab");Assert.AreEqual("pad-desert",appearance.Applied.padId);
            Click("Gear tab");Click("Filter Pad");Click("Select pad-turquoise");Click("Equip selected");
            Click("Filter Reins");
            Click("Select reins-crimson");Click("Equip selected");
            Assert.AreEqual("pad-turquoise",stable.Equipped.padId);Assert.AreEqual("reins-crimson",stable.Equipped.reinsId);

            // Inspecting headstall/gloves must neither save them nor keep a preview
            // when changing screens. Only the explicit equip button commits a choice.
            string saved=File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName));
            Click("Filter Headstall");Click("Select headstall-midnight");
            Assert.AreEqual("headstall-midnight",appearance.Applied.headstallId);
            Assert.AreEqual("headstall-ranch",stable.Equipped.headstallId);
            AssertMaterial(appearance.gameObject,"Fitted leather headstall","Midnight headstall",1);
            Assert.AreEqual(saved,File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName)));
            Click("Cancel tack preview");Assert.AreEqual("headstall-ranch",appearance.Applied.headstallId);
            AssertMaterial(appearance.gameObject,"Fitted leather headstall","Oiled bridle leather",1);
            Click("Gear tab");Click("Filter Headstall");Click("Select headstall-midnight");Click("Equip selected");
            saved=File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName));
            Click("Rider gear tab");Assert.IsTrue(stable.IsRiderGearOpen);
            var rider=GameObject.Find("Rider glove preview").GetComponent<StableAppearance>();
            Click("Select gloves-rodeo-red");
            Assert.AreEqual("gloves-rodeo-red",rider.Applied.glovesId);Assert.AreEqual("gloves-classic",stable.Equipped.glovesId);
            AssertMaterial(rider.gameObject,"Inspect glove shell","Rodeo red gloves",1);
            Assert.AreEqual(saved,File.ReadAllText(Path.Combine(directory,StableProfileStore.FileName)));
            Click("Cancel rider preview");Assert.AreEqual("gloves-classic",rider.Applied.glovesId);
            AssertMaterial(rider.gameObject,"Inspect glove shell","Worn chestnut gloves",1);
            Click("Rider gear tab");Click("Select gloves-rodeo-red");Click("Equip rider selected");
            Assert.AreEqual("gloves-rodeo-red",stable.Equipped.glovesId);
            Assert.IsFalse(GameObject.Find("Equip rider selected").GetComponent<Button>().interactable);
            // Simulate a process restart: the next scene must resolve disk state, not a surviving object.
            StableSession.UseForTests(new StableProfileStore(directory));
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
            stable=Object.FindFirstObjectByType<StableController>();Assert.AreEqual("pad-turquoise",stable.Equipped.padId);
            Assert.AreEqual("headstall-midnight",stable.Equipped.headstallId);Assert.AreEqual("gloves-rodeo-red",stable.Equipped.glovesId);
            Click("Rider gear tab");Click("Select gloves-blackout");
            Click("Race");yield return null;yield return null;
            var race=Object.FindFirstObjectByType<ReinsLabController>();Assert.IsNotNull(race);race.enabled=false;
            var horse=GameObject.Find("Horse proxy");appearance=horse.GetComponent<StableAppearance>();
            Assert.AreEqual("reins-crimson",appearance.Applied.reinsId);Assert.AreEqual("pad-turquoise",appearance.Applied.padId);
            Assert.AreEqual("headstall-midnight",appearance.Applied.headstallId);
            Assert.AreEqual("gloves-rodeo-red",appearance.Applied.glovesId,"Riding must discard an unequipped glove preview.");
            AssertMaterial(horse,"Fitted leather headstall","Midnight headstall",1);
            AssertMaterial(horse,"Glove shell","Rodeo red gloves",2);
            AssertMaterial(horse,"Sleeve and glove stitching","Fixed sleeve and glove stitching",2);
            var rein=horse.GetComponentsInChildren<Renderer>().First(r=>r.name=="Left braided rein");
            Assert.AreEqual("Crimson rein braid",rein.sharedMaterials[1].name);
            Assert.AreEqual(500,race.Run.Manifest.Horse.FirePermille);
            var playerSaddle=horse.GetComponentsInChildren<Renderer>().First(r=>r.name=="Contoured western leather");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly,playerSaddle.shadowCastingMode,"Own saddle must not obstruct the seated rider camera.");
            var ghost=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Own best — Reins recording");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,ghost.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Contoured western leather").shadowCastingMode,"Ghost retains visible tack independently of the rider camera.");
            Assert.IsTrue(race.CanOpenStable);race.Begin();Assert.IsFalse(race.CanOpenStable);race.OpenStable();
            Assert.AreEqual(ReinsLabController.SceneName,SceneManager.GetActiveScene().name,"Stable navigation must not abandon an active attempt.");
            Assert.IsTrue(StableSession.Store.Equip(StableSlot.Gloves,"gloves-whiskey"));
            Assert.AreEqual("gloves-rodeo-red",appearance.Applied.glovesId,"Race appearance freezes on scene initialization.");
            AssertMaterial(horse,"Glove shell","Rodeo red gloves",2);
            AssertMaterial(horse,"Sleeve and glove stitching","Fixed sleeve and glove stitching",2);
            race.ResetRun();race.OpenStable();yield return null;yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<StableController>());LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RaceGloveDyesPreserveCombinedFixedSurfacesAndGhostIsolation()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var race=Object.FindFirstObjectByType<ReinsLabController>();race.enabled=false;
            var horse=GameObject.Find("Horse proxy");var appearance=horse.GetComponent<StableAppearance>();
            var gloves=horse.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Glove shell").ToArray();
            var fixedParts=horse.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Sleeve and glove stitching").ToArray();
            Assert.AreEqual(2,gloves.Length);Assert.AreEqual(2,fixedParts.Length);
            foreach(var glove in gloves) {
                Assert.AreEqual(1,glove.sharedMaterials.Length);
                Assert.AreEqual("Barrel Rivals/Tailored Leather",glove.sharedMaterial.shader.name);
                Assert.Greater(glove.GetComponent<MeshFilter>().sharedMesh.colors.Count(c=>c.a==1),150,"Persisted thread must survive native mesh saving.");
                Assert.That(glove.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,Is.InRange(1408,4500),
                    "The anatomical grip plus sewn construction must stay within its local mesh budget.");
            }
            var fixedMaterial=fixedParts[0].sharedMaterial;
            var albedo=fixedMaterial.GetTexture("_BaseMap") as Texture2D;
            var surface=fixedMaterial.GetTexture("_MetallicGlossMap") as Texture2D;
            Assert.IsNotNull(albedo);Assert.IsNotNull(surface);
            AssertColor(new Color(.034f,.047f,.054f,1),albedo.GetPixel(0,0));
            AssertColor(new Color(.58f,.40f,.19f,1),albedo.GetPixel(1,0));
            Assert.That(surface.GetPixel(0,0).a,Is.EqualTo(.12f).Within(1f/255));
            Assert.That(surface.GetPixel(1,0).a,Is.EqualTo(.22f).Within(1f/255));
            var originalColors=albedo.GetPixels32();var originalSurface=surface.GetPixels32();
            foreach(var part in fixedParts) {
                Assert.AreSame(fixedMaterial,part.sharedMaterial);Assert.AreEqual(1,part.sharedMaterials.Length);
                Assert.AreEqual(432,part.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,"Sleeve and stitch geometry must both survive consolidation.");
                Assert.AreEqual(2,part.transform.parent.GetComponentsInChildren<Renderer>().Length,"Each independently moving hand uses exactly two renderers.");
            }
            var original=appearance.Applied.Copy();var variants=new HashSet<Material>();
            try {
                foreach(var item in StableCatalog.Gear.Where(g=>g.Slot==StableSlot.Gloves)) {
                    var profile=original.Copy();Assert.IsTrue(profile.TryEquip(StableSlot.Gloves,item.Id));appearance.Apply(profile);
                    Assert.AreEqual(item.Id,appearance.Applied.glovesId);
                    Assert.AreSame(gloves[0].sharedMaterial,gloves[1].sharedMaterial);variants.Add(gloves[0].sharedMaterial);
                    foreach(var part in fixedParts)Assert.AreSame(fixedMaterial,part.sharedMaterial,"A glove dye must not bind the sleeve/thread material.");
                    CollectionAssert.AreEqual(originalColors,albedo.GetPixels32());CollectionAssert.AreEqual(originalSurface,surface.GetPixels32());
                    Assert.AreEqual(Color.white,fixedMaterial.GetColor("_BaseColor"));
                }
                Assert.AreEqual(6,variants.Count,"All six existing glove IDs retain distinct materials.");
                Assert.AreEqual(original.glovesId,StableSession.Store.Current.glovesId,"Presentation checks must not mutate the saved loadout.");
            }
            finally { appearance.Apply(original); }
            var ghost=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Own best — Reins recording");
            var ghostParts=ghost.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Glove shell" || r.name=="Sleeve and glove stitching").ToArray();
            Assert.AreEqual(4,ghostParts.Length);
            foreach(var part in ghostParts) {
                Assert.AreSame(ghostParts[0].sharedMaterial,part.sharedMaterial,"Combined opaque parts retain the normal ghost tint.");
                Assert.AreNotSame(fixedMaterial,part.sharedMaterial);
            }
            foreach(var part in fixedParts)Assert.AreSame(fixedMaterial,part.sharedMaterial,"Ghost tint must not replace player assets.");
            LogAssert.NoUnexpectedReceived();
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
            yield return null;Capture("StableReference-MyStable");
            Click("Riding traits");Assert.IsTrue(stable.IsSkillsOpen);Click("Close riding traits");Assert.IsFalse(stable.IsSkillsOpen);
            Click("Gear tab");yield return null;
            Assert.IsFalse(horse.activeSelf,"The full horse must not peek through the equipment collection panels.");
            Assert.AreEqual(8,VisibleCards().Length);
            Click("Select saddle-rodeo-gold");yield return null;AssertReachable("Select saddle-rodeo-gold");
            Capture("StableReference-SaddlesPreview");Click("Equip selected");Capture("StableReference-SaddlesEquipped");
            Click("Filter Headstall");Assert.AreEqual(2,VisibleCards().Length);
            Click("Select headstall-midnight");yield return null;AssertReachable("Select headstall-midnight");
            Capture("StableReference-HeadstallPreview");
            Click("Filter Reins");Click("Select reins-crimson");yield return null;
            // Verify the transparent orbit region does not steal touches intended for gear cards.
            AssertReachable("Select reins-crimson");
            Click("Rider gear tab");yield return null;
            Assert.AreEqual(6,VisibleCards().Length);Assert.IsTrue(stable.IsRiderGearOpen);Assert.IsFalse(horse.activeSelf);
            var rider=GameObject.Find("Rider glove preview");var riderRotation=rider.transform.rotation;
            ExecuteEvents.Execute<IDragHandler>(GameObject.Find("Drag glove to rotate"),new PointerEventData(EventSystem.current){delta=new Vector2(60,0)},ExecuteEvents.dragHandler);
            Assert.Greater(Quaternion.Angle(riderRotation,rider.transform.rotation),1);stable.ResetView();
            Assert.Less(Quaternion.Angle(riderRotation,rider.transform.rotation),.01f);
            Click("Select gloves-rodeo-red");yield return null;
            AssertReachable("Select gloves-rodeo-red");AssertReachable("Equip rider selected");
            Capture("StableReference-RiderPreview");Click("Equip rider selected");Capture("StableReference-RiderEquipped");
            Capture("StableReference-RiderTablet",1024,768);
            Click("MyStable tab");yield return null;Assert.IsTrue(horse.activeSelf);Assert.IsFalse(rider.activeSelf);
            foreach(var renderer in horse.GetComponentsInChildren<Renderer>(true))foreach(var m in renderer.sharedMaterials){Assert.IsNotNull(m);Assert.IsTrue(m.shader.isSupported);}
            LogAssert.NoUnexpectedReceived();
        }
        private static Button[] VisibleCards()=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Select ",StringComparison.Ordinal)).ToArray();
        private static void AssertColor(Color expected,Color actual)
        {Assert.That(actual.r,Is.EqualTo(expected.r).Within(1f/255));Assert.That(actual.g,Is.EqualTo(expected.g).Within(1f/255));Assert.That(actual.b,Is.EqualTo(expected.b).Within(1f/255));Assert.That(actual.a,Is.EqualTo(expected.a).Within(1f/255));}
        private static void AssertMaterial(GameObject root,string rendererName,string materialName,int expectedCount)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name==rendererName).ToArray();
            Assert.AreEqual(expectedCount,renderers.Length,rendererName+" binding count");
            foreach(var renderer in renderers)Assert.AreEqual(materialName,renderer.sharedMaterials[0].name,rendererName);
        }
        private static void AssertReachable(string name)
        {
            Canvas.ForceUpdateCanvases();var card=GameObject.Find(name).GetComponent<RectTransform>();
            var results=new List<RaycastResult>();var point=RectTransformUtility.WorldToScreenPoint(null,card.TransformPoint(card.rect.center));
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
            Assert.IsNotEmpty(results,name);Assert.AreEqual(card.GetComponent<Button>(),results[0].gameObject.GetComponentInParent<Button>(),name);
        }
        private static void Click(string name)
        {var go=GameObject.Find(name);Assert.IsNotNull(go,name);var button=go.GetComponent<Button>();Assert.IsTrue(button.interactable,name);button.onClick.Invoke();}
        private static void Capture(string name,int width=1280,int height=720)
        {
            var camera=Camera.main;var canvas=GameObject.Find("Stable HUD").GetComponent<Canvas>();
            Texture2D image=null;
            var stable=Object.FindFirstObjectByType<StableController>();
            try {
                image=OverlayEvidenceCapture.Render(camera,canvas,width,height,()=> {
                    stable.RefreshLayout(width,height,new Rect(0,0,width,height));Canvas.ForceUpdateCanvases();
                    if(stable.IsRiderGearOpen) {
                        var glove=GameObject.Find("Inspect glove shell").GetComponent<Renderer>();
                        var center=camera.WorldToViewportPoint(glove.bounds.center);
                        Assert.That(center.x,Is.InRange(.10f,.39f),"Glove must be in the left inspection area, not hidden behind the collection.");
                        Assert.That(center.y,Is.InRange(.28f,.72f),"Glove must remain above the footer and below the header.");
                    }
                    var brand=canvas.GetComponentsInChildren<Text>(true).First(t=>t.name=="Brand");
                    Assert.Greater(brand.cachedTextGenerator.characterCountVisible,0,"Brand text must not be truncated by its rect.");
                });
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Evidence/"+name+".png"));File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {if(image)Object.DestroyImmediate(image);stable.RefreshLayout();Canvas.ForceUpdateCanvases();}
        }
    }
}
