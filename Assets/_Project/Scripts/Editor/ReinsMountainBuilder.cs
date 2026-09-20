using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Editor
{
    /// <summary>
    /// Deterministic western range geometry from credited USGS elevations. Editor generated, opaque, no colliders,
    /// runtime behaviour, additional lighting, external imagery, or changes to the racing layout.
    /// </summary>
    public static class ReinsMountainBuilder
    {
        private const string Root = "Assets/_Project/Art/Reins/Premium/Mountains";
        private const float Tau = Mathf.PI * 2;
        private const float InnerRadius = 105, OuterRadius = 910;
        private const int Around = 256, Rows = 47;
        private static readonly Vector3 Center = new Vector3(0, 0, 27);

        public static void Build(Transform parent)
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("The mountain builder requires the project's URP Lit shader.");
            var rockAlbedo = ImportRock("RockFace_Albedo_1K.jpg", false);
            var materials = new[]
            {
                LandscapeMaterial(rockAlbedo),
                Stone(shader, "Pine needles", new Color(.105f, .17f, .125f)),
                Stone(shader, "Pine trunks", new Color(.26f, .205f, .15f))
            };
            var terrain = new Geometry(1);
            AddLandscape(terrain,new ElevationField(Root+"/Elevation/EagleValley.bytes"));
            var trees = new Geometry(2);
            AddPines(trees, terrain);
            var scenery = new GameObject("Original western mountain ranges").transform;
            scenery.SetParent(parent, false);
            // Retain saved mesh asset identity while replacing its formerly overlapping topology.
            Save(scenery, "Layered eroded ridges", terrain, new[] { materials[0] });
            Save(scenery, "Sparse foothill pines", trees, new[] { materials[1], materials[2] });
            Debug.Log($"Original western scenery: {terrain.Vertices.Count + trees.Vertices.Count:N0} vertices, " +
                $"{terrain.TriangleCount + trees.TriangleCount:N0} triangles, 3 opaque material batches; one continuous terrain surface, no shadow casters or colliders.");
        }

        private static Material LandscapeMaterial(Texture2D rock)
        {
            var shader=Shader.Find("Barrel Rivals/Distant Terrain");
            if(!shader)throw new InvalidOperationException("The original distant terrain shader is missing.");
            string path=Root+"/Weathered ridge stone.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;
            material.SetTexture("_BaseMap",rock);
            var soil=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Reins/Premium/Textures/ArenaSoil_Albedo_2K.png");
            if(!soil)throw new InvalidOperationException("Reviewed CC0 soil texture is missing.");
            material.SetTexture("_GroundMap",soil);
            material.SetColor("_RockTint",new Color(.91f,.92f,.95f));
            material.SetColor("_GroundTint",new Color(.56f,.61f,.52f));
            material.SetColor("_DryTint",new Color(.76f,.70f,.60f));
            material.SetFloat("_RockScale",1f/18);material.SetFloat("_GroundScale",1f/13);
            material.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);return material;
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

        private static void AddLandscape(Geometry geometry,ElevationField elevation)
        {
            for (int row = 0; row <= Rows; row++)
            {
                // Concentrate rows near the visible foothills; the outer silhouette still has
                // 256 angular samples. One shared set of vertices/normals spans every region.
                float radius = Mathf.Lerp(InnerRadius, OuterRadius, Mathf.Pow(row / (float)Rows, 1.22f));
                for (int a = 0; a <= Around; a++)
                {
                    float angle = (a % Around) * Tau / Around;
                    var point = Center + new Vector3(Mathf.Sin(angle) * radius,
                        LandscapeHeight(angle, radius,elevation), Mathf.Cos(angle) * radius);
                    // World mapping in the shader removes the cylindrical chart's vertical
                    // stretch. Vertex channels supply broad land-cover variation, not bands.
                    float dryness=TerrainNoise(point.x*.008f+7,point.z*.008f-3,221);
                    float variation=TerrainNoise(point.x*.017f-9,point.z*.017f+4,241);
                    float exposed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(42,105,point.y));
                    geometry.Vertex(point,new Vector2(point.x/18,point.z/18),new Color(dryness,variation,1,exposed));
                }
            }
            for (int row = 0; row < Rows; row++)
                for (int a = 0; a < Around; a++)
                {
                    int p = row * (Around + 1) + a, q = p + Around + 1;
                    if (((row + a) & 1) == 0)
                    { Face(geometry, 0, p, q, p + 1); Face(geometry, 0, p + 1, q, q + 1); }
                    else
                    { Face(geometry, 0, p, q, q + 1); Face(geometry, 0, p, q + 1, p + 1); }
                }
            for (int row = 0; row <= Rows; row++)
                geometry.Seams.Add(new Vector2Int(row * (Around + 1), row * (Around + 1) + Around));
        }

        private static float LandscapeHeight(float angle,float radius,ElevationField elevation)
        {
            float x=Mathf.Sin(angle)*radius,z=Mathf.Cos(angle)*radius;
            // Actual USGS valley/ridge structure supplies the silhouette. Only its game
            // scale and the clear venue apron/buried outer boundary are art-directed.
            float height=Mathf.Max(0,(elevation.Sample(x,z)-elevation.Datum)*.17f);
            float inner=Mathf.SmoothStep(0,1,(radius-150)/110);
            float outer=Mathf.SmoothStep(0,1,(OuterRadius-radius)/120);
            return -.6f+height*inner*outer;
        }

        private sealed class ElevationField
        {
            private const int Size=513;
            private readonly float[] samples=new float[Size*Size];
            public float Datum=>samples[(Size*Size)/2];
            public ElevationField(string path)
            {
                if(!File.Exists(path))throw new InvalidOperationException("Credited USGS elevation source is missing: "+path);
                using(var reader=new BinaryReader(File.OpenRead(path)))
                {
                    if(reader.BaseStream.Length!=12+Size*Size*4 || reader.ReadUInt32()!=0x31485242
                        || reader.ReadInt32()!=Size || reader.ReadInt32()!=Size)
                        throw new InvalidDataException("Invalid BRH1 elevation header or dimensions.");
                    for(int i=0;i<samples.Length;i++)
                    {
                        float value=reader.ReadSingle();
                        if(!float.IsFinite(value) || value<0 || value>6000)
                            throw new InvalidDataException("Missing/non-finite elevation value at sample "+i);
                        samples[i]=value;
                    }
                }
            }
            public float Sample(float x,float z)
            {
                // The source raster is north-first; positive game Z samples north.
                float u=Mathf.Clamp01(x/(OuterRadius*2)+.5f)*(Size-1);
                float v=Mathf.Clamp01(.5f-z/(OuterRadius*2))*(Size-1);
                int x0=Mathf.Min(Size-2,Mathf.FloorToInt(u)),y0=Mathf.Min(Size-2,Mathf.FloorToInt(v));
                float a=Mathf.Lerp(samples[y0*Size+x0],samples[y0*Size+x0+1],u-x0);
                float b=Mathf.Lerp(samples[(y0+1)*Size+x0],samples[(y0+1)*Size+x0+1],u-x0);
                return Mathf.Lerp(a,b,v-y0);
            }
        }

        private static float TerrainNoise(float x, float z, uint seed)
        {
            // Original deterministic gradient noise: integer hashing keeps the shape independent
            // of engine-native noise implementations and reproducible in offline height previews.
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fz = z - iz;
            float u = fx * fx * fx * (fx * (fx * 6 - 15) + 10);
            float v = fz * fz * fz * (fz * (fz * 6 - 15) + 10);
            float a = Mathf.Lerp(TerrainGradient(ix, iz, seed, fx, fz), TerrainGradient(ix + 1, iz, seed, fx - 1, fz), u);
            float b = Mathf.Lerp(TerrainGradient(ix, iz + 1, seed, fx, fz - 1), TerrainGradient(ix + 1, iz + 1, seed, fx - 1, fz - 1), u);
            return Mathf.Clamp01(.5f + .5f * Mathf.Lerp(a, b, v));
        }

        private static float TerrainGradient(int x, int z, uint seed, float dx, float dz)
        {
            uint hash;
            unchecked
            {
                hash = (uint)x * 374761393u + (uint)z * 668265263u + seed * 1442695041u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
            }
            switch (hash & 7)
            {
                case 0: return dx; case 1: return -dx; case 2: return dz; case 3: return -dz;
                case 4: return (dx + dz) * .70710678f; case 5: return (dx - dz) * .70710678f;
                case 6: return (-dx + dz) * .70710678f; default: return (-dx - dz) * .70710678f;
            }
        }

        private static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f, 1);

        private static void Face(Geometry g, int material, int a, int b, int c)
        {
            g.Indices[material].Add(a); g.Indices[material].Add(b); g.Indices[material].Add(c);
        }

        private static void AddPines(Geometry g, Geometry terrain)
        {
            // Separated small clusters, all >130 m from the arena center. Nothing crosses a fence,
            // competes with barrel sightlines, or needs transparent-card sorting/animated foliage.
            for (int i = 0; i < 56; i++)
            {
                int cluster = i / 4;
                float angle = cluster * Tau / 14 + (Hash(i + 92) - .5f) * .13f + .17f;
                float radius = 137 + Hash(cluster + 25) * 97 + (Hash(i + 65) - .5f) * 20;
                var foot = Center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                foot.y = TerrainHeightAt(terrain, foot.x, foot.z) - .08f;
                Pine(g, foot, 6.5f + Hash(i + 19) * 7, i);
            }
        }

        private static float TerrainHeightAt(Geometry terrain, float x, float z)
        {
            // Editor-only placement on the actual triangles, rather than the analytic field:
            // low-resolution interpolation otherwise leaves some trunks floating or buried.
            var indices = terrain.Indices[0];
            for (int i = 0; i < indices.Count; i += 3)
            {
                var a = terrain.Vertices[indices[i]]; var b = terrain.Vertices[indices[i + 1]]; var c = terrain.Vertices[indices[i + 2]];
                float area = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(area) < .00001f) continue;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / area;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / area;
                float w = 1 - u - v;
                if (u >= -.00001f && v >= -.00001f && w >= -.00001f) return a.y * u + b.y * v + c.y * w;
            }
            throw new InvalidOperationException("A distant pine fell outside the continuous terrain mesh.");
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
            private readonly List<Color> colors = new List<Color>();
            public readonly List<Vector2Int> Seams = new List<Vector2Int>();
            public readonly List<int>[] Indices;
            public int TriangleCount { get { int count = 0; foreach (var indices in Indices) count += indices.Count / 3; return count; } }
            public Geometry(int materials) { Indices = new List<int>[materials]; for (int i = 0; i < materials; i++) Indices[i] = new List<int>(); }
            public int Vertex(Vector3 p, Vector2 texcoord = default, Color color = default)
            {
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z))
                    throw new InvalidOperationException("Mountain generation produced a non-finite vertex.");
                Vertices.Add(p); uv.Add(texcoord); colors.Add(color); return Vertices.Count - 1;
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
                mesh.SetColors(colors);
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
