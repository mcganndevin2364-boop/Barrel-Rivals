using BarrelRivals.Core.Reins;
using UnityEngine;

namespace BarrelRivals.Practice
{
    public sealed class ReinsFootingVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] patches;
        public void Configure(Renderer[] value)=>patches=value;
        public void Apply(ReinsManifest manifest)
        {
            if(patches==null)return;
            var block=new MaterialPropertyBlock();
            foreach(var patch in patches)
            {
                var p=patch.transform.position;Color color;
                switch(manifest.SurfaceAt(p.x,p.z))
                {
                    case ReinsSurface.LooseSand:color=new Color(.86f,.73f,.48f);break;
                    case ReinsSurface.TackyClay:color=new Color(.58f,.34f,.23f);break;
                    case ReinsSurface.MuddySlop:color=new Color(.33f,.27f,.19f);break;
                    default:color=new Color(.78f,.74f,.67f);break;
                }
                block.SetColor("_BaseColor",color);patch.SetPropertyBlock(block);
            }
        }
    }
}
