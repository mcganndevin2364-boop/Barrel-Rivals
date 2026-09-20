using System;
using UnityEditor;
using UnityEngine;

namespace BarrelRivals.Editor
{
    /// <summary>Shares the reviewed galvanized maps with a restrained, separate rail finish.</summary>
    public static class ReinsRailMaterialBuilder
    {
        public const string AssetPath = ReinsPremiumArenaBuilder.Root + "/Materials/Galvanized arena rails.mat";

        public static Material Get(Material source)
        {
            if (!source) throw new ArgumentNullException(nameof(source));
            if (!source.shader || source.shader.name != "Universal Render Pipeline/Lit")
                throw new ArgumentException("The galvanized source must use URP Lit.", nameof(source));
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
                if (!source.GetTexture(property))
                    throw new ArgumentException("The galvanized source is missing " + property + ".", nameof(source));

            var existing = AssetDatabase.LoadMainAssetAtPath(AssetPath);
            if (existing && !(existing is Material))
                throw new InvalidOperationException("The rail material path contains a different asset type: " + AssetPath);
            var material = existing as Material;
            if (!material)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, AssetPath);
            }

            // Reapply on every generation while retaining the asset GUID and original texture
            // references. Existing saved materials must not bypass finish updates.
            material.shader = source.shader;
            material.CopyPropertiesFromMaterial(source);
            material.name = "Galvanized arena rails";
            var tint = new Color(.94f, .95f, .94f, 1f);
            material.SetColor("_BaseColor", tint);
            material.SetColor("_Color", tint);
            material.SetFloat("_BumpScale", .12f);
            material.SetFloat("_Smoothness", .55f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }
    }
}
