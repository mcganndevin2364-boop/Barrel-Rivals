using System;
using BarrelRivals.Core.Stable;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Applies immutable saved material assets. Never writes shared materials, stats or race state.</summary>
    public sealed class StableAppearance : MonoBehaviour
    {
        [Serializable] public sealed class Binding { public Renderer renderer; public int materialIndex; public StableSlot slot; }
        [Serializable] public sealed class Palette { public string gearId; public Material material; }
        [SerializeField] private Binding[] bindings;
        [SerializeField] private Palette[] palettes;
        public StableProfile Applied { get; private set; }
        public void Configure(Binding[] targets,Palette[] materials) { bindings=targets;palettes=materials; }
        public void Apply(StableProfile profile)
        {
            if(profile==null || !profile.IsValid)profile=StableProfile.Starter();
            Applied=profile.Copy();
            foreach(var binding in bindings) {
                if(!binding.renderer)continue;
                var materials=binding.renderer.sharedMaterials;
                if(binding.materialIndex<0 || binding.materialIndex>=materials.Length)continue;
                foreach(var palette in palettes) if(palette.gearId==profile.Equipped(binding.slot)) {
                    materials[binding.materialIndex]=palette.material;break;
                }
                binding.renderer.sharedMaterials=materials;
            }
        }
    }
}
