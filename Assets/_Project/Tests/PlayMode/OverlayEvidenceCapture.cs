using System;
using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    /// <summary>Test-only SDR capture: graded world, then the real ungraded uGUI overlay.</summary>
    internal static class OverlayEvidenceCapture
    {
        public static Texture2D Render(Camera worldCamera,Canvas canvas,int width,int height,Action refreshLayout=null)
        {
            if(!worldCamera || !canvas)throw new ArgumentNullException("Capture requires a camera and Canvas.");
            if(canvas.renderMode!=RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("This evidence helper preserves a screen-space overlay, not world-space UI.");
            var previousTarget=worldCamera.targetTexture;var previousActive=RenderTexture.active;
            var previousCamera=canvas.worldCamera;float previousDistance=canvas.planeDistance;
            bool previousEnabled=canvas.enabled;
            var members=canvas.GetComponentsInChildren<Transform>(true);
            var layers=members.Select(t=>t.gameObject.layer).ToArray();
            int layer=UnusedLayer(canvas.transform);
            var world=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var result=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false,false);
            GameObject cameraObject=null,backdropObject=null;
            try
            {
                world.Create();result.Create();worldCamera.targetTexture=world;
                cameraObject=new GameObject("Evidence overlay camera",typeof(Camera)){hideFlags=HideFlags.HideAndDontSave};
                var uiCamera=cameraObject.GetComponent<Camera>();
                uiCamera.enabled=false;uiCamera.targetTexture=result;uiCamera.cullingMask=1<<layer;
                uiCamera.clearFlags=CameraClearFlags.SolidColor;uiCamera.backgroundColor=Color.black;
                uiCamera.nearClipPlane=.01f;uiCamera.farClipPlane=10;uiCamera.fieldOfView=60;
                uiCamera.allowHDR=false;uiCamera.allowMSAA=false;uiCamera.allowDynamicResolution=false;
                var uiData=uiCamera.GetUniversalAdditionalCameraData();
                uiData.renderPostProcessing=false;uiData.antialiasing=AntialiasingMode.None;
                uiData.renderShadows=false;uiData.allowHDROutput=false;uiData.volumeLayerMask=0;
                uiData.requiresDepthOption=CameraOverrideOption.Off;uiData.requiresColorOption=CameraOverrideOption.Off;
                foreach(var member in members)member.gameObject.layer=layer;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();refreshLayout?.Invoke();Canvas.ForceUpdateCanvases();
                foreach(var label in canvas.GetComponentsInChildren<Text>(true))
                {label.cachedTextGenerator.Invalidate();label.SetAllDirty();}
                Canvas.ForceUpdateCanvases();

                // URP's native overlay pass resolves only to the display. Camera-space
                // UI on this camera would instead be graded with the transparent world.
                canvas.enabled=false;worldCamera.Render();canvas.enabled=true;

                // URP17.6 clears the color of a Base camera even for a depth-only clear.
                // Sample the already graded sRGB world as an opaque background, then
                // blend the actual UI normally. No CPU alpha/color reconstruction.
                backdropObject=new GameObject("Evidence graded world",typeof(RectTransform),typeof(RawImage))
                    {hideFlags=HideFlags.HideAndDontSave,layer=layer};
                var backdrop=backdropObject.GetComponent<RawImage>();backdrop.raycastTarget=false;
                backdrop.color=Color.white;backdrop.texture=world;
                var rect=(RectTransform)backdrop.transform;rect.SetParent(canvas.transform,false);rect.SetAsFirstSibling();
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                Canvas.ForceUpdateCanvases();uiCamera.Render();RenderTexture.active=result;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                return image;
            }
            catch {Object.DestroyImmediate(image);throw;}
            finally
            {
                if(backdropObject)Object.DestroyImmediate(backdropObject);
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=previousCamera;
                canvas.planeDistance=previousDistance;canvas.enabled=previousEnabled;
                for(int i=0;i<members.Length;i++)if(members[i])members[i].gameObject.layer=layers[i];
                worldCamera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                if(cameraObject)Object.DestroyImmediate(cameraObject);
                world.Release();result.Release();Object.DestroyImmediate(world);Object.DestroyImmediate(result);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static int UnusedLayer(Transform canvas)
        {
            uint occupied=0;
            foreach(var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                if(!transform.IsChildOf(canvas))occupied|=1u<<transform.gameObject.layer;
            for(int layer=31;layer>=0;layer--)if((occupied&(1u<<layer))==0)return layer;
            throw new InvalidOperationException("No unused temporary layer for isolated evidence UI.");
        }
    }

    public sealed class OverlayEvidenceCaptureTests
    {
        [UnityTest]
        public IEnumerator ExposureChangesWorldButNotCapturedOverlayColors()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType);
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();
            var canvas=GameObject.Find("Reins HUD").GetComponent<Canvas>();
            var owner=new GameObject("Evidence exposure challenge");
            var volume=owner.AddComponent<Volume>();volume.isGlobal=true;volume.priority=2000;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            var grade=profile.Add<ColorAdjustments>(true);
            var swatchObject=new GameObject("Evidence opaque UI swatch",typeof(RectTransform),typeof(Image));
            var swatch=swatchObject.GetComponent<Image>();swatch.color=new Color(.22f,.68f,.84f,1);swatch.raycastTarget=false;
            var rect=(RectTransform)swatch.transform;rect.SetParent(canvas.transform,false);
            rect.anchorMin=new Vector2(.44f,.44f);rect.anchorMax=new Vector2(.56f,.56f);
            rect.offsetMin=rect.offsetMax=Vector2.zero;rect.SetAsLastSibling();
            Texture2D first=null,second=null;bool oldPost=data.renderPostProcessing;
            try
            {
                data.renderPostProcessing=true;
                grade.postExposure.Override(-1);VolumeManager.instance.Update(camera.transform,data.volumeLayerMask);
                first=OverlayEvidenceCapture.Render(camera,canvas,640,360);
                grade.postExposure.Override(2);VolumeManager.instance.Update(camera.transform,data.volumeLayerMask);
                second=OverlayEvidenceCapture.Render(camera,canvas,640,360);
                var a=first.GetPixels32();var b=second.GetPixels32();int changed=0;
                for(int i=0;i<a.Length;i++)
                {
                    int delta=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);
                    if(delta>30)changed++;
                    int x=i%640,y=i/640;
                    if(x>=300 && x<340 && y>=170 && y<190)
                        Assert.That(delta,Is.LessThanOrEqualTo(3),"Exposure must not grade an opaque HUD pixel.");
                }
                Assert.Greater(changed,a.Length/4,"The artifact must contain the genuinely graded world behind the UI.");
                var center=a[180*640+320];
                Assert.Greater(center.b,center.r+30,"The real colored UI swatch must be present, not merely omitted in both frames.");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay,canvas.renderMode);
                Assert.IsNull(camera.targetTexture,"Capture must restore the gameplay render destination.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                data.renderPostProcessing=oldPost;
                Object.DestroyImmediate(swatchObject);Object.DestroyImmediate(owner);
                foreach(var component in profile.components)Object.DestroyImmediate(component);Object.DestroyImmediate(profile);
                if(first)Object.DestroyImmediate(first);if(second)Object.DestroyImmediate(second);
            }
            yield return null;
        }
    }
}
