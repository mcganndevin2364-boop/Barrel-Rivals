using System;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Showroom-only cord skinning. Reads the rig; never changes the horse or its pose.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(280)]
    public sealed class StableReinDrape : MonoBehaviour
    {
        public const int Sides = 12;
        public const float Radius = .009f;
        [Serializable] public struct Knot
        {
            public BoneWeight weights;
            public Vector3 point0, point1, point2, point3;
        }
        [SerializeField] MeshFilter cord;
        [SerializeField] Transform[] bones;
        [SerializeField] Knot[] knots;
        [SerializeField] Transform horn, bit;
        [SerializeField] Vector3 hornOffset;
        Mesh sourceMesh, instance;
        Matrix4x4[] matrices;
        Vector3[] centers, vertices, normals;
        Vector4[] tangents;
        Vector2[] circle;
        public Mesh Instance => instance;
        public int KnotCount => knots == null ? 0 : knots.Length;
        public Vector3 StartWorld => horn.TransformPoint(hornOffset);
        public Vector3 EndWorld => bit.position;

        public void Configure(MeshFilter filter, Transform[] palette, Knot[] samples,
            Transform saddleHorn, Vector3 offset, Transform bitAnchor)
        {
            if (!filter || !filter.sharedMesh || palette == null || samples == null || samples.Length < 3 ||
                filter.sharedMesh.vertexCount != samples.Length * Sides || !saddleHorn || !bitAnchor)
                throw new ArgumentException("A stowed rein needs a complete rig, endpoints and ring topology.");
            if (palette.Length == 0 || Array.Exists(palette, bone => !bone))
                throw new ArgumentException("Stowed rein bone palette is incomplete.");
            foreach (var sample in samples)
            {
                var w = sample.weights;
                if (w.boneIndex0 < 0 || w.boneIndex0 >= palette.Length || w.boneIndex1 < 0 || w.boneIndex1 >= palette.Length ||
                    w.boneIndex2 < 0 || w.boneIndex2 >= palette.Length || w.boneIndex3 < 0 || w.boneIndex3 >= palette.Length ||
                    !float.IsFinite(w.weight0 + w.weight1 + w.weight2 + w.weight3) ||
                    Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) > .0001f)
                    throw new ArgumentException("Stowed rein has invalid skin weights.");
            }
            Release();
            cord = filter; bones = palette; knots = samples; horn = saddleHorn; hornOffset = offset; bit = bitAnchor;
        }
        void Awake() => Initialize();
        void LateUpdate() => RenderImmediate();
        void Initialize()
        {
            if (instance || !cord || !cord.sharedMesh || knots == null || bones == null) return;
            sourceMesh = cord.sharedMesh;
            instance = Instantiate(sourceMesh); instance.name = "Private animated stowed rein"; instance.MarkDynamic();
            cord.sharedMesh = instance;
            circle = new Vector2[Sides];
            for (int j = 0; j < Sides; j++) { float angle = j * Mathf.PI * 2 / Sides; circle[j] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); }
            matrices = new Matrix4x4[bones.Length]; centers = new Vector3[knots.Length];
            vertices = new Vector3[instance.vertexCount]; normals = new Vector3[vertices.Length]; tangents = new Vector4[vertices.Length];
        }
        public void RenderImmediate()
        {
            Initialize(); if (!instance || !horn || !bit) return;
            var toCord = cord.transform.worldToLocalMatrix;
            for (int i = 0; i < bones.Length; i++) matrices[i] = toCord * bones[i].localToWorldMatrix;
            for (int i = 0; i < knots.Length; i++)
            {
                var k = knots[i]; var w = k.weights;
                centers[i] = matrices[w.boneIndex0].MultiplyPoint3x4(k.point0) * w.weight0
                    + matrices[w.boneIndex1].MultiplyPoint3x4(k.point1) * w.weight1
                    + matrices[w.boneIndex2].MultiplyPoint3x4(k.point2) * w.weight2
                    + matrices[w.boneIndex3].MultiplyPoint3x4(k.point3) * w.weight3;
            }
            centers[0] = toCord.MultiplyPoint3x4(StartWorld);
            centers[centers.Length - 1] = toCord.MultiplyPoint3x4(EndWorld);
            var bounds = new Bounds(centers[0], Vector3.zero);
            for (int i = 0; i < centers.Length; i++)
            {
                var forward = (centers[Math.Min(i + 1, centers.Length - 1)] - centers[Math.Max(0, i - 1)]).normalized;
                var across = Vector3.Cross(Vector3.up, forward).normalized;
                if (across.sqrMagnitude < .01f) across = Vector3.right;
                var up = Vector3.Cross(forward, across).normalized;
                for (int j = 0; j < Sides; j++)
                {
                    var n = across * circle[j].x + up * circle[j].y;
                    var t = -across * circle[j].y + up * circle[j].x;
                    int at = i * Sides + j;
                    vertices[at] = centers[i] + n * Radius; normals[at] = n;
                    tangents[at] = new Vector4(t.x, t.y, t.z, 1);
                }
                bounds.Encapsulate(centers[i]);
            }
            instance.vertices = vertices; instance.normals = normals; instance.tangents = tangents;
            bounds.Expand(Radius * 2.1f); instance.bounds = bounds;
        }
        void Release()
        {
            if (!instance) return;
            if (cord && cord.sharedMesh == instance) cord.sharedMesh = sourceMesh;
            if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
            instance = null;
        }
        void OnDestroy() => Release();
    }
}
