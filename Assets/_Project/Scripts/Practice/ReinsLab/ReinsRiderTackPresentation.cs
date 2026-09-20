using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Cosmetic rider grip and flexible reins. Reads accepted presentation state only.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(220)]
    public sealed class ReinsRiderTackPresentation : MonoBehaviour
    {
        public const int Segments = 32;
        private const int TubeSides = 8, BraidSides = 5;
        [SerializeField] private ReinsHorsePresentation source;
        [SerializeField] private Transform leftHand, rightHand, leftGrip, rightGrip, leftBit, rightBit;
        [SerializeField] private MeshFilter leftRein, rightRein;
        [SerializeField] private Vector3 leftRest, rightRest;
        private Mesh leftMesh, rightMesh;
        private Vector3[] leftVertices, rightVertices, leftNormals, rightNormals;
        private Vector4[] leftTangents, rightTangents;
        private bool initialized;

        public void Configure(ReinsHorsePresentation presentation, Transform lh, Transform rh,
            Transform lg, Transform rg, Transform lb, Transform rb, MeshFilter lr, MeshFilter rr)
        {
            source = presentation; leftHand = lh; rightHand = rh; leftGrip = lg; rightGrip = rg;
            leftBit = lb; rightBit = rb; leftRein = lr; rightRein = rr;
            leftRest = lh.localPosition; rightRest = rh.localPosition;
        }

        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized || !leftRein || !rightRein || !leftRein.sharedMesh || !rightRein.sharedMesh) return;
            // Never deform persistent source assets or another horse/ghost's meshes.
            leftMesh = Instantiate(leftRein.sharedMesh); leftMesh.name = "Left flexible rein instance"; leftMesh.MarkDynamic();
            rightMesh = Instantiate(rightRein.sharedMesh); rightMesh.name = "Right flexible rein instance"; rightMesh.MarkDynamic();
            leftRein.sharedMesh = leftMesh; rightRein.sharedMesh = rightMesh;
            leftVertices = new Vector3[leftMesh.vertexCount]; rightVertices = new Vector3[rightMesh.vertexCount];
            leftNormals = new Vector3[leftMesh.vertexCount]; rightNormals = new Vector3[rightMesh.vertexCount];
            leftTangents = new Vector4[leftMesh.vertexCount]; rightTangents = new Vector4[rightMesh.vertexCount];
            initialized = true;
        }

        private void LateUpdate() => RenderImmediate();

        /// <summary>Updates a paused inspection after accepted pose and Animator have been applied.</summary>
        public void RenderImmediate()
        {
            if (!source || !leftHand || !rightHand || !leftGrip || !rightGrip || !leftBit || !rightBit) return;
            Initialize(); if (!initialized) return;
            float speed = Mathf.Clamp(source.Speed, 0, 14), turn = Mathf.Clamp(source.Turn, -1.5f, 1.5f);
            float left = Mathf.Clamp01(source.LeftRein), right = Mathf.Clamp01(source.RightRein);
            // A subtle pose response, without free-running noise or any modification of the race root.
            leftHand.localPosition = leftRest + new Vector3(-turn * .009f, left * .016f, -.082f * left - speed * .0006f);
            rightHand.localPosition = rightRest + new Vector3(-turn * .009f, right * .016f, -.082f * right - speed * .0006f);
            leftHand.localRotation = Quaternion.Euler(-left * 5, -8, -9 - left * 3);
            rightHand.localRotation = Quaternion.Euler(-right * 5, 8, 9 + right * 3);
            Deform(leftRein.transform, leftGrip.position, leftBit.position, left, -1, leftMesh, leftVertices, leftNormals, leftTangents);
            Deform(rightRein.transform, rightGrip.position, rightBit.position, right, 1, rightMesh, rightVertices, rightNormals, rightTangents);
        }

        private static void Deform(Transform space, Vector3 startWorld, Vector3 endWorld, float tension,
            int side, Mesh mesh, Vector3[] vertices, Vector3[] normals, Vector4[] tangents)
        {
            var start = space.InverseTransformPoint(startWorld); var end = space.InverseTransformPoint(endWorld);
            float sag = Mathf.Lerp(.17f, .035f, tension);
            Vector3 c1 = Vector3.Lerp(start, end, .30f) + new Vector3(side * .045f, -sag, 0);
            Vector3 c2 = Vector3.Lerp(start, end, .72f) + new Vector3(side * .025f, -sag * .5f, 0);
            WriteTube(vertices, normals, tangents, 0, TubeSides, 0, .0085f, 0, start, c1, c2, end);
            int offset = (Segments + 1) * TubeSides;
            WriteTube(vertices, normals, tangents, offset, BraidSides, .0084f, .0022f, 0, start, c1, c2, end);
            WriteTube(vertices, normals, tangents, offset + (Segments + 1) * BraidSides, BraidSides,
                .0084f, .0022f, Mathf.PI, start, c1, c2, end);
            mesh.vertices = vertices; mesh.normals = normals; mesh.tangents = tangents;
            // Set explicitly rather than RecalculateBounds scanning the complete mesh every frame.
            var bounds = new Bounds(start, Vector3.zero); bounds.Encapsulate(end); bounds.Encapsulate(c1); bounds.Encapsulate(c2);
            bounds.Expand(.04f); mesh.bounds = bounds;
        }

        private static void WriteTube(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, int offset, int sides,
            float orbit, float radius, float phase, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            for (int ring = 0; ring <= Segments; ring++)
            {
                float t = ring / (float)Segments, q = 1 - t;
                var center = q * q * q * a + 3 * q * q * t * b + 3 * q * t * t * c + t * t * t * d;
                var forward = (3 * q * q * (b - a) + 6 * q * t * (c - b) + 3 * t * t * (d - c)).normalized;
                var across = Vector3.Cross(Vector3.up, forward).normalized;
                if (across.sqrMagnitude < .01f) across = Vector3.right;
                var up = Vector3.Cross(forward, across).normalized;
                float braid = t * Mathf.PI * 2 * 14 + phase;
                center += (across * Mathf.Cos(braid) + up * Mathf.Sin(braid)) * orbit;
                for (int j = 0; j < sides; j++)
                {
                    float angle = j * Mathf.PI * 2 / sides;
                    var normal = across * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    int index = offset + ring * sides + j; vertices[index] = center + normal * radius; normals[index] = normal;
                    var tangent = -across * Mathf.Sin(angle) + up * Mathf.Cos(angle);
                    tangents[index] = new Vector4(tangent.x,tangent.y,tangent.z,1);
                }
            }
        }

        /// <summary>Editor-authored persistent topology; runtime creates private deformable copies.</summary>
        public static Mesh CreateReinTemplate()
        {
            int count = (Segments + 1) * (TubeSides + BraidSides * 2);
            var mesh = new Mesh { name = "Braided rein topology" };
            var vertices = new Vector3[count]; var normals = new Vector3[count]; var uv = new Vector2[count];
            int offset = 0;
            foreach (int sides in new[] { TubeSides, BraidSides, BraidSides })
            {
                for (int r=0;r<=Segments;r++) for (int s=0;s<sides;s++)
                    uv[offset+r*sides+s] = new Vector2(s/(float)sides,r/(float)Segments*8);
                offset += (Segments+1)*sides;
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.subMeshCount = 3;
            mesh.SetTriangles(TubeIndices(TubeSides, 0), 0);
            mesh.SetTriangles(TubeIndices(BraidSides, (Segments + 1) * TubeSides), 1);
            mesh.SetTriangles(TubeIndices(BraidSides, (Segments + 1) * (TubeSides + BraidSides)), 2);
            PoseTemplate(mesh, vertices, normals, new Vector4[count]);
            return mesh;
        }

        // Template helper needs no Transform, and keeps the runtime deformation path allocation-free.
        private static void PoseTemplate(Mesh mesh, Vector3[] vertices, Vector3[] normals, Vector4[] tangents)
        {
            var start = Vector3.zero; var end = Vector3.forward;
            var c1 = Vector3.Lerp(start, end, .3f) + Vector3.down * .15f;
            var c2 = Vector3.Lerp(start, end, .72f) + Vector3.down * .08f;
            WriteTube(vertices, normals, tangents, 0, TubeSides, 0, .0085f, 0, start, c1, c2, end);
            WriteTube(vertices, normals, tangents, (Segments + 1) * TubeSides, BraidSides, .0084f, .0022f, 0, start, c1, c2, end);
            WriteTube(vertices, normals, tangents, (Segments + 1) * (TubeSides + BraidSides), BraidSides, .0084f, .0022f, Mathf.PI, start, c1, c2, end);
            mesh.vertices = vertices; mesh.normals = normals; mesh.tangents = tangents; mesh.RecalculateBounds();
        }

        private static int[] TubeIndices(int sides, int offset)
        {
            var indices = new int[Segments * sides * 6]; int n = 0;
            for (int r = 0; r < Segments; r++) for (int j = 0; j < sides; j++)
            {
                int a = offset + r * sides + j, b = offset + r * sides + (j + 1) % sides;
                int c = a + sides, d = b + sides;
                indices[n++] = a; indices[n++] = b; indices[n++] = c;
                indices[n++] = b; indices[n++] = d; indices[n++] = c;
            }
            return indices;
        }

        private void OnDestroy()
        {
            if (leftMesh) Destroy(leftMesh); if (rightMesh) Destroy(rightMesh);
        }
    }
}
