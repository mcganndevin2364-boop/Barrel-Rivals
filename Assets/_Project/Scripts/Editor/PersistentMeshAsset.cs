using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Persists generated geometry without replacing its asset GUID or native channel data.</summary>
    public static class PersistentMeshAsset
    {
        /// <summary>
        /// Takes ownership of a transient mesh on success. Callers retain ownership on validation failure.
        /// Embedded mesh LODs and blend shapes are intentionally unsupported by this generator path.
        /// </summary>
        public static Mesh Save(Mesh generated, string path)
        {
            if (!generated) throw new ArgumentNullException(nameof(generated));
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Generated meshes require an Assets/... .asset path.", nameof(path));
            if (EditorUtility.IsPersistent(generated))
                throw new ArgumentException("Pass a transient generated mesh, not a shared asset.", nameof(generated));
            if (!generated.isReadable)
                throw new NotSupportedException("Generated mesh data must remain readable until saved.");
            if (generated.blendShapeCount != 0 || generated.lodCount > 1)
                throw new NotSupportedException("PersistentMeshAsset does not yet copy blend shapes or embedded mesh LODs.");

            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing && !(existing is Mesh))
                throw new ArgumentException("The destination asset is not a Mesh: " + path, nameof(path));
            if (!existing)
            {
                AssetDatabase.CreateAsset(generated, path);
                AssetDatabase.SaveAssetIfDirty(generated);
                return generated;
            }

            var saved = (Mesh)existing;
            if (saved.blendShapeCount != 0 || saved.lodCount > 1)
                throw new NotSupportedException("The destination contains blend shapes or embedded mesh LODs; refusing to discard unsupported data.");
            // Acquire and validate all source data before touching the existing asset. Raw streams
            // preserve every used attribute, including packed colors, UV0–7 dimensions and formats.
            // CopySerialized can update a Mesh's bounds while retaining its old native vertex buffer.
            var attributes = generated.GetVertexAttributes();
            var submeshes = new SubMeshDescriptor[generated.subMeshCount];
            for (int i = 0; i < submeshes.Length; i++) submeshes[i] = generated.GetSubMesh(i);
            var bindposes = generated.bindposes;
            var bonesPerVertex = generated.GetBonesPerVertex();
            var weights = generated.GetAllBoneWeights();
            if (weights.Length > 0 && bonesPerVertex.Length != generated.vertexCount)
                throw new ArgumentException("Skin weights do not cover the generated vertex array.", nameof(generated));

            using (var data = Mesh.AcquireReadOnlyMeshData(generated))
            {
                var source = data[0];
                saved.Clear(false);
                saved.ClearBlendShapes();
                saved.SetVertexBufferParams(generated.vertexCount, attributes);
                for (int stream = 0; stream < source.vertexBufferCount; stream++)
                {
                    var bytes = source.GetVertexData<byte>(stream);
                    saved.SetVertexBufferData(bytes, 0, 0, bytes.Length, stream, MeshUpdateFlags.DontRecalculateBounds);
                }
                if (source.indexFormat == IndexFormat.UInt16)
                {
                    var indices = source.GetIndexData<ushort>();
                    saved.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
                    saved.SetIndexBufferData(indices, 0, 0, indices.Length, MeshUpdateFlags.DontRecalculateBounds);
                }
                else
                {
                    var indices = source.GetIndexData<uint>();
                    saved.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
                    saved.SetIndexBufferData(indices, 0, 0, indices.Length, MeshUpdateFlags.DontRecalculateBounds);
                }
                // Preserve submesh ranges, base vertices, topology and authored per-submesh bounds.
                saved.SetSubMeshes(submeshes, MeshUpdateFlags.DontRecalculateBounds);
                saved.bindposes = bindposes;
                // The variable-influence API also preserves future meshes with >4 weights/vertex.
                // These arrays borrow the source's storage; do not dispose or destroy it early.
                if (weights.Length > 0) saved.SetBoneWeights(bonesPerVertex, weights);
                else
                {
                    // Clear(false) plus the legacy boneWeights setter can leave stale native
                    // variable-influence data after a skinned -> unskinned rebuild. Two empty
                    // arrays clear it; per-vertex zero counts are rejected by Unity 6000.6.
                    using (var noCounts = new NativeArray<byte>(0, Allocator.Temp))
                    using (var noWeights = new NativeArray<BoneWeight1>(0, Allocator.Temp))
                        saved.SetBoneWeights(noCounts, noWeights);
                }
                saved.bounds = generated.bounds;
                saved.name = generated.name;
            }
            EditorUtility.SetDirty(saved);
            AssetDatabase.SaveAssetIfDirty(saved);
            Object.DestroyImmediate(generated);
            return saved;
        }
    }
}
