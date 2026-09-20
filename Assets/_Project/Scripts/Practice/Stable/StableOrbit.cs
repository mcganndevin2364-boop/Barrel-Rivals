using UnityEngine;
using UnityEngine.EventSystems;
namespace BarrelRivals.Practice
{
    public sealed class StableOrbit : MonoBehaviour, IDragHandler
    {
        [SerializeField] private StableController controller;
        public void Configure(StableController owner) { controller=owner; }
        public void OnDrag(PointerEventData data)
        { if(controller && Screen.width>0)controller.RotateHorse(-data.delta.x/Screen.width*220); }
    }
}
