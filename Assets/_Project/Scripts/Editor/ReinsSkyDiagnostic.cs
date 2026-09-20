using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BarrelRivals.Editor
{
    /// <summary>Bounded, opt-in world/sky isolation captures; never modifies the saved scene.</summary>
    public static class ReinsSkyDiagnostic
    {
        public static void Capture()
        {
            string directory = Environment.GetEnvironmentVariable("BARREL_SKY_DIAGNOSTIC_DIRECTORY");
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Set BARREL_SKY_DIAGNOSTIC_DIRECTORY to an evidence directory.");
            Directory.CreateDirectory(directory);
            EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0, 2.3f, 18), Quaternion.Euler(5, 180, 0));
            camera.fieldOfView = 65;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            Write(camera, Path.Combine(directory, "south-world-and-sky.png"));
            int mask = camera.cullingMask;
            camera.cullingMask = 0;
            Write(camera, Path.Combine(directory, "south-sky-only.png"));
            camera.cullingMask = mask;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.10f,.12f,.15f);
            Write(camera, Path.Combine(directory, "south-world-only.png"));
            Debug.Log("BARREL_SKY: three isolated actual-camera diagnostic renders saved; scene unchanged.");
        }

        private static void Write(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);
                image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
