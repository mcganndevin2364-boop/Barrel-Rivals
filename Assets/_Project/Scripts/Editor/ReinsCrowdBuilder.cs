using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Seeds persistent crowd-card art and places it in the existing distant stands.</summary>
    public static class ReinsCrowdBuilder
    {
        private const string ArtRoot = "Assets/_Project/Art/Reins";
        private const string TexturePath = ArtRoot + "/Textures/RodeoCrowdStrip.png";
        private const string MaterialPath = ArtRoot + "/Materials/Seated rodeo crowd.mat";
        private const string CrowdName = "Reins seated crowd";

        // Original PNG is 2079x756. Substantial subject bounds are (17,164)-(2057,599)
        // in top-left pixels. Eight pixels of transparent padding retain the silhouette edges.
        private const float SourceWidth = 2079, SourceHeight = 756;
        private const float CropLeft = 9, CropRight = 2065, CropTop = 156, CropBottom = 607;
        private const float CardHeight = 1.46f;
        private const float CardWidth = CardHeight * (CropRight - CropLeft) / (CropBottom - CropTop);
        // The visible lap/seat level lies near source y=430; align it to each timber bench.
        private const float SeatFromBottom = (CropBottom - 430) / (CropBottom - CropTop) * CardHeight;

        public static void Build(Transform parent)
        {
            if (!parent) throw new ArgumentNullException(nameof(parent));
            Directory.CreateDirectory(ArtRoot + "/Materials");
            Directory.CreateDirectory(ArtRoot + "/Meshes");
            AssetDatabase.Refresh();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (!texture) throw new FileNotFoundException("The approved crowd strip must be imported before building the stands.", TexturePath);

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                // First-use defaults only. Subsequent regeneration preserves authored material/import tuning.
                var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                if (importer)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.anisoLevel = 2;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                }
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) throw new InvalidOperationException("Crowd cards require the project's URP Lit shader.");
                material = new Material(shader) { name = "Seated rodeo crowd" };
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Surface", 0);
                material.SetFloat("_AlphaClip", 1);
                material.SetFloat("_Cutoff", .5f);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetFloat("_ZWrite", 1);
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_Smoothness", .05f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Mesh regular = CardMesh(false), mirrored = CardMesh(true);
            Transform old = parent.Find(CrowdName);
            if (old) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(CrowdName).transform;
            root.SetParent(parent, false);
            const int cardsPerRow = 7;
            const float spacing = .18f;
            float occupiedLength = cardsPerRow * CardWidth + (cardsPerRow - 1) * spacing;
            float firstCenter = 29 - occupiedLength * .5f + CardWidth * .5f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 5; row++)
                {
                    for (int section = 0; section < cardsPerRow; section++)
                    {
                        var card = new GameObject($"{(side < 0 ? "West" : "East")} seated row {row + 1} section {section + 1}", typeof(MeshFilter), typeof(MeshRenderer));
                        card.transform.SetParent(root, false);
                        float shift = row % 2 == 0 ? -.08f : .08f;
                        card.transform.position = new Vector3(side * (31 + row * 1.55f - .18f), .51f + row * .53f - SeatFromBottom, firstCenter + section * (CardWidth + spacing) + shift);
                        card.transform.rotation = Quaternion.Euler(0, -side * 90, 0);
                        card.GetComponent<MeshFilter>().sharedMesh = (section + row + (side > 0 ? 1 : 0)) % 2 == 0 ? regular : mirrored;
                        var renderer = card.GetComponent<MeshRenderer>();
                        renderer.sharedMaterial = material;
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.Off;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                        // Build-time static batching keeps mesh assets persistent without transient scene meshes.
                        GameObjectUtility.SetStaticEditorFlags(card, StaticEditorFlags.BatchingStatic);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static Mesh CardMesh(bool mirrored)
        {
            string path = ArtRoot + "/Meshes/Seated crowd card" + (mirrored ? " mirrored" : "") + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;
            float u0 = CropLeft / SourceWidth, u1 = CropRight / SourceWidth;
            if (mirrored) { float swap = u0; u0 = u1; u1 = swap; }
            float v0 = 1 - CropBottom / SourceHeight, v1 = 1 - CropTop / SourceHeight;
            var mesh = new Mesh
            {
                name = mirrored ? "Seated crowd card mirrored" : "Seated crowd card",
                vertices = new[] { new Vector3(-CardWidth * .5f, 0, 0), new Vector3(CardWidth * .5f, 0, 0), new Vector3(-CardWidth * .5f, CardHeight, 0), new Vector3(CardWidth * .5f, CardHeight, 0) },
                uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u0, v1), new Vector2(u1, v1) },
                normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
                triangles = new[] { 0, 1, 2, 2, 1, 3 }
            };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
