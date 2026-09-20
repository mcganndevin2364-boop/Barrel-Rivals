using System.Collections.Generic;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>
    /// Owns only private presentation materials. Kept active outside the ghost because a ghost with
    /// no saved recording can remain inactive for its whole lifetime and never receive OnDestroy.
    /// </summary>
    public sealed class ReinsGhostMaterialResources : MonoBehaviour
    {
        private Transform ghost;
        private bool initialized;
        private readonly List<Material> materials = new List<Material>();

        internal void Initialize(Transform target) { ghost=target;initialized=true; }
        internal void Own(Material material) { if(material)materials.Add(material); }
        private void LateUpdate()
        {
            if(!initialized || ghost)return;
            Release();
            Destroy(gameObject);
        }
        internal void Release()
        {
            foreach(var material in materials)
                if(material)
                {
                    if(Application.isPlaying)Destroy(material);
                    else DestroyImmediate(material);
                }
            materials.Clear();
        }
        private void OnDestroy() { Release(); }
    }
}
