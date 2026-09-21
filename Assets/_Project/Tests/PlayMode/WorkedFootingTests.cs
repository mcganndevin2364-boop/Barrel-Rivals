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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class WorkedFootingTests
    {
        [UnityTest]
        public IEnumerator ManifestProfilesRetainPatchCentersAndDriveDryAndWetAppearance()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName); yield return null;
            var controller = Object.FindAnyObjectByType<ReinsLabController>(); controller.enabled = false;
            var visual = Object.FindAnyObjectByType<ReinsFootingVisual>(); Assert.IsNotNull(visual);
            var patches = Enumerable.Range(0, 6).Select(i => GameObject.Find("Footing patch " + i).GetComponent<Renderer>()).ToArray();
            var centers = patches.Select(p => p.transform.position).ToArray();
            var block = new MaterialPropertyBlock(); var colors = new HashSet<Color>();
            foreach (ReinsSurface surface in Enum.GetValues(typeof(ReinsSurface)))
            {
                var manifest = new ReinsManifest(78, surface, 1); visual.Apply(manifest);
                for (int i = 0; i < patches.Length; i++)
                {
                    Assert.AreEqual(centers[i], patches[i].transform.position);
                    Assert.IsNull(patches[i].GetComponent<Collider>());
                    patches[i].GetPropertyBlock(block); float wet = block.GetFloat("_Wetness");
                    var actual = manifest.SurfaceAt(centers[i].x, centers[i].z);
                    if (actual == ReinsSurface.MuddySlop) Assert.That(wet, Is.GreaterThan(.5f));
                    else if (actual == ReinsSurface.TackyClay) Assert.That(wet, Is.InRange(.05f, .3f));
                    else Assert.AreEqual(0, wet, "Dry sand and hard pack must not inherit the preceding mud appearance.");
                    Assert.That(block.GetFloat("_TrackStrength"), Is.InRange(.5f, 1.3f));
                    colors.Add(block.GetColor("_BaseColor"));
                }
            }
            Assert.AreEqual(4, colors.Count); Assert.AreEqual(0, controller.Run.Tick);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SavedSurfaceHasDirectionalReliefAndContinuousWorldMappingAcrossSplitMeshes()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName); yield return null;
            Object.FindAnyObjectByType<ReinsLabController>().enabled = false;
            Camera.main.Render(); // Initialize URP before querying active shader passes in batch mode.
            var original = GameObject.Find("Footing patch 0").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual("Barrel Rivals/Worked Arena Footing", original.shader.name);
            foreach (string pass in new[] { "FootingForward", "ShadowCaster", "DepthOnly", "DepthNormals" })
                Assert.GreaterOrEqual(original.FindPass(pass), 0, pass);
            var track = (Texture2D)original.GetTexture("_TrackMap");
            Assert.AreEqual(1024, track.width); Assert.AreEqual(1024, track.height);
            Assert.Greater(track.mipmapCount, 1); Assert.IsFalse(track.isReadable);
            Assert.AreEqual(TextureWrapMode.Repeat, track.wrapMode);
            string output = Environment.GetEnvironmentVariable("BARREL_FOOTING_PROOF_DIRECTORY");
            if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
            var objects = new List<GameObject>(); var meshes = new List<Mesh>();
            var material = new Material(original); var oldSun = RenderSettings.sun;
            var oldMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
            var cameraObject = new GameObject("Footing proof camera"); objects.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.transform.position = new Vector3(0, 8, 0); camera.transform.LookAt(Vector3.zero, Vector3.forward);
            camera.orthographic = true; camera.orthographicSize = 4; camera.nearClipPlane = .05f; camera.farClipPlane = 20;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.allowHDR = false; camera.allowMSAA = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var lightObject = new GameObject("Footing proof light"); objects.Add(lightObject);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.4f; light.color = Color.white; light.shadows = LightShadows.None; light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(25, 35, 0); RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.1f, .1f, .1f);
            var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false, true);
            GameObject Plane(float x0, float x1, Vector3 position, float yaw)
            {
                var go = new GameObject("World mapped footing sample"); objects.Add(go); go.layer = 31;
                go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
                go.transform.localScale = new Vector3(1.3f, 1, .7f);
                var points = new[] { new Vector3(x0,0,-4), new Vector3(x0,0,4), new Vector3(x1,0,-4), new Vector3(x1,0,4) };
                var mesh = new Mesh(); meshes.Add(mesh);
                mesh.vertices = points.Select(go.transform.InverseTransformPoint).ToArray(); mesh.triangles = new[] { 0,1,2,2,1,3 };
                mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
                mesh.uv = Enumerable.Repeat(new Vector2(17, -92), 4).ToArray(); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; return go;
            }
            Color32[] Capture(string name)
            {
                var active = RenderTexture.active; var oldTarget = camera.targetTexture;
                try
                {
                    camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0,0,512,512),0,0); texture.Apply(false);
                    if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
                    return texture.GetPixels32();
                }
                finally { camera.targetTexture = oldTarget; RenderTexture.active = active; }
            }
            try
            {
                var whole = Plane(-4, 4, Vector3.zero, 0);
                var reference = Capture("whole-world-surface"); Assert.Greater(reference.Count(p => p.a > 250), 250000);
                whole.SetActive(false);
                var left = Plane(-4, 0, new Vector3(5,0,-7), 33);
                var right = Plane(0, 4, new Vector3(-8,0,2), -51);
                var split = Capture("split-transformed-surface");
                float sum = 0, maximum = 0;
                for (int i = 0; i < reference.Length; i++)
                {
                    float delta = Mathf.Max(Mathf.Abs(reference[i].r-split[i].r), Mathf.Abs(reference[i].g-split[i].g), Mathf.Abs(reference[i].b-split[i].b));
                    sum += delta; maximum = Mathf.Max(maximum, delta);
                }
                Assert.Less(sum/reference.Length, .2f, "Moving mesh origins/rotating local UV space must not reset the surface.");
                Assert.Less(maximum, 9, "Patch edges must not introduce visible seams.");
                left.SetActive(false); right.SetActive(false); whole.SetActive(true);
                material.SetFloat("_BumpScale", 0); material.SetFloat("_TrackStrength", 0);
                var flatA = Capture("relief-off"); material.SetFloat("_TrackStrength", 1); var reliefA = Capture("relief-on");
                light.transform.rotation = Quaternion.Euler(25, 215, 0);
                material.SetFloat("_TrackStrength", 0); var flatB = Capture("reverse-light-flat");
                material.SetFloat("_TrackStrength", 1); var reliefB = Capture("reverse-light-relief");
                int directional = 0; float energy = 0;
                for (int i=0;i<flatA.Length;i++)
                {
                    float a = ((int)reliefA[i].r+reliefA[i].g+reliefA[i].b-flatA[i].r-flatA[i].g-flatA[i].b)/765f;
                    float b = ((int)reliefB[i].r+reliefB[i].g+reliefB[i].b-flatB[i].r-flatB[i].g-flatB[i].b)/765f;
                    if (a*b < -.000004f) directional++; energy += Mathf.Abs(a);
                }
                Assert.Greater(directional, 500, "Impressions must react to lighting as surface normals, not only painted dark marks.");
                Assert.Greater(energy/flatA.Length, .003f);
                material.SetFloat("_Wetness", .62f); var wet = Capture("wet-surface");
                float wetDifference = wet.Select((p,i) => Mathf.Abs(p.r-reliefB[i].r)/255f).Average();
                Assert.Greater(wetDifference, .015f, "The manifest wetness property must visibly affect the material.");
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output,"render-proof.json"), JsonUtility.ToJson(new Proof {
                    worldMeanChannelDelta=sum/reference.Length,worldMaximumChannelDelta=maximum,
                    oppositelyLitReliefPixels=directional,meanReliefChange=energy/flatA.Length,meanWetChange=wetDifference },true)+"\n");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                RenderSettings.sun = oldSun; RenderSettings.ambientMode = oldMode; RenderSettings.ambientLight = oldAmbient;
                foreach(var go in objects) Object.Destroy(go); foreach(var mesh in meshes) Object.Destroy(mesh);
                Object.Destroy(material); rt.Release(); Object.Destroy(rt); Object.Destroy(texture);
            }
        }
        [Serializable] private sealed class Proof
        {
            public float worldMeanChannelDelta, worldMaximumChannelDelta, meanReliefChange, meanWetChange;
            public int oppositelyLitReliefPixels;
            public string scope = "Actual saved material under controlled directional lights and split/transformed meshes. Not photographic quality, continuous displacement, or phone performance evidence.";
        }
    }
}
