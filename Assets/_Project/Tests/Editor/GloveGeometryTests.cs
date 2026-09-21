using System;
using System.Collections.Generic;
using System.Linq;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class GloveGeometryTests
    {
        [Test] public void OpenAndRidingGlovesHaveOneConnectedSurfaceAndOnlyAnOpenWrist()
        {
            foreach(bool open in new[]{true,false})
            {
                var mesh=open?ReinsGloveBuilder.Inspection():ReinsGloveBuilder.Riding(-1);
                try
                {
                    var positions=mesh.vertices;var indices=mesh.triangles;
                    Assert.That(indices.Length/3,Is.InRange(1000,open?2600:1600));
                    Assert.That(mesh.bounds.size.z,Is.InRange(.14f,.24f),"Gloves must retain metre-scale anatomy.");
                    var lookup=new Dictionary<Vector3,int>();var weld=new int[positions.Length];
                    for(int i=0;i<positions.Length;i++)
                    {
                        if(!lookup.TryGetValue(positions[i],out int id)){id=lookup.Count;lookup.Add(positions[i],id);}
                        weld[i]=id;
                    }
                    var parent=Enumerable.Range(0,lookup.Count).ToArray();var edges=new Dictionary<(int,int),int>();
                    int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
                    for(int i=0;i<indices.Length;i+=3)for(int k=0;k<3;k++)
                    {
                        int a=weld[indices[i+k]],b=weld[indices[i+(k+1)%3]];Assert.AreNotEqual(a,b);
                        parent[Find(a)]=Find(b);var key=a<b?(a,b):(b,a);
                        edges[key]=edges.TryGetValue(key,out int count)?count+1:1;
                    }
                    Assert.AreEqual(1,Enumerable.Range(0,parent.Length).Select(Find).Distinct().Count(),
                        "The palm, fingers, thumb and cuff must share one surface, not intersecting tubes.");
                    var unique=lookup.ToDictionary(p=>p.Value,p=>p.Key);
                    Assert.That(edges.Values.Count(n=>n==1),Is.GreaterThan(8));
                    foreach(var edge in edges)
                    {
                        Assert.That(edge.Value,Is.InRange(1,2),"Non-manifold glove edge.");
                        if(edge.Value==1)
                        {Assert.Less(unique[edge.Key.Item1].z,-.05f);Assert.Less(unique[edge.Key.Item2].z,-.05f);}
                    }
                    for(int i=0;i<positions.Length;i++)
                    {
                        var n=mesh.normals[i];var t=mesh.tangents[i];
                        Assert.That(n.sqrMagnitude,Is.EqualTo(1).Within(.02f));
                        Assert.That(new Vector3(t.x,t.y,t.z).sqrMagnitude,Is.EqualTo(1).Within(.02f));
                        Assert.That(Mathf.Abs(Vector3.Dot(n,new Vector3(t.x,t.y,t.z))),Is.LessThan(.02f));
                    }
                }
                finally{Object.DestroyImmediate(mesh);}
            }
        }

        [Test] public void BothGripsPreserveWindingAndLeaveRoomForTheBraidedRein()
        {
            var left=ReinsGloveBuilder.Riding(-1);var right=ReinsGloveBuilder.Riding(1);
            try
            {
                var lv=left.vertices;var rv=right.vertices;
                Assert.AreEqual(lv.Length,rv.Length);
                for(int i=0;i<lv.Length;i++)Assert.That(Vector3.Distance(new Vector3(-lv[i].x,lv[i].y,lv[i].z),rv[i]),Is.LessThan(.000001f));
                foreach(var mesh in new[]{left,right})
                {
                    var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;int outward=0;
                    for(int i=0;i<t.Length;i+=3)
                        if(Vector3.Dot(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]),n[t[i]]+n[t[i+1]]+n[t[i+2]])>0)outward++;
                    Assert.That(outward/(t.Length/3f),Is.GreaterThan(.98f),"Mirroring must not turn the glove inside out.");
                    var center=new Vector3(mesh==left?-.010f:.010f,-.031f,.075f);
                    foreach(var direction in new[]{Vector3.up,Vector3.down})
                    {
                        float nearest=float.PositiveInfinity;
                        for(int i=0;i<t.Length;i+=3)
                        {
                            var a=v[t[i]];var e1=v[t[i+1]]-a;var e2=v[t[i+2]]-a;
                            var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);
                            if(Mathf.Abs(det)<1e-10f)continue;
                            var s=center-a;float u=Vector3.Dot(s,p)/det;var q=Vector3.Cross(s,e1);float w=Vector3.Dot(direction,q)/det;
                            float distance=Vector3.Dot(e2,q)/det;
                            if(u>=0 && w>=0 && u+w<=1 && distance>0)nearest=Mathf.Min(nearest,distance);
                        }
                        Assert.That(nearest,Is.InRange(.0106f,.02f),"The rein must fit between the palm and flexed fingers.");
                    }
                }
            }
            finally{Object.DestroyImmediate(left);Object.DestroyImmediate(right);}
        }
    }
}
