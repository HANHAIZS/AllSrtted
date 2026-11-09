using UnityEngine;
namespace AllSorted
{
    public sealed partial class SortPopupView
    {
        public float OpenSeconds=.25f,CloseSeconds=.15f;
        private enum MotionPhase {None,Opening,Closing}
        private MotionPhase motionPhase;
        private RectTransform motionContent;
        private CanvasGroup contentFade,dimFade;
        private Vector3 authoredScale;
        private float authoredContentAlpha,authoredDimAlpha;
        private bool authoredContentInput,authoredContentRaycasts,authoredDimInput,authoredDimRaycasts;
        private bool motionBound,dimHandedOff;
        private float motionElapsed,motionDuration,startFactor,startOpacity,startDim;

        private bool BindMotion()
        {
            if(motionBound)return true;
            motionContent=transform.Find("PopupContent") as RectTransform;var dim=transform.Find("DimBackground");
            if(!motionContent || !dim)return false;
            authoredScale=motionContent.localScale;
            contentFade=motionContent.GetComponent<CanvasGroup>();if(!contentFade)contentFade=motionContent.gameObject.AddComponent<CanvasGroup>();
            dimFade=dim.GetComponent<CanvasGroup>();if(!dimFade)dimFade=dim.gameObject.AddComponent<CanvasGroup>();
            authoredContentAlpha=contentFade.alpha;authoredDimAlpha=dimFade.alpha;
            authoredContentInput=contentFade.interactable;authoredContentRaycasts=contentFade.blocksRaycasts;authoredDimInput=dimFade.interactable;authoredDimRaycasts=dimFade.blocksRaycasts;
            motionBound=true;return true;
        }
        private void BeginOpenMotion()
        {
            bool resume=motionBound && motionPhase==MotionPhase.Closing && gameObject.activeSelf;
            if(!BindMotion()){logicallyOpen=true;gameObject.SetActive(true);return;}
            float inheritedDim=resume?DimFraction():0;
            float initialFactor=resume?ScaleFraction():.85f;
            float initialOpacity=resume?ContentFraction():.005f;
            // Only one backdrop remains visible when Replace cross-fades two panels.
            var parent=transform.parent;
            if(parent)for(int i=0;i<parent.childCount;i++){
                var other=parent.GetChild(i).GetComponent<SortPopupView>();
                if(!other || other==this || !other.gameObject.activeSelf || !other.motionBound || other.motionPhase!=MotionPhase.Closing)continue;
                inheritedDim=Mathf.Max(inheritedDim,other.DimFraction());other.dimHandedOff=true;other.dimFade.alpha=0;other.dimFade.blocksRaycasts=false;
            }
            logicallyOpen=true;gameObject.SetActive(true);transform.SetAsLastSibling();
            contentFade.interactable=authoredContentInput;contentFade.blocksRaycasts=authoredContentRaycasts;dimFade.interactable=authoredDimInput;dimFade.blocksRaycasts=authoredDimRaycasts;
            motionElapsed=0;motionDuration=Mathf.Max(.001f,OpenSeconds);startFactor=initialFactor;startOpacity=initialOpacity;startDim=inheritedDim;dimHandedOff=false;motionPhase=MotionPhase.Opening;
            ApplyMotion(startFactor,startOpacity,startDim);
            if(!Application.isPlaying || !enabled)FinishOpenMotion();
        }
        private void BeginCloseMotion()
        {
            if(!motionBound || !gameObject.activeInHierarchy || !enabled || !Application.isPlaying){gameObject.SetActive(false);return;}
            startFactor=ScaleFraction();startOpacity=ContentFraction();startDim=DimFraction();motionElapsed=0;motionDuration=Mathf.Max(.001f,CloseSeconds);motionPhase=MotionPhase.Closing;
            contentFade.interactable=false;contentFade.blocksRaycasts=false;dimFade.interactable=false;dimFade.blocksRaycasts=false;
        }
        private void Update(){if(motionPhase!=MotionPhase.None)AdvanceMotion(Time.unscaledDeltaTime);}
        private void AdvanceMotion(float delta)
        {
            if(!motionBound || motionPhase==MotionPhase.None)return;
            motionElapsed+=Mathf.Max(0,delta);float t=Mathf.Clamp01(motionElapsed/motionDuration);
            if(motionPhase==MotionPhase.Opening){
                float scale=t<=.7f?Mathf.LerpUnclamped(startFactor,1.03f,EaseOutCubic(t/.7f)):Mathf.LerpUnclamped(1.03f,1,Smooth((t-.7f)/.3f));
                ApplyMotion(scale,Mathf.Lerp(startOpacity,1,Smooth(t)),Mathf.Lerp(startDim,1,Smooth(t)));
                if(t>=1)FinishOpenMotion();
            }else{
                float progress=Smooth(t);ApplyMotion(Mathf.Lerp(startFactor,.85f,progress),Mathf.Lerp(startOpacity,0,progress),dimHandedOff?0:Mathf.Lerp(startDim,0,progress));
                if(t>=1){motionPhase=MotionPhase.None;gameObject.SetActive(false);}
            }
        }
        private void ApplyMotion(float factor,float opacity,float dim){motionContent.localScale=authoredScale*factor;contentFade.alpha=authoredContentAlpha*opacity;dimFade.alpha=authoredDimAlpha*dim;}
        private void FinishOpenMotion(){ApplyMotion(1,1,1);motionPhase=MotionPhase.None;}
        private float ScaleFraction(){return Mathf.Abs(authoredScale.x)>.00001f?motionContent.localScale.x/authoredScale.x:1;}
        private float ContentFraction(){return authoredContentAlpha>.00001f?Mathf.Clamp01(contentFade.alpha/authoredContentAlpha):0;}
        private float DimFraction(){return authoredDimAlpha>.00001f?Mathf.Clamp01(dimFade.alpha/authoredDimAlpha):0;}
        private static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        private static float EaseOutCubic(float t){t=1-Mathf.Clamp01(t);return 1-t*t*t;}
        private void OnDisable()
        {
            logicallyOpen=false;motionPhase=MotionPhase.None;motionElapsed=0;dimHandedOff=false;
            if(!motionBound || !motionContent || !contentFade || !dimFade)return;
            motionContent.localScale=authoredScale;contentFade.alpha=authoredContentAlpha;dimFade.alpha=authoredDimAlpha;
            contentFade.interactable=authoredContentInput;contentFade.blocksRaycasts=authoredContentRaycasts;dimFade.interactable=authoredDimInput;dimFade.blocksRaycasts=authoredDimRaycasts;
        }
    }
}
