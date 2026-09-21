using System;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Fits the existing credited rider and gloves to the candidate's measured saddle anchors.</summary>
    public static class HeroHorseRiderBuilder
    {
        public static HeroHorseAttachments Build(HorseRigBindings horse)
        {
            var model=horse.ModelSpace;
            Transform Anchor(string name)=>model.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
            var horn=Anchor("Fitted saddle horn");
            var hornPoint=model.InverseTransformPoint(horn.position);
            HeroHorseBridleBuilder.Build(horse,out var leftBit,out var rightBit);
            var container=Child(model,"Fitted western rider");
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReinsRiderBodyBuilder.Root+"/WesternRider.fbx"),container);
            foreach(var animator in imported.GetComponentsInChildren<Animator>())Object.DestroyImmediate(animator);
            var nodes=imported.GetComponentsInChildren<Transform>();
            Transform Bone(string name)=>nodes.Single(t=>t.name==name);
            container.localScale=Vector3.one*1.10f;
            if(model.InverseTransformVector(Bone("ball_l").position-Bone("foot_l").position).z<0)
                container.localRotation=Quaternion.Euler(0,180,0);
            var skins=imported.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length!=6)throw new InvalidOperationException("Expected the existing six skinned rider surfaces.");
            foreach(var skin in skins)
            {
                string n=skin.name;
                string material=n.Contains("male_casualsuit")?"Rider shirt and jeans":n.Contains("ankle_boots")?"Rider brown boots":
                    n.Contains("low-poly")?"Rider eyes":n.Contains("ponytail")?"Rider ponytail":n.Contains("hat")?"Rider felt hat":"Rider skin";
                var shared=AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderBodyBuilder.Root+"/"+material+".mat");
                if(!shared)throw new InvalidOperationException("Missing existing rider material: "+material);
                skin.sharedMaterial=shared;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;
            }
            WesternHatBuilder.Apply(skins,model);
            var tack=Child(model,"Fitted riding hands and reins");
            Transform Hand(int side)
            {
                string label=side<0?"Left":"Right";
                var hand=Child(tack,label+" fitted grip");
                hand.position=model.TransformPoint(hornPoint+new Vector3(side*.28f,.13f,.165f));
                hand.rotation=model.rotation*Quaternion.Euler(0,side*8,side*9);
                MeshPart(hand,"Glove shell",AssetDatabase.LoadAssetAtPath<Mesh>(ReinsRiderTackBuilder.Root+"/"+label+" glove shell.asset"),
                    new[]{AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Worn chestnut gloves.mat")});
                return hand;
            }
            var left=Hand(-1);var right=Hand(1);
            var lg=Child(left,"Left closed rein grip");lg.localPosition=new Vector3(-.010f,-.031f,.075f);
            var rg=Child(right,"Right closed rein grip");rg.localPosition=new Vector3(.010f,-.031f,.075f);
            var lw=Child(left,"Left fitted wrist");lw.localPosition=new Vector3(0,0,-.035f);
            var rw=Child(right,"Right fitted wrist");rw.localPosition=new Vector3(0,0,-.035f);
            ReinsRiderBodyPresentation.Limb Limb(string suffix,bool arm,Transform target,int side)=>new ReinsRiderBodyPresentation.Limb{
                upper=Bone((arm?"upperarm_":"thigh_")+suffix),lower=Bone((arm?"lowerarm_":"calf_")+suffix),end=Bone((arm?"hand_":"foot_")+suffix),target=target,
                pole=hornPoint+new Vector3(side*(arm?.6f:.28f),arm?.11f:-.45f,arm?-.4f:.20f)};
            bool leftNegative=model.InverseTransformPoint(Bone("thigh_l").position).x<0;
            string l=leftNegative?"l":"r",r=leftNegative?"r":"l";
            var body=container.gameObject.AddComponent<ReinsRiderBodyPresentation>();
            body.Configure(null,Bone("pelvis"),Anchor("Fitted rider pelvis"),model,nodes.Where(t=>t!=imported.transform).ToArray(),
                Limb(l,true,lw,-1),Limb(r,true,rw,1),Limb(l,false,Anchor("Fitted left ankle"),-1),Limb(r,false,Anchor("Fitted right ankle"),1));
            var adapter=horse.gameObject.AddComponent<HeroHorseAttachments>();
            adapter.horse=horse;adapter.rider=body;adapter.leftHand=left;adapter.rightHand=right;
            adapter.leftRest=model.InverseTransformPoint(left.position);adapter.rightRest=model.InverseTransformPoint(right.position);
            adapter.leftGrip=lg;adapter.rightGrip=rg;adapter.leftBit=leftBit;adapter.rightBit=rightBit;
            adapter.headSurfaces=skins.Where(s=>!s.name.Contains("casualsuit") && !s.name.Contains("boots")).Cast<Renderer>().ToArray();
            adapter.PoseRig();
            foreach(string name in new[]{"Fitted left ankle","Fitted right ankle"})
            {
                var target=Anchor(name);float clearance=MeasureBootClearance(horse,target);
                if(Mathf.Abs(clearance)>.08f)throw new InvalidOperationException("Boot is too far from fitted tread: "+clearance);
                target.position-=target.up*clearance;adapter.PoseRig();
                Debug.Log("HERO_BOOT_CONTACT "+name+" before="+clearance+" after="+MeasureBootClearance(horse,target));
            }
            var materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Oiled bridle leather.mat"),
                AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderTackBuilder.Root+"/Turquoise rein braid.mat"),AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderTackBuilder.Root+"/Flax rein braid.mat")};
            MeshFilter Rein(int side,Transform grip,Transform bit)
            {
                var mesh=ReinsRiderTackPresentation.CreateReinTemplate();
                var part=MeshPart(tack,side<0?"Fitted left rein":"Fitted right rein",mesh,materials);
                ReinsRiderTackPresentation.UpdateReinGeometry(part.transform,grip.position,bit.position,0,side,mesh,new Vector3[mesh.vertexCount],new Vector3[mesh.vertexCount],new Vector4[mesh.vertexCount]);
                part.sharedMesh=PersistentMeshAsset.Save(mesh,HeroHorseBenchmarkBuilder.Root+"/"+part.name+".asset");return part;
            }
            adapter.leftRein=Rein(-1,lg,leftBit);adapter.rightRein=Rein(1,rg,rightBit);
            Debug.Log("HERO_RIDER_FIT horn="+hornPoint.ToString("F4")+" reachError="+body.MaximumReachError);
            return adapter;
        }
        // Contact is measured from the actual boot sole and refitted metal tread, in ankle metres.
        public static float MeasureBootClearance(HorseRigBindings horse,Transform ankle)
        {
            var boots=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("ankle_boots"));
            var hardware=horse.GetComponentsInChildren<MeshFilter>().Single(m=>m.name=="Saddle hardware");
            var baked=new Mesh();
            try
            {
                boots.BakeMesh(baked,true);
                var sole=baked.vertices.Select(p=>ankle.InverseTransformPoint(boots.transform.TransformPoint(p)))
                    .Where(p=>Mathf.Abs(p.x)<.055f && p.z>.03f && p.z<.18f).ToArray();
                var tread=hardware.sharedMesh.vertices.Select(p=>ankle.InverseTransformPoint(hardware.transform.TransformPoint(p)))
                    .Where(p=>Mathf.Abs(p.x)<.06f && p.z>.04f && p.z<.16f && p.y<-.04f && p.y>-.25f).ToArray();
                if(sole.Length==0 || tread.Length==0)throw new InvalidOperationException("Fitted boot/tread contact samples missing for "+ankle.name);
                return sole.Min(p=>p.y)-tread.Max(p=>p.y);
            }
            finally{Object.DestroyImmediate(baked);}
        }
        private static Transform Child(Transform parent,string name)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static MeshFilter MeshPart(Transform parent,string name,Mesh mesh,Material[] materials)
        {
            if(!mesh || materials.Any(m=>!m))throw new InvalidOperationException("Missing original mesh/material for "+name);
            var part=Child(parent,name);var filter=part.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;return filter;
        }
    }
}
