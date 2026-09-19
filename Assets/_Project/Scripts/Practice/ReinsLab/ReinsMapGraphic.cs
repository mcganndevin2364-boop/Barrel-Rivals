using BarrelRivals.Core;
using BarrelRivals.Core.Reins;
using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReinsMapGraphic : MaskableGraphic
    {
        private ReinsRun run;
        public void Show(ReinsRun value) { run=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(run==null) return;
            Vector2 P(double x,double z) => new Vector2((float)x/70f*rectTransform.rect.width,(float)(z-20)/80f*rectTransform.rect.height);
            var origin=P(0,-9); var a=StandardCourse.Barrel(0);var b=StandardCourse.Barrel(1);var c=StandardCourse.Barrel(2);
            Line(vh,origin,P(a.X,a.Z),new Color(.45f,.6f,.6f,.5f),1);
            Line(vh,P(a.X,a.Z),P(b.X,b.Z),new Color(.45f,.6f,.6f,.5f),1);
            Line(vh,P(b.X,b.Z),P(c.X,c.Z),new Color(.45f,.6f,.6f,.5f),1);
            Line(vh,P(c.X,c.Z),P(0,0),new Color(.45f,.6f,.6f,.5f),1);
            Line(vh,P(-7,0),P(7,0),Color.white,2);
            for(int i=0;i<3;i++)
            {
                var p=StandardCourse.Barrel(i);
                Color tint=run.IsBarrelKnocked(i)?new Color(1,.35f,.3f):i==run.BarrelIndex?new Color(1,.76f,.3f):new Color(.35f,.75f,.63f);
                Disc(vh,P(p.X,p.Z),i==run.BarrelIndex?7:4,tint);
            }
            var horse=P(run.X,run.Z); float heading=(float)run.HeadingRadians;
            Vector2 forward=new Vector2(Mathf.Sin(heading),Mathf.Cos(heading));
            Vector2 right=new Vector2(forward.y,-forward.x);
            Triangle(vh,horse+forward*8,horse-forward*5+right*5,horse-forward*5-right*5,Color.white);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,Color tint,float width)
        {
            var side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            int n=vh.currentVertCount;vh.AddVert(a-side,tint,Vector2.zero);vh.AddVert(a+side,tint,Vector2.zero);
            vh.AddVert(b+side,tint,Vector2.zero);vh.AddVert(b-side,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        private static void Disc(VertexHelper vh,Vector2 p,float r,Color tint)
        { for(int i=0;i<16;i++) Triangle(vh,p,p+new Vector2(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8))*r,p+new Vector2(Mathf.Cos((i+1)*Mathf.PI/8),Mathf.Sin((i+1)*Mathf.PI/8))*r,tint); }
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {int n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
    }
}
