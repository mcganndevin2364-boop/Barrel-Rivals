using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    /// <summary>Original vector pull arrow and rein arc; no font glyph or texture dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReinsPullGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float scale = Mathf.Min(r.width / 108f, r.height / 62f);
            Vector2 P(float x, float y) => center + new Vector2(x, y) * scale;
            Quad(vh, P(-5, 28), P(5, 28), P(5, 2), P(-5, 2), color);
            Triangle(vh, P(-14, 9), P(14, 9), P(0, -9), color);
            const int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Lerp(210, 330, i / (float)segments) * Mathf.Deg2Rad;
                float b = Mathf.Lerp(210, 330, (i + 1) / (float)segments) * Mathf.Deg2Rad;
                Vector2 Arc(float angle, float radius) => P(Mathf.Cos(angle) * radius, 16 + Mathf.Sin(angle) * radius);
                float strength = Mathf.Sin((i + .5f) / segments * Mathf.PI);
                Color tint = color; tint.a *= Mathf.Lerp(.25f, .9f, strength);
                Quad(vh, Arc(a, 40), Arc(b, 40), Arc(b, 44), Arc(a, 44), tint);
            }
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero); vh.AddVert(c, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
