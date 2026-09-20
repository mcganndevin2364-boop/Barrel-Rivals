using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Editor
{
    /// <summary>
    /// Original, deterministic western range geometry. Editor generated, opaque, no colliders,
    /// runtime behaviour, additional lighting, external imagery, or changes to the racing layout.
    /// </summary>
    public static class ReinsMountainBuilder
    {
        private const string Root = "Assets/_Project/Art/Reins/Premium/Mountains";
        private const float Tau = Mathf.PI * 2;
        private static readonly Vector3 Center = new Vector3(0, 0, 27);

        private readonly struct Range
        {
            public readonly float Inner, Outer, Height, Relief, Seed;
            public readonly int Around, Rows, Material;
            public Range(float inner, float outer, float height, float relief, float seed,
                int around, int rows, int material)
            { Inner = inner; Outer = outer; Height = height; Relief = relief; Seed = seed;
                Around = around; Rows = rows; Material = material; }
        }

        // Staggered crests avoid a single circular wall. These ranges stay inside the existing
        // 1,000 m camera far plane and use the scene's existing atmospheric fog unchanged.
        private static readonly Range Valley = new Range(105, 235, 1.1f, 2.8f, 24.2f, 96, 6, 3);
        private static readonly Range Foothills = new Range(155, 445, 6.8f, 24.4f, 3.7f, 192, 10, 2);
        private static readonly Range MainRange = new Range(248, 708, 32.2f, 65.5f, 9.3f, 256, 16, 1);
        private static readonly Range Skyline = new Range(415, 910, 60.3f, 82.7f, 17.8f, 192, 10, 0);

        public static void Build(Transform parent)
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("The mountain builder requires the project's URP Lit shader.");
            var rockAlbedo = ImportRock("RockFace_Albedo_1K.jpg", false);
            var rockNormal = ImportRock("RockFace_NormalGL_1K.jpg", true);
            var valleyAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Reins/Premium/Textures/ArenaSoil_Albedo_2K.png");
            var materials = new[]
            {
                Stone(shader, "Distant blue shale", new Color(.80f, 1.14f, 1.50f), rockAlbedo, rockNormal, .18f),
                Stone(shader, "Weathered ridge stone", new Color(.76f, 1.04f, 1.34f), rockAlbedo, rockNormal, .32f),
                Stone(shader, "Warm scree shoulders", new Color(.70f, .91f, 1.07f), rockAlbedo, rockNormal, .23f),
                Stone(shader, "Dry sage foothills", new Color(.79f, .83f, .74f), valleyAlbedo),
                Stone(shader, "Pine needles", new Color(.105f, .17f, .125f)),
                Stone(shader, "Pine trunks", new Color(.26f, .205f, .15f))
            };
            var terrain = new Geometry(4);
            AddRange(terrain, Valley);
            AddRange(terrain, Foothills);
            AddRange(terrain, MainRange);
            AddRange(terrain, Skyline);
            var trees = new Geometry(2);
            AddPines(trees);
            var scenery = new GameObject("Original western mountain ranges").transform;
            scenery.SetParent(parent, false);
            Save(scenery, "Layered eroded ridges", terrain, new[] { materials[0], materials[1], materials[2], materials[3] });
            Save(scenery, "Sparse foothill pines", trees, new[] { materials[4], materials[5] });
            Debug.Log($"Original western scenery: {terrain.Vertices.Count + trees.Vertices.Count:N0} vertices, " +
                $"{terrain.TriangleCount + trees.TriangleCount:N0} triangles, 6 opaque material batches; no shadow casters or colliders.");
        }

        private static Texture2D ImportRock(string filename, bool normal)
        {
            string path = Root + "/Textures/" + filename;
            if (!File.Exists(path)) throw new InvalidOperationException("Verified CC0 mountain texture is missing: " + path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Stone(Shader shader, string name, Color color, Texture2D albedo = null,
            Texture2D normal = null, float normalStrength = 0)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", .08f);
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", normalStrength);
            if (normal) material.EnableKeyword("_NORMALMAP"); else material.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddRange(Geometry geometry, Range range)
        {
            int first = geometry.Vertices.Count;
            for (int row = 0; row <= range.Rows; row++)
            {
                float t = row / (float)range.Rows;
                for (int a = 0; a <= range.Around; a++)
                {
                    float angle = (a % range.Around) * Tau / range.Around;
                    float radius = Mathf.Lerp(range.Inner, range.Outer, t);
                    var point = Center + new Vector3(Mathf.Sin(angle) * radius,
                        Height(range, angle, t), Mathf.Cos(angle) * radius);
                    Vector2 uv;
                    if (range.Material == 3) uv = new Vector2(point.x, point.z) / 3.6f;
                    else
                    {
                        // A floor projection stretches badly on the inward-facing mountain walls.
                        // Map around the range and up its actual height instead. Large feature
                        // scales and small periodic offsets avoid a dense checker/stripe repeat.
                        float feature = range.Material == 0 ? 70 : range.Material == 1 ? 50 : 35;
                        int wraps = Mathf.Max(1, Mathf.RoundToInt(Tau * Mathf.Lerp(range.Inner, range.Outer, .48f) / feature));
                        float u = a / (float)range.Around * wraps + .12f * Mathf.Sin(angle * 3 + range.Seed);
                        float v = point.y / feature + .09f * Mathf.Sin(angle * 7 + range.Seed) + .04f * Mathf.Sin(angle * 13);
                        // The duplicated final vertex uses U + an integer number of texture tiles;
                        // all periodic offsets match the first vertex exactly at the seam.
                        uv = new Vector2(u, v);
                    }
                    geometry.Vertex(point, uv);
                }
            }
            for (int row = 0; row < range.Rows; row++)
                for (int a = 0; a < range.Around; a++)
                {
                    int p = first + row * (range.Around + 1) + a, q = p + range.Around + 1;
                    // Alternating diagonals remove the long regular strips of the former ring.
                    if (((row + a) & 1) == 0)
                    { Face(geometry, range.Material, p, q, p + 1); Face(geometry, range.Material, p + 1, q, q + 1); }
                    else
                    { Face(geometry, range.Material, p, q, q + 1); Face(geometry, range.Material, p, q + 1, p + 1); }
                }
            // Duplicated seam vertices carry identical positions and smooth normals after Mesh().
            for (int row = 0; row <= range.Rows; row++)
                geometry.Seams.Add(new Vector2Int(first + row * (range.Around + 1), first + row * (range.Around + 1) + range.Around));
        }

        private static float Height(Range range, float angle, float t)
        {
            float crest = .48f + .055f * Mathf.Sin(angle * 5 + range.Seed) + .025f * Mathf.Sin(angle * 11 - range.Seed);
            float broad = Mathf.PerlinNoise(Mathf.Cos(angle) * 1.9f + range.Seed, Mathf.Sin(angle) * 1.9f + 21);
            // Smooth broad massing plus much smaller secondary peaks keeps a broken natural
            // skyline without the first candidate's sawteeth and regularly repeated ribs.
            float summit = AngularNoise(angle, 9, range.Seed) * .65f +
                AngularNoise(angle, 21, range.Seed + 4) * .25f + AngularNoise(angle, 43, range.Seed + 9) * .10f;
            float height = range.Height + range.Relief * Mathf.Clamp01(.55f * broad + .55f * summit);
            float flank = t <= crest ? t / crest : (1 - t) / (1 - crest);
            flank = Mathf.Clamp01(flank);
            float profile = Mathf.Pow(Mathf.Max(0, Mathf.Sin(flank * Mathf.PI * .5f)), 1.9f);
            float radius = Mathf.Lerp(range.Inner, range.Outer, t);
            float x = Mathf.Sin(angle) * radius, z = Mathf.Cos(angle) * radius;
            // Local two-dimensional relief breaks the shoulders across the slope, rather than
            // producing a flute running from every peak to the valley. It remains subordinate
            // to the main ridge and fades out before terrain joins the existing outer apron.
            float shoulders = Mathf.PerlinNoise(x * .018f + range.Seed, z * .018f + 31) - .5f;
            float chips = Mathf.PerlinNoise(x * .046f + 11, z * .046f + range.Seed) - .5f;
            float relief = height * (.13f * shoulders + .045f * chips) * Mathf.Sin(t * Mathf.PI);
            return -.6f + Mathf.Max(0, height * profile + relief);
        }

        private static float AngularNoise(float angle, int segments, float seed)
        {
            float position = Mathf.Repeat(angle / Tau, 1) * segments;
            int index = Mathf.FloorToInt(position);
            return Mathf.Lerp(Hash(index + seed * 37), Hash((index + 1) % segments + seed * 37), Mathf.SmoothStep(0, 1, position - index));
        }

        private static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f, 1);

        private static void Face(Geometry g, int material, int a, int b, int c)
        {
            // One continuous material per landform. Classifying triangle centroids produced
            // conspicuous isolated brown triangles in the first actual Unity render.
            g.Indices[material].Add(a); g.Indices[material].Add(b); g.Indices[material].Add(c);
        }

        private static void AddPines(Geometry g)
        {
            // Separated small clusters, all >130 m from the arena center. Nothing crosses a fence,
            // competes with barrel sightlines, or needs transparent-card sorting/animated foliage.
            for (int i = 0; i < 56; i++)
            {
                int cluster = i / 4;
                float angle = cluster * Tau / 14 + (Hash(i + 92) - .5f) * .13f + .17f;
                float radius = 137 + Hash(cluster + 25) * 97 + (Hash(i + 65) - .5f) * 20;
                float t = Mathf.InverseLerp(Foothills.Inner, Foothills.Outer, radius);
                float ground = Mathf.Max(Height(Foothills, angle, t), Height(Valley, angle, Mathf.InverseLerp(Valley.Inner, Valley.Outer, radius)));
                var foot = Center + new Vector3(Mathf.Sin(angle) * radius, ground - .08f, Mathf.Cos(angle) * radius);
                Pine(g, foot, 6.5f + Hash(i + 19) * 7, i);
            }
        }

        private static void Pine(Geometry g, Vector3 foot, float height, int seed)
        {
            const int sides = 7;
            var lean = new Vector3((Hash(seed + 302) - .5f) * .035f, 0, (Hash(seed + 182) - .5f) * .035f);
            Vector3 Axis(float y) => foot + Vector3.up * y + lean * y;
            for (int side = 0; side < sides; side++)
            {
                float a = side * Tau / sides, b = (side + 1) * Tau / sides;
                var ra = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var rb = new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b));
                g.Quad(1, Axis(0) + ra * .11f, Axis(0) + rb * .11f, Axis(height * .86f) + rb * .038f, Axis(height * .86f) + ra * .038f);
            }
            for (int tier = 0; tier < 5; tier++)
            {
                float f = tier / 5f, y = height * (.20f + f * .69f);
                float width = height * (.19f - f * .157f);
                var tip = Axis(Mathf.Min(height, y + height * (.37f - f * .13f)));
                var underside = Axis(y - height * .033f);
                var ring = new Vector3[sides];
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Tau / sides + tier * .73f + seed;
                    float reach = width * (.79f + Hash(seed * 91 + side * 7 + tier) * .36f);
                    ring[side] = Axis(y + (Hash(seed * 31 + side + tier * 23) - .5f) * height * .041f) +
                        new Vector3(Mathf.Sin(angle) * reach, 0, Mathf.Cos(angle) * reach);
                }
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    g.Triangle(0, tip, ring[side], ring[next]);
                    g.Triangle(0, underside, ring[next], ring[side]);
                }
            }
        }

        private static void Save(Transform parent, string name, Geometry geometry, Material[] materials)
        {
            var mesh = geometry.Mesh(name);
            var saved = PersistentMeshAsset.Save(mesh, Root + "/" + name + ".asset");
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { isStatic = true };
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = saved;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<Vector2Int> Seams = new List<Vector2Int>();
            public readonly List<int>[] Indices;
            public int TriangleCount { get { int count = 0; foreach (var indices in Indices) count += indices.Count / 3; return count; } }
            public Geometry(int materials) { Indices = new List<int>[materials]; for (int i = 0; i < materials; i++) Indices[i] = new List<int>(); }
            public int Vertex(Vector3 p, Vector2 texcoord = default)
            {
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z))
                    throw new InvalidOperationException("Mountain generation produced a non-finite vertex.");
                Vertices.Add(p); uv.Add(texcoord); return Vertices.Count - 1;
            }
            public void Triangle(int material, Vector3 a, Vector3 b, Vector3 c)
            { Indices[material].Add(Vertex(a)); Indices[material].Add(Vertex(b)); Indices[material].Add(Vertex(c)); }
            public void Quad(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            { Triangle(material, a, b, c); Triangle(material, a, c, d); }
            public Mesh Mesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
                mesh.SetVertices(Vertices);
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = Indices.Length;
                for (int i = 0; i < Indices.Length; i++) mesh.SetTriangles(Indices[i], i);
                mesh.RecalculateNormals();
                var normals = mesh.normals;
                foreach (var pair in Seams) normals[pair.x] = normals[pair.y] = (normals[pair.x] + normals[pair.y]).normalized;
                mesh.normals = normals;
                if (Seams.Count > 0) mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
