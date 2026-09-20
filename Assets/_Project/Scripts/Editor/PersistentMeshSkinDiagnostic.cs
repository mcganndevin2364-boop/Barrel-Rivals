using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Opt-in native skin replacement probe. Operates only on disposable test assets.</summary>
    public static class PersistentMeshSkinDiagnostic
    {
        public static void Run()
        {
            string output = Environment.GetEnvironmentVariable("BARREL_MESH_SKIN_DIAGNOSTIC_PATH");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set BARREL_MESH_SKIN_DIAGNOSTIC_PATH to a scratch JSON path.");
            string folder = "Assets/__MeshSkinDiagnostic_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            var results = new List<Result>();
            var variants = new[] { "clear-only", "legacy-empty-before", "legacy-null-after", "legacy-empty-after", "variable-empty-before", "variable-empty-after" };
            try
            {
                foreach (string variant in variants)
                {
                    var result = new Result { variant = variant };
                    var messages = new List<string>();
                    Application.LogCallback callback = (message, stack, type) =>
                    {
                        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                            messages.Add(type + ": " + message);
                    };
                    Application.logMessageReceived += callback;
                    Mesh seed = null, simple = null;
                    try
                    {
                        string path = folder + "/" + variant + ".asset";
                        seed = SkinnedSeed(); seed.name = variant;
                        AssetDatabase.CreateAsset(seed, path);
                        AssetDatabase.SaveAssetIfDirty(seed);
                        result.before = Inspect(seed);
                        simple = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up } };
                        simple.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
                        simple.RecalculateBounds();
                        seed.Clear(false); seed.ClearBlendShapes();
                        if (variant == "legacy-empty-before") seed.boneWeights = Array.Empty<BoneWeight>();
                        if (variant == "variable-empty-before") SetEmptyVariableWeights(seed);
                        CopyGeometry(simple, seed);
                        seed.bindposes = Array.Empty<Matrix4x4>();
                        if (variant == "legacy-null-after") seed.boneWeights = null;
                        if (variant == "legacy-empty-after") seed.boneWeights = Array.Empty<BoneWeight>();
                        if (variant == "variable-empty-after") SetEmptyVariableWeights(seed);
                        result.afterReplacement = Inspect(seed);
                        EditorUtility.SetDirty(seed); AssetDatabase.SaveAssetIfDirty(seed);
                        Resources.UnloadAsset(seed); seed = null;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        result.afterReload = Inspect(AssetDatabase.LoadAssetAtPath<Mesh>(path));
                    }
                    catch (Exception exception) { result.exception = exception.GetType().Name + ": " + exception.Message; }
                    finally
                    {
                        if (simple) Object.DestroyImmediate(simple);
                        if (seed && !EditorUtility.IsPersistent(seed)) Object.DestroyImmediate(seed);
                        Application.logMessageReceived -= callback;
                    }
                    result.errors = messages.ToArray(); results.Add(result);
                }
            }
            finally { AssetDatabase.DeleteAsset(folder); }
            var report = new Report { unityVersion = Application.unityVersion, results = results.ToArray() };
            string full = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("BARREL_MESH_SKIN_DIAGNOSTIC: " + full);
        }

        private static void SetEmptyVariableWeights(Mesh mesh)
        {
            using (var counts = new NativeArray<byte>(0, Allocator.Temp))
            using (var weights = new NativeArray<BoneWeight1>(0, Allocator.Temp))
                mesh.SetBoneWeights(counts, weights);
        }

        private static Mesh SkinnedSeed()
        {
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, Vector3.one, Vector3.right * 2 } };
            mesh.SetIndices(new[] { 0, 1, 2, 3, 4, 5 }, MeshTopology.Triangles, 0);
            mesh.bindposes = new[] { Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity };
            var counts = new NativeArray<byte>(6, Allocator.Temp);
            var weights = new NativeArray<BoneWeight1>(30, Allocator.Temp);
            try
            {
                var influence = new[] { .4f, .25f, .15f, .125f, .075f };
                for (int vertex = 0; vertex < 6; vertex++)
                {
                    counts[vertex] = 5;
                    for (int bone = 0; bone < 5; bone++) weights[vertex * 5 + bone] = new BoneWeight1 { boneIndex = bone, weight = influence[bone] };
                }
                mesh.SetBoneWeights(counts, weights);
            }
            finally { weights.Dispose(); counts.Dispose(); }
            mesh.RecalculateBounds(); return mesh;
        }

        private static void CopyGeometry(Mesh source, Mesh destination)
        {
            using (var data = Mesh.AcquireReadOnlyMeshData(source))
            {
                destination.SetVertexBufferParams(source.vertexCount, source.GetVertexAttributes());
                for (int stream = 0; stream < data[0].vertexBufferCount; stream++)
                {
                    var bytes = data[0].GetVertexData<byte>(stream);
                    destination.SetVertexBufferData(bytes, 0, 0, bytes.Length, stream, MeshUpdateFlags.DontRecalculateBounds);
                }
                var indices = data[0].GetIndexData<ushort>();
                destination.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
                destination.SetIndexBufferData(indices, 0, 0, indices.Length, MeshUpdateFlags.DontRecalculateBounds);
                destination.SetSubMeshes(new[] { source.GetSubMesh(0) }, MeshUpdateFlags.DontRecalculateBounds);
                destination.bounds = source.bounds;
            }
        }

        private static State Inspect(Mesh mesh)
        {
            var counts = mesh.GetBonesPerVertex().ToArray();
            var weights = mesh.GetAllBoneWeights();
            bool clear = weights.Length == 0 && (counts.Length == 0 || counts.Length == mesh.vertexCount);
            foreach (byte count in counts) clear &= count == 0;
            return new State { vertexCount = mesh.vertexCount, counts = Array.ConvertAll(counts, value => (int)value), weightCount = weights.Length,
                bindposeCount = mesh.bindposeCount, noSkinInfluences = clear, hasBlendWeightChannel = mesh.HasVertexAttribute(VertexAttribute.BlendWeight) };
        }

        [Serializable] private sealed class Report { public string unityVersion; public Result[] results; }
        [Serializable] private sealed class Result { public string variant, exception; public string[] errors; public State before, afterReplacement, afterReload; }
        [Serializable] private sealed class State { public int vertexCount, weightCount, bindposeCount; public int[] counts; public bool noSkinInfluences, hasBlendWeightChannel; }
    }
}
