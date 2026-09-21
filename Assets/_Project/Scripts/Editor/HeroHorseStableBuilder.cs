using System;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Matching showroom for the full-course development rig. No player-scene replacement.</summary>
    public static class HeroHorseStableBuilder
    {
        public const string Root=HeroHorseBenchmarkBuilder.Root+"/Stable";
        public const string ScenePath=Root+"/HeroHorseStableReview.unity";
        public const string Thumbnails=Root+"/Thumbnails";

        [MenuItem("Barrel Rivals/Development/Build matching horse stable review")]
        public static void BuildInEditor()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            HeroHorseRaceBuilder.Build();
            var scene=EditorSceneManager.OpenScene(StableBuilder.ScenePath);
            var actor=GameObject.Find("Copper").transform;var camera=Camera.main;
            if(PrefabUtility.IsPartOfPrefabInstance(actor))
                PrefabUtility.UnpackPrefabInstance(actor.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var oldChildren=Enumerable.Range(0,actor.childCount).Select(actor.GetChild).ToArray();
            var review=EditorSceneManager.OpenScene(HeroHorseRaceBuilder.ScenePath,OpenSceneMode.Additive);
            var original=review.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HorseRigBindings>(true)).Single();
            var clone=Object.Instantiate(original.gameObject);clone.name="Fitted stable horse";
            SceneManager.MoveGameObjectToScene(clone,scene);clone.transform.SetParent(actor,false);
            clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;
            var horse=clone.GetComponent<HorseRigBindings>();var driver=clone.GetComponent<HeroHorseLocomotion>();
            driver.source=null;driver.attachments=null;driver.targetSpeed=0;
            var attachments=clone.GetComponent<HeroHorseAttachments>();attachments.source=null;
            Object.DestroyImmediate(attachments);
            EditorSceneManager.CloseScene(review,true);SceneManager.SetActiveScene(scene);
            foreach(var child in oldChildren)Object.DestroyImmediate(child.gameObject);
            horse.Animator.enabled=false;
            AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroHorseBenchmarkBuilder.Root+"/Neutral.anim").SampleAnimation(horse.Animator.gameObject,0);
            foreach(var child in horse.ModelSpace.GetComponentsInChildren<Transform>(true))
                if(child && (child.name=="Fitted western rider" || child.name=="Left fitted grip" || child.name=="Right fitted grip"))
                    Object.DestroyImmediate(child.gameObject);
            foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=ShadowCastingMode.On;
            StowReins(horse);
            horse.Animator.enabled=true;

            var appearance=actor.GetComponent<StableAppearance>();
            var bindings=clone.GetComponentsInChildren<MeshRenderer>().Select(renderer=>
            {
                StableSlot slot;
                switch(renderer.name)
                {
                    case "Western saddle":slot=StableSlot.Saddle;break;
                    case "Woven saddle pad":slot=StableSlot.Pad;break;
                    case "Fitted left rein":case "Fitted right rein":slot=StableSlot.Reins;break;
                    case "Fitted leather headstall":slot=StableSlot.Headstall;break;
                    default:return null;
                }
                return new StableAppearance.Binding{renderer=renderer,materialIndex=slot==StableSlot.Reins?1:0,slot=slot};
            }).Where(b=>b!=null).ToArray();
            if(bindings.Length!=5)throw new InvalidOperationException("Incomplete candidate showroom tack bindings.");
            appearance.Configure(bindings,StableWardrobeArtBuilder.Palettes());appearance.Apply(StableProfile.Starter());
            StableShowroomBuilder.FrameHorse(camera,actor);
            var rider=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StableAppearance>(true))
                .Single(a=>a.gameObject.name=="Rider glove preview").transform;
            StableThumbnailBuilder.Generate(actor,rider,Thumbnails);
            foreach(var sprite in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Image>(true)))
            {
                if(!sprite.sprite)continue;
                string path=AssetDatabase.GetAssetPath(sprite.sprite);
                if(!path.StartsWith(StableThumbnailBuilder.Root+"/",StringComparison.Ordinal))continue;
                var replacement=AssetDatabase.LoadAssetAtPath<Sprite>(Thumbnails+"/"+Path.GetFileName(path));
                if(!replacement)throw new InvalidOperationException("Missing candidate product thumbnail for "+path);
                sprite.sprite=replacement;
            }
            var controller=Object.FindFirstObjectByType<StableController>();
            var navigation=new SerializedObject(controller);
            navigation.FindProperty("developmentRaceScene").stringValue=HeroHorseRaceBuilder.ScenePath;
            navigation.ApplyModifiedPropertiesWithoutUndo();
            if(clone.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Showroom art cannot add colliders.");
            if(EditorBuildSettings.scenes.Any(s=>s.path==ScenePath || s.path==HeroHorseRaceBuilder.ScenePath))
                throw new InvalidOperationException("Development review scenes must remain outside mobile build lists.");
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("HERO_HORSE_STABLE_REVIEW_SAVED "+ScenePath);
        }

        static void StowReins(HorseRigBindings horse)
        {
            var model=horse.ModelSpace;var surface=new ReinsHorseSurface(horse.Body,model);
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var horn=nodes.Single(t=>t.name=="Fitted saddle horn");
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name!="Fitted left rein" && filter.name!="Fitted right rein")continue;
                int side=filter.name=="Fitted left rein"?-1:1;
                var bit=nodes.Single(t=>t.name==(side<0?"Left bit anchor":"Right bit anchor"));
                var offset=new Vector3(side*.029f,-.055f,-.012f);
                var start=model.InverseTransformPoint(horn.TransformPoint(offset));
                var end=model.InverseTransformPoint(bit.position);
                var points=new Vector3[41];var minimum=new float[41];
                for(int i=0;i<points.Length;i++)
                {
                    float t=i/40f;var p=Vector3.Lerp(start,end,t)+new Vector3(side*.08f*Mathf.Sin(t*Mathf.PI),-.33f*Mathf.Sin(t*Mathf.PI),0);
                    minimum[i]=side*p.x;
                    if(i>0 && i<40 && surface.TryRay(new Vector3(side*2,p.y,p.z),Vector3.left*side,out var hit))
                        minimum[i]=Mathf.Max(minimum[i],side*hit.Point.x+.029f);
                    p.x=side*minimum[i];points[i]=p;
                }
                for(int pass=0;pass<80;pass++)for(int i=1;i<40;i++)
                {
                    var p=points[i];p.x=side*Mathf.Max(minimum[i],side*(points[i-1].x+points[i+1].x)*.5f);points[i]=p;
                }
                var local=points.Select(p=>filter.transform.InverseTransformPoint(model.TransformPoint(p))).ToArray();
                filter.sharedMesh=PersistentMeshAsset.Save(Cord(local),Root+(side<0?"/Stowed left rein.asset":"/Stowed right rein.asset"));
                StableReinDrapeBuilder.Configure(filter,model,horse.Body,horn,bit,offset,points);
            }
        }

        static Mesh Cord(Vector3[] points)
        {
            const int sides=StableReinDrape.Sides;
            var vertices=new Vector3[points.Length*sides];var uv=new Vector2[vertices.Length];
            var triangles=new int[(points.Length-1)*sides*6];float distance=0;
            for(int i=0;i<points.Length;i++)
            {
                if(i>0)distance+=Vector3.Distance(points[i],points[i-1]);
                var forward=(points[Math.Min(i+1,points.Length-1)]-points[Math.Max(i-1,0)]).normalized;
                var across=Vector3.Cross(Vector3.up,forward).normalized;
                if(across.sqrMagnitude<.01f)across=Vector3.right;
                var up=Vector3.Cross(forward,across).normalized;
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides;int index=i*sides+j;
                    vertices[index]=points[i]+(across*Mathf.Cos(a)+up*Mathf.Sin(a))*StableReinDrape.Radius;
                    uv[index]=new Vector2(j/(float)sides,distance*4);
                    if(i==0)continue;
                    int at=((i-1)*sides+j)*6,old=(i-1)*sides+j,next=(i-1)*sides+(j+1)%sides;
                    triangles[at]=old;triangles[at+1]=old+sides;triangles[at+2]=next;
                    triangles[at+3]=next;triangles[at+4]=old+sides;triangles[at+5]=next+sides;
                }
            }
            var mesh=new Mesh{name="Candidate stowed rein"};mesh.vertices=vertices;mesh.uv=uv;
            mesh.subMeshCount=3;mesh.SetTriangles(Array.Empty<int>(),0);mesh.SetTriangles(triangles,1);mesh.SetTriangles(Array.Empty<int>(),2);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
