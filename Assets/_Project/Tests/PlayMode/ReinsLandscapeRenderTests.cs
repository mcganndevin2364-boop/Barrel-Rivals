using System;
using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsLandscapeRenderTests
    {
        [UnityTest] public IEnumerator ActiveTerrainPassesRenderTheMappedLandscapeRatherThanFallbackOrFogOnly()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var renderer=GameObject.Find("Layered eroded ridges").GetComponent<MeshRenderer>();
            var original=renderer.sharedMaterial;var candidate=new Material(original);
            var camera=Camera.main;var priorTarget=camera.targetTexture;var priorActive=RenderTexture.active;
            var target=new RenderTexture(320,180,24);var texture=new Texture2D(320,180,TextureFormat.RGB24,false);
            float priorTimeScale=Time.timeScale;
            try
            {
                Time.timeScale=0;renderer.sharedMaterial=candidate;camera.targetTexture=target;
                yield return null;
                Color32[] Capture(Color tint)
                {
                    foreach(string slot in new[]{"_RockTint","_GroundTint","_DryTint"})candidate.SetColor(slot,tint);
                    camera.Render();RenderTexture.active=target;
                    texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
                    return texture.GetPixels32();
                }
                var red=Capture(new Color(.95f,.02f,.02f));var blue=Capture(new Color(.02f,.02f,.95f));
                var names=Enumerable.Range(0,candidate.passCount).Select(candidate.GetPassName).ToArray();
                Assert.IsTrue(names.Any(n=>string.Equals(n,"DistantTerrainForward",StringComparison.OrdinalIgnoreCase)),string.Join(",",names));
                Assert.IsTrue(names.Any(n=>string.Equals(n,"DepthOnly",StringComparison.OrdinalIgnoreCase)),string.Join(",",names));
                int changed=0;
                for(int i=0;i<red.Length;i++)
                    if(Math.Abs(red[i].r-blue[i].r)+Math.Abs(red[i].g-blue[i].g)+Math.Abs(red[i].b-blue[i].b)>30)changed++;
                Assert.That(changed,Is.GreaterThan(red.Length/100),
                    "Changing terrain-only tints must change visible world pixels; a supported shader asset is not enough.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                renderer.sharedMaterial=original;camera.targetTexture=priorTarget;RenderTexture.active=priorActive;Time.timeScale=priorTimeScale;
                Object.Destroy(candidate);Object.Destroy(target);Object.Destroy(texture);
            }
            yield return null;
        }
    }
}
