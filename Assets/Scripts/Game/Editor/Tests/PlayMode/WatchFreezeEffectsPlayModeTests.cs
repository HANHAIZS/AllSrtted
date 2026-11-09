using System.Collections;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
 public sealed class WatchFreezeEffectsPlayModeTests : WalletTestBase
 {
  [UnityTest] public IEnumerator ThawFadesAndEffectsNeverBlockInput()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return null;var game=Object.FindObjectOfType<SortGameViewController>();Assert.IsNotNull(game.WatchFreezeFx);
   foreach(var g in game.WatchFreezeFx.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))Assert.IsFalse(g.raycastTarget);
   game.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.6f);Assert.IsTrue(game.WatchFreezeFx.IsSnowing);Assert.Greater(game.WatchFreezeFx.Frame.color.a,0);
   AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=15});yield return null;Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);Assert.IsFalse(game.WatchFreezeFx.IsSnowing);Assert.Greater(game.WatchFreezeFx.Frame.color.a,0);
   yield return new WaitForSecondsRealtime(2.7f);Assert.AreEqual(0,game.WatchFreezeFx.Frame.color.a);foreach(var snow in game.WatchFreezeFx.Snow)Assert.IsFalse(snow.gameObject.activeSelf);
  }
  [UnityTest] public IEnumerator WatchHitsBeforeFreezeAndCleansOnRestart()
  {
   yield return SceneManager.LoadSceneAsync("GameScene");yield return null;yield return null;
   var game=Object.FindObjectOfType<SortGameViewController>();var board=AllSortedApp.Interface.GetModel<SortBoardModel>();var stock=AllSortedApp.Interface.GetModel<SortBoosterModel>();
   var t=typeof(SortGameViewController).GetField("WatchFreezeFx");Assert.IsNotNull(t,"Authored watch presentation missing");
   int count=stock.GetCharges(SortBooster.Freeze);game.BoosterButtons[3].onClick.Invoke();game.BoosterButtons[3].onClick.Invoke();
   Assert.AreEqual(0,board.Board.FreezeSeconds);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);yield return new WaitForSecondsRealtime(.5f);
   Assert.Greater(board.Board.FreezeSeconds,14.7f);Assert.AreEqual(count-1,stock.GetCharges(SortBooster.Freeze));Assert.IsTrue(game.FrozenTimerPanel.gameObject.activeSelf);
   game.RequestPause();float held=board.Board.FreezeSeconds;yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(held,board.Board.FreezeSeconds);
   game.RestartLevel();yield return SceneEntryTestWait.Complete();Assert.AreEqual(0,board.Board.FreezeSeconds);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);
  }
 }
}
