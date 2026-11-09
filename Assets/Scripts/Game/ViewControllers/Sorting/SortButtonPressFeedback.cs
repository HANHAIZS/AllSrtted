using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Presentation-only press and rebound using the once-authored scale and unscaled time.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class SortButtonPressFeedback : AllSortedController,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ICancelHandler
    {
        private Button button;
        private Vector3 authoredScale;
        private float scale=1,from=1,target=1,elapsed,duration;
        private int pointer;
        private bool initialized,held,animating,rebound;

        /// <summary>Captures the authored scale once so interrupted animations cannot accumulate drift.</summary>
        private void Awake()
        {
            button=GetComponent<Button>();authoredScale=transform.localScale;initialized=true;
        }

        /// <summary>Owns one eligible pointer without executing any gameplay action.</summary>
        public void OnPointerDown(PointerEventData data)
        {
            if(held || data.button!=PointerEventData.InputButton.Left || !button || !button.IsInteractable()) return;
            held=true;pointer=data.pointerId;rebound=false;Animate(.91f,.065f);
        }

        /// <summary>Releases the owning pointer and starts a small visual rebound.</summary>
        public void OnPointerUp(PointerEventData data)
        {
            if(!held || data.pointerId!=pointer) return;
            held=false;rebound=true;Animate(1.045f,.085f);
        }

        /// <summary>Dragging off the button returns to the authored scale without an activation bounce.</summary>
        public void OnPointerExit(PointerEventData data)
        {
            if(!held || data.pointerId!=pointer) return;
            held=false;rebound=false;Animate(1,.12f);
        }

        /// <summary>Cancellation clears transient state immediately.</summary>
        public void OnCancel(BaseEventData data) {ResetFeedback();}

        /// <summary>Restores the original scale on level transitions, cancellation and disable.</summary>
        public void ResetFeedback()
        {
            held=false;animating=false;rebound=false;scale=1;
            if(initialized) transform.localScale=authoredScale;
        }

        /// <summary>Disabling a pressed control cannot leave a shrunken authored transform.</summary>
        private void OnDisable() {ResetFeedback();}

        /// <summary>Starts from the current visual scale rather than recapturing the authored transform.</summary>
        private void Animate(float value,float seconds)
        {
            from=scale;target=value;duration=seconds;elapsed=0;animating=true;
        }

        /// <summary>Uses unscaled time and performs no work for settled buttons.</summary>
        private void Update()
        {
            if((held || animating) && (!button || !button.isActiveAndEnabled || !button.IsInteractable())) {ResetFeedback();return;}
            if(!animating) return;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);
            scale=Mathf.Lerp(from,target,t*t*(3-2*t));transform.localScale=authoredScale*scale;
            if(t<1) return;
            animating=false;
            if(rebound) {rebound=false;Animate(1,.12f);}
        }
    }
}
