using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using BarrelRivals.Practice;
using BarrelRivals.Core.Reins;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original arena geometry with licensed photographic materials. Presentation only.</summary>
    public static class ReinsPremiumArenaBuilder
    {
        public const string Root = "Assets/_Project/Art/Reins/Premium";
        private const string Textures = Root + "/Textures/";
        private static Transform world;
        private static Material soil, wood, iron, steel, rails, dark, cream, red, lamp, distant;
        private static readonly Dictionary<Material, Geometry> batches = new Dictionary<Material, Geometry>();

        public static void Apply(Camera camera, Transform horse, Transform[] barrels, Renderer[] patches)
        {
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Meshes"); AssetDatabase.Refresh();
            soil = Pbr("Arena soil", "ArenaSoil_Albedo_2K.png", "ArenaSoil_NormalGL_2K.png", "ArenaSoil_Roughness_1K.jpg", null, Color.white, 0);
            wood = Pbr("Weathered wood", "WeatheredWood_Albedo_2K.jpg", "WeatheredWood_NormalGL_1K.png", "WeatheredWood_Roughness_1K.jpg", null, Color.white, 0);
            iron = Pbr("Corrugated galvanized roof", "CorrugatedIron_Albedo_1K.jpg", "CorrugatedIron_NormalGL_1K.png", "CorrugatedIron_Roughness_1K.jpg", "CorrugatedIron_Metallic_1K.jpg", new Color(.72f,.73f,.7f), .8f);
            steel = Pbr("Worn painted steel", "PaintedSteel_Albedo_1K.jpg", "PaintedSteel_NormalGL_1K.png", "PaintedSteel_Roughness_1K.jpg", null, new Color(.52f,.55f,.59f), .55f);
            rails = ReinsRailMaterialBuilder.Get(iron);
            dark = Solid("Arena charcoal canvas", new Color(.028f,.037f,.043f), .15f);
            cream = Solid("Warm ivory enamel", new Color(.82f,.76f,.61f), .37f);
            red = Solid("Oxide red enamel", new Color(.44f,.038f,.019f), .4f, .25f);
            lamp = Solid("Floodlight emissive glass", new Color(.95f,.74f,.43f), .45f);
            // URP 17.6 validates emission from GI flags. Enabling only the keyword
            // is transient: reimport otherwise strips the intended lamp glow.
            lamp.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.64f,.29f)*2.3f);EditorUtility.SetDirty(lamp);
            distant = Pbr("Mountain shale", "ArenaSoil_Albedo_2K.png", "ArenaSoil_NormalGL_2K.png", "ArenaSoil_Roughness_1K.jpg", null, new Color(.42f,.48f,.57f), 0);
            DrySurfaceFinish();
            foreach (string name in new[] { "Practice presentation", "Reins arena detail", "Premium rodeo arena" })
            { var old = GameObject.Find(name); if (old) Object.DestroyImmediate(old); }
            world = new GameObject("Premium rodeo arena").transform; batches.Clear();
            var originalGround = GameObject.Find("Arena dirt"); if (originalGround) originalGround.GetComponent<Renderer>().enabled = false;
            Ground(patches);
            Stands(); Fences(); Alley(); Structures(); Mountains();
            Flush();
            ReinsCrowdBuilder.Build(world);
            BuildBarrels(barrels);
            CharacterMaterials(horse);
            Lighting(camera);
            AssetDatabase.SaveAssets();
        }

        private static void Ground(Renderer[] patches)
        {
            // Six existing surface-rule regions share world-space UVs and the same photographic tile scale.
            // Relief is only centimetres and has no collider; competitive contact remains in Core.
            for (int i=0;i<patches.Length;i++)
            {
                var patch = patches[i];var bounds = patch.bounds;
                float x0=bounds.min.x,x1=bounds.max.x,z0=bounds.min.z,z1=bounds.max.z;
                var center = new Vector3((x0+x1)*.5f, 0, (z0+z1)*.5f);
                var mesh = GroundMesh(x0,x1,z0,z1,1,1.3f);
                var vertices = mesh.vertices;
                for (int v=0;v<vertices.Length;v++) vertices[v] -= center;
                mesh.vertices=vertices; mesh.RecalculateBounds();
                patch.transform.SetPositionAndRotation(center,Quaternion.identity);patch.transform.localScale=Vector3.one;
                patch.GetComponent<MeshFilter>().sharedMesh=SaveMesh("Footing surface " + i, mesh);
                patch.sharedMaterial=soil;patch.receiveShadows=true;
            }
            // An outer apron meets the same UV origin; the course patches above remain independently tintable.
            AddGround(-110,-28,-100,160);AddGround(28,110,-100,160);AddGround(-28,28,-100,-16);AddGround(-28,28,64,160);
        }
        private static void AddGround(float x0,float x1,float z0,float z1)
        { Group(soil).Append(GroundMesh(x0,x1,z0,z1,3,1.3f),Matrix4x4.identity); }
        private static Mesh GroundMesh(float x0,float x1,float z0,float z1,float step,float tile)
        {
            var g=new Geometry();int nx=Mathf.CeilToInt((x1-x0)/step),nz=Mathf.CeilToInt((z1-z0)/step);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {
                float px=Mathf.Lerp(x0,x1,x/(float)nx),pz=Mathf.Lerp(z0,z1,z/(float)nz);
                float rough=Mathf.PerlinNoise(px*.75f+91,pz*.75f+26)*.022f-.014f;
                float rake=Mathf.Sin(px*8.5f+pz*.19f)*.003f;
                g.vertices.Add(new Vector3(px,rough+rake,pz));g.uv.Add(new Vector2(px/tile,pz/tile));
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++) {int a=z*(nx+1)+x,b=a+nx+1;g.triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            return g.Mesh();
        }
        private static void Fences()
        {
            Fence(new Vector3(-28,0,-16),new Vector3(-28,0,64));Fence(new Vector3(28,0,-16),new Vector3(28,0,64));
            Fence(new Vector3(-28,0,64),new Vector3(28,0,64));Fence(new Vector3(-28,0,-16),new Vector3(-6,0,-16));Fence(new Vector3(6,0,-16),new Vector3(28,0,-16));
            for(int side=-1;side<=1;side+=2)for(int s=0;s<6;s++)
            {
                float z=3+s*10;
                Box(new Vector3(side*27.84f,.92f,z),new Vector3(.065f,1.23f,7.5f),dark);
                for(int j=0;j<5;j++)
                    Tube(new Vector3(side*27.76f,.45f,z-3+j*1.5f),new Vector3(side*27.76f,.52f,z-3+j*1.5f),.02f,cream,6);
                Label(s%2==0?"BARREL RIVALS":"DUST & GLORY",new Vector3(side*27.76f,1.0f,z),Quaternion.Euler(0,side*90,0),.5f,6.6f,Color.white);
            }
        }
        private static void Fence(Vector3 a,Vector3 b)
        {
            int panels=Mathf.CeilToInt(Vector3.Distance(a,b)/3.2f);
            for(int i=0;i<=panels;i++) {var p=Vector3.Lerp(a,b,i/(float)panels);Tube(p,p+Vector3.up*1.7f,.045f,rails,10);}
            for(int r=0;r<5;r++){var up=Vector3.up*(.24f+r*.32f);Tube(a+up,b+up,.029f,rails,8);}
        }
        private static void Alley()
        {
            // These visible centerlines use the same swept-capsule bounds as Core.
            // No Unity collider or decorative timing line can decide contact or the start.
            foreach(int side in new[]{-1,1})
            {
                float x=side*(float)ReinsAlley.HalfWidth;
                float back=(float)ReinsAlley.BackZ,front=(float)ReinsAlley.FrontZ;
                float radius=(float)ReinsAlley.RailRadius;
                for(int row=0;row<5;row++)
                { float y=.25f+row*.32f;Tube(new Vector3(x,y,back),new Vector3(x,y,front),radius,rails,12); }
                for(int post=0;post<=4;post++)
                {
                    float z=Mathf.Lerp(back,front,post/4f);
                    Tube(new Vector3(x,0,z),new Vector3(x,1.76f,z),radius,rails,12);
                }
            }
        }
        private static void Stands()
        {
            foreach(int side in new[]{-1,1})
            {
                for(int row=0;row<5;row++)
                {
                    float x=side*(31+row*1.55f),y=.4f+row*.53f;
                    Box(new Vector3(x,y,29),new Vector3(1.5f,.2f,48),wood);
                    Box(new Vector3(x+side*.40f,y+.35f,29),new Vector3(.065f,.42f,48),wood);
                    for(int p=0;p<9;p++)Box(new Vector3(x,Mathf.Max(.06f,y*.5f),6+p*5.7f),new Vector3(.10f,Mathf.Max(.12f,y),.10f),steel);
                }
                for(int p=0;p<8;p++)
                {
                    float z=2+p*7.8f;var back=new Vector3(side*40,0,z);var front=new Vector3(side*29.1f,0,z);
                    Tube(back,back+Vector3.up*7.9f,.082f,steel,12);
                    Tube(front,front+Vector3.up*6.3f,.066f,steel,12);
                    Tube(front+Vector3.up*6.3f,back+Vector3.up*7.9f,.06f,steel,10);
                    Tube(front+Vector3.up*5.8f,back+Vector3.up*7.35f,.04f,steel,8);
                    for(int cross=0;cross<5;cross++)
                    {
                        var a=Vector3.Lerp(front,back,cross/5f)+Vector3.up*Mathf.Lerp(5.8f,7.35f,cross/5f);
                        var b=Vector3.Lerp(front,back,(cross+1)/5f)+Vector3.up*Mathf.Lerp(6.3f,7.9f,(cross+1)/5f);
                        Tube(a,b,.028f,steel,6);
                    }
                }
                // Corrugation is real silhouette geometry as well as a matched scanned normal map.
                var roofMesh=new Geometry();int across=504;
                for(int i=0;i<=across;i++)for(int edge=0;edge<2;edge++)
                {
                    float t=i/(float)across,x=side*Mathf.Lerp(28.3f,41.0f,t),y=Mathf.Lerp(6.18f,8.06f,t)+Mathf.Sin(t*Mathf.PI*2*63)*.026f;
                    roofMesh.vertices.Add(new Vector3(x,y,edge==0?-.8f:58.0f));roofMesh.uv.Add(new Vector2(t*12.7f/1.1f,edge*58.8f/1.1f));
                    if(i<across){int a=i*2;if(side>0)roofMesh.triangles.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});else roofMesh.triangles.AddRange(new[]{a,a+2,a+1,a+2,a+3,a+1});}
                }
                Group(iron).Append(roofMesh.Mesh(),Matrix4x4.identity);
                Tube(new Vector3(side*28.3f,6.18f,-.8f),new Vector3(side*28.3f,6.18f,58),.067f,steel,10);
                // Decorative pennants stay high and outside the riding bounds.
                for(int f=0;f<25;f++)
                {
                    float z=f*2.3f;var g=Group(f%3==0?cream:red);float x=side*28.15f;
                    g.Triangle(new Vector3(x,2.7f,z),new Vector3(x,2.7f,z+1),new Vector3(x,2.18f,z+.5f),new Vector2(0,0),new Vector2(1,0),new Vector2(.5f,1));
                }
            }
        }
        private static void Structures()
        {
            foreach(int side in new[]{-1,1})for(int tower=0;tower<3;tower++)
            {
                float x=side*29.8f,z=2+tower*27;
                Tube(new Vector3(x,0,z),new Vector3(x,12.8f,z),.11f,steel,12);
                var center=new Vector3(x,12.3f,z);
                var facing=Quaternion.LookRotation(new Vector3(-side,-.35f,0).normalized,Vector3.up);
                Box(center,new Vector3(3.3f,1.75f,.14f),steel,facing);
                for(int c=0;c<5;c++)for(int r=0;r<3;r++)
                {
                    var p=center+facing*new Vector3(-1.22f+c*.61f,-.6f+r*.58f,.13f);
                    Box(p,new Vector3(.51f,.48f,.27f),dark,facing);
                    Box(p+facing*Vector3.forward*.145f,new Vector3(.40f,.35f,.025f),lamp,facing);
                }
            }
            // An elevated announcer booth anchors the far end without blocking barrels.
            Box(new Vector3(0,2.7f,70),new Vector3(6.8f,5.4f,4),wood);
            Box(new Vector3(0,6.25f,70),new Vector3(7.2f,1.5f,4.3f),dark);
            for(int p=-3;p<=3;p++)Tube(new Vector3(p,5.55f,67.8f),new Vector3(p,7.1f,67.8f),.04f,steel,8);
            Box(new Vector3(0,7.2f,70),new Vector3(7.8f,.16f,4.8f),iron);
            Label("BARREL RIVALS",new Vector3(0,4.6f,67.94f),Quaternion.identity,.60f,6,Color.white);
            Label("RIDE WITH HEART",new Vector3(0,3.7f,67.94f),Quaternion.identity,.35f,5.7f,new Color(.86f,.71f,.44f));
            foreach(int side in new[]{-1,1})
            {
                Tube(new Vector3(side*6.6f,0,-1),new Vector3(side*6.6f,5.1f,-1),.11f,steel,10);
                for(int a=0;a<6;a++)Box(new Vector3(side*6.6f,.8f+a*.7f,-1),new Vector3(.32f,.08f,.22f),cream);
            }
            Box(new Vector3(0,4.78f,-1),new Vector3(13.4f,.86f,.2f),dark);
            Label("B A R R E L   R I V A L S",new Vector3(0,4.82f,-1.12f),Quaternion.identity,.53f,11.9f,new Color(.94f,.79f,.52f));
            Label("B A R R E L   R I V A L S",new Vector3(0,4.82f,-.88f),Quaternion.Euler(0,180,0),.53f,11.9f,new Color(.94f,.79f,.52f));
        }
        private static void Mountains()
        {
            ReinsMountainBuilder.Build(world);
        }
        private static void BuildBarrels(Transform[] barrels)
        {
            for(int index=0;index<barrels.Length;index++)
            {
                var barrel=barrels[index];foreach(var r in barrel.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                var old=barrel.Find("Detailed painted drum");if(old)Object.DestroyImmediate(old.gameObject);
                var parent=new GameObject("Detailed painted drum").transform;parent.SetParent(barrel,false);
                var bands=new[]{new Vector2(0,.305f),new Vector2(.025f,.309f),new Vector2(.05f,.300f),new Vector2(.21f,.300f),new Vector2(.23f,.308f),new Vector2(.245f,.309f),new Vector2(.26f,.3f),new Vector2(.59f,.3f),new Vector2(.615f,.309f),new Vector2(.632f,.308f),new Vector2(.65f,.30f),new Vector2(.855f,.30f),new Vector2(.88f,.309f),new Vector2(.898f,.309f)};
                var groups=new[]{new Geometry(),new Geometry(),new Geometry()};const int n=64;
                for(int row=0;row<bands.Length-1;row++)
                {
                    var g=groups[bands[row].x<.26f?0:bands[row].x<.65f?1:2];
                    for(int i=0;i<n;i++)
                    {
                        float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                        Vector3 P(float angle,Vector2 band)=>new Vector3(Mathf.Sin(angle)*band.y,band.x,Mathf.Cos(angle)*band.y);
                        g.Quad(P(a,bands[row]),P(b,bands[row]),P(b,bands[row+1]),P(a,bands[row+1]),new Vector2(i/(float)n, bands[row].x),new Vector2((i+1)/(float)n,bands[row].x),new Vector2((i+1)/(float)n,bands[row+1].x),new Vector2(i/(float)n,bands[row+1].x));
                    }
                }
                var mats=new[]{steel,cream,red};for(int i=0;i<3;i++)MeshObject(parent,"Enamel drum band "+i,SaveMesh("Drum band "+i,groups[i].Mesh()),mats[i],false);
                var top=new Geometry();for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;top.Triangle(new Vector3(0,.89f,0),new Vector3(Mathf.Sin(a)*.30f,.89f,Mathf.Cos(a)*.30f),new Vector3(Mathf.Sin(b)*.30f,.89f,Mathf.Cos(b)*.30f),new Vector2(.5f,.5f),new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.5f+Vector2.one*.5f,new Vector2(Mathf.Sin(b),Mathf.Cos(b))*.5f+Vector2.one*.5f);}
                MeshObject(parent,"Sealed barrel lid",SaveMesh("Barrel lid",top.Mesh()),red,false);
                foreach(int angle in new[]{0,90,180,270})
                {
                    var direction=Quaternion.Euler(0,angle,0)*Vector3.back;
                    Label("BR",barrel.position+direction*.310f+Vector3.up*.47f,Quaternion.Euler(0,angle,0),.13f,.30f,new Color(.07f,.10f,.13f),parent);
                }
            }
        }
        private static void CharacterMaterials(Transform horse)
        {
            var body=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Bay horse coat.mat");
            if(!body){body=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(ReinsReferenceArtBuilder.Root+"/Materials/Horse coat.mat"));body.name="Bay horse coat";body.SetColor("_BaseColor",new Color(.70f,.56f,.46f));body.SetFloat("_Smoothness",.43f);body.SetFloat("_BumpScale",.30f);AssetDatabase.CreateAsset(body,Root+"/Materials/Bay horse coat.mat");}
            // Broad plastic highlights obscure the textured coat. Keep a restrained
            // fur sheen without changing the original color/normal source maps.
            body.SetFloat("_Smoothness",.28f);EditorUtility.SetDirty(body);
            var hair=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Dark horse mane.mat");
            if(!hair){hair=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(ReinsReferenceArtBuilder.Root+"/Materials/Horse mane and tail.mat"));hair.name="Dark horse mane";hair.SetColor("_BaseColor",new Color(.13f,.09f,.065f));hair.SetFloat("_Smoothness",.37f);AssetDatabase.CreateAsset(hair,Root+"/Materials/Dark horse mane.mat");}
            foreach(var r in horse.GetComponentsInChildren<Renderer>(true))
                if(r.name.StartsWith("HorseBody"))r.sharedMaterial=body;else if(r.name.StartsWith("HorseHair"))r.sharedMaterial=hair;
        }
        private static void Lighting(Camera camera)
        {
            var skyTexture=Import("DuskSky_2K.hdr",false,true);string path=Root+"/Materials/Photographic dusk sky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!sky){sky=new Material(Shader.Find("Skybox/Panoramic"));sky.SetTexture("_MainTex",skyTexture);sky.SetFloat("_Exposure",.48f);sky.SetFloat("_Rotation",75);AssetDatabase.CreateAsset(sky,path);}
            sky.SetFloat("_Exposure",.48f);EditorUtility.SetDirty(sky);
            RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.58f,.65f,.76f);RenderSettings.ambientEquatorColor=new Color(.50f,.45f,.38f);RenderSettings.ambientGroundColor=new Color(.26f,.20f,.14f);
            RenderSettings.reflectionIntensity=.65f;
            RenderSettings.sun.color=new Color(1,.90f,.76f);RenderSettings.sun.intensity=2.5f;RenderSettings.sun.transform.rotation=Quaternion.Euler(32,-32,0);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=180;RenderSettings.fogEndDistance=1400;RenderSettings.fogColor=new Color(.52f,.56f,.61f);
            camera.farClipPlane=1000;camera.allowHDR=true;
            var source=(UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
            string pipelinePath=Root+"/Premium mobile pipeline.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(!pipeline){pipeline=Object.Instantiate(source);pipeline.supportsHDR=true;pipeline.msaaSampleCount=2;pipeline.shadowDistance=68;AssetDatabase.CreateAsset(pipeline,pipelinePath);}
            // A camera/Volume alone does not enable URP post processing. The legacy
            // Foundation renderer has no PostProcessData; retain it as history and
            // give this pipeline its own persistent renderer with installed resources.
            var pipelineData=new SerializedObject(pipeline);
            var rendererList=pipelineData.FindProperty("m_RendererDataList");
            var defaultRenderer=pipelineData.FindProperty("m_DefaultRendererIndex");
            string rendererPath=Root+"/Premium mobile renderer.asset";
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if(!renderer)
            {
                int index=defaultRenderer.intValue;
                var template=index>=0 && index<rendererList.arraySize
                    ? rendererList.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData : null;
                if(!template)throw new InvalidOperationException("The premium pipeline requires a valid Universal Renderer template.");
                renderer=Object.Instantiate(template);renderer.name="Premium mobile renderer";
                AssetDatabase.CreateAsset(renderer,rendererPath);
            }
            // This is the same package-relative asset used by URP17.6's renderer
            // creation API, not a guessed GUID or a transient ScriptableObject.
            const string postPath="Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
            var post=AssetDatabase.LoadAssetAtPath<PostProcessData>(postPath);
            if(!post || post.shaders==null || !post.shaders.lutBuilderHdrPS || !post.shaders.uberPostPS || !post.shaders.finalPostPassPS)
                throw new InvalidOperationException("Installed URP post-processing resources are missing: "+postPath);
            renderer.postProcessData=post;renderer.SetDirty();EditorUtility.SetDirty(renderer);
            rendererList.arraySize=1;rendererList.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            defaultRenderer.intValue=0;pipelineData.ApplyModifiedPropertiesWithoutUndo();
            pipeline.colorGradingMode=ColorGradingMode.HighDynamicRange;EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline=pipeline;
            int active=QualitySettings.GetQualityLevel();for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}QualitySettings.SetQualityLevel(active,false);
            var volume=GameObject.Find("Reins reference grade").GetComponent<Volume>();path=Root+"/Premium dusk grade.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);
            }
            // Generation must migrate an existing saved profile as well as create one.
            if(!profile.TryGet<ColorAdjustments>(out var grade))
            { grade=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(grade,profile); }
            grade.postExposure.Override(.35f);grade.contrast.Override(7);grade.saturation.Override(2);EditorUtility.SetDirty(grade);
            if(!profile.TryGet<Tonemapping>(out var tone))
            { tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile); }
            tone.mode.Override(TonemappingMode.ACES);EditorUtility.SetDirty(tone);
            volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
            var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
            cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        }
        public static Material Pbr(string name,string albedo,string normal,string roughness,string metal,Color tint,float metallic)
        {
            string path=Root+"/Materials/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            material.SetTexture("_BaseMap",Import(albedo,false,false));material.SetColor("_BaseColor",tint);
            material.SetTexture("_BumpMap",Import(normal,true,false));material.SetFloat("_BumpScale",.65f);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",PackSurface(name,roughness,metal,metallic));material.SetFloat("_Smoothness",1);material.SetFloat("_Metallic",metallic);material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,path);return material;
        }
        private static void DrySurfaceFinish()
        {
            // The measured source roughness averages only .53. At full smoothness
            // multiplier it reads as wet earth at grazing angles, rather than dry footing.
            // Scale its existing roughness-derived smoothness; never rewrite source maps.
            soil.SetFloat("_Smoothness",.34f);soil.SetFloat("_BumpScale",.78f);
            distant.SetFloat("_Smoothness",.12f);distant.SetFloat("_BumpScale",.12f);
            distant.SetTextureScale("_BaseMap",Vector2.one*.18f);
            // Macro variation breaks up the 1.3m photograph without enlarging its clods.
            // URP's installed LitInput multiplies albedo by 2*detail (linear .5 is neutral).
            const int size=128;string path=Root+"/Materials/Original arena macro variation.asset";
            var map=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!map){map=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="Original arena macro variation"};AssetDatabase.CreateAsset(map,path);}
            var pixels=new Color[size*size];
            float Seamless(float u,float v,float scale)
            {
                float Sample(float x,float y)=>Mathf.PerlinNoise(x*scale+31.7f,y*scale+17.3f);
                return Mathf.Lerp(Mathf.Lerp(Sample(u,v),Sample(u-1,v),u),Mathf.Lerp(Sample(u,v-1),Sample(u-1,v-1),u),v);
            }
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)(size-1),v=y/(float)(size-1);
                float n=Seamless(u,v,4)*.72f+Seamless(u,v,11)*.28f;
                float value=.5f+(n-.5f)*.5f;pixels[y*size+x]=new Color(value,value,value,1);
            }
            map.SetPixels(pixels);map.wrapMode=TextureWrapMode.Repeat;map.filterMode=FilterMode.Trilinear;map.Apply(true,false);EditorUtility.SetDirty(map);
            soil.SetTexture("_DetailAlbedoMap",map);soil.SetTextureScale("_DetailAlbedoMap",Vector2.one*(1.3f/42));
            soil.SetFloat("_DetailAlbedoMapScale",1);soil.SetFloat("_DetailNormalMapScale",0);
            soil.DisableKeyword("_DETAIL_SCALED");soil.EnableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(soil);EditorUtility.SetDirty(distant);
        }
        private static Texture2D Import(string file,bool normal,bool data,bool readable=false)
        {
            string path=Textures+file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new FileNotFoundException("Missing reviewed material source",path);
            var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;bool srgb=!normal&&!data;
            // Panoramic's implicit gradients cross its wrapped longitude. Mipped sampling
            // produced a dashed white seam on Metal even with no world geometry rendered.
            // The sky is always magnified at this 2K/mobile field of view; surface maps retain mips.
            bool sky=file=="DuskSky_2K.hdr";bool mips=!sky;int anisotropy=sky?1:8;
            if(importer.textureType!=type || importer.sRGBTexture!=srgb || importer.isReadable!=readable || importer.maxTextureSize!=2048 || importer.anisoLevel!=anisotropy || importer.mipmapEnabled!=mips)
            {importer.textureType=type;importer.sRGBTexture=srgb;importer.isReadable=readable;importer.maxTextureSize=2048;importer.mipmapEnabled=mips;importer.anisoLevel=anisotropy;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Texture2D PackSurface(string name,string roughness,string metallic,float metalDefault)
        {
            string path=Root+"/Materials/"+name+" surface.png";var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(existing)return existing;
            // Channel conversion for URP: source maps remain untouched. Masks are linear data, not color images.
            var rough=Import(roughness,false,true,true);var metal=metallic==null?null:Import(metallic,false,true,true);
            const int size=1024;var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true);var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {float u=(x+.5f)/size,v=(y+.5f)/size;pixels[y*size+x]=new Color(metal?metal.GetPixelBilinear(u,v).r:metalDefault,0,0,1-rough.GetPixelBilinear(u,v).r);}
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            Import(roughness,false,true);if(metallic!=null)Import(metallic,false,true);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Solid(string name,Color color,float smooth,float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);m.SetFloat("_Cull",0);AssetDatabase.CreateAsset(m,path);return m;
        }
        private static void Label(string value,Vector3 position,Quaternion rotation,float height,float maxWidth,Color color,Transform parent=null)
        {
            var go=new GameObject("Arena lettering "+value);go.transform.SetParent(parent?parent:world,false);go.transform.SetPositionAndRotation(position,rotation);
            var text=go.AddComponent<TextMesh>();text.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Cinzel-SemiBold.ttf")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=.1f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;text.text=value;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            go.AddComponent<ReinsArenaLettering>().Configure(text.font,Shader.Find("BarrelRivals/World Lettering"));
            var size=renderer.bounds.size;float currentHeight=Mathf.Max(size.y,.01f),width=Mathf.Max(size.x,size.z);go.transform.localScale=Vector3.one*Mathf.Min(height/currentHeight,maxWidth/Mathf.Max(width,.01f));
        }
        private static Geometry Group(Material m){if(!batches.TryGetValue(m,out var g)){g=new Geometry();batches.Add(m,g);}return g;}
        private static void Box(Vector3 p,Vector3 size,Material m,Quaternion? rotation=null)
        {
            var g=Group(m);var r=rotation??Quaternion.identity;Vector3 P(float x,float y,float z)=>p+r*Vector3.Scale(new Vector3(x,y,z),size);
            g.Quad(P(-.5f,-.5f,-.5f),P(-.5f,.5f,-.5f),P(.5f,.5f,-.5f),P(.5f,-.5f,-.5f),new Vector2(0,0),new Vector2(0,size.y/1.8f),new Vector2(size.x/1.8f,size.y/1.8f),new Vector2(size.x/1.8f,0));
            g.Quad(P(.5f,-.5f,.5f),P(.5f,.5f,.5f),P(-.5f,.5f,.5f),P(-.5f,-.5f,.5f),Vector2.zero,new Vector2(0,size.y/1.8f),new Vector2(size.x/1.8f,size.y/1.8f),new Vector2(size.x/1.8f,0));
            g.Quad(P(-.5f,.5f,-.5f),P(-.5f,.5f,.5f),P(.5f,.5f,.5f),P(.5f,.5f,-.5f),Vector2.zero,new Vector2(0,size.z/1.8f),new Vector2(size.x/1.8f,size.z/1.8f),new Vector2(size.x/1.8f,0));
            g.Quad(P(-.5f,-.5f,.5f),P(-.5f,-.5f,-.5f),P(.5f,-.5f,-.5f),P(.5f,-.5f,.5f),Vector2.zero,new Vector2(0,size.z/1.8f),new Vector2(size.x/1.8f,size.z/1.8f),new Vector2(size.x/1.8f,0));
            g.Quad(P(-.5f,-.5f,.5f),P(-.5f,.5f,.5f),P(-.5f,.5f,-.5f),P(-.5f,-.5f,-.5f),Vector2.zero,new Vector2(0,size.y/1.8f),new Vector2(size.z/1.8f,size.y/1.8f),new Vector2(size.z/1.8f,0));
            g.Quad(P(.5f,-.5f,-.5f),P(.5f,.5f,-.5f),P(.5f,.5f,.5f),P(.5f,-.5f,.5f),Vector2.zero,new Vector2(0,size.y/1.8f),new Vector2(size.z/1.8f,size.y/1.8f),new Vector2(size.z/1.8f,0));
        }
        private static void Tube(Vector3 a,Vector3 b,float radius,Material material,int sides)
        {
            var g=Group(material);var direction=(b-a).normalized;var cross=Vector3.Cross(direction,Vector3.up);if(cross.sqrMagnitude<.1f)cross=Vector3.Cross(direction,Vector3.right);cross.Normalize();var up=Vector3.Cross(direction,cross);float length=(b-a).magnitude;
            for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,u=(i+1)*Mathf.PI*2/sides;var c=(cross*Mathf.Cos(t)+up*Mathf.Sin(t))*radius;var d=(cross*Mathf.Cos(u)+up*Mathf.Sin(u))*radius;g.Quad(a+c,a+d,b+d,b+c,Vector2.zero,new Vector2(radius*Mathf.PI*2/sides,0),new Vector2(radius*Mathf.PI*2/sides,length),new Vector2(0,length));}
        }
        private static void Flush(){foreach(var item in batches)MeshObject(world,"Arena mesh "+item.Key.name,SaveMesh("Arena "+item.Key.name,item.Value.Mesh()),item.Key);}
        private static void MeshObject(Transform parent,string name,Mesh mesh,Material material,bool isStatic=true)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.isStatic=isStatic;}
        private static Mesh SaveMesh(string name,Mesh mesh)
        {
            foreach(var vertex in mesh.vertices)
                if(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.x)||float.IsInfinity(vertex.y)||float.IsInfinity(vertex.z))
                    throw new InvalidOperationException("Invalid generated mesh vertex: "+name);
            mesh.name=name;return PersistentMeshAsset.Save(mesh,Root+"/Meshes/"+name+".asset");}
        private sealed class Geometry
        {
            public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> triangles=new List<int>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc){int n=vertices.Count;vertices.AddRange(new[]{a,b,c});uv.AddRange(new[]{ua,ub,uc});triangles.AddRange(new[]{n,n+1,n+2});}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{ua,ub,uc,ud});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public Mesh Mesh(){var m=new Mesh{indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
            public void Append(Mesh mesh,Matrix4x4 matrix){int n=vertices.Count;foreach(var v in mesh.vertices)vertices.Add(matrix.MultiplyPoint3x4(v));uv.AddRange(mesh.uv);foreach(int t in mesh.triangles)triangles.Add(n+t);Object.DestroyImmediate(mesh);}
        }
    }
}
