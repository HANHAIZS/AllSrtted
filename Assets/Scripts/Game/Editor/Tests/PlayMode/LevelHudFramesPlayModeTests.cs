using System.Collections;
using System.Reflection;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace AllSorted.PlayTests
{
    public sealed class LevelHudFramesPlayModeTests : WalletTestBase
    {
        private SortGameViewController game;
        private SortBoardModel Board {get{return AllSortedApp.Interface.GetModel<SortBoardModel>();}}
        [UnitySetUp] public IEnumerator Load(){if(SortLevelTransitionView.Active)SortLevelTransitionView.Active.Finish();yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;game=Object.FindObjectOfType<SortGameViewController>();}
        private void Capture(string name,int width,int height)
        {
            var camera=game.BoardCamera;float oldSize=camera.orthographicSize;var oldScale=game.Background.transform.localScale;
            try{float aspect=(float)width/height;camera.orthographicSize=Mathf.Max(6.4f,4.3f/aspect);game.SendMessage("FitBackground",aspect);typeof(LevelEntryTransitionPlayModeTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{camera,null,name,width,height});}
            finally{camera.orthographicSize=oldSize;game.Background.transform.localScale=oldScale;}
        }
        private void Refresh(){game.SendMessage("RefreshBoosterUi",true);}
        [UnityTest] public IEnumerator LiveWhiteTimerSwitchesToIceForWatchAndBackAtExpiry()
        {
            Assert.IsNotNull(game.FrozenTimerPanel);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);Assert.AreEqual("Normal",game.TimerLabel.transform.parent.GetComponent<Image>().sprite.name);Assert.IsNull(game.TimerLabel.GetComponent<SortHudGoldGradient>());
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=12});yield return null;Assert.AreEqual("02:48",game.TimerLabel.text);Capture("hud-normal-phone.png",720,1280);Capture("hud-normal-ipad.png",768,1024);
            game.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Assert.Greater(Board.Board.FreezeSeconds,0);Assert.IsTrue(game.FrozenTimerPanel.gameObject.activeSelf);Assert.AreEqual("Frozen",game.FrozenTimerPanel.sprite.name);Assert.AreEqual(Color.white,game.TimerLabel.color);
            float clock=Board.Board.RemainingSeconds;Capture("hud-frozen-phone.png",720,1280);Capture("hud-frozen-ipad.png",768,1024);AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=Board.Board.FreezeSeconds*.5f});Refresh();Assert.AreEqual(clock,Board.Board.RemainingSeconds);Assert.IsTrue(game.FrozenTimerPanel.gameObject.activeSelf);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=Board.Board.FreezeSeconds+.1f});Refresh();Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);Assert.AreEqual(Color.white,game.TimerLabel.color);yield return null;Assert.IsNull(game.TimerLabel.GetComponent<SortHudGoldGradient>());
        }
        [UnityTest] public IEnumerator LevelsStayDynamicAndManualPauseDoesNotCreateIce()
        {
            for(int level=1;level<=3;level++){AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(level));yield return null;Assert.AreEqual("LEVEL "+level.ToString("00"),game.LevelLabel.text);Assert.AreEqual("Level",game.LevelLabel.transform.parent.GetComponent<Image>().sprite.name);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);}
            game.RequestPause();Assert.AreEqual(SortPhase.Paused,Board.Board.Phase);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);game.ResumeLevel();yield return null;Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator PauseKeepsActiveIceAndRestartResetsFrameAndWhiteTimer()
        {
            game.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Assert.IsTrue(game.FrozenTimerPanel.gameObject.activeSelf);game.RequestPause();float frozen=Board.Board.FreezeSeconds;AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=20});Refresh();Assert.AreEqual(frozen,Board.Board.FreezeSeconds);Assert.IsTrue(game.FrozenTimerPanel.gameObject.activeSelf);
            game.ActivePopup.Secondary.onClick.Invoke();Assert.IsTrue(game.IsEntering);Assert.AreEqual(0,Board.Board.FreezeSeconds);Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);Assert.AreEqual("03:00",game.TimerLabel.text);yield return SceneEntryTestWait.Complete();Assert.IsFalse(game.FrozenTimerPanel.gameObject.activeSelf);Assert.AreEqual(Color.white,game.TimerLabel.color);
        }
    }
}
