using System;
using System.Collections;
using System.IO;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsLightingTests
    {
        [UnityTest] public IEnumerator SavedRendererActuallyAppliesExposureToRenderedWorld()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType);
            var controller=Object.FindAnyObjectByType<ReinsLabController>();controller.enabled=false;
            var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();
            var source=GameObject.Find("Horse proxy").GetComponent<ReinsHorsePresentation>();
            var animator=source.GetComponentInChildren<Animator>();var hair=source.GetComponentInChildren<ReinsHairMotion>();
            var tack=source.GetComponentInChildren<ReinsRiderTackPresentation>();var rig=camera.GetComponent<RiderCameraRig>();
            bool sourceEnabled=source.enabled,rigEnabled=rig.enabled,hairEnabled=hair.enabled,tackEnabled=tack.enabled;
            float oldTimeScale=Time.timeScale;bool oldPost=data.renderPostProcessing;
            var owner=new GameObject("Temporary exposure verification");var volume=owner.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1000;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(2);
            var sun=RenderSettings.sun;var oldSunColor=sun.color;var oldSunIntensity=sun.intensity;var oldSunRotation=sun.transform.rotation;
            var oldSky=RenderSettings.ambientSkyColor;var oldEquator=RenderSettings.ambientEquatorColor;var oldGround=RenderSettings.ambientGroundColor;
            float oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance;var oldFogColor=RenderSettings.fogColor;
            var target=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);var picture=new Texture2D(960,540,TextureFormat.RGB24,false);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            try
            {
                Time.timeScale=0;source.enabled=false;rig.enabled=false;hair.enabled=false;tack.enabled=false;
                yield return null;
                controller.RefreshPresentation();animator.Update(0);hair.RenderImmediate();tack.RenderImmediate();rig.RenderImmediate();
                camera.targetTexture=target;
                Color32[] Read(string path=null)
                {
                    VolumeManager.instance.Update(camera.transform,data.volumeLayerMask);camera.Render();RenderTexture.active=target;
                    picture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);picture.Apply();
                    if(path!=null)File.WriteAllBytes(path,picture.EncodeToPNG());return picture.GetPixels32();
                }
                // An assigned Volume or shader resource alone does not prove that the renderer runs the pass.
                data.renderPostProcessing=false;var ungraded=Read();
                data.renderPostProcessing=true;var exposed=Read();
                double delta=0;int changed=0;
                for(int i=0;i<ungraded.Length;i++)
                {
                    int d=Math.Abs(ungraded[i].r-exposed[i].r)+Math.Abs(ungraded[i].g-exposed[i].g)+Math.Abs(ungraded[i].b-exposed[i].b);
                    delta+=d;if(d>30)changed++;
                }
                delta/=ungraded.Length*3;
                Assert.That(delta,Is.GreaterThan(10),"The actual saved renderer must visibly execute its exposure/post-processing path.");
                Assert.That(changed,Is.GreaterThan(ungraded.Length/4),"Exposure must affect the world, not just a few changed pixels.");
                volume.enabled=false;
                string output=Environment.GetEnvironmentVariable("BARREL_LIGHTING_STUDY_DIRECTORY");
                if(!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);Assert.IsEmpty(Directory.GetFiles(output,"*.png"),"Use a fresh lighting-study output folder.");
                    var reflection=ReflectionProbe.defaultTexture;
                    File.WriteAllText(Path.Combine(output,"render-proof.json"),JsonUtility.ToJson(new Proof{meanAbsoluteChannelChange=delta,changedPixels=changed,width=960,height=540,
                        defaultReflection=reflection?reflection.name:"none",defaultReflectionType=reflection?reflection.GetType().Name:"none",defaultReflectionWidth=reflection?reflection.width:0,
                        context="Controlled Ready pose in actual saved Reins scene; world-only art study, HUD intentionally omitted; exposure challenge proves execution, not reference quality or device speed."},true)+"\n");
                    Read(Path.Combine(output,"00-enabled-grade-baseline.png"));
                    for(int candidate=0;candidate<2;candidate++)
                    {
                        volume.enabled=true;grade.postExposure.Override(candidate==0?.12f:.35f);grade.contrast.Override(7);grade.saturation.Override(2);
                        sun.color=new Color(1,.90f,.76f);sun.intensity=candidate==0?2.0f:2.5f;sun.transform.rotation=Quaternion.Euler(32,-32,0);
                        RenderSettings.ambientSkyColor=new Color(.58f,.65f,.76f);RenderSettings.ambientEquatorColor=new Color(.50f,.45f,.38f);RenderSettings.ambientGroundColor=new Color(.26f,.20f,.14f);
                        RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogColor=oldFogColor;
                        yield return null;Read(Path.Combine(output,"0"+(candidate+1)+"-afternoon-fill.png"));
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;data.renderPostProcessing=oldPost;
                sun.color=oldSunColor;sun.intensity=oldSunIntensity;sun.transform.rotation=oldSunRotation;
                RenderSettings.ambientSkyColor=oldSky;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;
                RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogColor=oldFogColor;
                Time.timeScale=oldTimeScale;source.enabled=sourceEnabled;rig.enabled=rigEnabled;hair.enabled=hairEnabled;tack.enabled=tackEnabled;
                Object.Destroy(owner);foreach(var component in profile.components)Object.Destroy(component);Object.Destroy(profile);Object.Destroy(target);Object.Destroy(picture);
            }
            yield return null;
        }
        [Serializable] private sealed class Proof {public double meanAbsoluteChannelChange;public int changedPixels,width,height,defaultReflectionWidth;public string defaultReflection,defaultReflectionType,context;}
    }
}
