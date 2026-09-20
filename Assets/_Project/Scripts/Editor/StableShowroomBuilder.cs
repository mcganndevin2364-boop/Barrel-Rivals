using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>
    /// Original enclosed showroom geometry. Photographic maps reuse the reviewed CC0 arena sources;
    /// no reference-image pixels, colliders or gameplay behaviours are added. Lighting stays bounded
    /// to one shadowed sun, one soft horse fill and one local lantern pool.
    /// </summary>
    public static class StableShowroomBuilder
    {
        public const string Root = "Assets/_Project/Art/Reins/Stable/Showroom";
        private static readonly Dictionary<Material, Shape> Batches = new Dictionary<Material, Shape>();
        private static Transform world;
        private static Material wood, beam, iron, soil, straw, paleStraw, rope, glass, lamp;
        private static System.Random random;

        public static void Build(Camera camera, Transform horse)
        {
            if (!camera || !horse) throw new ArgumentException("A stable camera and horse are required.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Meshes");
            AssetDatabase.Refresh();
            var previous = GameObject.Find("Original timber stable");
            if (previous) Object.DestroyImmediate(previous);
            world = new GameObject("Original timber stable").transform;
            Batches.Clear();
            random = new System.Random(39117);

            wood = Surface("Oiled weatherboards", "Weathered wood", new Color(.90f, .77f, .60f), .075f, .23f);
            beam = Surface("Aged oak framing", "Weathered wood", new Color(.57f, .43f, .29f), .045f, .31f);
            iron = Surface("Blackened stall iron", "Worn painted steel", new Color(.19f, .19f, .17f), .20f, .20f, .48f);
            soil = Surface("Fine packed stable earth", "Arena soil", new Color(.72f, .63f, .49f), .018f, .055f);
            straw = Plain("Golden straw", new Color(.40f, .285f, .12f), .025f);
            paleStraw = Plain("Dry straw highlights", new Color(.60f, .45f, .22f), .025f);
            rope = Plain("Natural binding twine", new Color(.30f, .23f, .13f), .035f);
            glass = WindowMaterial();
            lamp = Plain("Lantern candle glass", new Color(.15f, .055f, .012f), .035f);
            // The glass itself glows; its one nearby point light supplies the wall/bench pool.
            // AnyEmissive preserves URP's keyword after import, not a baked-lighting claim.
            lamp.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            lamp.EnableKeyword("_EMISSION");
            lamp.SetColor("_EmissionColor", new Color(1, .45f, .105f) * .90f);
            lamp.SetFloat("_SpecularHighlights", 0);
            lamp.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(lamp);

            Architecture();
            Stalls();
            Furnishings();
            Bedding();
            Flush();
            Lighting(camera);
            // No floor platform: the hooves remain planted directly on the fine earth and loose straw.
            // Frame the whole horse between the roster and inspector, including ears and hooves.
            // The slightly forward three-quarter angle keeps its face and equipped tack readable.
            camera.transform.position = new Vector3(3.65f, 2.35f, 5.25f);
            camera.transform.LookAt(horse.position + new Vector3(0, 1.18f, .48f));
            camera.fieldOfView = 43;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 45;
            AssetDatabase.SaveAssets();
        }

        private static void Architecture()
        {
            Box(soil, new Vector3(0, -.055f, 2.2f), new Vector3(10.6f, .10f, 14.0f), Quaternion.identity, 1.55f);

            // Close rear wall, with a real window opening. Fine board-scale UVs replace the former
            // stretched primitive maps; opaque walls prevent an unbounded horizon through the stable.
            const float back = -3.55f, plank = .22f;
            for (int i = 0; i < 47; i++)
            {
                float x = -5.06f + i * plank;
                bool atWindow = x > -3.67f && x < -.35f;
                if (atWindow)
                {
                    Box(wood, new Vector3(x, .53f, back), new Vector3(plank - .009f, 1.06f, .115f));
                    Box(wood, new Vector3(x, 3.89f, back), new Vector3(plank - .009f, .62f, .115f));
                }
                else Box(wood, new Vector3(x, 2.1f, back), new Vector3(plank - .009f, 4.2f, .115f));
            }
            Window(new Vector3(-2.02f, 2.32f, back + .10f), 3.42f, 2.56f);

            // Enclosed side walls and ceiling also give a plausible depth silhouette when the horse rotates.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 58; i++)
                    Box(wood, new Vector3(side * 5.2f, 2.1f, -3.45f + i * plank), new Vector3(.12f, 4.2f, plank - .009f));
                Box(beam, new Vector3(side * 5.10f, .18f, 2.75f), new Vector3(.16f, .29f, 12.7f));
                Box(beam, new Vector3(side * 5.06f, 3.82f, 2.75f), new Vector3(.24f, .24f, 12.7f));
            }
            for (int i = 0; i < 48; i++)
                Box(beam, new Vector3(-5.17f + i * .22f, 4.20f, 2.50f), new Vector3(.21f, .09f, 12.25f));
            foreach (float z in new[] { -3.42f, -.9f, 2.45f, 5.7f })
            {
                Box(beam, new Vector3(0, 3.95f, z), new Vector3(10.45f, .25f, .23f));
                foreach (int side in new[] { -1, 1 })
                {
                    // Frame the edges of the room; no foreground post is placed across the horse.
                    Box(beam, new Vector3(side * 4.88f, 1.94f, z), new Vector3(.28f, 3.88f, .27f));
                    Beam(beam, new Vector3(side * 4.85f, 2.96f, z), new Vector3(side * 4.08f, 3.83f, z), .16f, .16f);
                    Box(iron, new Vector3(side * 4.88f, .37f, z), new Vector3(.303f, .13f, .293f));
                }
            }
            Box(beam, new Vector3(0, .17f, back + .12f), new Vector3(10.3f, .27f, .13f));
            // A warm timber threshold and strips of fine stone at the wall edges ground the room.
            Box(beam, new Vector3(0, -.005f, -3.26f), new Vector3(10.2f, .055f, .16f));
        }

        private static void Window(Vector3 center, float width, float height)
        {
            // One physical frosted pane recessed behind the wooden sash. Normalized UVs keep
            // its original transmission gradient continuous across all twelve framed openings.
            var pane = center + new Vector3(0, 0, -.17f);
            var horizontal = Vector3.right * (width - .08f) * .5f;
            var vertical = Vector3.up * (height - .08f) * .5f;
            Batch(glass).Quad(pane-horizontal-vertical,pane+horizontal-vertical,
                pane+horizontal+vertical,pane-horizontal+vertical,1,1);
            foreach (int side in new[] { -1, 1 })
            {
                Box(beam, center + new Vector3(side * width * .5f, 0, 0), new Vector3(.12f, height + .20f, .22f));
                Box(beam, center + new Vector3(0, side * height * .5f, 0), new Vector3(width + .25f, .14f, .24f));
                // Shallow inner reveal catches the grazing sun and gives the aperture depth.
                Box(wood, center + new Vector3(side * (width * .5f - .08f), 0, -.08f), new Vector3(.05f, height - .12f, .16f));
                Box(wood, center + new Vector3(0, side * (height * .5f - .08f), -.08f), new Vector3(width - .12f, .045f, .16f));
            }
            for (int i = 1; i < 6; i++)
                Box(beam, center + new Vector3(-width * .5f + i * width / 6, 0, .015f), new Vector3(.065f, height, .09f));
            Box(beam, center, new Vector3(width, .075f, .10f));
            Box(wood, center + new Vector3(0, -height * .5f - .055f, .16f), new Vector3(width + .34f, .11f, .54f));
        }

        private static void Stalls()
        {
            // Two shallow stall fronts create layered joinery behind the horse without blocking its outline.
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 3.61f;
                for (int i = 0; i < 10; i++)
                    Box(wood, new Vector3(x - .9f + i * .20f, .61f, -1.90f), new Vector3(.19f, 1.22f, .075f));
                foreach (float end in new[] { -.99f, .99f })
                {
                    Box(beam, new Vector3(x + end, 1.47f, -1.88f), new Vector3(.15f, 2.94f, .17f));
                    Box(iron, new Vector3(x + end, 2.99f, -1.88f), new Vector3(.18f, .09f, .20f));
                }
                foreach (float y in new[] { .18f, 1.19f, 1.32f, 2.69f })
                    Box(y < 1.3f ? beam : iron, new Vector3(x, y, -1.85f), new Vector3(1.95f, .07f, .10f));
                for (int i = 0; i < 13; i++)
                    Rod(iron, new Vector3(x - .86f + i * .145f, 1.35f, -1.85f), new Vector3(x - .86f + i * .145f, 2.65f, -1.85f), .012f, 8);
                Beam(beam, new Vector3(x - .90f, .23f, -1.79f), new Vector3(x + .90f, 1.14f, -1.79f), .075f, .055f);
                Box(iron, new Vector3(x + .57f, 1.09f, -1.75f), new Vector3(.24f, .055f, .055f));
            }
        }

        private static void Furnishings()
        {
            Lantern(new Vector3(1.40f, 2.54f, -3.31f));
            Lantern(new Vector3(-4.45f, 2.58f, -1.66f));
            Lantern(new Vector3(4.53f, 2.62f, .58f));

            // A workbench, open slatted crates, hay rack and loose bales use actual geometry.
            Box(wood, new Vector3(1.94f, .80f, -2.90f), new Vector3(1.76f, .10f, .69f));
            foreach (float x in new[] { 1.25f, 2.63f })
                foreach (float z in new[] { -3.13f, -2.66f })
                    Box(beam, new Vector3(x, .385f, z), new Vector3(.10f, .77f, .10f));
            Box(wood, new Vector3(1.94f, .23f, -2.90f), new Vector3(1.57f, .075f, .53f));
            Crate(new Vector3(2.54f, .27f, -2.03f), new Vector3(.68f, .54f, .59f), -11);
            Crate(new Vector3(3.92f, .25f, -.83f), new Vector3(.65f, .50f, .70f), 9);
            Crate(new Vector3(4.05f, .74f, -.84f), new Vector3(.56f, .47f, .52f), -4);
            Bale(new Vector3(-2.42f, .26f, -1.89f), new Vector3(.88f, .51f, .58f), 14);
            Bale(new Vector3(-3.26f, .245f, -.57f), new Vector3(.89f, .48f, .58f), -9);
            Bale(new Vector3(-3.67f, .24f, .11f), new Vector3(.87f, .47f, .58f), 8);
            Bale(new Vector3(-3.45f, .72f, -.09f), new Vector3(.82f, .45f, .55f), -13);

            // Closed feed bin and its hand-forged hoops, with a shallow bowl on the workbench.
            Barrel(new Vector3(3.56f, 0, -2.85f), .31f, .79f);
            Rod(iron, new Vector3(1.68f, .87f, -2.93f), new Vector3(1.68f, 1.02f, -2.93f), .125f, 24);
            Ring(iron, new Vector3(1.68f, 1.025f, -2.93f), .127f, .008f);
        }

        private static void Crate(Vector3 center, Vector3 size, float yaw)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            for (int level = 0; level < 4; level++)
            {
                float y = -size.y * .5f + .065f + level * (size.y - .13f) / 3;
                foreach (int side in new[] { -1, 1 })
                {
                    Box(wood, center + rotation * new Vector3(0, y, side * size.z * .5f), new Vector3(size.x, .104f, .026f), rotation);
                    Box(wood, center + rotation * new Vector3(side * size.x * .5f, y, 0), new Vector3(.027f, .104f, size.z), rotation);
                }
            }
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Box(beam, center + rotation * new Vector3(x * (size.x * .5f - .022f), 0, z * (size.z * .5f - .022f)), new Vector3(.047f, size.y + .015f, .047f), rotation);
            Box(wood, center - Vector3.up * (size.y * .5f - .015f), new Vector3(size.x, .035f, size.z), rotation);
        }

        private static void Bale(Vector3 center, Vector3 size, float yaw)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            Box(straw, center, size * .96f, rotation);
            // Irregular long stems break up the flat bale faces; deterministic generation is editor-only.
            for (int i = 0; i < 125; i++)
            {
                bool top = i % 3 == 0;
                float x = Range(-.44f, .44f) * size.x;
                float y = top ? size.y * .493f : Range(-.47f, .47f) * size.y;
                float z = top ? Range(-.49f, .49f) * size.z : (i % 2 == 0 ? -1 : 1) * size.z * .493f;
                var from = new Vector3(x - size.x * .22f, y, z);
                var to = from + new Vector3(size.x * Range(.29f, .46f), Range(-.015f, .015f), Range(-.009f, .009f));
                Rod(i % 4 == 0 ? paleStraw : straw, center + rotation * from, center + rotation * to, .0024f, 3);
            }
            foreach (float x in new[] { -.29f, .29f })
            {
                var a = new Vector3(x * size.x, size.y * .51f, -size.z * .51f);
                var b = new Vector3(x * size.x, size.y * .51f, size.z * .51f);
                var c = new Vector3(x * size.x, -size.y * .51f, size.z * .51f);
                var d = new Vector3(x * size.x, -size.y * .51f, -size.z * .51f);
                Rod(rope, center + rotation * a, center + rotation * b, .005f, 6);
                Rod(rope, center + rotation * b, center + rotation * c, .005f, 6);
                Rod(rope, center + rotation * c, center + rotation * d, .005f, 6);
                Rod(rope, center + rotation * d, center + rotation * a, .005f, 6);
            }
        }

        private static void Bedding()
        {
            for (int i = 0; i < 980; i++)
            {
                float x = Range(-4.85f, 4.85f), z = Range(-3.35f, 5.9f);
                // Sparse clean grooming area; denser straw bedding near the stalls and timber edges.
                if (Mathf.Abs(x) < 2.1f && z > -.6f && random.NextDouble() < .77) continue;
                float angle = Range(0, Mathf.PI * 2), length = Range(.055f, .22f);
                var start = new Vector3(x, .002f + Range(0, .013f), z);
                var end = start + new Vector3(Mathf.Cos(angle) * length, Range(0, .007f), Mathf.Sin(angle) * length);
                var across = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)) * Range(.0015f, .0038f);
                Batch(i % 4 == 0 ? paleStraw : straw).Quad(start - across, start + across, end + across, end - across, .06f, .35f);
            }
        }

        private static void Barrel(Vector3 position, float radius, float height)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                Box(wood, position + new Vector3(Mathf.Sin(a) * radius, height * .5f, Mathf.Cos(a) * radius), new Vector3(.091f, height, .04f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
            }
            foreach (float y in new[] { .08f, height * .48f, height - .08f })
                Ring(iron, position + Vector3.up * y, radius + .024f, .013f);
            Rod(wood, position + Vector3.up * (height - .036f), position + Vector3.up * height, radius, 32);
        }

        private static void Lantern(Vector3 p)
        {
            Box(iron, p + new Vector3(0, .04f, -.09f), new Vector3(.18f, .35f, .035f));
            Box(lamp, p + new Vector3(0, -.017f, .07f), new Vector3(.13f, .22f, .105f));
            foreach (float y in new[] { -.16f, .13f })
                Box(iron, p + new Vector3(0, y, .07f), new Vector3(.22f, .045f, .19f));
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Rod(iron, p + new Vector3(x * .085f, -.15f, .07f + z * .066f), p + new Vector3(x * .085f, .13f, .07f + z * .066f), .008f, 6);
            Rod(iron, p + new Vector3(0, .15f, -.065f), p + new Vector3(0, .21f, .07f), .016f, 8);
        }

        private static void Lighting(Camera camera)
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // Lower, slightly cooler diffuse bounce leaves warm sunlight and the local lantern
            // readable as separate sources instead of flattening every wall into the same orange.
            RenderSettings.ambientSkyColor = new Color(.38f, .405f, .43f);
            RenderSettings.ambientEquatorColor = new Color(.30f, .26f, .22f);
            RenderSettings.ambientGroundColor = new Color(.145f, .115f, .08f);
            RenderSettings.reflectionIntensity = .24f;
            var sunObject = GameObject.Find("Stable evening sunlight");
            if (!sunObject) sunObject = new GameObject("Stable evening sunlight");
            var sun = sunObject.GetComponent<Light>();
            if (!sun) sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1, .81f, .58f);
            sun.intensity = 1.8f;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = .025f;
            sun.shadowNormalBias = .12f;
            // Through the rear opening toward the grooming floor. Existing sash/stall geometry
            // produces the bands; no transparent shaft planes or full-room haze are needed.
            sun.transform.rotation = Quaternion.Euler(18, 20, 0);
            RenderSettings.sun = sun;
            var fillObject = GameObject.Find("Stable soft lantern fill");
            if (!fillObject) fillObject = new GameObject("Stable soft lantern fill");
            var fill = fillObject.GetComponent<Light>();
            if (!fill) fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(.90f, .92f, 1);
            fill.intensity = 8;
            fill.range = 7.6f;
            fill.shadows = LightShadows.None;
            fill.transform.position = new Vector3(.95f, 2.9f, 2.25f);
            var poolObject = GameObject.Find("Stable rear lantern pool");
            if (!poolObject) poolObject = new GameObject("Stable rear lantern pool");
            var pool = poolObject.GetComponent<Light>();
            if (!pool) pool = poolObject.AddComponent<Light>();
            pool.type = LightType.Point;
            pool.color = new Color(1, .57f, .28f);
            pool.intensity = 3.2f;
            pool.range = 3.0f;
            pool.shadows = LightShadows.None;
            pool.transform.position = new Vector3(1.40f, 2.38f, -3.07f);
            camera.backgroundColor = new Color(.06f, .045f, .03f);
        }

        private static Material WindowMaterial()
        {
            // The old bright albedo plus 2.1 HDR emission clipped all pane detail to white under
            // the shared ACES grade. Keep the diffuse term near black and bound transmitted light.
            // This is a small original glass transmittance map, not an exterior photograph/sky card.
            const int width = 64, height = 128;
            string path = Root + "/Materials/Frosted pane transmission.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                    { name = "Original frosted pane transmission", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                AssetDatabase.CreateAsset(texture, path);
            }
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1), v = y / (float)(height - 1);
                var color = Color.Lerp(new Color(.48f, .31f, .16f), new Color(.70f, .62f, .48f), v);
                float warmCenter = Mathf.Exp(-((u-.40f)*(u-.40f)*7 + (v-.50f)*(v-.50f)*4)) * .11f;
                // Low-amplitude irregularity suggests old frosted glass without repeating a scene.
                float grain = (Mathf.PerlinNoise(u*8.2f+1.7f,v*14.4f+3.8f)-.5f)*.028f;
                pixels[y*width+x] = new Color(color.r+warmCenter+grain,
                    color.g+warmCenter*.83f+grain,color.b+warmCenter*.57f+grain,1);
            }
            texture.SetPixels(pixels);texture.Apply(false,false);EditorUtility.SetDirty(texture);
            var material = Plain("Frosted evening window", new Color(.025f, .025f, .025f), .025f);
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_EmissionMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_EmissionColor", Color.white * .85f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.EnableKeyword("_EMISSION");
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_EnvironmentReflections", 0);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Surface(string name, string source, Color tint, float smoothness, float normal, float metallic = 0)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var original = AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root + "/Materials/" + source + ".mat");
                if (!original) throw new InvalidOperationException("Missing reviewed source material: " + source);
                material = Object.Instantiate(original);
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_BumpScale", normal);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureScale("_BumpMap", Vector2.one);
            material.SetTextureScale("_MetallicGlossMap", Vector2.one);
            material.SetFloat("_Cull", (float)CullMode.Back);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Plain(string name, Color tint, float smoothness)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Cull", (float)CullMode.Back);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shape Batch(Material material)
        {
            if (!Batches.TryGetValue(material, out var shape)) Batches.Add(material, shape = new Shape());
            return shape;
        }
        private static void Box(Material material, Vector3 center, Vector3 size) => Box(material, center, size, Quaternion.identity);
        private static void Box(Material material, Vector3 center, Vector3 size, Quaternion rotation, float uvPerMetre = .70f)
            => Batch(material).Box(center, size, rotation, uvPerMetre);
        private static void Rod(Material material, Vector3 start, Vector3 end, float radius, int segments)
            => Batch(material).Rod(start, end, radius, segments);
        private static void Beam(Material material, Vector3 start, Vector3 end, float width, float depth)
            => Box(material, (start + end) * .5f, new Vector3(width, (end - start).magnitude, depth), Quaternion.FromToRotation(Vector3.up, (end - start).normalized));
        private static float Range(float min, float max) => min + (max - min) * (float)random.NextDouble();
        private static void Ring(Material material, Vector3 center, float radius, float thickness)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2 / 32, b = (i + 1) * Mathf.PI * 2 / 32;
                Rod(material, center + new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius), center + new Vector3(Mathf.Sin(b) * radius, 0, Mathf.Cos(b) * radius), thickness, 6);
            }
        }

        private static void Flush()
        {
            foreach (var item in Batches)
            {
                var mesh = item.Value.Mesh();
                mesh.name = "Showroom " + item.Key.name;
                string path = Root + "/Meshes/" + mesh.name + ".asset";
                var saved = PersistentMeshAsset.Save(mesh, path);
                var go = new GameObject(saved.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(world, false);
                go.GetComponent<MeshFilter>().sharedMesh = saved;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = item.Key;
                bool luminous = item.Key == glass || item.Key == lamp;
                renderer.shadowCastingMode = luminous ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = !luminous;
                go.isStatic = true;
            }
        }

        private sealed class Shape
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> indices = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, float height, bool alignGrain = false)
            {
                int first = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                bool vertical = alignGrain && height > width;
                uvs.Add(Vector2.zero);
                uvs.Add(vertical ? new Vector2(0, width) : new Vector2(width, 0));
                uvs.Add(vertical ? new Vector2(height, width) : new Vector2(width, height));
                uvs.Add(vertical ? new Vector2(height, 0) : new Vector2(0, height));
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
                indices.Add(first); indices.Add(first + 2); indices.Add(first + 3);
            }
            public void Box(Vector3 center, Vector3 size, Quaternion rotation, float uvPerMetre)
            {
                var s = size * .5f;
                Vector3 P(float x, float y, float z) => center + rotation * new Vector3(x * s.x, y * s.y, z * s.z);
                float xuv = size.x * uvPerMetre, yuv = size.y * uvPerMetre, zuv = size.z * uvPerMetre;
                Quad(P(-1,-1,1), P(1,-1,1), P(1,1,1), P(-1,1,1), xuv,yuv,true);
                Quad(P(1,-1,-1), P(-1,-1,-1), P(-1,1,-1), P(1,1,-1), xuv,yuv,true);
                Quad(P(1,-1,1), P(1,-1,-1), P(1,1,-1), P(1,1,1), zuv,yuv,true);
                Quad(P(-1,-1,-1), P(-1,-1,1), P(-1,1,1), P(-1,1,-1), zuv,yuv,true);
                Quad(P(-1,1,1), P(1,1,1), P(1,1,-1), P(-1,1,-1), xuv,zuv,true);
                Quad(P(-1,-1,-1), P(1,-1,-1), P(1,-1,1), P(-1,-1,1), xuv,zuv,true);
            }
            public void Rod(Vector3 start, Vector3 end, float radius, int segments)
            {
                var delta = end - start;
                if (delta.sqrMagnitude < .00000001f) return;
                var forward = delta.normalized;
                var right = Vector3.Cross(forward, Mathf.Abs(forward.y) < .9f ? Vector3.up : Vector3.right).normalized;
                var up = Vector3.Cross(forward, right);
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                    var ra = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
                    var rb = (right * Mathf.Cos(b) + up * Mathf.Sin(b)) * radius;
                    Quad(start + ra, start + rb, end + rb, end + ra, radius * 2 * Mathf.PI / segments, delta.magnitude);
                    Triangle(start, start + rb, start + ra);
                    Triangle(end, end + ra, end + rb);
                }
            }
            private void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int first = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uvs.Add(new Vector2(.5f, .5f)); uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
            }
            public Mesh Mesh()
            {
                foreach (var v in vertices)
                    if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z)) throw new InvalidOperationException("Non-finite showroom geometry.");
                var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
            private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
