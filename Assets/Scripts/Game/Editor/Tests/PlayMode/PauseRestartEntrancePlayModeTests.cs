using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace AllSorted.PlayTests
{
    public sealed class PauseRestartEntrancePlayModeTests : WalletTestBase
    {
        private SortGameViewController game;
        private SortWalletModel Wallet {get{return AllSortedApp.Interface.GetModel<SortWalletModel>();}}
        private SortBoardModel Board {get{return AllSortedApp.Interface.GetModel<SortBoardModel>();}}
        [UnitySetUp] public IEnumerator Load(){if(SortLevelTransitionView.Active)SortLevelTransitionView.Active.Finish();yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;game=Object.FindObjectOfType<SortGameViewController>();}
        private void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();var d=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(game.BoardCamera,button.transform.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);Assert.Greater(hits.Count,0);Assert.AreSame(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));ExecuteEvents.Execute(button.gameObject,d,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator PauseRestartPopsWithoutLoadingAndHoldsTimeUntilSettle()
        {
            game.BoosterButtons[1].onClick.Invoke();Assert.AreEqual(2,AllSortedApp.Interface.GetModel<SortBoosterModel>().GetCharges(SortBooster.Shuffle));game.RequestPause();yield return new WaitForSecondsRealtime(.3f);var pause=game.ActivePopup;var old=Board.Board;int scene=SceneManager.GetActiveScene().handle;
            Click(pause.Secondary);Assert.IsTrue(game.IsEntering);Assert.IsTrue(Board.IsEntering);Assert.IsTrue(game.Layers.GameplayBlocked);Assert.IsNull(game.ActivePopup);Assert.AreNotSame(old,Board.Board);Assert.AreEqual(4,Wallet.Energy);Assert.AreEqual(180,Board.Board.RemainingSeconds);Assert.IsNull(SortLevelTransitionView.Active);Assert.AreEqual(scene,SceneManager.GetActiveScene().handle);
            Assert.Greater((float)typeof(SortGameViewController).GetField("localEntryStarted",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game),Time.realtimeSinceStartup,"Merchandise must wait until the pause panel has closed.");
            var restarted=Board.Board;game.RestartLevel();pause.Secondary.onClick.Invoke();Assert.AreSame(restarted,Board.Board);Assert.AreEqual(4,Wallet.Energy);AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=999});Assert.AreEqual(180,Board.Board.RemainingSeconds);
            var targets=(Dictionary<Transform,Vector3>)typeof(SortGameViewController).GetField("entryPieceScales",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);var positions=new Dictionary<Transform,Vector3>();foreach(var item in targets){Assert.AreEqual(Vector3.zero,item.Key.localScale);positions.Add(item.Key,item.Key.position);}
            pause.SendMessage("AdvanceMotion",.2f);typeof(SortGameViewController).GetField("localEntryStarted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,Time.realtimeSinceStartup-.18f);game.SendMessage("AdvanceSceneEntrance");
            foreach(var item in targets){Assert.Less(Vector3.Distance(item.Key.localScale,item.Value*1.15f),.001f);Assert.AreEqual(positions[item.Key],item.Key.position);}
            var expected=new Dictionary<Transform,Vector3>(targets);Capture("restart-peak-phone.png",720,1280);Capture("restart-peak-ipad.png",768,1024);
            typeof(SortGameViewController).GetField("localEntryStarted",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,Time.realtimeSinceStartup-.281f);game.SendMessage("AdvanceSceneEntrance");Assert.IsFalse(game.IsEntering);Assert.IsFalse(Board.IsEntering);Assert.IsFalse(game.Layers.GameplayBlocked);foreach(var item in expected)Assert.AreEqual(item.Value,item.Key.localScale);Assert.AreEqual(180,Board.Board.RemainingSeconds);Assert.AreEqual(2,AllSortedApp.Interface.GetModel<SortBoosterModel>().GetCharges(SortBooster.Shuffle));
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=1});Assert.AreEqual(179,Board.Board.RemainingSeconds);yield return null;
        }
        [UnityTest] public IEnumerator LastLifeFundsOneAnimatedRestartThenZeroCannotRestartAgain()
        {
            for(int i=0;i<4;i++){AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));}Assert.AreEqual(1,Wallet.Energy);game.SendMessage("ReconcilePieces");game.RequestPause();yield return new WaitForSecondsRealtime(.3f);Click(game.ActivePopup.Secondary);Assert.AreEqual(0,Wallet.Energy);Assert.IsTrue(game.IsEntering);yield return SceneEntryTestWait.Complete();game.RequestPause();yield return null;var board=Board.Board;var popup=game.ActivePopup;game.RestartLevel();Assert.AreSame(board,Board.Board);Assert.AreSame(popup,game.ActivePopup);Assert.IsFalse(game.IsEntering);Assert.AreEqual(0,Wallet.Energy);
        }
        private sealed class Disk : ISaveStore
        {
            private readonly MemorySaveStore memory=new MemorySaveStore();public bool Fail;
            public bool TryRead(string k,out string v){return memory.TryRead(k,out v);}public bool TryReadBackup(string k,out string v){return memory.TryReadBackup(k,out v);}public void Write(string k,string v){if(Fail)throw new System.IO.IOException("restart test failure");memory.Write(k,v);}public void Restore(string k,string v){memory.Restore(k,v);}public void Delete(string k){memory.Delete(k);}
        }
        [UnityTest] public IEnumerator FailedDebitKeepsPausedBoardAndRetryStartsPopOnce()
        {
            var disk=new Disk();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(disk,WalletTestScope.Clock),null);AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));game.SendMessage("ReconcilePieces");game.RequestPause();yield return new WaitForSecondsRealtime(.3f);var popup=game.ActivePopup;var old=Board.Board;disk.Fail=true;Click(popup.Secondary);Assert.AreSame(old,Board.Board);Assert.AreSame(popup,game.ActivePopup);Assert.AreEqual(SortPhase.Paused,Board.Board.Phase);Assert.IsFalse(game.IsEntering);Assert.AreEqual(5,Wallet.Energy);
            disk.Fail=false;AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());game.RestartLevel();Assert.IsTrue(game.IsEntering);Assert.AreNotSame(old,Board.Board);Assert.AreEqual(4,Wallet.Energy);yield return SceneEntryTestWait.Complete();Assert.IsFalse(game.IsEntering);Assert.AreEqual(4,Wallet.Energy);
        }
        private void Capture(string name,int width,int height){typeof(LevelEntryTransitionPlayModeTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game.BoardCamera,null,name,width,height});}
    }
}
