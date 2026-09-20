using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    /// <summary>Imported-clip geometry QA. CPU BakeMesh measurements do not replace rendered-motion/device review.</summary>
    public sealed class GaitGeometryTests
    {
        private const string HorsePath = "Assets/_Project/Art/Reins/Horse/RodeoHorse.fbx";
        private const int Samples = 121;
        private static readonly string[] Labels = { "LF", "RF", "LH", "RH" };
        private static readonly string[] DistalBones = { "Bone_R.002", "Bone_L.002", "Bone_L.005", "Bone_R.005" };

        [Test]
        public void ImportedGaitsKeepMeasuredSolesGroundedWithoutMovingObjectRoots()
        {
            var importer = AssetImporter.GetAtPath(HorsePath) as ModelImporter;
            Assert.IsNotNull(importer);
            Assert.AreEqual(ModelImporterAnimationCompression.Off, importer.animationCompression,
                "This benchmark requires authored curves without key-reduction compression.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorsePath);
            Assert.IsNotNull(prefab);
            var holder = new GameObject("Gait geometry measurement");
            // Match the art builder's correction to canonical +Z without allowing
            // root-level clip bindings to overwrite our measurement coordinates.
            holder.transform.rotation = Quaternion.Euler(0, 180, 0);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            var baked = new Mesh();
            try
            {
                foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                { animator.applyRootMotion = false; animator.enabled = false; }
                var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single(r => r.name.StartsWith("HorseBody", StringComparison.Ordinal));
                var mesh = body.sharedMesh;
                var clips = AssetDatabase.LoadAllAssetsAtPath(HorsePath).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                // Imported FBX vertices retain a different coordinate basis from
                // the live skeleton. Resolve its bind matrices through skinning
                // before selecting soles; raw sharedMesh coordinates were 65m high.
                body.BakeMesh(baked, true);
                var defaultPose = InspectBindCoordinates(body, baked.vertices.Select(body.transform.TransformPoint).ToArray());
                TestContext.WriteLine("Gait imported default pose: " + JsonUtility.ToJson(defaultPose));
                // This FBX initially poses the first (Gallop) take: its torso is
                // 106mm lower and its feet are already flexed. Idle0 restores the
                // authored leg bind pose and neutral torso before sole selection.
                // Blender verified body-only height2.119999m at Idle0, vs2.007997m
                // in the imported Gallop0 default; hair/ears are not this mismatch.
                var idle = clips.Single(c => c.name.EndsWith("|Idle", StringComparison.Ordinal) || c.name == "Idle");
                idle.SampleAnimation(model, 0);
                body.BakeMesh(baked, true);
                var rest = baked.vertices.Select(body.transform.TransformPoint).ToArray();
                var bindDiagnostic = InspectBindCoordinates(body, rest);
                TestContext.WriteLine("Gait bind coordinates: " + JsonUtility.ToJson(bindDiagnostic));
                Assert.That(rest.Max(v => v.y) - rest.Min(v => v.y), Is.EqualTo(2.12f).Within(.015f),
                    "Bind geometry must be measured in metres; do not compensate incorrect scale with looser tolerances.");
                var selections = SelectHooves(body, rest);
                var bodyBone = body.bones.Single(b => b.name == "Bone");
                var head = body.bones.Single(b => b.name == "Bone.002");
                Assert.Greater(head.position.z, bodyBone.position.z, "The measured horse must face canonical +Z.");
                Vector3 bodyBind = bodyBone.position;
                var armature = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Armature");
                Transform[] objectRoots = { holder.transform, model.transform, armature };
                var rootMatrices = objectRoots.Select(t => t.localToWorldMatrix).ToArray();
                var results = new List<ClipResult>();
                foreach (string name in new[] { "Walk", "Gallop" })
                {
                    var clip = clips.Single(c => c.name.EndsWith("|" + name, StringComparison.Ordinal) || c.name == name);
                    results.Add(Measure(name, clip, model, body, baked, bodyBone, bodyBind, selections, objectRoots, rootMatrices));
                }
                var report = new GeometryReport
                {
                    unityVersion = Application.unityVersion, assetPath = HorsePath,
                    assetGuid = AssetDatabase.AssetPathToGUID(HorsePath), fbxSha256 = HashFile(HorsePath),
                    animationCompression = importer.animationCompression.ToString(), resampleCurves = importer.resampleCurves,
                    sampleCountPerCycle = Samples, importedVertexCount = mesh.vertexCount, selections = selections,
                    importedDefaultPoseCoordinates = defaultPose, bindCoordinates = bindDiagnostic,
                    method = "Direct imported AnimationClip.SampleAnimation and CPU BakeMesh(true), with renderer transform applied once. " +
                        "Bind selection samples Idle0 to restore neutral torso/leg pose before baking; the imported prefab initially poses Gallop0. " +
                        "It does not use raw FBX mesh coordinates. " +
                        "Ground vertices: distal-bone influence >0.8. Sole centroid: those baked bind vertices within 0.023m of bind minimum. " +
                        "Same geometric selection as Blender authoring; Unity indices are rediscovered after FBX reordering/splits. " +
                        "Nominal stance drift adds constant authored forward travel to an in-place clip. " +
                        "Not a rendered-motion, blend-tree, turning, collision, native or device acceptance test.",
                    clips = results.ToArray()
                };
                WriteOptionalReport(report);
                foreach (var result in results) AssertGeometry(result);
            }
            finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(holder); }
        }

        private static BindCoordinates InspectBindCoordinates(SkinnedMeshRenderer body, Vector3[] bakedWorld)
        {
            var mesh = body.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
            Assert.AreEqual(vertices.Length, bakedWorld.Length);
            var matrices = body.bones.Select((bone, index) => bone.localToWorldMatrix * mesh.bindposes[index]).ToArray();
            float rawMin = float.PositiveInfinity, rawMax = float.NegativeInfinity;
            float manualMin = float.PositiveInfinity, manualMax = float.NegativeInfinity, difference = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                var weight = weights[i]; Vector3 vertex = vertices[i];
                Vector3 manual = matrices[weight.boneIndex0].MultiplyPoint3x4(vertex) * weight.weight0
                    + matrices[weight.boneIndex1].MultiplyPoint3x4(vertex) * weight.weight1
                    + matrices[weight.boneIndex2].MultiplyPoint3x4(vertex) * weight.weight2
                    + matrices[weight.boneIndex3].MultiplyPoint3x4(vertex) * weight.weight3;
                float raw = body.transform.TransformPoint(vertex).y;
                rawMin = Mathf.Min(rawMin, raw); rawMax = Mathf.Max(rawMax, raw);
                manualMin = Mathf.Min(manualMin, manual.y); manualMax = Mathf.Max(manualMax, manual.y);
                difference = Mathf.Max(difference, Vector3.Distance(manual, bakedWorld[i]));
            }
            return new BindCoordinates
            {
                rendererLossyScale = body.transform.lossyScale, rawMeshTransformHeight = rawMax - rawMin,
                bakedHeight = bakedWorld.Max(v => v.y) - bakedWorld.Min(v => v.y),
                bakedMinimumY = bakedWorld.Min(v => v.y), manualLinearSkinHeight = manualMax - manualMin,
                maximumManualVsBakeDifference = difference
            };
        }

        private static HoofSelection[] SelectHooves(SkinnedMeshRenderer body, Vector3[] bind)
        {
            var weights = body.sharedMesh.boneWeights;
            Assert.AreEqual(bind.Length, weights.Length);
            var result = new HoofSelection[Labels.Length];
            for (int limb = 0; limb < Labels.Length; limb++)
            {
                int bone = Array.FindIndex(body.bones, b => b && b.name == DistalBones[limb]);
                Assert.GreaterOrEqual(bone, 0, "Missing distal bone " + DistalBones[limb]);
                var ground = Enumerable.Range(0, bind.Length).Where(i => Influence(weights[i], bone) > .8f).ToArray();
                Assert.Greater(ground.Length, 20, "Missing weighted distal geometry for " + Labels[limb]);
                float lowest = ground.Min(i => bind[i].y);
                var sole = ground.Where(i => bind[i].y < lowest + .023f).ToArray();
                Assert.Greater(sole.Length, 3, "Missing bind sole for " + Labels[limb]);
                result[limb] = new HoofSelection
                {
                    label = Labels[limb], bone = DistalBones[limb], groundVertexIndices = ground, soleVertexIndices = sole,
                    bindMinimumHeight = lowest, bindSoleCenter = Center(bind, sole)
                };
            }
            return result;
        }

        private static ClipResult Measure(string name, AnimationClip clip, GameObject model, SkinnedMeshRenderer body, Mesh baked,
            Transform bodyBone, Vector3 bodyBind, HoofSelection[] selections, Transform[] roots, Matrix4x4[] rootMatrices)
        {
            bool walk = name == "Walk";
            float speed = walk ? 1.5f : 8f, duty = walk ? .64f : .20f;
            // Array order LF, RF, LH, RH; gallop uses a fixed left lead and one suspension interval.
            float[] touchdown = walk ? new[] { .25f, .75f, 0f, .50f } : new[] { .44f, .30f, .12f, 0f };
            var frames = new List<GeometryFrame>();
            float rootError = 0, scaleError = 0, loopError = 0;
            Vector3[] firstVertices = null;
            for (int sample = 0; sample < Samples; sample++)
            {
                float phase = sample / (float)(Samples - 1);
                clip.SampleAnimation(model, phase * clip.length);
                // Unity6 useScale=true compensates the renderer's transform scale;
                // TransformPoint then applies the hierarchy once. Validate metres above.
                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html
                body.BakeMesh(baked, true);
                var points = baked.vertices.Select(body.transform.TransformPoint).ToArray();
                Assert.AreEqual(body.sharedMesh.vertexCount, points.Length);
                if (sample == 0) firstVertices = points;
                if (sample == Samples - 1)
                    for (int vertex = 0; vertex < points.Length; vertex++)
                        loopError = Mathf.Max(loopError, Vector3.Distance(firstVertices[vertex], points[vertex]));
                for (int root = 0; root < roots.Length; root++)
                    for (int element = 0; element < 16; element++)
                        rootError = Mathf.Max(rootError, Mathf.Abs(roots[root].localToWorldMatrix[element] - rootMatrices[root][element]));
                foreach (var bone in body.bones)
                {
                    Vector3 scale = bone.localScale;
                    scaleError = Mathf.Max(scaleError, Mathf.Abs(scale.x - 1), Mathf.Abs(scale.y - 1), Mathf.Abs(scale.z - 1));
                }
                var hooves = new HoofFrame[selections.Length];
                for (int limb = 0; limb < selections.Length; limb++)
                {
                    var set = selections[limb]; float cycle = Mathf.Repeat(phase - touchdown[limb], 1);
                    Vector3 center = Center(points, set.soleVertexIndices);
                    hooves[limb] = new HoofFrame
                    {
                        label = set.label, contact = cycle < duty, localCycle = cycle, center = center,
                        minimumHeight = set.groundVertexIndices.Min(i => points[i].y),
                        nominalWorldTravel = center.z + speed * cycle * clip.length
                    };
                }
                frames.Add(new GeometryFrame { phase = phase, bodyOffsetY = bodyBone.position.y - bodyBind.y, hooves = hooves });
            }
            var stats = new List<HoofMetrics>();
            for (int limb = 0; limb < selections.Length; limb++)
            {
                var all = frames.Select(f => f.hooves[limb]).ToArray();
                var contact = all.Where(f => f.contact).ToArray(); var swing = all.Where(f => !f.contact).ToArray();
                Assert.IsNotEmpty(contact); Assert.IsNotEmpty(swing);
                stats.Add(new HoofMetrics
                {
                    label = selections[limb].label, minimumHeight = all.Min(f => f.minimumHeight),
                    stanceMinimumHeight = contact.Min(f => f.minimumHeight), stanceMaximumHeight = contact.Max(f => f.minimumHeight),
                    nominalStanceResidualTravel = contact.Max(f => f.nominalWorldTravel) - contact.Min(f => f.nominalWorldTravel),
                    peakSwingClearance = swing.Max(f => f.minimumHeight)
                });
            }
            return new ClipResult
            {
                name = name, seconds = clip.length, authoredSpeed = speed, stanceFraction = duty,
                maximumObjectRootMatrixDeviation = rootError, maximumBoneScaleDeviation = scaleError,
                maximumLoopVertexDifference = loopError, minimumBodyOffsetY = frames.Min(f => f.bodyOffsetY),
                maximumBodyOffsetY = frames.Max(f => f.bodyOffsetY), hooves = stats.ToArray(), samples = frames.ToArray()
            };
        }

        private static void AssertGeometry(ClipResult clip)
        {
            bool walk = clip.name == "Walk";
            Assert.That(clip.seconds, Is.EqualTo((walk ? 32f : 20f) / 30).Within(.001f), clip.name + " duration changed.");
            Assert.That(clip.maximumObjectRootMatrixDeviation, Is.LessThan(.00001f), clip.name + " moved an object root.");
            Assert.That(clip.maximumBoneScaleDeviation, Is.LessThan(.00001f), clip.name + " stretched a bone.");
            Assert.That(clip.maximumLoopVertexDifference, Is.LessThan(.001f), clip.name + " has a mesh loop seam.");
            Assert.That(clip.minimumBodyOffsetY, Is.EqualTo(walk ? -.116f : -.134f).Within(.003f), clip.name + " lost torso compression.");
            Assert.That(clip.maximumBodyOffsetY, Is.EqualTo(walk ? -.104f : -.106f).Within(.003f), clip.name + " lost torso compression.");
            foreach (var hoof in clip.hooves)
            {
                string label = clip.name + "/" + hoof.label;
                // Export roundtrip was 3–8mm above ground, <=0.1mm walk and
                // <=7.2mm gallop stance drift. These fixed budgets allow Unity's
                // uncompressed resampling, not the old 70–100mm ground penetration.
                Assert.That(hoof.minimumHeight, Is.GreaterThanOrEqualTo(-.005f), label + " penetrates ground.");
                Assert.That(hoof.stanceMaximumHeight, Is.LessThanOrEqualTo(.020f), label + " floats during stance.");
                Assert.That(hoof.nominalStanceResidualTravel, Is.LessThanOrEqualTo(walk ? .010f : .025f), label + " slides at its authored speed.");
                Assert.That(hoof.peakSwingClearance, Is.InRange(walk ? .080f : .280f, walk ? .140f : .370f), label + " lost its swing path.");
            }
        }

        private static float Influence(BoneWeight weight, int bone)
            => (weight.boneIndex0 == bone ? weight.weight0 : 0) + (weight.boneIndex1 == bone ? weight.weight1 : 0)
             + (weight.boneIndex2 == bone ? weight.weight2 : 0) + (weight.boneIndex3 == bone ? weight.weight3 : 0);
        private static Vector3 Center(Vector3[] points, int[] indices)
        { Vector3 sum = Vector3.zero; foreach (int index in indices) sum += points[index]; return sum / indices.Length; }
        private static string HashFile(string path)
        { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        private static void WriteOptionalReport(GeometryReport report)
        {
            string path = Environment.GetEnvironmentVariable("BARREL_GAIT_GEOMETRY_REPORT");
            if (string.IsNullOrEmpty(path)) return;
            Assert.IsTrue(Path.IsPathRooted(path), "Use an absolute gait geometry report path.");
            Assert.IsFalse(File.Exists(path), "Use a fresh gait report filename; preserve earlier evidence.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable] private sealed class GeometryReport
        {
            public string unityVersion, assetPath, assetGuid, fbxSha256, animationCompression, method;
            public bool resampleCurves; public int sampleCountPerCycle, importedVertexCount;
            public BindCoordinates importedDefaultPoseCoordinates, bindCoordinates;
            public HoofSelection[] selections; public ClipResult[] clips;
        }
        [Serializable] private sealed class BindCoordinates
        {
            public Vector3 rendererLossyScale;
            public float rawMeshTransformHeight, bakedHeight, bakedMinimumY, manualLinearSkinHeight, maximumManualVsBakeDifference;
        }
        [Serializable] private sealed class HoofSelection
        {
            public string label, bone; public int[] groundVertexIndices, soleVertexIndices;
            public float bindMinimumHeight; public Vector3 bindSoleCenter;
        }
        [Serializable] private sealed class ClipResult
        {
            public string name; public float seconds, authoredSpeed, stanceFraction, maximumObjectRootMatrixDeviation,
                maximumBoneScaleDeviation, maximumLoopVertexDifference, minimumBodyOffsetY, maximumBodyOffsetY;
            public HoofMetrics[] hooves; public GeometryFrame[] samples;
        }
        [Serializable] private sealed class HoofMetrics
        {
            public string label; public float minimumHeight, stanceMinimumHeight, stanceMaximumHeight,
                nominalStanceResidualTravel, peakSwingClearance;
        }
        [Serializable] private sealed class GeometryFrame
        { public float phase, bodyOffsetY; public HoofFrame[] hooves; }
        [Serializable] private sealed class HoofFrame
        { public string label; public bool contact; public float localCycle, minimumHeight, nominalWorldTravel; public Vector3 center; }
    }
}
