using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace AllSorted.PlayTests
{
    public sealed class LeavingPlayModeTests : WalletTestBase
    {
        private SortGameViewController game;
        private SortWalletModel wallet;
        [UnitySetUp] public IEnumerator Load(){yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;game=Object.FindObjectOfType<SortGameViewController>();wallet=AllSortedApp.Interface.GetModel<SortWalletModel>();}
        private static void Click(Button button,Camera camera)
        {
            var d=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,button.transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);Assert.Greater(hits.Count,0);Assert.AreSame(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
            ExecuteEvents.Execute(button.gameObject,d,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator QuitCloseAndLeaveKeepPauseAndDebitOnlyOnConfirmation()
        {
            game.RequestPause();var pause=game.ActivePopup;float time=AllSortedApp.Interface.GetModel<SortBoardModel>().Board.RemainingSeconds;
            pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;
            Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name,"Quit must wait for confirmation.");var confirm=game.ActivePopup;Assert.AreNotSame(pause,confirm);Assert.AreEqual("Leaving",confirm.Title.text);Assert.AreEqual(5,wallet.Energy);
            pause.Primary.onClick.Invoke();Assert.AreSame(confirm,game.ActivePopup);AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=10});Assert.AreEqual(time,AllSortedApp.Interface.GetModel<SortBoardModel>().Board.RemainingSeconds);
            Click(confirm.Secondary,game.BoardCamera);yield return null;Assert.AreSame(pause,game.ActivePopup);Assert.AreEqual(SortPhase.Paused,AllSortedApp.Interface.GetModel<SortBoardModel>().Phase.Value);Assert.AreEqual(5,wallet.Energy);
            confirm.Primary.onClick.Invoke();Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name);
            pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;Assert.AreSame(confirm,game.ActivePopup);Click(confirm.Primary,game.BoardCamera);confirm.Primary.onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(4,wallet.Energy);
        }
        [UnityTest] public IEnumerator ZeroEnergyCanCloseConfirmationAndLeaveWithoutNegativeBalance()
        {
            for(int i=0;i<5;i++){AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(wallet.AttemptId));AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));}
            game.RequestPause();var pause=game.ActivePopup;pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;var confirm=game.ActivePopup;Assert.AreNotSame(pause,confirm);Assert.IsTrue(confirm.Secondary.IsInteractable());Click(confirm.Secondary,game.BoardCamera);Assert.AreSame(pause,game.ActivePopup);
            pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;Click(confirm.Primary,game.BoardCamera);yield return null;yield return null;Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(0,wallet.Energy);
        }
        [UnityTest] public IEnumerator MissingConfirmationRetainsPauseAndRecoversWithoutCharge()
        {
            var field=typeof(SortGameViewController).GetField("LeavingPopupKey");Assert.IsNotNull(field);var key=(string)field.GetValue(game);game.RequestPause();var pause=game.ActivePopup;field.SetValue(game,"Prefabs/UI/Popups/MissingLeaving");
            LogAssert.Expect(LogType.Error,new Regex("Leaving popup resource missing"));pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;Assert.AreSame(pause,game.ActivePopup);Assert.AreEqual(5,wallet.Energy);Assert.IsTrue(game.Layers.GameplayBlocked);
            field.SetValue(game,key);pause.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();yield return null;Assert.AreNotSame(pause,game.ActivePopup);Assert.AreEqual(5,wallet.Energy);
        }
    }
}
