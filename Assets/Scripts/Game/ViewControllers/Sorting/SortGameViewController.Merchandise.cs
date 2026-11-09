using UnityEngine;
namespace AllSorted
{
 public sealed partial class SortGameViewController
 {
  public GameObject[] OriginalMerchandisePrefabs;
  public Material[] OriginalFrontMaterials,OriginalRearMaterials;
  private Renderer DecorateMerchandise(GameObject root,int kind)
  {
   var primitive=root.GetComponent<Renderer>();if(IsSourceStage)return DecorateSourceArtwork(root,kind);
   if(kind==SortBoardState.GoldenKind || kind<0 || OriginalMerchandisePrefabs==null || kind>=OriginalMerchandisePrefabs.Length || !OriginalMerchandisePrefabs[kind])return primitive;
   var visual=Instantiate(OriginalMerchandisePrefabs[kind],root.transform,false);visual.name="OriginalArtwork";var scale=PieceScale(kind);visual.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);var renderer=visual.GetComponentInChildren<Renderer>();if(!renderer){Destroy(visual);return primitive;}primitive.enabled=false;return renderer;
  }
  private Material OriginalMerchandiseMaterial(int kind,bool behind)
  {
   if(kind==SortBoardState.GoldenKind || kind<0 || OriginalMerchandisePrefabs==null || kind>=OriginalMerchandisePrefabs.Length || !OriginalMerchandisePrefabs[kind])return null;
   var materials=behind?OriginalRearMaterials:OriginalFrontMaterials;return materials!=null && kind<materials.Length?materials[kind]:null;
  }
 }
}
