using System.Collections.Generic;
using UnityEngine;
namespace AllSorted
{
 public sealed partial class SortGameViewController
 {
  private Material sourceFrameMaterial,sourceBackMaterial,sourceLockMaterial;
  private Transform sourceShelves,legacyShelves;private Vector2 sourceCenter;
  private readonly List<GameObject> sourceLockBars=new List<GameObject>();
  private readonly List<TextMesh> sourceLockLabels=new List<TextMesh>();
  private readonly Dictionary<int,Material> sourceFronts=new Dictionary<int,Material>(),sourceRears=new Dictionary<int,Material>();
  private readonly Dictionary<int,SortItemDefinition> sourceItems=new Dictionary<int,SortItemDefinition>();
  private readonly List<Material> sourceOwnedMaterials=new List<Material>();
  private bool IsSourceStage {get{return model!=null && model.Board!=null && model.Board.Definition!=null;}}
  public Vector3 StageSlotWorld(int shelf,int slot)
  {
   if(!IsSourceStage)return SlotWorld(shelf,slot);
   return SortShelfGeometry.SlotWorld(SourceShelfCenter(shelf),slot);
  }
  private float SourceCameraSize(float aspect)
  {
   if(!IsSourceStage)return Mathf.Max(6.4f,4.3f/aspect);
   float minX=float.MaxValue,maxX=float.MinValue,minY=minX,maxY=maxX;
   for(int i=0;i<model.Board.Definition.shelves.Length;i++){var s=SourceShelfCenter(i);minX=Mathf.Min(minX,s.x);maxX=Mathf.Max(maxX,s.x);minY=Mathf.Min(minY,s.y);maxY=Mathf.Max(maxY,s.y);}
   float height=sourceConnectedArtwork?SortShelfGeometry.ConnectedRowPitch:SortShelfGeometry.Height;
   return Mathf.Max(sourceConnectedArtwork?4.8f:6.4f,((maxX-minX)+SortShelfGeometry.Width+.55f)/2/aspect,(((maxY-minY)+height)*Mathf.Abs(BoardCamera.transform.up.y)+(sourceConnectedArtwork?1.8f:1.25f)*Mathf.Abs(BoardCamera.transform.up.z)+.26f)/1.2f);
  }
  private Material SourceColor(string name,Color color)
  {
   var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.4f);sourceOwnedMaterials.Add(m);return m;
  }
  private void ConfigureSourceStage()
  {
   sourceModularArtwork=IsSourceStage && ValidModularShelf(ModularShelfPrefab);
   if(sourceShelves)Destroy(sourceShelves.gameObject);sourceLockBars.Clear();sourceLockLabels.Clear();
   if(!legacyShelves){var legacy=GameObject.Find("Primitive Shelves");if(legacy)legacyShelves=legacy.transform;}
   if(legacyShelves)legacyShelves.gameObject.SetActive(!IsSourceStage);
   sourceConnectedArtwork=false;if(cabinetBackdrop)cabinetBackdrop.SetActive(false);if(!IsSourceStage)return;
   var shelves=model.Board.Definition.shelves;float minX=float.MaxValue,maxX=float.MinValue,minY=minX,maxY=maxX;
   foreach(var s in shelves){minX=Mathf.Min(minX,s.x);maxX=Mathf.Max(maxX,s.x);minY=Mathf.Min(minY,s.y);maxY=Mathf.Max(maxY,s.y);}sourceCenter=new Vector2((minX+maxX)/2,(minY+maxY)/2);
   sourceConnectedArtwork=ValidConnectedCabinet(ConnectedCabinetPrefab());
   sourceShelves=new GameObject("Source Shelves").transform;sourceShelves.SetParent(transform,false);
   if(sourceConnectedArtwork)AddConnectedCabinet();
   if(!sourceFrameMaterial)sourceFrameMaterial=SourceColor("Source shelf ivory",new Color(.85f,.92f,.97f));
   if(!sourceBackMaterial)sourceBackMaterial=SourceColor("Source shelf interior",new Color(.045f,.12f,.18f));
   if(!sourceLockMaterial)sourceLockMaterial=SourceColor("Source shelf locked",new Color(.18f,.32f,.49f));
   var frame=sourceFrameMaterial;var back=sourceBackMaterial;var locked=sourceLockMaterial;
   for(int i=0;i<shelves.Length;i++){
    var root=new GameObject("SourceShelf_"+i).transform;root.SetParent(sourceShelves,false);root.position=SourceShelfCenter(i);
    bool artwork=sourceConnectedArtwork || AddSourceShelfArtwork(root);
    if(!artwork){
    SourceBox(root,"Back",new Vector3(0,.12f,1.1f),new Vector3(3.45f,1.65f,.12f),back);
    SourceBox(root,"Base",new Vector3(0,-.65f,.65f),new Vector3(3.55f,.14f,.95f),frame);
    SourceBox(root,"Top",new Vector3(0,.9f,.8f),new Vector3(3.55f,.12f,.6f),frame);
    SourceBox(root,"Left",new Vector3(-1.72f,.12f,.85f),new Vector3(.1f,1.55f,.5f),frame);
    SourceBox(root,"Right",new Vector3(1.72f,.12f,.85f),new Vector3(.1f,1.55f,.5f),frame);
    }
    var bar=CreateSourceLock(root,locked);sourceLockBars.Add(bar);

   }
  }
  private static GameObject SourceBox(Transform root,string name,Vector3 position,Vector3 scale,Material material)
  {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go;
  }
  private void RefreshSourceLocks()
  {
   if(!IsSourceStage)return;for(int i=0;i<sourceLockBars.Count;i++){int count=model.Board.LockRemaining(i);sourceLockBars[i].SetActive(count>0);sourceLockLabels[i].text=SourceLockDoorMaterial?count.ToString():"LOCK  "+count;}
  }
  private SortItemDefinition SourceItem(int kind)
  {
   SortItemDefinition item;if(!sourceItems.TryGetValue(kind,out item)){var repository=model.Levels as SortExpressCampaign;item=repository==null?null:repository.GetItem(kind);sourceItems.Add(kind,item);}return item;
  }
  private PrimitiveType SourcePrimitive(int kind)
  {
   var item=SourceItem(kind);return item.shape==0?PrimitiveType.Sphere:item.shape==1?PrimitiveType.Cube:item.shape==2?PrimitiveType.Capsule:PrimitiveType.Cylinder;
  }
  private Vector3 StagePieceScale(int kind)
  {
   if(!IsSourceStage || kind==SortBoardState.GoldenKind)return PieceScale(kind);var item=SourceItem(kind);int family=0;foreach(char c in item.family)family=family*31+c;int profile=(family&int.MaxValue)%5;
   if(item.shape==0)return new Vector3(.67f,.62f+profile*.04f,.58f);
   if(item.shape==1)return new Vector3(.55f+profile*.04f,.65f,.55f);
   if(item.shape==2)return new Vector3(.45f,.33f+profile*.025f,.45f);
   return new Vector3(.62f,.32f+profile*.025f,.62f);
  }
  private Material SourceMaterial(int kind,bool behind)
  {
   var artwork=SourceArtworkMaterial(kind,behind);if(artwork)return artwork;
   var cache=behind?sourceRears:sourceFronts;Material material;if(cache.TryGetValue(kind,out material))return material;
   var item=SourceItem(kind);Color color;if(!ColorUtility.TryParseHtmlString("#"+item.colorHex,out color))color=Color.magenta;
   float h,s,v;Color.RGBToHSV(color,out h,out s,out v);color=Color.HSVToRGB(Mathf.Repeat(h+(item.variant%7-3)*.012f,1),Mathf.Clamp01(s),Mathf.Clamp01(v-(item.variant%3)*.035f));if(behind)color*=.68f;color.a=1;
   material=SourceColor(item.iid+(behind?" rear":""),color);cache.Add(kind,material);return material;
  }
  private void AddSourceIdentifier(GameObject piece,int kind)
  {
   var label=new GameObject("Match ID").AddComponent<TextMesh>();label.transform.SetParent(piece.transform,false);label.transform.localPosition=new Vector3(0,.15f,-.72f);label.fontSize=64;label.characterSize=.085f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=Color.white;label.text=(kind-99).ToString("00");
  }
  private void DisposeSourcePresentation(){if(sourceShelves)Destroy(sourceShelves.gameObject);foreach(var m in sourceOwnedMaterials)if(m)Destroy(m);sourceOwnedMaterials.Clear();sourceFronts.Clear();sourceRears.Clear();sourceItems.Clear();}
 }
}
