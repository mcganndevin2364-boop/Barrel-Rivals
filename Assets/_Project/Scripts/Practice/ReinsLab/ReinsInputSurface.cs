using UnityEngine;
using UnityEngine.EventSystems;

namespace BarrelRivals.Practice
{
    public enum ReinsPad { Left, Right, Rhythm }

    /// <summary>A pad owns one pointer until release. Leaving a pad cannot transfer a held touch.</summary>
    public sealed class ReinsInputSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IInitializePotentialDragHandler
    {
        [SerializeField] private ReinsLabController owner;
        [SerializeField] private ReinsPad pad;
        private int? pointer;
        private Vector2 origin;
        public void Configure(ReinsLabController controller, ReinsPad kind) { owner=controller; pad=kind; }
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold=false; }
        public void OnPointerDown(PointerEventData e)
        {
            if(pointer.HasValue || !owner || !owner.Press(pad,e.pointerId)) return;
            pointer=e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out origin);
        }
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId || !owner) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var local);
            float height=Mathf.Max(80,((RectTransform)transform).rect.height*.65f);
            owner.Pull(pad,e.pointerId,Mathf.Clamp01((origin.y-local.y)/height));
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(pointer!=e.pointerId) return;
            if(owner) owner.Release(pad,e.pointerId);
            pointer=null;
        }
        public void Clear() { if(pointer.HasValue && owner) owner.Release(pad,pointer.Value); pointer=null; }
        private void OnDisable() => Clear();
    }
}
