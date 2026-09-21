using System;
using System.Collections.Generic;
using System.Linq;
using BarrelRivals.Practice;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static class StableReinDrapeBuilder
    {
        // The already surface-fitted path is sampled against the neutral body. Four
        // nearby vertices blend bone influences, avoiding a nearest-bone step at the neck.
        public static void Configure(MeshFilter cord, Transform model, SkinnedMeshRenderer body,
            Transform horn, Transform bit, Vector3 hornOffset, Vector3[] modelPoints)
        {
            var baked = new Mesh();
            try
            {
                body.BakeMesh(baked, true);
                var matrix = model.worldToLocalMatrix * body.localToWorldMatrix;
                var surface = baked.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var skin = body.sharedMesh.boneWeights; var bones = body.bones;
                var knots = new StableReinDrape.Knot[modelPoints.Length];
                for (int k = 0; k < knots.Length; k++)
                {
                    var nearest = new int[4]; var distance = Enumerable.Repeat(float.PositiveInfinity, 4).ToArray();
                    for (int i = 0; i < surface.Length; i++)
                    {
                        float d = (surface[i] - modelPoints[k]).sqrMagnitude;
                        for (int n = 0; n < 4; n++) if (d < distance[n])
                        {
                            for (int j = 3; j > n; j--) { distance[j] = distance[j - 1]; nearest[j] = nearest[j - 1]; }
                            distance[n] = d; nearest[n] = i; break;
                        }
                    }
                    var combined = new Dictionary<int, float>();
                    void Add(int bone, float weight) { combined[bone] = combined.GetValueOrDefault(bone) + weight; }
                    for (int i = 0; i < 4; i++)
                    {
                        var w = skin[nearest[i]]; float influence = 1 / Mathf.Max(.00001f, distance[i]);
                        Add(w.boneIndex0, w.weight0 * influence); Add(w.boneIndex1, w.weight1 * influence);
                        Add(w.boneIndex2, w.weight2 * influence); Add(w.boneIndex3, w.weight3 * influence);
                    }
                    var ranked = combined.Where(p => p.Value > 0).OrderByDescending(p => p.Value).Take(4).ToArray();
                    float total = ranked.Sum(p => p.Value);
                    var indices = new int[4]; var weights = new float[4];
                    for (int i = 0; i < ranked.Length; i++) { indices[i] = ranked[i].Key; weights[i] = ranked[i].Value / total; }
                    var point = model.TransformPoint(modelPoints[k]);
                    knots[k] = new StableReinDrape.Knot {
                        weights = new BoneWeight { boneIndex0 = indices[0], boneIndex1 = indices[1], boneIndex2 = indices[2], boneIndex3 = indices[3],
                            weight0 = weights[0], weight1 = weights[1], weight2 = weights[2], weight3 = weights[3] },
                        point0 = bones[indices[0]].InverseTransformPoint(point), point1 = bones[indices[1]].InverseTransformPoint(point),
                        point2 = bones[indices[2]].InverseTransformPoint(point), point3 = bones[indices[3]].InverseTransformPoint(point)
                    };
                }
                var drape = cord.GetComponent<StableReinDrape>(); if (!drape) drape = cord.gameObject.AddComponent<StableReinDrape>();
                drape.Configure(cord, bones, knots, horn, hornOffset, bit);
            }
            finally { Object.DestroyImmediate(baked); }
        }
    }
}
