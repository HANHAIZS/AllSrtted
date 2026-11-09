using UnityEngine;
using QFramework;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        private readonly float[] weights=new float[3],fromWeights=new float[3],fromAlphas=new float[3];
        private bool transitioning;
        private float elapsed,fromX,lastWidth,lastHeight;
        private int targetIndex=1;
        private float TargetX {get {return (targetIndex-1)*NavigationArea.rect.width/3;}}

        /// <summary>Retargets from rendered values; there is only one animation owner and no queued transitions.</summary>
        private void BeginTransition()
        {
            if(!ready) return;int requested=(int)navigation.SelectedTab.Value;
            if(!HasPage(requested)) {
                Fail("Menu page is unavailable: "+requested);
                this.SendCommand(new SelectSortHomeTabCommand((SortHomeTab)targetIndex));return;
            }
            targetIndex=requested;fromX=SelectionPlate.anchoredPosition.x;
            for(int i=0;i<3;i++) {fromWeights[i]=weights[i];fromAlphas[i]=Pages[i]?Pages[i].alpha:0;}
            elapsed=0;transitioning=true;SetPageInput(-1);SetStartButtonVisible(false);
        }
        private void SnapToSelection()
        {
            targetIndex=(int)navigation.SelectedTab.Value;transitioning=false;
            var pos=SelectionPlate.anchoredPosition;pos.x=TargetX;SelectionPlate.anchoredPosition=pos;
            for(int i=0;i<3;i++) {weights[i]=i==targetIndex?1:0;if(Pages[i]) Pages[i].alpha=weights[i];RenderIcon(i);}
            UpdateRails();SetStartButtonVisible(targetIndex==1);SetPageInput(loading?-1:targetIndex);
        }
        private void AdvanceTransition(float seconds)
        {
            elapsed+=seconds;float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,TransitionSeconds));float eased=t*t*(3-2*t);
            float fade=Mathf.Clamp01(elapsed/Mathf.Max(.01f,PageFadeSeconds));fade=fade*fade*(3-2*fade);
            var position=SelectionPlate.anchoredPosition;position.x=Mathf.Lerp(fromX,TargetX,eased);SelectionPlate.anchoredPosition=position;
            for(int i=0;i<3;i++) {float target=i==targetIndex?1:0;weights[i]=Mathf.Lerp(fromWeights[i],target,eased);if(Pages[i]) Pages[i].alpha=Mathf.Lerp(fromAlphas[i],target,fade);RenderIcon(i);}
            UpdateRails();SetStartButtonVisible(targetIndex==1 && fade>=1);if(t>=1) {transitioning=false;SetPageInput(loading?-1:targetIndex);}
        }
        /// <summary>Matches the reference's quick visibility without fading a green ghost over another page.</summary>
        private void SetStartButtonVisible(bool visible)
        {
            if(StartButton && StartButton.gameObject.activeSelf!=visible)StartButton.gameObject.SetActive(visible);
        }
        private void RenderIcon(int i)
        {
            var p=TabIcons[i].anchoredPosition;p.y=78+weights[i]*44;TabIcons[i].anchoredPosition=p;TabIcons[i].localScale=Vector3.one*(1+.35f*weights[i]);
            var color=TabLabels[i].color;color.a=weights[i];TabLabels[i].color=color;TabLabels[i].transform.localScale=Vector3.one*Mathf.Lerp(.7f,1,weights[i]);
        }
        private void UpdateRails()
        {
            float width=NavigationArea.rect.width,segment=width/3,left=SelectionPlate.anchoredPosition.x+width/2-segment/2;
            LeftRail.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(0,left+4));
            RightRail.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(0,width-left-segment+4));
        }

        /// <summary>Recomputes authored page/selector geometry after canvas size changes, including in capture tools.</summary>
        public void RefreshLayout()
        {
            if(!NavigationArea || !Layers || !Layers.Hosts[0]) return;
            float width=NavigationArea.rect.width;
            if(lastWidth>0 && transitioning) {var p=SelectionPlate.anchoredPosition;p.x*=width/lastWidth;SelectionPlate.anchoredPosition=p;fromX*=width/lastWidth;}
            lastWidth=width;lastHeight=((RectTransform)Layers.Hosts[0].transform).rect.height;
            float segment=width/3;SelectionPlate.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,segment);
            for(int i=0;i<2;i++) {var p=Dividers[i].anchoredPosition;p.x=(i==0?-1:1)*width/6;Dividers[i].anchoredPosition=p;}
            var cap=SelectionPlate.Find("GoldCap") as RectTransform;if(cap) cap.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,segment+13);
            for(int i=0;i<3;i++) {
                var tab=(RectTransform)TabButtons[i].transform;var p=tab.anchoredPosition;p.x=(i-1)*segment;tab.anchoredPosition=p;tab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,segment);
                if(i==0 || !PageArt[i] || !Pages[i]) continue;
                var size=((RectTransform)Pages[i].transform).rect.size;float aspect=1088f/1920f;
                float artWidth=Mathf.Max(size.x,size.y*aspect);PageArt[i].sizeDelta=new Vector2(artWidth,artWidth/aspect);
            }
            if(ready && !transitioning) {var p=SelectionPlate.anchoredPosition;p.x=TargetX;SelectionPlate.anchoredPosition=p;}
            UpdateRails();if(resourcesReady)LayoutResources();LayoutCoinShop();
        }
    }
}
