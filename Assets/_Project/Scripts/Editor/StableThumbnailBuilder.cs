using System;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Real Unity renders of the equipped geometry, not generated product illustrations.</summary>
    public static class StableThumbnailBuilder
    {
        public const string Root=StableTackBuilder.Root+"/Thumbnails";
        public static void Generate(Transform horse,Transform rider)
        {
            Directory.CreateDirectory(Root);
            foreach(var item in StableCatalog.Gear)Render(item.Id,item.Slot==StableSlot.Gloves?rider:horse,item.Slot);
            Render(StableCatalog.HorseId,horse,null);
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root,"*.png")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;
                importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.sRGBTexture=true;importer.SaveAndReimport();
            }
            Debug.Log("BARREL_STABLE: rendered "+(StableCatalog.Gear.Count+1)+" catalog thumbnails from real equipped meshes.");
        }
        private static void Render(string id,Transform source,StableSlot? slot)
        {
            var stage=new GameObject("Temporary item photography");stage.transform.position=new Vector3(100,20,100);
            var clone=Object.Instantiate(source.gameObject,stage.transform);clone.name="Photographed item";
            clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;clone.SetActive(true);
            foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            if(slot.HasValue){var profile=StableProfile.Starter();profile.TryEquip(slot.Value,id);clone.GetComponent<StableAppearance>().Apply(profile);}
            var renderers=clone.GetComponentsInChildren<Renderer>(true);Bounds bounds=default;bool first=true;
            foreach(var renderer in renderers) {
                bool visible=renderer.gameObject.activeInHierarchy && (!slot.HasValue || Include(renderer.name,slot.Value));
                renderer.enabled=visible;if(!visible)continue;
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
            }
            if(first){Object.DestroyImmediate(stage);throw new InvalidOperationException("No renderable item geometry: "+id);}
            var camera=new GameObject("Item camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,true);camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.032f,.031f,1);camera.cullingMask=1<<31;
            camera.orthographic=true;camera.nearClipPlane=.02f;camera.farClipPlane=30;camera.allowHDR=false;camera.allowMSAA=true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            // A roster portrait should show the horse's face, rather than shrinking the entire body into the card.
            if(!slot.HasValue) {
                var head=clone.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Bone.002");
                if(head)bounds=new Bounds(head.position+new Vector3(0,-.12f,0),new Vector3(1.30f,1.50f,1.30f));
            }
            var center=bounds.center;float radius=Mathf.Max(bounds.extents.magnitude,.1f);
            Vector3 direction=slot==StableSlot.Gloves?new Vector3(.36f,.10f,1):new Vector3(1.2f,.65f,1.8f);
            camera.transform.position=center+direction.normalized*radius*4;camera.transform.LookAt(center);
            // Project all bounds corners into the actual product camera before fitting its view.
            float halfHeight=0;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                var p=camera.transform.InverseTransformPoint(center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                halfHeight=Mathf.Max(halfHeight,Mathf.Abs(p.y),Mathf.Abs(p.x)/(256f/192));
            }
            camera.orthographicSize=halfHeight*1.12f;
            var key=new GameObject("Item key").AddComponent<Light>();key.transform.SetParent(stage.transform,false);key.type=LightType.Directional;key.intensity=2.0f;key.color=new Color(1,.9f,.78f);key.cullingMask=1<<31;key.transform.rotation=Quaternion.Euler(35,-30,0);
            var fill=new GameObject("Item fill").AddComponent<Light>();fill.transform.SetParent(stage.transform,false);fill.type=LightType.Point;fill.intensity=6;fill.color=new Color(.82f,.88f,1);fill.range=radius*8;fill.cullingMask=1<<31;fill.transform.position=center+new Vector3(-.4f,.5f,1)*radius*2;
            var target=new RenderTexture(256,192,24);var image=new Texture2D(256,192,TextureFormat.RGB24,false);var active=RenderTexture.active;
            try {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,192),0,0);image.Apply();
                File.WriteAllBytes(Root+"/"+id+".png",image.EncodeToPNG());
            }
            finally {camera.targetTexture=null;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(target);Object.DestroyImmediate(stage);}
        }
        private static bool Include(string name,StableSlot slot)
        {
            switch(slot) {
                case StableSlot.Saddle:return name=="Contoured western leather" || name=="Saddle hardware";
                case StableSlot.Pad:return name=="Woven saddle pad" || name=="Woven blanket stripes";
                case StableSlot.Reins:return name=="Left braided rein" || name=="Right braided rein";
                case StableSlot.Headstall:return name=="Fitted leather headstall" || name=="Bit rings and buckles" || name=="Headstall stitching";
                case StableSlot.Gloves:return name=="Inspect glove shell" || name=="Inspect glove seams";
                default:return false;
            }
        }
    }
}
