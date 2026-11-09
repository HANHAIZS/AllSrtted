using UnityEngine;
namespace AllSorted
{
    /// <summary>Fits only the victory artwork subtree; the popup animation keeps its authored outer scale.</summary>
    public sealed class SortVictoryLayout : AllSortedController
    {
        public RectTransform BoundsRoot;
        public Vector2 AuthoredExtent=new Vector2(660,1180);
        public float OuterScale=.9f;
        private Vector2 lastSize;
        private void OnEnable(){Fit();}
        private void LateUpdate(){if(BoundsRoot && BoundsRoot.rect.size!=lastSize)Fit();}
        private void Fit()
        {
            if(!BoundsRoot)return;lastSize=BoundsRoot.rect.size;if(lastSize.x<=0 || lastSize.y<=0)return;
            float factor=Mathf.Clamp(Mathf.Min((lastSize.x-40)/(AuthoredExtent.x*OuterScale),(lastSize.y-64)/(AuthoredExtent.y*OuterScale)),.1f,1);
            transform.localScale=Vector3.one*factor;
        }
    }
}
