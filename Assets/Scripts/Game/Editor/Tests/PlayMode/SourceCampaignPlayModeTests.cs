using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QFramework;
using GameFoundation.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace AllSorted.SourcePlayTests
{
 public sealed class SourceCampaignPlayModeTests
 {
  private static readonly FieldInfo ArchitectureField=typeof(Architecture<AllSortedApp>).GetField("mArchitecture",BindingFlags.Static|BindingFlags.NonPublic);
  private object oldArchitecture;private Action<AllSortedApp> oldPatch;
  [UnitySetUp] public IEnumerator Setup()
  {
   foreach(var owner in Object.FindObjectsOfType<AllSortedController>())Object.DestroyImmediate(owner);
   oldArchitecture=ArchitectureField.GetValue(null);oldPatch=AllSortedApp.OnRegisterPatch;
   AllSortedApp.OnRegisterPatch=app=>{if(oldPatch!=null)oldPatch(app);app.RegisterUtility<ISortLevelRepository>(new SortExpressCampaign());app.RegisterUtility<ISaveStore>(new MemorySaveStore());};ArchitectureField.SetValue(null,null);Time.timeScale=1;yield break;
  }
  [UnityTearDown] public IEnumerator Cleanup()
  {
   foreach(var owner in Object.FindObjectsOfType<AllSortedController>())Object.DestroyImmediate(owner);
   ArchitectureField.SetValue(null,oldArchitecture);AllSortedApp.OnRegisterPatch=oldPatch;yield break;
  }
  [UnityTest] public IEnumerator HomeStartsSourceCampaignAndAllFiveWinThroughPointerMoves()
  {
   yield return SceneManager.LoadSceneAsync("HomeScene");yield return null;yield return null;
   var home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsNotNull(home);Assert.AreEqual("Level 1",home.LevelLabel.text);
   home.StartButton.onClick.Invoke();Assert.IsNotNull(home.ActiveEntryPopup);home.ActiveEntryPopup.Primary.onClick.Invoke();yield return AllSorted.PlayTests.SceneEntryTestWait.Complete();
   var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();var campaign=(SortExpressCampaign)model.Levels;
   for(int number=1;number<=5;number++){
    if(number>1){int coins=AllSortedApp.Interface.GetModel<SortWalletModel>().Coins;view.ActivePopup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(coins+30,AllSortedApp.Interface.GetModel<SortWalletModel>().Coins);}
    Assert.AreEqual(number,model.LevelNumber.Value);Assert.IsNotNull(model.Board.Definition);AssertConnectedCabinet(view,model.Board.ShelfCount);Assert.IsFalse(GameObject.Find("Primitive Shelves"));
    if(number>1)foreach(Transform piece in view.PieceRoot){Assert.IsNull(piece.Find("OriginalArtwork"));Assert.IsNull(piece.Find("SourceArtwork"),"This art batch is scoped to source1");}
    Capture(view,number,720,1280);Capture(view,number,768,1024);
    int moveIndex=0;
    foreach(var move in campaign.GetDefinition(number).solution){
     var front=model.Board.GetFront(move.a,move.b);var visual=view.PieceRoot.Find("Piece_"+front.InstanceId+"_Kind"+front.Kind);
     string diagnostic="Pick "+number+":"+move.a+","+move.b+" move="+moveIndex+" phase="+model.Board.Phase+" blocked="+view.Layers.GameplayBlocked+" lock="+model.Board.LockRemaining(move.a)+" visual="+(visual?visual.position.ToString():"missing")+" slot="+view.StageSlotWorld(move.a,move.b);
     Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a,move.b))),diagnostic);
     Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c,move.d))),"Drop "+number);yield return new WaitForSecondsRealtime(.18f);
     moveIndex++;
    }
    Assert.AreEqual(SortPhase.Won,model.Board.Phase);Assert.AreEqual(0,model.Board.RemainingCount);Assert.IsNotNull(view.ActivePopup);
   }
   view.ActivePopup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual(9,model.Board.InitialCount);
  }
  [UnityTest] public IEnumerator FirstSourceStageUsesReworkedArtAndKeepsItThroughRefillAndRestart()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return new WaitForSecondsRealtime(.4f);
   var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
   Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual(9,model.Board.InitialCount);
   AssertConnectedCabinet(view,3);
   CheckFirstStageArtwork(view);
   Capture(view,1,720,1280);Capture(view,1,768,1024);
   var solution=((SortExpressCampaign)model.Levels).GetDefinition(1).solution;
   var move=solution[0];Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.a,move.b))));
   Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(view.StageSlotWorld(move.c,move.d))));yield return new WaitForSecondsRealtime(.4f);
   Assert.AreEqual(1,model.Board.Matches);AssertConnectedCabinet(view,3);CheckFirstStageArtwork(view);
   view.RequestPause();view.ActivePopup.Secondary.onClick.Invoke();yield return AllSorted.PlayTests.SceneEntryTestWait.Complete();
   Assert.AreEqual(9,model.Board.RemainingCount);AssertConnectedCabinet(view,3);CheckFirstStageArtwork(view);
  }
  [UnityTest] public IEnumerator MissingFirstStageArtFallsBackWithoutAffectingTheBoard()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return new WaitForSecondsRealtime(.3f);
   var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
   SetConnectedPrefabs(view,null);view.SourceLevel1Prefabs=null;view.SourceLevel1ShelfPrefab=null;view.ModularShelfPrefab=null;
   AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));yield return new WaitForSecondsRealtime(.3f);
   Assert.AreEqual(9,model.Board.InitialCount);Assert.AreEqual(9,model.Board.RemainingCount);
   foreach(Transform piece in view.PieceRoot){Assert.IsTrue(piece.GetComponent<Renderer>().enabled);Assert.IsNull(piece.Find("SourceArtwork"));Assert.IsNotNull(piece.Find("Match ID"));}
   foreach(Transform shelf in GameObject.Find("Source Shelves").transform){Assert.IsNull(shelf.Find("SourceShelfArtwork"));Assert.IsNotNull(shelf.Find("Back"));}
  }
  [UnityTest] public IEnumerator MalformedFirstStageArtRetainsVisibleIdentifiablePrimitives()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return new WaitForSecondsRealtime(.3f);
   var view=Object.FindObjectOfType<SortGameViewController>();var empty=new GameObject("Malformed art without renderer");var noMesh=new GameObject("Malformed renderer without mesh",typeof(MeshRenderer),typeof(MeshFilter));
   empty.transform.position=Vector3.one*1000;noMesh.transform.position=Vector3.one*1000;
   try{
    SetConnectedPrefabs(view,new[]{noMesh,noMesh,noMesh,noMesh,noMesh});view.SourceLevel1Prefabs=new[]{empty,noMesh,empty};view.SourceLevel1ShelfPrefab=noMesh;view.ModularShelfPrefab=noMesh;
    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));yield return new WaitForSecondsRealtime(.3f);
    foreach(Transform piece in view.PieceRoot){Assert.IsTrue(piece.GetComponent<Renderer>().enabled,"Malformed art must not make goods invisible");Assert.IsNotNull(piece.Find("Match ID"),"Fallback needs its numeric identity");Assert.IsNull(piece.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"),"Fallback must retain its solid family color");}
    foreach(Transform shelf in GameObject.Find("Source Shelves").transform)Assert.IsNotNull(shelf.Find("Back"),"Invalid shelf art must retain the primitive frame");
   }finally{Object.Destroy(empty);Object.Destroy(noMesh);}
  }
  private static void SetConnectedPrefabs(SortGameViewController view,GameObject[] prefabs)
  {
   var field=typeof(SortGameViewController).GetField("ConnectedShelfPrefabs",BindingFlags.Instance|BindingFlags.Public);
   if(field!=null)field.SetValue(view,prefabs);
  }
  private static void AssertConnectedCabinet(SortGameViewController view,int shelfCount)
  {
   var shelves=view.transform.Find("Source Shelves");Assert.IsNotNull(shelves);Assert.AreEqual(shelfCount+1,shelves.childCount);
   var art=shelves.Find("ConnectedArtwork");Assert.IsNotNull(art,"Source stages display one continuous cabinet");Assert.AreEqual(2,art.GetComponentsInChildren<MeshRenderer>(true).Length);Assert.AreEqual(0,art.GetComponentsInChildren<Collider>(true).Length);
   for(int index=0;index<shelfCount;index++){var shelf=shelves.Find("SourceShelf_"+index);Assert.IsNotNull(shelf);Assert.IsNull(shelf.Find("SourceShelfArtwork"),"The continuous cabinet replaces independent row frames");Assert.IsNotNull(shelf.Find("MatchLock"));}
  }
  private static void CheckFirstStageArtwork(SortGameViewController view)
  {
   int count=0;
   foreach(Transform piece in view.PieceRoot){
    if(piece.localScale.magnitude<.01f)continue;
    var art=piece.Find("SourceArtwork");Assert.IsNotNull(art,"Source1 goods need family artwork: "+piece.name);
    Assert.IsFalse(piece.GetComponent<Renderer>().enabled);Assert.IsNull(piece.Find("Match ID"));
    var filters=art.GetComponentsInChildren<MeshFilter>();Assert.AreEqual(1,filters.Length);Assert.Greater(filters[0].sharedMesh.vertexCount,100);
    int kind=int.Parse(piece.name.Substring(piece.name.LastIndexOf("Kind",StringComparison.Ordinal)+4));
    string expected=kind==100?"SM_S1_Apple":kind==134?"SM_S1_Hanoi":"SM_S1_Sunflower";
    StringAssert.Contains(expected,filters[0].sharedMesh.name,"Each source item must use its corresponding family mesh");
    var renderer=art.GetComponentInChildren<Renderer>();Assert.IsNotNull(renderer.sharedMaterial.GetTexture("_BaseMap"));
    Assert.Greater(art.lossyScale.y,.5f,"Primitive capsule scaling must not flatten the flower");count++;
   }
   Assert.Greater(count,0);
  }
  [UnityTest] public IEnumerator SourceRetryAndBoostersPreserveWalletAndUi()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return new WaitForSecondsRealtime(.3f);
   var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();var wallet=AllSortedApp.Interface.GetModel<SortWalletModel>();
   AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(5));yield return new WaitForSecondsRealtime(.3f);
   Assert.AreEqual(81,model.Board.InitialCount);var stock=AllSortedApp.Interface.GetModel<SortBoosterModel>();int before=stock.GetCharges(SortBooster.Freeze);
   view.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.55f);Assert.Greater(model.Board.FreezeSeconds,14.5f);Assert.AreEqual(before-1,stock.GetCharges(SortBooster.Freeze));Assert.IsTrue(view.FrozenTimerPanel.gameObject.activeSelf);
   view.RequestPause();Assert.IsNotNull(view.ActivePopup.GetComponent<SortPauseSettingsView>());int energy=wallet.Energy;view.ActivePopup.Secondary.onClick.Invoke();yield return AllSorted.PlayTests.SceneEntryTestWait.Complete();
   Assert.AreEqual(5,model.LevelNumber.Value);Assert.AreEqual(81,model.Board.RemainingCount);Assert.Greater(model.Board.RemainingSeconds,299);Assert.AreEqual(energy-1,wallet.Energy);Assert.AreEqual(before-1,stock.GetCharges(SortBooster.Freeze));Assert.AreEqual(0,model.Board.FreezeSeconds);
  }
  private static void Capture(SortGameViewController view,int number,int width,int height)
  {
   var camera=view.BoardCamera;float size=camera.orthographicSize;var scale=view.Background.transform.localScale;
   try{float aspect=(float)width/height;camera.orthographicSize=(float)typeof(SortGameViewController).GetMethod("SourceCameraSize",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{aspect});view.SendMessage("FitBackground",aspect);
    typeof(AllSorted.PlayTests.LevelEntryTransitionPlayModeTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{camera,null,"source-stage-"+number+"-"+width+"x"+height+".png",width,height});}
   finally{camera.orthographicSize=size;view.Background.transform.localScale=scale;}
  }
 }
}
