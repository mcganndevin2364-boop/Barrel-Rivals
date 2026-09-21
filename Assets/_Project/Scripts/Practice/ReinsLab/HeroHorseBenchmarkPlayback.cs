using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Development scene camera only. No race input, rewards or root motion.</summary>
    public sealed class HeroHorseBenchmarkPlayback : MonoBehaviour
    {
        public HorseRigBindings horse;
        public Camera reviewCamera;
        public bool riderView;
        public bool reducedMotion;
        public Vector3 neutralRiderPosition=new Vector3(0,2.20f,-.35f);
        void LateUpdate()
        {
            if (!horse || !reviewCamera || !riderView)return;
            reviewCamera.transform.position=reducedMotion ? horse.ModelSpace.TransformPoint(neutralRiderPosition) : horse.FollowSupportPoint(neutralRiderPosition);
            reviewCamera.transform.rotation=horse.ModelSpace.rotation*Quaternion.Euler(8,0,0);
        }
    }
}
