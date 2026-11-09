using UnityEngine;
namespace AllSorted
{
    /// <summary>Fits the one source-pixel subtree without changing relative Sprite/button geometry.</summary>
    public sealed class SortHomeArtworkScale : AllSortedController
    {
        private RectTransform parent;
        private float width;
        private void Awake() {parent=transform.parent as RectTransform;Fit();}
        private void LateUpdate() {if(parent && !Mathf.Approximately(width,parent.rect.width)) Fit();}
        private void Fit() {if(!parent) return;width=parent.rect.width;transform.localScale=Vector3.one*(width/1088f);}
    }
}
