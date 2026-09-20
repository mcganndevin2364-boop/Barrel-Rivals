using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Depth-tested, one-sided world text using the font's live atlas; never changes HUD font materials.</summary>
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class ReinsArenaLettering : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private Shader letteringShader;
        private Material instance;

        public void Configure(Font source, Shader shader) { font = source; letteringShader = shader; }
        private void OnEnable()
        {
            if (!font || !letteringShader) return;
            instance = new Material(font.material) { name = "World lettering instance", shader = letteringShader };
            GetComponent<MeshRenderer>().sharedMaterial = instance;
            Font.textureRebuilt += AtlasRebuilt;
            AtlasRebuilt(font);
        }
        private void AtlasRebuilt(Font changed)
        {
            if (changed == font && instance) instance.mainTexture = font.material.mainTexture;
        }
        private void OnDisable()
        {
            Font.textureRebuilt -= AtlasRebuilt;
            if (instance) Destroy(instance);
            instance = null;
        }
    }
}
