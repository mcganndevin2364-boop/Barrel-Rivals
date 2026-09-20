using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Owns original, generated practice art. No race coordinates or collision shapes are changed.</summary>
    public static class PracticePresentationBuilder
    {
        public const string Root = "Assets/_Project/Generated/Practice/Presentation";
        private static Mesh sphere, cube, cylinder;

        public static PracticeHorseVisual Build(Transform horse, Transform barrel, Camera camera, bool includeAlleyRails = true, bool includeCrowd = true)
        {
            if (!horse || !barrel || !camera) throw new ArgumentException("Presentation requires the existing horse, barrel and camera.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            sphere = PrimitiveMesh(PrimitiveType.Sphere); cube = PrimitiveMesh(PrimitiveType.Cube); cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
            var previous = GameObject.Find("Practice presentation");
            if (previous) Object.DestroyImmediate(previous);
            var scenery = new GameObject("Practice presentation").transform;
            var dirt = Material("Arena loam", new Color(.78f, .74f, .67f), .08f);
            Texture2D normal;
            var albedo = DirtTextures(out normal);
            dirt.SetTexture("_BaseMap", albedo); dirt.SetTextureScale("_BaseMap", new Vector2(12, 17));
            dirt.SetTexture("_BumpMap", normal); dirt.SetFloat("_BumpScale", .38f); dirt.EnableKeyword("_NORMALMAP");
            var ground = GameObject.Find("Arena dirt");
            if (ground) ground.GetComponent<Renderer>().sharedMaterial = dirt;
            var steel = Material("Weathered ivory rail", new Color(.72f, .70f, .61f), .3f, .22f);
            var timber = Material("Aged cedar", new Color(.27f, .17f, .105f), .1f);
            var roof = Material("Oxide red roof", new Color(.30f, .10f, .065f), .22f, .2f);
            var teal = Material("Arena teal", new Color(.035f, .17f, .19f), .18f);
            var distant = Material("Distant ridge", new Color(.43f, .45f, .42f), .02f);
            var red = Material("Enamel vermilion", new Color(.61f, .065f, .035f), .34f, .25f);
            var cream = Material("Barrel ivory", new Color(.89f, .81f, .61f), .3f, .2f);
            var rut = Material("Compacted dirt", new Color(.32f, .285f, .23f), .05f);
            var batch = new StaticGeometry(scenery, includeAlleyRails ? "" : "Reins ");
            batch.Add(cube, dirt, new Vector3(0, -.36f, 30), new Vector3(210, .25f, 220));

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string n = renderer.gameObject.name;
                if (n == "Fence post" || n == "Fence rail" || n == "Gate left" || n == "Gate right") renderer.enabled = false;
            }
            Fence(batch, new Vector3(-28, 0, -16), new Vector3(-28, 0, 64), steel);
            Fence(batch, new Vector3(28, 0, -16), new Vector3(28, 0, 64), steel);
            Fence(batch, new Vector3(-28, 0, 64), new Vector3(28, 0, 64), steel);
            Fence(batch, new Vector3(-28, 0, -16), new Vector3(-6, 0, -16), steel);
            Fence(batch, new Vector3(6, 0, -16), new Vector3(28, 0, -16), steel);
            // Alley rails frame the launch without introducing any new collider.
            if (includeAlleyRails)
            {
                Fence(batch, new Vector3(-5.4f, 0, -16), new Vector3(-5.4f, 0, -1), steel);
                Fence(batch, new Vector3(5.4f, 0, -16), new Vector3(5.4f, 0, -1), steel);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 5; row++)
                {
                    float x = side * (31 + row * 1.55f), y = .4f + row * .53f;
                    batch.Add(cube, timber, new Vector3(x, y, 29), new Vector3(1.5f, .22f, 48));
                    batch.Add(cube, teal, new Vector3(x + side * .3f, y + .45f, 29), new Vector3(.12f, .7f, 48));
                    for (int person = 0; person < 17; person++)
                    {
                        if (!includeCrowd || (row * 7 + person * 11) % 5 == 0) continue;
                        float z = 7 + person * 2.7f;
                        Material shirt = (row + person) % 3 == 0 ? cream : (person % 3 == 1 ? roof : teal);
                        batch.Add(sphere, shirt, new Vector3(x, y + .64f, z), new Vector3(.43f, .67f, .33f));
                        batch.Add(sphere, cream, new Vector3(x, y + 1.05f, z), Vector3.one * .26f);
                    }
                }
                for (int post = 0; post < 7; post++)
                    batch.Add(cube, timber, new Vector3(side * 38.5f, 3.4f, 3 + post * 8.5f), new Vector3(.28f, 6.8f, .28f));
                batch.Add(cube, roof, new Vector3(side * 36, 6.7f, 29), new Vector3(14, .18f, 54), Quaternion.Euler(0, 0, side * -7));
                for (int sign = 0; sign < 5; sign++)
                {
                    float z = 5 + sign * 11;
                    batch.Add(cube, sign % 2 == 0 ? teal : roof, new Vector3(side * 27.9f, 1, z), new Vector3(.12f, 1.35f, 7));
                    batch.Add(cube, cream, new Vector3(side * 27.79f, 1.08f, z), new Vector3(.13f, .17f, 5.9f));
                    batch.Add(cube, cream, new Vector3(side * 27.79f, .68f, z), new Vector3(.13f, .1f, 3.2f));
                }
            }
            // One low irregular heightfield gives the horizon depth without round primitive hills.
            batch.Add(SaveMesh("Original horizon ridge", HorizonRidge()), distant, Vector3.zero, Vector3.one);
            // Score gantry sits above the existing gate/score line.
            batch.Add(cube, timber, new Vector3(-6, 2.8f, 0), new Vector3(.32f, 5.6f, .32f));
            batch.Add(cube, timber, new Vector3(6, 2.8f, 0), new Vector3(.32f, 5.6f, .32f));
            batch.Add(cube, teal, new Vector3(0, 5.28f, 0), new Vector3(12.4f, .72f, .22f));
            for (int i = 0; i < 7; i++) batch.Add(cube, cream, new Vector3(-4.2f + i * 1.4f, 5.28f, -.14f), new Vector3(.7f, .14f, .07f));
            for (int i = 1; i <= 3; i++)
            {
                var b = GameObject.Find("Barrel " + i);
                if (!b) continue;
                foreach (var r in b.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = r.name == "Drum" ? red : cream;
                var ring = Ring(2.12f, 2.3f, 64);
                batch.Add(ring, rut, b.transform.position + Vector3.up * .013f, Vector3.one);
            }
            batch.Flush();
            ConfigureLight(camera);
            var visual = Horse(horse);
            AssetDatabase.SaveAssets();
            Debug.Log("BARREL_ART: original procedural arena and articulated prototype horse generated; no race coordinates or colliders changed.");
            return visual;
        }

        private static PracticeHorseVisual Horse(Transform horse)
        {
            foreach (var r in horse.GetComponentsInChildren<Renderer>()) r.enabled = false;
            var previous = horse.Find("Articulated horse");
            if (previous) Object.DestroyImmediate(previous.gameObject);
            var body = Joint("Articulated horse", horse, Vector3.zero);
            body.localScale = Vector3.one * .88f;
            Material coat = Material("Chestnut coat", new Color(.30f, .115f, .047f), .30f);
            Material muzzle = Material("Muzzle and hooves", new Color(.068f, .055f, .043f), .22f);
            Material mane = Material("Dark mane", new Color(.045f, .025f, .015f), .13f);
            Material leather = Material("Oiled saddle leather", new Color(.14f, .062f, .029f), .24f);
            Material blanket = Material("Woven turquoise blanket", new Color(.045f, .25f, .26f), .05f);
            Material marking = Material("Ivory markings", new Color(.87f, .80f, .64f), .1f);
            Material eye = Material("Deep brown eyes", new Color(.015f, .009f, .006f), .7f);
            Material buckle = Material("Brass tack", new Color(.59f, .38f, .13f), .58f, .55f);
            Mesh barrelMesh = SaveMesh("Horse barrel", Loft(new[] {
                new Vector4(-1.04f, 1.22f, .16f, .22f), new Vector4(-.81f, 1.23f, .36f, .41f),
                new Vector4(-.45f, 1.26f, .415f, .46f), new Vector4(.06f, 1.27f, .41f, .47f),
                new Vector4(.49f, 1.34f, .355f, .46f), new Vector4(.76f, 1.34f, .25f, .34f),
                new Vector4(.84f, 1.36f, .10f, .2f) }, 20));
            Part("Sculpted chestnut body", barrelMesh, coat, body, Vector3.zero, Vector3.one);
            Mesh neck = SaveMesh("Horse neck", Loft(new[] {
                new Vector4(.36f, 1.63f, .25f, .28f), new Vector4(.52f, 1.80f, .23f, .40f),
                new Vector4(.70f, 1.98f, .175f, .39f), new Vector4(.88f, 2.11f, .15f, .28f),
                new Vector4(1.02f, 2.13f, .115f, .18f) }, 16));
            Part("Arched neck", neck, coat, body, Vector3.zero, Vector3.one);
            Segment("Neck mane", body, new Vector3(0, 1.77f, .26f), new Vector3(0, 2.34f, .78f), .14f, mane);
            var head = Joint("Head joint", body, new Vector3(0, 2.14f, .9f));
            Mesh headMesh = SaveMesh("Horse head", Loft(new[] {
                new Vector4(-.15f, .03f, .12f, .20f), new Vector4(.02f, .035f, .18f, .205f),
                new Vector4(.23f, -.045f, .145f, .17f), new Vector4(.48f, -.15f, .12f, .12f),
                new Vector4(.62f, -.17f, .13f, .10f) }, 16));
            Part("Equine head", headMesh, coat, head, Vector3.zero, Vector3.one);
            Part("Velvet muzzle", sphere, muzzle, head, new Vector3(0, -.16f, .57f), new Vector3(.29f, .23f, .3f));
            Part("Forehead blaze", sphere, marking, head, new Vector3(0, .09f, .18f), new Vector3(.067f, .10f, .36f), Quaternion.Euler(16, 0, 0));
            var ears = new Transform[2];
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1 : 1;
                ears[side] = Joint("Ear joint " + side, head, new Vector3(sign * .115f, .17f, -.06f));
                ears[side].localRotation = Quaternion.Euler(-9, 0, sign * -12);
                Part("Tapered ear", sphere, coat, ears[side], new Vector3(0, .095f, 0), new Vector3(.09f, .25f, .09f));
                Part("Inner ear", sphere, muzzle, ears[side], new Vector3(0, .105f, .032f), new Vector3(.051f, .165f, .022f));
                Part("Eye", sphere, eye, head, new Vector3(sign * .163f, .025f, .1f), new Vector3(.045f, .043f, .071f));
                Part("Nostril", sphere, mane, head, new Vector3(sign * .127f, -.13f, .62f), new Vector3(.034f, .048f, .069f));
                Segment("Cheek strap", head, new Vector3(sign * .185f, .12f, -.04f), new Vector3(sign * .132f, -.16f, .48f), .025f, leather);
                Segment("Rein", body, new Vector3(sign * .16f, 1.98f, 1.40f), new Vector3(sign * .32f, 1.82f, -.35f), .012f, leather);
            }
            Segment("Noseband", head, new Vector3(-.145f, -.066f, .45f), new Vector3(.145f, -.066f, .45f), .028f, leather);
            var hips = new Transform[4]; var knees = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2; float x = i % 2 == 0 ? -.285f : .285f;
                hips[i] = Joint((front ? "Foreleg " : "Hindleg ") + i, body, new Vector3(x, 1.23f, front ? .54f : -.70f));
                Part("Upper leg", sphere, coat, hips[i], new Vector3(0, -.245f, front ? 0 : .025f), new Vector3(front ? .19f : .30f, .64f, front ? .22f : .35f));
                knees[i] = Joint("Lower leg joint", hips[i], new Vector3(0, -.55f, front ? .015f : .035f));
                Part("Knee", sphere, coat, knees[i], Vector3.zero, new Vector3(.15f, .17f, .15f));
                Part("Cannon bone", sphere, coat, knees[i], new Vector3(0, -.23f, 0), new Vector3(.10f, .48f, .115f));
                Part("Fetlock", sphere, marking, knees[i], new Vector3(0, -.45f, 0), new Vector3(.12f, .16f, .13f));
                Part("Hoof", sphere, muzzle, knees[i], new Vector3(0, -.57f, .025f), new Vector3(.18f, .20f, .24f));
            }
            Part("Saddle blanket", sphere, blanket, body, new Vector3(0, 1.62f, -.13f), new Vector3(.91f, .23f, .99f));
            Part("Western saddle seat", sphere, leather, body, new Vector3(0, 1.76f, -.16f), new Vector3(.69f, .23f, .66f));
            Part("Cantle", sphere, leather, body, new Vector3(0, 1.86f, -.42f), new Vector3(.67f, .25f, .17f));
            Part("Saddle horn", cylinder, leather, body, new Vector3(0, 1.94f, .14f), new Vector3(.085f, .11f, .085f));
            Part("Horn cap", sphere, leather, body, new Vector3(0, 2.05f, .14f), new Vector3(.16f, .065f, .12f));
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Segment("Saddle fender", body, new Vector3(sign * .35f, 1.76f, -.05f), new Vector3(sign * .49f, .95f, .02f), .09f, leather);
                Part("Stirrup", cube, buckle, body, new Vector3(sign * .49f, .91f, .035f), new Vector3(.09f, .035f, .22f));
            }
            var tail = Joint("Tail joint", body, new Vector3(0, 1.47f, -1));
            Segment("Tail dock", tail, Vector3.zero, new Vector3(0, -.36f, -.28f), .18f, coat);
            Part("Flowing tail", sphere, mane, tail, new Vector3(0, -.55f, -.34f), new Vector3(.23f, 1.05f, .26f), Quaternion.Euler(18, 0, 0));
            var animator = horse.GetComponent<PracticeHorseVisual>();
            if (!animator) animator = horse.gameObject.AddComponent<PracticeHorseVisual>();
            animator.Configure(body, head, tail, hips, knees, ears);
            return animator;
        }

        private static void ConfigureLight(Camera camera)
        {
            var sun = RenderSettings.sun;
            if (!sun) sun = new GameObject("Practice sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1, .975f, .93f); sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .8f; sun.shadowBias = .035f; sun.shadowNormalBias = .22f;
            sun.transform.rotation = Quaternion.Euler(37, -38, 0); RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.54f, .65f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.47f, .46f, .42f);
            RenderSettings.ambientGroundColor = new Color(.25f, .235f, .21f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70; RenderSettings.fogEndDistance = 180; RenderSettings.fogColor = new Color(.64f, .72f, .74f);
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader)
            {
                var sky = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Rodeo sky.mat");
                if (!sky) { sky = new Material(skyShader); AssetDatabase.CreateAsset(sky, Root + "/Rodeo sky.mat"); }
                sky.SetColor("_SkyTint", new Color(.49f, .56f, .64f)); sky.SetColor("_GroundColor", new Color(.49f, .46f, .40f));
                sky.SetFloat("_AtmosphereThickness", .85f); sky.SetFloat("_Exposure", 1.15f); sky.SetFloat("_SunSize", .035f);
                RenderSettings.skybox = sky; EditorUtility.SetDirty(sky);
            }
            camera.clearFlags = CameraClearFlags.Skybox; camera.farClipPlane = 220;
            var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(FoundationBuilder.PipelinePath);
            if (source)
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Practice mobile pipeline.asset");
                if (!pipeline) { pipeline = Object.Instantiate(source); AssetDatabase.CreateAsset(pipeline, Root + "/Practice mobile pipeline.asset"); }
                pipeline.supportsHDR = false; pipeline.msaaSampleCount = 2; pipeline.renderScale = 1;
                // These setters are internal in pinned URP 17.6; author its serialized settings.
                var serialized = new SerializedObject(pipeline);
                serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
                serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.mainLightShadowmapResolution = 2048; pipeline.shadowDistance = 65; pipeline.shadowCascadeCount = 2;
                GraphicsSettings.defaultRenderPipeline = pipeline;
                int quality = QualitySettings.GetQualityLevel();
                for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
                QualitySettings.SetQualityLevel(quality, false); EditorUtility.SetDirty(pipeline);
            }
        }

        private static Texture2D DirtTextures(out Texture2D normal)
        {
            const int size = 512; var heights = new float[size * size]; var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float broad = Noise(x, y, size, 8), medium = Noise(x, y, size, 32), grain = Hash(x, y);
                float rake = Mathf.Pow(.5f + .5f * Mathf.Sin(y * Mathf.PI * 2 / size * 64 + broad * 5), 14);
                float h = broad * .45f + medium * .32f + grain * .15f - rake * .08f;
                heights[y * size + x] = h;
                float tone = Mathf.Clamp01(.27f + h * .70f + (grain > .985f ? .14f : 0));
                colors[y * size + x] = Color.Lerp(new Color(.45f, .405f, .34f), new Color(.82f, .75f, .65f), tone);
            }
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false) { name = "Original raked dirt albedo", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            albedo.SetPixels(colors); albedo.Apply(true, false);
            albedo = SaveAsset(albedo, "Original dirt albedo.asset");
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = heights[y * size + (x + 1) % size] - heights[y * size + (x + size - 1) % size];
                float dy = heights[((y + 1) % size) * size + x] - heights[((y + size - 1) % size) * size + x];
                Vector3 n = new Vector3(-dx * 2, -dy * 2, 1).normalized;
                colors[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
            }
            // Native linear RGBA asset: RGB tangent normals, alpha=1; no PNG importer conversion needed.
            normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "Original raked dirt normal", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            normal.SetPixels(colors); normal.Apply(true, false); normal = SaveAsset(normal, "Original dirt normal.asset");
            return albedo;
        }

        private static Mesh HorizonRidge()
        {
            const int segments = 112, rings = 5;
            var vertices = new Vector3[(segments + 1) * rings];
            var uv = new Vector2[vertices.Length];
            var indices = new int[segments * (rings - 1) * 6];
            float[] radii = { 78, 96, 124, 153, 186 };
            float[] profiles = { 0, .28f, 1, .48f, 0 };
            int triangle = 0;
            for (int r = 0; r < rings; r++) for (int i = 0; i <= segments; i++)
            {
                float a = (i % segments) * Mathf.PI * 2 / segments;
                // Integer-frequency waves close the seam and create unequal shoulders and summits.
                float height = 10 + 7 * Mathf.Sin(a * 3 + 1.1f) + 5 * Mathf.Sin(a * 7 + .4f)
                    + 2.6f * Mathf.Sin(a * 13 + 2.1f) + 1.7f * Mathf.Sin(a * 19 + .7f);
                height = Mathf.Max(3, height);
                float radius = radii[r] + Mathf.Sin(a * 5 + r * .8f) * (r == 0 ? 0 : 5);
                int n = r * (segments + 1) + i;
                vertices[n] = new Vector3(Mathf.Sin(a) * radius, -2 + height * profiles[r], 30 + Mathf.Cos(a) * radius);
                uv[n] = new Vector2(i / (float)segments, r / (float)(rings - 1));
                if (r == 0 || i == 0) continue;
                int inner = n - segments - 2;
                indices[triangle++] = inner; indices[triangle++] = n - 1; indices[triangle++] = n;
                indices[triangle++] = inner; indices[triangle++] = n; indices[triangle++] = inner + 1;
            }
            var mesh = new Mesh { vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static float Noise(int x, int y, int size, int frequency)
        {
            float fx = x / (float)size * frequency, fy = y / (float)size * frequency;
            int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy); float u = fx - ix, v = fy - iy;
            u = u * u * (3 - 2 * u); v = v * v * (3 - 2 * v);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash((ix + 1) % frequency, iy), u), Mathf.Lerp(Hash(ix, (iy + 1) % frequency), Hash((ix + 1) % frequency, (iy + 1) % frequency), u), v);
        }
        private static float Hash(int x, int y)
        {
            unchecked { uint h = (uint)(x * 374761393 + y * 668265263) + 413u; h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 65535u) / 65535f; }
        }
        private static Material Material(string name, Color color, float smoothness, float metallic = 0)
        {
            string path = Root + "/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.name = name;
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", smoothness); m.SetFloat("_Metallic", metallic); m.enableInstancing = true;
            EditorUtility.SetDirty(m); return m;
        }
        private static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (type == PrimitiveType.Sphere)
            {
                var rings = new Vector4[13];
                for (int i = 0; i < rings.Length; i++)
                {
                    float a = Mathf.Lerp(-Mathf.PI * .5f, Mathf.PI * .5f, i / 12f);
                    float radius = Mathf.Max(.0001f, Mathf.Cos(a) * .5f);
                    rings[i] = new Vector4(Mathf.Sin(a) * .5f, 0, radius, radius);
                }
                return SaveMesh("Rounded original primitive", Loft(rings, 20));
            }
            var go = GameObject.CreatePrimitive(type); var mesh = go.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(go); return mesh;
        }
        private static Transform Joint(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        private static GameObject Part(string name, Mesh mesh, Material material, Transform parent, Vector3 position, Vector3 scale, Quaternion? rotation = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.transform.localRotation = rotation ?? Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; return go;
        }
        private static void Segment(string name, Transform parent, Vector3 a, Vector3 b, float width, Material material)
        {
            Part(name, cylinder, material, parent, (a + b) * .5f, new Vector3(width, Vector3.Distance(a, b) * .5f, width), Quaternion.FromToRotation(Vector3.up, b - a));
        }
        private static void Fence(StaticGeometry batch, Vector3 a, Vector3 b, Material material)
        {
            float length = Vector3.Distance(a, b); int count = Mathf.CeilToInt(length / 3.1f);
            for (int i = 0; i <= count; i++) batch.Add(cylinder, material, Vector3.Lerp(a, b, i / (float)count) + Vector3.up * .9f, new Vector3(.115f, .9f, .115f));
            for (int i = 0; i < 3; i++) batch.Add(cylinder, material, (a + b) * .5f + Vector3.up * (.42f + i * .55f), new Vector3(.075f, length * .5f, .075f), Quaternion.FromToRotation(Vector3.up, b - a));
        }
        private static Mesh Loft(Vector4[] rings, int sides)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int r = 0; r < rings.Length; r++) for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides; var p = rings[r];
                vertices.Add(new Vector3(Mathf.Cos(a) * p.z, p.y + Mathf.Sin(a) * p.w, p.x)); uv.Add(new Vector2(i / (float)sides, r / (float)(rings.Length - 1)));
                if (r == 0 || i == 0) continue;
                int n = r * (sides + 1) + i, prev = n - sides - 1;
                triangles.Add(prev - 1); triangles.Add(prev); triangles.Add(n); triangles.Add(prev - 1); triangles.Add(n); triangles.Add(n - 1);
            }
            for (int end = 0; end < 2; end++)
            {
                int r = end == 0 ? 0 : rings.Length - 1, start = r * (sides + 1), center = vertices.Count;
                vertices.Add(new Vector3(0, rings[r].y, rings[r].x)); uv.Add(Vector2.one * .5f);
                for (int i = 0; i < sides; i++) { triangles.Add(center); triangles.Add(start + (end == 0 ? i + 1 : i)); triangles.Add(start + (end == 0 ? i : i + 1)); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh Ring(float inner, float outer, int count)
        {
            var v = new Vector3[(count + 1) * 2]; var indices = new int[count * 6];
            for (int i = 0; i <= count; i++)
            {
                float a = i * Mathf.PI * 2 / count; Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); v[i * 2] = p * inner; v[i * 2 + 1] = p * outer;
                if (i == count) continue;
                int n = i * 2, t = i * 6; indices[t] = n; indices[t + 1] = n + 2; indices[t + 2] = n + 1; indices[t + 3] = n + 1; indices[t + 4] = n + 2; indices[t + 5] = n + 3;
            }
            var mesh = new Mesh { vertices = v, triangles = indices }; mesh.RecalculateNormals(); return mesh;
        }
        private static Mesh SaveMesh(string name, Mesh mesh) { mesh.name = name; return PersistentMeshAsset.Save(mesh, Root + "/" + name + ".asset"); }
        private static T SaveAsset<T>(T value, string file) where T : Object
        {
            string path = Root + "/" + file; var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(value, path); return value; }
            EditorUtility.CopySerialized(value, existing); Object.DestroyImmediate(value); EditorUtility.SetDirty(existing); return existing;
        }
        private sealed class StaticGeometry
        {
            private readonly Transform parent;
            private readonly string variant;
            private readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
            public StaticGeometry(Transform root, string meshVariant = "") { parent = root; variant = meshVariant; }
            public void Add(Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion? rotation = null)
            {
                if (!groups.TryGetValue(material, out var list)) { list = new List<CombineInstance>(); groups.Add(material, list); }
                list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation ?? Quaternion.identity, scale) });
            }
            public void Flush()
            {
                foreach (var item in groups)
                {
                    var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(item.Value.ToArray(), true, true); mesh.RecalculateBounds();
                    mesh = SaveMesh(variant + "Arena batch " + item.Key.name, mesh);
                    var go = Part("Arena batch " + item.Key.name, mesh, item.Key, parent, Vector3.zero, Vector3.one); go.isStatic = true;
                    if (item.Key.name == "Distant ridge" || item.Key.name == "Dry foothills" || item.Key.name == "Compacted dirt") go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }
    }
}
