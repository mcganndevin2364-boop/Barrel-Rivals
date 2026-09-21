using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original offline surface relief; no race state, route assistance or runtime stamping.</summary>
    public static class ReinsFootingArtBuilder
    {
        public const string Root = ReinsPremiumArenaBuilder.Root + "/Footing";
        public const string TrackPath = Root + "/Original worked earth.png";
        public const string MaterialPath = Root + "/Worked arena footing.mat";
        public const int Resolution = 1024;
        public const float TileMetres = 8;

        [MenuItem("Barrel Rivals/Art/Refresh arena footing material")]
        public static void Refresh()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root + "/Materials/Arena soil.mat");
            if (!source) throw new InvalidOperationException("Generate the Reins arena before refreshing its surface.");
            Build(source);
        }

        public static Material Build(Material source)
        {
            Directory.CreateDirectory(Root);
            var map = BuildSurface();
            var shader = Shader.Find("Barrel Rivals/Worked Arena Footing");
            if (!shader) throw new InvalidOperationException("Missing arena footing shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = shader;
            foreach (string slot in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_DetailAlbedoMap" })
                material.SetTexture(slot, source.GetTexture(slot));
            material.SetTexture("_TrackMap", map);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_PhotographScale", 1f / 1.3f);
            material.SetFloat("_MacroScale", 1f / 42f);
            material.SetFloat("_TrackScale", 1f / TileMetres);
            material.SetFloat("_BumpScale", .70f);
            material.SetFloat("_Smoothness", .34f);
            material.SetFloat("_TrackStrength", .8f);
            material.SetFloat("_Wetness", 0);
            material.SetFloat("_Cull", 2);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("BARREL_FOOTING: persistent worked-earth data, 1024 square, 8m period; shared photographic maps, no new geometry/colliders.");
            return material;
        }

        private static Texture2D BuildSurface()
        {
            const int n = Resolution;
            float[] height = new float[n * n], wear = new float[n * n];
            float Tau = Mathf.PI * 2;
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) / n, v = (z + .5f) / n;
                // Integer frequencies give a periodic field, including its first derivative.
                float drift = .37f * Mathf.Sin(v * Tau) + .21f * Mathf.Sin((u + v) * Tau * 2);
                float ridge = .5f + .5f * Mathf.Sin(Tau * (u * 64 + drift));
                float broken = .48f + .26f * Mathf.Sin((u * 3 + v * 5) * Tau) + .19f * Mathf.Cos((u * 7 - v * 3) * Tau);
                height[z * n + x] = -.014f * ridge * ridge * ridge * Mathf.Clamp01(broken)
                    + .0008f * Mathf.Sin((u * 19 + v * 23) * Tau) * Mathf.Sin((u * 11 - v * 17) * Tau);
            }
            var random = new System.Random(180924);
            float Next(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
            for (int mark = 0; mark < 210; mark++)
            {
                float cx = Next(0, TileMetres), cz = Next(0, TileMetres), angle = Next(0, Tau);
                bool scuff = mark % 4 == 0;
                float width = Next(.044f, .063f), length = scuff ? Next(.20f, .44f) : Next(.059f, .082f);
                float depth = Next(.005f, .012f), c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                int radius = Mathf.CeilToInt(Mathf.Max(width, length) * 1.55f / TileMetres * n);
                int centerX = Mathf.FloorToInt(cx / TileMetres * n), centerZ = Mathf.FloorToInt(cz / TileMetres * n);
                for (int dz = -radius; dz <= radius; dz++) for (int dx = -radius; dx <= radius; dx++)
                {
                    float px = (centerX + dx + .5f) / n * TileMetres - cx;
                    float pz = (centerZ + dz + .5f) / n * TileMetres - cz;
                    float a = (c * px + s * pz) / width, b = (-s * px + c * pz) / length;
                    float radiusSquared = a * a + b * b;
                    if (radiusSquared > 2.2f) continue;
                    float r = Mathf.Sqrt(radiusSquared);
                    float center = Mathf.Exp(-radiusSquared * 2.8f);
                    float edge = Mathf.Exp(-Mathf.Pow((r - .86f) / .15f, 2));
                    float heelOpening = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.90f, -.50f, b));
                    float shoe = scuff ? 0 : Mathf.Exp(-Mathf.Pow((r - .65f) / .11f, 2)) * heelOpening;
                    int index = ((centerZ + dz + n) % n) * n + (centerX + dx + n) % n;
                    height[index] += depth * (-center * .72f - shoe * .34f + edge * .22f);
                    wear[index] = Mathf.Max(wear[index], center * .65f + edge * .25f);
                }
            }
            var pixels = new Color32[n * n];
            float spacing = TileMetres / n;
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                int i = z * n + x;
                float sx = (height[z * n + (x + 1) % n] - height[z * n + (x + n - 1) % n]) / (2 * spacing);
                float sz = (height[((z + 1) % n) * n + x] - height[((z + n - 1) % n) * n + x]) / (2 * spacing);
                // Linear data: RG signed metre/metre slopes (+/-1.6), B height (+/-40mm), A disturbed soil.
                pixels[i] = new Color(.5f + sx / 3.2f, .5f + sz / 3.2f, .5f + height[i] / .08f, wear[i]);
            }
            var authored = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            authored.SetPixels32(pixels); authored.Apply(false, false);
            File.WriteAllBytes(TrackPath, authored.EncodeToPNG()); Object.DestroyImmediate(authored);
            AssetDatabase.ImportAsset(TrackPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TrackPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false; importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8;
            importer.maxTextureSize = Resolution; importer.isReadable = false;
            // Preserve fine signed slopes for the first actual review; device compression remains a qualification task.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TrackPath);
        }
    }
}
