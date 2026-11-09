using System.Collections;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
namespace AllSorted.PlayTests
{
 public sealed class OriginalMerchandisePlayModeTests : WalletTestBase
 {
  [UnityTest] public IEnumerator AllFourFamiliesDecorateAllThreeLevels()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return null;
   var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
   Assert.AreEqual(4,view.OriginalMerchandisePrefabs.Length);Assert.AreEqual(4,view.OriginalFrontMaterials.Length);Assert.AreEqual(4,view.OriginalRearMaterials.Length);
   for(int level=1;level<=3;level++){
    AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(level));yield return null;yield return new WaitForSecondsRealtime(.3f);
    Assert.AreEqual(level==3?30:24,model.Board.InitialCount);Assert.AreEqual(level==3?10:8,model.TotalMatches);bool[] kinds=new bool[4];
    foreach(Transform piece in view.PieceRoot){int at=piece.name.LastIndexOf("_Kind",System.StringComparison.Ordinal);if(at<0)continue;int kind=int.Parse(piece.name.Substring(at+5));if(kind==SortBoardState.GoldenKind)continue;
     Assert.That(kind,Is.InRange(0,3));kinds[kind]=true;var art=piece.Find("OriginalArtwork");Assert.IsNotNull(art,piece.name+" level"+level);Assert.IsFalse(piece.GetComponent<Renderer>().enabled);var renderer=art.GetComponentInChildren<Renderer>();Assert.IsNotNull(renderer);Assert.AreEqual(1,renderer.sharedMaterials.Length);Assert.AreEqual("Universal Render Pipeline/Lit",renderer.sharedMaterial.shader.name);
     if(kind==3){var scale=art.lossyScale;Assert.That(scale.x/scale.y,Is.EqualTo(1).Within(.001));Assert.That(scale.z/scale.y,Is.EqualTo(1).Within(.001),"Cylinder kind3 must not flatten Pear");}
    }
    for(int k=0;k<4;k++)Assert.IsTrue(kinds[k],"Missing kind"+k+" in level"+level);
    Capture(view,level,720,1280);Capture(view,level,768,1024);
   }
   view.BoosterButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(1.25f);int gold=0;
   foreach(Transform piece in view.PieceRoot)if(piece.name.EndsWith("_Kind"+SortBoardState.GoldenKind)){gold++;Assert.IsNull(piece.Find("OriginalArtwork"));Assert.IsNotNull(piece.Find("Visual"));Assert.IsNotNull(piece.GetComponent<BoxCollider>());}
   Assert.Greater(gold,0,"Card booster must use the authored CardItem prefab");
  }
  private static void Capture(SortGameViewController view,int level,int w,int h)
  {
   var camera=view.BoardCamera;float size=camera.orthographicSize;var scale=view.Background.transform.localScale;
   try{float aspect=(float)w/h;camera.orthographicSize=Mathf.Max(6.4f,4.3f/aspect);view.SendMessage("FitBackground",aspect);typeof(LevelEntryTransitionPlayModeTests).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{camera,null,"four-families-level"+level+"-"+w+"x"+h+".png",w,h});}
   finally{camera.orthographicSize=size;view.Background.transform.localScale=scale;}
  }
  [UnityTest] public IEnumerator OriginalVisualsKeepExistingBoardAndDragRules()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return null;var game=Object.FindObjectOfType<SortGameViewController>();var board=AllSortedApp.Interface.GetModel<SortBoardModel>().Board;
   Assert.AreEqual(6,board.ShelfCount);Assert.AreEqual(24,board.RemainingCount);Assert.AreEqual(24,game.RenderedPieceCount);
   bool[] seen=new bool[4];foreach(Transform go in game.PieceRoot){int k=int.Parse(go.name.Substring(go.name.LastIndexOf("_Kind",System.StringComparison.Ordinal)+5));if(k<4){seen[k]=true;Assert.IsNotNull(go.Find("OriginalArtwork"),"Every family must have artwork");}}foreach(bool found in seen)Assert.IsTrue(found);
   var source=game.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0));yield return new WaitForSecondsRealtime(.25f);Assert.IsTrue(game.TryBeginDrag(source));Assert.IsTrue(game.ReleaseDrag(game.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));Assert.AreEqual(21,board.RemainingCount);
   game.RequestPause();game.RestartLevel();yield return SceneEntryTestWait.Complete();Assert.AreEqual(24,AllSortedApp.Interface.GetModel<SortBoardModel>().Board.RemainingCount);
  }
 }
}
