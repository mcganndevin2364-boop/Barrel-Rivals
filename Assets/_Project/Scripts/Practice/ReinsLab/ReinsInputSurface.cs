using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace BarrelRivals.Practice
{
    public enum ReinsPad { Left, Right, Rhythm }

    /// <summary>A pad owns one pointer until release. Leaving a pad cannot transfer a held touch.</summary>
    public sealed class ReinsInputSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IInitializePotentialDragHandler, ICancelHandler
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
            float height=Mathf.Max(80,((RectTransform)transform).rect.height*.50f);
            owner.Pull(pad,e.pointerId,Mathf.Clamp01((origin.y-local.y)/height));
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(pointer!=e.pointerId) return;
            pointer=null;
            if(!owner)return;
            // A cancelled/removed touch must stop the attempt before any launch grading.
            // Pointer IDs combine device/touch IDs; use the actual touchId supplied by the UI module.
            if(CancelledTouch(e))owner.CancelRun();else owner.Release(pad,e.pointerId);
        }
        private static bool CancelledTouch(PointerEventData e)
        {
            if(!(e is ExtendedPointerEventData extended) || !(extended.device is Touchscreen screen))return false;
            if(!screen.added)return true;
            foreach(var touch in screen.touches)
                if(touch.touchId.ReadValue()==extended.touchId)
                    return touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled;
            return true;
        }
        public void OnCancel(BaseEventData e)
        {if(pointer.HasValue && owner)owner.CancelRun();pointer=null;}
        // Explicit controller reset owns cancellation; clearing a pad cannot synthesize release.
        public void Clear() { pointer=null; }
        private void OnDisable()
        {if(pointer.HasValue){pointer=null;if(owner)owner.CancelRun();}}
    }
}
