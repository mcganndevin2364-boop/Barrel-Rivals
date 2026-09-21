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
        [Range(60,85)] public float riderFieldOfView=72;
        public Vector3 neutralRiderPosition=new Vector3(0,2.20f,-.35f);
        private Vector3 inspectionPosition;
        private Quaternion inspectionRotation;
        private float inspectionFieldOfView;
        private bool savedInspectionView,wasRiderView;

        void LateUpdate()
        {
            if(!horse || !reviewCamera)return;
            if(!savedInspectionView)
            {
                inspectionPosition=horse.ModelSpace.InverseTransformPoint(reviewCamera.transform.position);
                inspectionRotation=Quaternion.Inverse(horse.ModelSpace.rotation)*reviewCamera.transform.rotation;
                inspectionFieldOfView=reviewCamera.fieldOfView;savedInspectionView=true;
            }
            var attachments=horse.GetComponent<HeroHorseAttachments>();
            if(attachments)attachments.SetRiderView(riderView);
            if(riderView)
            {
                reviewCamera.fieldOfView=riderFieldOfView;
                reviewCamera.transform.position=reducedMotion?horse.ModelSpace.TransformPoint(neutralRiderPosition):horse.FollowSupportPoint(neutralRiderPosition);
                reviewCamera.transform.rotation=horse.ModelSpace.rotation*Quaternion.Euler(8,0,0);
            }
            else if(wasRiderView)
            {
                reviewCamera.transform.position=horse.ModelSpace.TransformPoint(inspectionPosition);
                reviewCamera.transform.rotation=horse.ModelSpace.rotation*inspectionRotation;
                reviewCamera.fieldOfView=inspectionFieldOfView;
            }
            wasRiderView=riderView;
        }
    }
}
