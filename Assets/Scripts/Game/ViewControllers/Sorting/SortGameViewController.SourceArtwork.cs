using UnityEngine;
namespace AllSorted
{
 public sealed partial class SortGameViewController
 {
  public GameObject[] SourceLevel1Prefabs;
  public Material[] SourceLevel1FrontMaterials,SourceLevel1RearMaterials;
  public GameObject SourceLevel1ShelfPrefab;

  private int SourceArtworkIndex(int kind)
  {
   if(!IsSourceStage || model.Board.Definition.number!=1 || kind==SortBoardState.GoldenKind)return -1;
   var item=SourceItem(kind);if(item==null)return -1;
   return item.family=="apple"?0:item.family=="hanoi"?1:item.family=="sunflower"?2:-1;
  }
  private bool CanUseSourceArtwork(int kind)
  {
   int i=SourceArtworkIndex(kind);
   return i>=0 && SourceLevel1Prefabs!=null && i<SourceLevel1Prefabs.Length && RenderableSourcePrefab(SourceLevel1Prefabs[i])
    && SourceLevel1FrontMaterials!=null && i<SourceLevel1FrontMaterials.Length && SourceLevel1FrontMaterials[i]
    && SourceLevel1RearMaterials!=null && i<SourceLevel1RearMaterials.Length && SourceLevel1RearMaterials[i];
  }
  private static bool RenderableSourcePrefab(GameObject prefab)
  {
   if(!prefab || !prefab.activeSelf)return false;
   var filter=prefab.GetComponentInChildren<MeshFilter>();
   var renderer=filter?filter.GetComponent<MeshRenderer>():null;
   return renderer && renderer.enabled && filter.sharedMesh && filter.sharedMesh.vertexCount>0;
  }
  private Renderer DecorateSourceArtwork(GameObject root,int kind)
  {
   var primitive=root.GetComponent<Renderer>();if(!CanUseSourceArtwork(kind))return primitive;
   var visual=Instantiate(SourceLevel1Prefabs[SourceArtworkIndex(kind)],root.transform,false);visual.name="SourceArtwork";
   var renderer=visual.GetComponentInChildren<Renderer>();if(!renderer){Destroy(visual);return primitive;}
   var scale=StagePieceScale(kind);visual.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
   visual.transform.localPosition=new Vector3(0,-.14f/scale.y,0);
   primitive.enabled=false;return renderer;
  }
  private Material SourceArtworkMaterial(int kind,bool behind)
  {
   if(!CanUseSourceArtwork(kind))return null;
   int i=SourceArtworkIndex(kind);return behind?SourceLevel1RearMaterials[i]:SourceLevel1FrontMaterials[i];
  }
  private bool AddSourceShelfArtwork(Transform root)
  {
   if(AddModularShelf(root))return true;
   if(!RenderableSourcePrefab(SourceLevel1ShelfPrefab))return false;
   var visual=Instantiate(SourceLevel1ShelfPrefab,root,false);visual.name="SourceShelfArtwork";
   if(!visual.GetComponentInChildren<Renderer>()){Destroy(visual);return false;}
   visual.transform.localPosition=new Vector3(0,.12f,.05f);return true;
  }
 }
}
