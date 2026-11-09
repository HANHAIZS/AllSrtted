using UnityEngine;
using UnityEngine.Rendering;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public Material SourceLockDoorMaterial;
        public Font SourceLockCountFont;
        private GameObject CreateSourceLock(Transform parent,Material fallback)
        {
            GameObject bar;
            if(SourceLockDoorMaterial && ContactShadowMesh){
                bar=new GameObject("MatchLock",typeof(MeshFilter),typeof(MeshRenderer));bar.transform.SetParent(parent,false);
                bar.transform.localPosition=new Vector3(0,.1765f,-.85f);bar.transform.localScale=new Vector3(3.28f,1.327f,1);
                bar.GetComponent<MeshFilter>().sharedMesh=ContactShadowMesh;
                var renderer=bar.GetComponent<MeshRenderer>();renderer.sharedMaterial=SourceLockDoorMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }else bar=SourceBox(parent,"MatchLock",new Vector3(0,.05f,-.85f),new Vector3(3.25f,1.15f,.15f),fallback);
            var label=new GameObject("Matches remaining").AddComponent<TextMesh>();label.transform.SetParent(bar.transform,false);
            label.transform.localPosition=SourceLockDoorMaterial?new Vector3(0,.025f,-.02f):new Vector3(0,0,-1);
            var size=bar.transform.localScale;label.transform.localScale=new Vector3(1/size.x,1/size.y,1/size.z);
            label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=64;label.characterSize=SourceLockDoorMaterial?.048f:.075f;label.color=Color.white;
            if(SourceLockCountFont){label.font=SourceLockCountFont;label.GetComponent<MeshRenderer>().sharedMaterial=SourceLockCountFont.material;}
            sourceLockLabels.Add(label);return bar;
        }
    }
}
