using UnityEngine;
using UnityEngine.UI;

namespace BarrelRivals.Practice
{
    /// <summary>Sprite-free, chamfered rodeo HUD card. Uses the shared uGUI material.</summary>
    [AddComponentMenu("Barrel Rivals/UI/Reins HUD Panel")]
    public sealed class ReinsHudPanel : Image
    {
        [SerializeField] private Color borderColor = new Color(.83f, .69f, .43f, .66f);
        [SerializeField, Min(0)] private float cornerCut = 7;
        [SerializeField, Min(0)] private float borderWidth = 1;
        [SerializeField] private bool showGrid;

        public bool ShowGrid
        {
            get => showGrid;
            set { if (showGrid == value) return; showGrid = value; SetVerticesDirty(); }
        }

        public void Configure(Color outline, float cut, float width)
        {
            borderColor = outline;
            cornerCut = Mathf.Max(0, cut);
            borderWidth = Mathf.Max(0, width);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // Meter Images retain normal uGUI filled-sprite behavior if this is reused for a fill.
            if (type != Type.Simple) { base.OnPopulateMesh(vh); return; }
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float cut = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .5f);
            float border = Mathf.Min(borderWidth, Mathf.Min(rect.width, rect.height) * .25f);
            vh.AddVert(rect.center, color, Vector2.zero);
            for (int i = 0; i < 8; i++) vh.AddVert(Corner(rect, cut, i), color, Vector2.zero);
            for (int i = 0; i < 8; i++) vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);

            if (showGrid)
            {
                Color grid = new Color(.76f, .77f, .69f, .065f * color.a);
                float inset = Mathf.Max(cut + 2, 10);
                for (int i = 1; i < 5; i++)
                {
                    float x = Mathf.Lerp(rect.xMin + inset, rect.xMax - inset, i / 5f);
                    float y = Mathf.Lerp(rect.yMin + inset, rect.yMax - inset, i / 5f);
                    Quad(vh, new Vector2(x, rect.yMin + inset), new Vector2(x + .7f, rect.yMin + inset), new Vector2(x + .7f, rect.yMax - inset), new Vector2(x, rect.yMax - inset), grid);
                    Quad(vh, new Vector2(rect.xMin + inset, y), new Vector2(rect.xMax - inset, y), new Vector2(rect.xMax - inset, y + .7f), new Vector2(rect.xMin + inset, y + .7f), grid);
                }
            }

            if (border <= 0) return;
            var inner = new Rect(rect.xMin + border, rect.yMin + border, rect.width - 2 * border, rect.height - 2 * border);
            float innerCut = Mathf.Max(0, cut - border * .586f);
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                Quad(vh, Corner(rect, cut, i), Corner(rect, cut, next), Corner(inner, innerCut, next), Corner(inner, innerCut, i), borderColor);
            }
        }

        private static Vector2 Corner(Rect r, float cut, int index)
        {
            switch (index)
            {
                case 0: return new Vector2(r.xMin + cut, r.yMin);
                case 1: return new Vector2(r.xMax - cut, r.yMin);
                case 2: return new Vector2(r.xMax, r.yMin + cut);
                case 3: return new Vector2(r.xMax, r.yMax - cut);
                case 4: return new Vector2(r.xMax - cut, r.yMax);
                case 5: return new Vector2(r.xMin + cut, r.yMax);
                case 6: return new Vector2(r.xMin, r.yMax - cut);
                default: return new Vector2(r.xMin, r.yMin + cut);
            }
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int first = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
