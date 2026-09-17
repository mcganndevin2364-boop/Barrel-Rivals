using System.Collections.Generic;
using BarrelRivals.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PatternGraphic : MaskableGraphic
    {
        private TracePoint[] _template;
        private IReadOnlyList<TracePoint> _trace;
        private bool _showTemplate;
        public void Configure(PatternKind kind,IReadOnlyList<TracePoint> trace)
        { _template=PatternScoring.Template(kind); _trace=trace; raycastTarget=false; SetVerticesDirty(); }
        public void Refresh(bool showTemplate) { _showTemplate=showTemplate; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect rect=rectTransform.rect;
            for(int i=1;i<4;i++)
            {
                float f=i/4f;
                Line(mesh,new Vector2(rect.xMin+rect.width*f,rect.yMin),new Vector2(rect.xMin+rect.width*f,rect.yMax),1,new Color(.5f,.7f,.7f,.12f));
                Line(mesh,new Vector2(rect.xMin,rect.yMin+rect.height*f),new Vector2(rect.xMax,rect.yMin+rect.height*f),1,new Color(.5f,.7f,.7f,.12f));
            }
            if(_showTemplate) Plot(mesh,_template,rect,new Color(1,.76f,.35f,.9f),4);
            Plot(mesh,_trace,rect,new Color(.32f,1,.82f,1),5);
        }
        private static void Plot(VertexHelper mesh,IReadOnlyList<TracePoint> points,Rect rect,Color tint,float width)
        {
            if(points==null) return;
            for(int i=1;i<points.Count;i++)
                Line(mesh,new Vector2(rect.xMin+(float)points[i-1].X*rect.width,rect.yMin+(float)points[i-1].Y*rect.height),
                    new Vector2(rect.xMin+(float)points[i].X*rect.width,rect.yMin+(float)points[i].Y*rect.height),width,tint);
        }
        private static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 delta=b-a;
            if(delta.sqrMagnitude<.0001f) return;
            Vector2 normal=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            int first=mesh.currentVertCount;
            mesh.AddVert(a-normal,tint,Vector2.zero); mesh.AddVert(a+normal,tint,Vector2.zero);
            mesh.AddVert(b+normal,tint,Vector2.zero); mesh.AddVert(b-normal,tint,Vector2.zero);
            mesh.AddTriangle(first,first+1,first+2); mesh.AddTriangle(first,first+2,first+3);
        }
    }
}
