using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Editor;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class PersistentMeshAssetTests
    {
        private string folder;
        private readonly List<Mesh> transient = new List<Mesh>();
        private string Path => folder + "/Geometry.asset";

        [SetUp]
        public void CreateIsolatedAssetFolder()
        {
            folder = "Assets/__MeshPersistenceTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
        }

        [TearDown]
        public void RemoveOnlyTestAssets()
        {
            foreach (var mesh in transient)
                if (mesh && !EditorUtility.IsPersistent(mesh)) Object.DestroyImmediate(mesh);
            transient.Clear();
            AssetDatabase.DeleteAsset(folder);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RebuildReloadKeepsIdentityAndChangedChannelsThenRemovesUnusedChannels(bool packedColors)
        {
            var saved = PersistentMeshAsset.Save(Track(Minimal()), Path);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(saved, out string guid, out long localId));
            saved = Reload(saved);
            var expected = Track(RichMesh(packedColors));
            saved = PersistentMeshAsset.Save(CopyForSave(expected), Path);
            saved = Reload(saved);
            AssertIdentity(saved, guid, localId);
            AssertEquivalent(expected, saved);

            // A later simpler rebuild must not retain old UVs, colors, tangent or skin buffers.
            var simple = Track(Minimal());
            simple.name = "Rebuilt simple points";
            simple.vertices = new[] { new Vector3(11, 2, -4), new Vector3(15, 7, -6), new Vector3(12, 4, -8) };
            simple.SetIndices(new[] { 2, 0, 1 }, MeshTopology.Points, 0);
            simple.RecalculateBounds();
            saved = PersistentMeshAsset.Save(CopyForSave(simple), Path);
            AssertNoSkinInfluences(saved);
            saved = Reload(saved);
            AssertIdentity(saved, guid, localId);
            AssertEquivalent(simple, saved);
            Assert.AreEqual(0, saved.bindposes.Length);
            Assert.AreEqual(0, saved.GetAllBoneWeights().Length);
            Assert.AreEqual(IndexFormat.UInt16, saved.indexFormat);
        }

        [Test]
        public void RebuildPreservesPackedAttributesInSeparateVertexStreams()
        {
            var saved = PersistentMeshAsset.Save(Track(Minimal()), Path);
            var expected = Track(new Mesh { name = "Packed separate streams" });
            expected.SetVertexBufferParams(3,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord7, VertexAttributeFormat.Float16, 2, 2));
            expected.SetVertexBufferData(new[] { Vector3.zero, Vector3.right * 8, Vector3.up * 5 }, 0, 0, 3, 0);
            expected.SetVertexBufferData(new[] { new Color32(5, 25, 125, 250), new Color32(15, 35, 135, 240), new Color32(25, 45, 145, 230) }, 0, 0, 3, 1);
            expected.SetVertexBufferData(new ushort[] { 0, 0x3c00, 0x3800, 0x3c00, 0x3c00, 0 }, 0, 0, 6, 2);
            expected.SetIndices(new[] { 0, 2, 1 }, MeshTopology.Triangles, 0);
            expected.RecalculateBounds();
            saved = PersistentMeshAsset.Save(CopyForSave(expected), Path);
            AssertEquivalent(expected, Reload(saved));
        }

        [Test]
        public void UnsupportedBlendShapesFailBeforeMutatingTheAssetOrConsumingSource()
        {
            var saved = PersistentMeshAsset.Save(Track(Minimal()), Path);
            var before = File.ReadAllBytes(Path);
            string guid = AssetDatabase.AssetPathToGUID(Path);
            var unsupported = Track(Minimal());
            unsupported.AddBlendShapeFrame("Do not silently drop", 100,
                new[] { Vector3.up, Vector3.right, Vector3.forward }, new Vector3[3], new Vector3[3]);
            Assert.Throws<NotSupportedException>(() => PersistentMeshAsset.Save(unsupported, Path));
            Assert.IsTrue(unsupported, "Rejected input must remain owned by the caller.");
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(Path));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Path));
            Assert.AreEqual(3, Reload(saved).vertexCount);
        }

        [Test]
        public void ExistingUnsupportedBlendShapesArePreservedWhenReplacementIsRejected()
        {
            var protectedMesh = Track(Minimal());
            protectedMesh.AddBlendShapeFrame("Authored shape must survive", 100,
                new[] { Vector3.up, Vector3.right, Vector3.forward }, new Vector3[3], new Vector3[3]);
            // This asset was authored outside the supported generator path.
            AssetDatabase.CreateAsset(protectedMesh, Path);
            AssetDatabase.SaveAssetIfDirty(protectedMesh);
            var before = File.ReadAllBytes(Path);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(protectedMesh, out string guid, out long localId));
            var replacement = Track(Minimal());
            replacement.name = "Must not replace protected geometry";
            replacement.vertices = new[] { Vector3.one * 10, Vector3.right * 12, Vector3.up * 15 };

            Assert.Throws<NotSupportedException>(() => PersistentMeshAsset.Save(replacement, Path));
            Assert.IsTrue(replacement, "Rejected source must remain owned by the caller.");
            // Flush before comparing so accidentally dirtying the destination cannot hide a mutation.
            AssetDatabase.SaveAssets();
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Path));
            protectedMesh = Reload(protectedMesh);
            AssertIdentity(protectedMesh, guid, localId);
            Assert.AreEqual(1, protectedMesh.blendShapeCount);
            Assert.AreEqual("Authored shape must survive", protectedMesh.GetBlendShapeName(0));
            CollectionAssert.AreEqual(new[] { Vector3.zero, Vector3.right, Vector3.up }, protectedMesh.vertices);
        }

        private Mesh CopyForSave(Mesh source)
        {
            var copy = Track(Object.Instantiate(source));
            copy.name = source.name;
            return copy;
        }

        private Mesh Track(Mesh mesh) { transient.Add(mesh); return mesh; }

        private Mesh Reload(Mesh mesh)
        {
            AssetDatabase.SaveAssets();
            // Drop the loaded native object so passing only in-memory state cannot hide stale disk data.
            Resources.UnloadAsset(mesh);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Mesh>(Path);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private static void AssertIdentity(Mesh mesh, string guid, long localId)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string actualGuid, out long actualId));
            Assert.AreEqual(guid, actualGuid, "Rebuild must preserve scene asset references.");
            Assert.AreEqual(localId, actualId);
        }

        private static Mesh Minimal()
        {
            var mesh = new Mesh { name = "Initial triangle", vertices = new[] { Vector3.zero, Vector3.right, Vector3.up } };
            mesh.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh RichMesh(bool packedColors)
        {
            const int count = 6;
            var mesh = new Mesh { name = "Changed skinned mesh", indexFormat = IndexFormat.UInt32 };
            var positions = new Vector3[count]; var normals = new Vector3[count]; var tangents = new Vector4[count];
            var colors = new Color[count]; var colors32 = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(5 + i, i % 3 - 2, i * -.75f);
                normals[i] = Vector3.up; tangents[i] = new Vector4(1, 0, 0, i % 2 == 0 ? -1 : 1);
                colors[i] = new Color(i / 8f, .375f, .625f, .875f);
                colors32[i] = new Color32((byte)(11 + i), 47, 143, 201);
            }
            mesh.vertices = positions; mesh.normals = normals; mesh.tangents = tangents;
            if (packedColors) mesh.colors32 = colors32; else mesh.colors = colors;
            for (int channel = 0; channel < 8; channel++)
            {
                var uv2 = new List<Vector2>(); var uv3 = new List<Vector3>(); var uv4 = new List<Vector4>();
                for (int i = 0; i < count; i++)
                {
                    uv2.Add(new Vector2(i / 8f, channel / 8f));
                    uv3.Add(new Vector3(i / 8f, channel / 8f, .75f));
                    uv4.Add(new Vector4(i / 8f, channel / 8f, .75f, .875f));
                }
                if (channel % 3 == 0) mesh.SetUVs(channel, uv2);
                else if (channel % 3 == 1) mesh.SetUVs(channel, uv3);
                else mesh.SetUVs(channel, uv4);
            }
            var poses = new Matrix4x4[5];
            for (int i = 0; i < poses.Length; i++) poses[i] = Matrix4x4.Translate(new Vector3(i * .5f, 1, -2));
            mesh.bindposes = poses;
            var bones = new NativeArray<byte>(count, Allocator.Temp);
            var weights = new NativeArray<BoneWeight1>(count * 5, Allocator.Temp);
            try
            {
                var influences = new[] { .4f, .25f, .15f, .125f, .075f };
                for (int i = 0; i < count; i++)
                {
                    bones[i] = 5;
                    for (int bone = 0; bone < 5; bone++) weights[i * 5 + bone] = new BoneWeight1 { boneIndex = bone, weight = influences[bone] };
                }
                mesh.SetBoneWeights(bones, weights);
            }
            finally { weights.Dispose(); bones.Dispose(); }
            mesh.subMeshCount = 3;
            mesh.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
            mesh.SetIndices(new[] { 0, 2 }, MeshTopology.Lines, 1, false, 3);
            mesh.SetIndices(Array.Empty<int>(), MeshTopology.Triangles, 2, false);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var submesh = mesh.GetSubMesh(i);
                submesh.bounds = new Bounds(new Vector3(7, -1, -2), new Vector3(9, 6, 8));
                mesh.SetSubMesh(i, submesh, MeshUpdateFlags.DontRecalculateBounds);
            }
            // Deliberately larger than vertex extrema: authored culling bounds must survive unchanged.
            mesh.bounds = new Bounds(new Vector3(7, -1, -2), new Vector3(12, 10, 11));
            return mesh;
        }

        private static void AssertEquivalent(Mesh expected, Mesh actual)
        {
            Assert.AreEqual(expected.name, actual.name);
            Assert.AreEqual(expected.vertexCount, actual.vertexCount);
            Assert.AreEqual(expected.indexFormat, actual.indexFormat);
            Assert.AreEqual(expected.bounds, actual.bounds);
            CollectionAssert.AreEqual(expected.GetVertexAttributes(), actual.GetVertexAttributes(), "Attribute formats/dimensions/streams changed.");
            CollectionAssert.AreEqual(expected.vertices, actual.vertices, "Persisted vertex shape is stale.");
            CollectionAssert.AreEqual(expected.normals, actual.normals);
            CollectionAssert.AreEqual(expected.tangents, actual.tangents);
            for (int channel = 0; channel < 8; channel++)
            {
                var a = new List<Vector4>(); var b = new List<Vector4>();
                expected.GetUVs(channel, a); actual.GetUVs(channel, b);
                CollectionAssert.AreEqual(a, b, "UV channel " + channel);
            }
            CollectionAssert.AreEqual(expected.bindposes, actual.bindposes);
            var ew = expected.GetAllBoneWeights(); var aw = actual.GetAllBoneWeights();
            Assert.AreEqual(ew.Length, aw.Length);
            if (ew.Length == 0) AssertNoSkinInfluences(actual);
            else CollectionAssert.AreEqual(expected.GetBonesPerVertex().ToArray(), actual.GetBonesPerVertex().ToArray());
            for (int i = 0; i < ew.Length; i++)
            {
                Assert.AreEqual(ew[i].boneIndex, aw[i].boneIndex);
                Assert.That(aw[i].weight, Is.EqualTo(ew[i].weight).Within(.00002f));
            }
            Assert.AreEqual(expected.subMeshCount, actual.subMeshCount);
            for (int i = 0; i < expected.subMeshCount; i++)
            {
                Assert.AreEqual(expected.GetSubMesh(i), actual.GetSubMesh(i), "Submesh " + i);
                CollectionAssert.AreEqual(expected.GetIndices(i, false), actual.GetIndices(i, false));
            }
            // Compare persisted bytes too, catching packed-color or less-used channels that getters omit.
            using (var a = Mesh.AcquireReadOnlyMeshData(expected))
            using (var b = Mesh.AcquireReadOnlyMeshData(actual))
            {
                Assert.AreEqual(a[0].vertexBufferCount, b[0].vertexBufferCount);
                for (int stream = 0; stream < a[0].vertexBufferCount; stream++)
                    CollectionAssert.AreEqual(a[0].GetVertexData<byte>(stream).ToArray(), b[0].GetVertexData<byte>(stream).ToArray(), "Vertex stream " + stream);
            }
        }

        private static void AssertNoSkinInfluences(Mesh mesh)
        {
            Assert.AreEqual(0, mesh.GetAllBoneWeights().Length, "Unskinned rebuild retained native weights.");
            // The isolated 6000.6 save/reload probe establishes this canonical empty result.
            Assert.AreEqual(0, mesh.GetBonesPerVertex().Length, "Unskinned rebuild retained native influence counts.");
        }
    }
}
