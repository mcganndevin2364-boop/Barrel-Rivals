using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public const string Root = "Assets/_Project/Development/HeroHorse";
        public const string ModelPath = Root + "/HeroHorse.fbx";
        public static string Output => Environment.GetEnvironmentVariable("BARREL_HORSE_BENCHMARK_OUTPUT") ?? "Temp/HorseBenchmarkReview";

        public static void InspectImport()
        {
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.minBoneWeight=0.0000001f;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = true;
            importer.importCameras = false; importer.importLights = false;
            importer.SaveAndReimport();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Directory.CreateDirectory(Output);
            var lines = skins.Select(r => r.name + " vertices=" + r.sharedMesh.vertexCount + " colors=" + r.sharedMesh.colors.Length + " materials=" + r.sharedMaterials.Length + " matrix=" + r.localToWorldMatrix).ToList();
            foreach (var skin in skins)
            {
                var m = new Mesh(); skin.BakeMesh(m, true);
                var points = m.vertices.Select(v => skin.transform.TransformPoint(v)).ToArray();
                var b = new Bounds(points[0],Vector3.zero); foreach(var p in points)b.Encapsulate(p);
                lines.Add(skin.name + " bounds min=" + b.min.ToString("F6") + " max=" + b.max.ToString("F6"));
                Object.DestroyImmediate(m);
            }
            lines.AddRange(model.GetComponentsInChildren<Transform>().Select(t=>t.name+" pos="+t.position.ToString("F6")+" scale="+t.lossyScale.ToString("F4")));
            lines.AddRange(AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Select(c=>"CLIP "+c.name+" length="+c.length+" fps="+c.frameRate));
            File.WriteAllLines(Path.Combine(Output,"import.txt"),lines);
            Debug.Log("HORSE_IMPORT_INSPECTED " + Output);
        }
    }
}
