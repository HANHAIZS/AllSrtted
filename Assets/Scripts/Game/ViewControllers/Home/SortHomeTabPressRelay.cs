using UnityEngine.EventSystems;
namespace AllSorted
{
    /// <summary>Forwards press feedback to a visual child while keeping the actual tab hit region stable.</summary>
    public sealed class SortHomeTabPressRelay : AllSortedController,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ICancelHandler
    {
        public SortButtonPressFeedback Feedback;
        public void OnPointerDown(PointerEventData data) {if(Feedback) Feedback.OnPointerDown(data);}
        public void OnPointerUp(PointerEventData data) {if(Feedback) Feedback.OnPointerUp(data);}
        public void OnPointerExit(PointerEventData data) {if(Feedback) Feedback.OnPointerExit(data);}
        public void OnCancel(BaseEventData data) {if(Feedback) Feedback.OnCancel(data);}
        private void OnDisable() {if(Feedback) Feedback.ResetFeedback();}
    }
}
