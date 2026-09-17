using BarrelRivals.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BarrelRivals.Practice
{
    public sealed class PracticeInputSurface : MonoBehaviour, IPointerDownHandler, IDragHandler,
        IPointerUpHandler, IInitializePotentialDragHandler, ICancelHandler
    {
        [SerializeField] private PracticeController owner;
        [SerializeField] private bool drawing;
        public void Configure(PracticeController controller,bool isDrawing) { owner=controller; drawing=isDrawing; }
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold=false; }
        public void OnPointerDown(PointerEventData data) { owner.PointerDown(data.pointerId,drawing,Point(data)); }
        public void OnDrag(PointerEventData data) { if(drawing) owner.PointerMove(data.pointerId,Point(data)); }
        public void OnPointerUp(PointerEventData data) { owner.PointerUp(data.pointerId,Point(data)); }
        public void OnCancel(BaseEventData data) { owner.CancelPractice(); }
        private TracePoint Point(PointerEventData data)
        {
            var rect=(RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out Vector2 local);
            return new TracePoint((local.x-rect.rect.xMin)/rect.rect.width,(local.y-rect.rect.yMin)/rect.rect.height);
        }
    }
}
