using System.Collections;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsGhostHairTests
    {
        [UnityTest]
        public IEnumerator GhostStrandsKeepTheirMaskAndReleasePrivateMaterialsEvenWhenNeverShown()
        {
            int ownersBefore=Object.FindObjectsByType<ReinsGhostMaterialResources>(FindObjectsSortMode.None).Length;
            var source=new GameObject("Hair ghost fixture");source.SetActive(false);
            source.transform.position=new Vector3(10000,10000,10000);
            var bone=new GameObject("Private skin fixture bone");bone.transform.SetParent(source.transform,false);
            var child=new GameObject("Horse strand hair");child.transform.SetParent(source.transform,false);child.layer=31;
            var mesh=new Mesh {name="Strand alpha regression quad"};
            mesh.vertices=new[]{new Vector3(-1,-.5f,0),new Vector3(1,-.5f,0),new Vector3(1,.5f,0),new Vector3(-1,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};
            mesh.bindposes=new[]{Matrix4x4.identity};mesh.boneWeights=new[]{Weight(),Weight(),Weight(),Weight()};mesh.RecalculateBounds();
            var atlas=new Texture2D(2,1,TextureFormat.RGBA32,false) {name="Half-transparent strand atlas",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            atlas.SetPixels(new[]{Color.clear,Color.white});atlas.Apply();
            var lit=Shader.Find("Universal Render Pipeline/Lit");Assert.IsNotNull(lit);
            var unlit=Shader.Find("Universal Render Pipeline/Unlit");Assert.IsNotNull(unlit);
            var sourceMaterial=new Material(lit);
            sourceMaterial.SetTexture("_BaseMap",atlas);sourceMaterial.SetColor("_BaseColor",Color.red);
            sourceMaterial.SetFloat("_AlphaClip",1);sourceMaterial.SetFloat("_Cutoff",.36f);sourceMaterial.SetFloat("_Cull",0);
            sourceMaterial.EnableKeyword("_ALPHATEST_ON");
            sourceMaterial.SetTextureScale("_BaseMap",new Vector2(.8f,1));sourceMaterial.SetTextureOffset("_BaseMap",new Vector2(.1f,0));
            var tint=new Material(unlit);var tintColor=new Color(.35f,.88f,.77f,.28f);tint.SetColor("_BaseColor",tintColor);
            var skin=child.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=sourceMaterial;
            skin.rootBone=bone.transform;skin.bones=new[]{bone.transform};skin.updateWhenOffscreen=true;
            Transform shown=null,neverShown=null;Camera camera=null;RenderTexture render=null;Texture2D readback=null;
            Material shownCopy=null,unshownCopy=null;
            var previous=RenderTexture.active;
            try
            {
                shown=ReinsLabController.CreatePresentationGhost(source.transform,tint);
                neverShown=ReinsLabController.CreatePresentationGhost(source.transform,tint);
                Assert.IsFalse(neverShown.gameObject.activeSelf);
                shownCopy=shown.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
                unshownCopy=neverShown.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
                Assert.AreNotSame(sourceMaterial,shownCopy);Assert.AreNotSame(tint,shownCopy);Assert.AreNotSame(shownCopy,unshownCopy);
                Assert.AreEqual("Barrel Rivals/Reins Ghost Hair",shownCopy.shader.name);
                Assert.AreSame(atlas,shownCopy.GetTexture("_BaseMap"));
                Assert.AreEqual(new Vector2(.8f,1),shownCopy.GetTextureScale("_BaseMap"));
                Assert.AreEqual(new Vector2(.1f,0),shownCopy.GetTextureOffset("_BaseMap"));
                Assert.AreEqual(.36f,shownCopy.GetFloat("_Cutoff"),.00001f);
                Assert.AreEqual(0,shownCopy.GetFloat("_Cull"));Assert.AreEqual(1,shownCopy.GetFloat("_AlphaClip"));
                AssertColorNear(tintColor,shownCopy.GetColor("_BaseColor"),"Private ghost tint");
                Assert.IsTrue(shown.GetComponentInChildren<SkinnedMeshRenderer>(true).bones[0].IsChildOf(shown));
                Assert.AreSame(sourceMaterial,skin.sharedMaterial);Assert.AreSame(atlas,sourceMaterial.GetTexture("_BaseMap"));
                AssertColorNear(Color.red,sourceMaterial.GetColor("_BaseColor"),"Unmodified source color");
                Assert.AreEqual(.36f,sourceMaterial.GetFloat("_Cutoff"),.00001f);
                AssertColorNear(tintColor,tint.GetColor("_BaseColor"),"Unmodified shared ghost tint");
                Assert.AreEqual(ownersBefore+2,Object.FindObjectsByType<ReinsGhostMaterialResources>(FindObjectsSortMode.None).Length);

                // Render the actual shader: the clear half must disappear and the visible half must
                // survive even though ghost opacity .28 is below the preserved source cutoff .36.
                shown.gameObject.SetActive(true);
                camera=new GameObject("Ghost mask regression camera").AddComponent<Camera>();camera.enabled=false;
                camera.transform.position=source.transform.position+new Vector3(0,0,-2);camera.transform.rotation=Quaternion.identity;
                camera.orthographic=true;camera.orthographicSize=.5f;camera.aspect=2;camera.nearClipPlane=.05f;camera.farClipPlane=5;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.allowHDR=false;
                render=new RenderTexture(32,16,24,RenderTextureFormat.ARGB32);render.Create();camera.targetTexture=render;
                yield return null;
                camera.Render();RenderTexture.active=render;
                readback=new Texture2D(32,16,TextureFormat.RGB24,false);readback.ReadPixels(new Rect(0,0,32,16),0,0);readback.Apply();
                var clear=readback.GetPixel(7,8);var strand=readback.GetPixel(24,8);
                Assert.Less(clear.maxColorComponent,.025f,"Transparent atlas pixels must not become ghost card rectangles.");
                Assert.Greater(strand.g,.07f,"Faint ghost opacity must not erase the source alpha-clipped strands.");
                Assert.Greater(strand.g,strand.r,"Strands use the ghost tint, not the source red coat.");
                RenderTexture.active=previous;

                Object.Destroy(shown.gameObject);Object.Destroy(neverShown.gameObject);
                yield return null;yield return null;
                Assert.IsTrue(shownCopy==null,"Shown ghost material must be released.");
                Assert.IsTrue(unshownCopy==null,"A never-activated ghost must also release its material.");
                Assert.AreEqual(ownersBefore,Object.FindObjectsByType<ReinsGhostMaterialResources>(FindObjectsSortMode.None).Length);
                Assert.IsTrue(sourceMaterial);Assert.IsTrue(tint);Assert.IsTrue(atlas);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                RenderTexture.active=previous;
                if(shown)Object.Destroy(shown.gameObject);if(neverShown)Object.Destroy(neverShown.gameObject);
                if(camera){camera.targetTexture=null;Object.Destroy(camera.gameObject);}
                if(render){render.Release();Object.Destroy(render);}if(readback)Object.Destroy(readback);
                Object.Destroy(source);Object.Destroy(mesh);Object.Destroy(atlas);Object.Destroy(sourceMaterial);Object.Destroy(tint);
            }
        }
        private static void AssertColorNear(Color expected,Color actual,string message)
        {
            // Material color conversion may change the last floating-point bits. A 1e-5 per-channel
            // tolerance still catches visible tint/opacity changes without requiring bitwise identity.
            for(int channel=0;channel<4;channel++)
                Assert.AreEqual(expected[channel],actual[channel],.00001f,message+" channel "+channel);
        }
        private static BoneWeight Weight()=>new BoneWeight{boneIndex0=0,weight0=1};
    }
}
