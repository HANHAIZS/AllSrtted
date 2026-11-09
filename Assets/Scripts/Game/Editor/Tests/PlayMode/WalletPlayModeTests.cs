using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace AllSorted.PlayTests
{
    public sealed class WalletPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private SortWalletModel Wallet {get{return AllSortedApp.Interface.GetModel<SortWalletModel>();}}
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;
            home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsNotNull(home);
        }
        private T Field<T>(string name) where T:class
        {
            var field=typeof(SortHomeViewController).GetField(name);Assert.IsNotNull(field,"Missing resource HUD field: "+name);
            var result=field.GetValue(home) as T;Assert.IsNotNull(result,"Unbound resource HUD: "+name);return result;
        }
        private static void PointerClick(Button button,Camera camera)
        {
            var data=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,button.transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Assert.Greater(hits.Count,0);
            Assert.AreSame(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private void Capture(string name,int width,int height)
        {
            var fixture=new HomeNavigationPlayModeTests();typeof(HomeNavigationPlayModeTests).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture,home);
            typeof(HomeNavigationPlayModeTests).GetMethod("Capture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture,new object[]{name,width,height});
        }
        private sealed class FailingStore : GameFoundation.Save.ISaveStore
        {
            private readonly GameFoundation.Save.MemorySaveStore store=new GameFoundation.Save.MemorySaveStore();public bool Fail;
            public bool TryRead(string slot,out string value){return store.TryRead(slot,out value);}
            public bool TryReadBackup(string slot,out string value){return store.TryReadBackup(slot,out value);}
            public void Write(string slot,string value){if(Fail)throw new System.IO.IOException("test write failure");store.Write(slot,value);}
            public void Restore(string slot,string value){store.Restore(slot,value);}public void Delete(string slot){store.Delete(slot);}
        }
        [UnityTest] public IEnumerator EnergyBadgeTracksCapAfterPurchaseRecoveryAndPageChanges()
        {
            var plus=Field<Button>("EnergyPlus");Assert.IsFalse(plus.gameObject.activeSelf,"Full energy must hide its badge.");
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));yield return null;
            Assert.AreEqual(4,Wallet.Energy);Assert.IsTrue(plus.gameObject.activeSelf);Capture("energy-below-cap.png",720,1280);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.IsTrue(plus.gameObject.activeSelf);PointerClick(plus,home.HomeCamera);Assert.AreEqual(5,Wallet.Energy);Assert.IsFalse(plus.gameObject.activeSelf);Capture("energy-full-shop.png",720,1280);
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));yield return null;Assert.IsTrue(plus.gameObject.activeSelf);
            WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddSeconds(900);AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());yield return null;Assert.AreEqual(5,Wallet.Energy);Assert.IsFalse(plus.gameObject.activeSelf);
            home.TabButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.IsFalse(plus.gameObject.activeSelf);Capture("energy-full-home.png",720,1280);
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsFalse(Field<Button>("EnergyPlus").gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator QuitKeepsFailedDebitPendingUntilSaveRecoversBeforeNextStart()
        {
            var disk=new FailingStore();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(disk,WalletTestScope.Clock),null);
            Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();
            var game=Object.FindObjectOfType<SortGameViewController>();game.RequestPause();disk.Fail=true;
            game.ActivePopup.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();game.ActivePopup.Primary.onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);home=Object.FindObjectOfType<SortHomeViewController>();Assert.AreEqual(5,Wallet.Energy);Assert.IsFalse(home.TryStartGame());Assert.IsTrue(home.ErrorLabel.gameObject.activeSelf);
            disk.Fail=false;AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());Assert.AreEqual(4,Wallet.Energy);Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();Assert.AreEqual(4,Wallet.Energy);
        }
        [UnityTest] public IEnumerator HudAddsWithRealPointersSharesPagesAndRejectsHiddenCallbacks()
        {
            Assert.AreEqual("1600",Field<Text>("CoinValue").text);Assert.AreEqual("5",Field<Text>("EnergyValue").text);Assert.AreEqual("Full",Field<Text>("EnergyRecoveryLabel").text);
            Capture("wallet-home-phone.png",720,1280);Capture("wallet-home-ipad.png",768,1024);
            PointerClick(Field<Button>("CoinPlus"),home.HomeCamera);Assert.AreEqual(2400,Wallet.Coins);Assert.IsFalse(Field<Button>("EnergyPlus").gameObject.activeSelf);Assert.AreEqual(5,Wallet.Energy);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.AreEqual(1,Field<CanvasGroup>("ResourceHud").alpha,.001f);
            Assert.AreEqual("2400",Field<Text>("CoinValue").text);PointerClick(Field<Button>("CoinPlus"),home.HomeCamera);Assert.AreEqual(3200,Wallet.Coins);
            Capture("wallet-shop-phone.png",720,1280);Capture("wallet-shop-ipad.png",768,1024);
            home.TabButtons[2].onClick.Invoke();Field<Button>("CoinPlus").onClick.Invoke();Assert.AreEqual(3200,Wallet.Coins);yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(0,Field<CanvasGroup>("ResourceHud").alpha,.001f);Field<Button>("CoinPlus").onClick.Invoke();Assert.AreEqual(3200,Wallet.Coins);
            home.TabButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);home.enabled=false;Field<Button>("CoinPlus").onClick.Invoke();Assert.AreEqual(3200,Wallet.Coins);home.enabled=true;yield return null;
        }
        [UnityTest] public IEnumerator QuitAndRestartDebitOnceAndCountdownRecoversAtTheUtcBoundary()
        {
            Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();
            var game=Object.FindObjectOfType<SortGameViewController>();game.RestartLevel();Assert.AreEqual(4,Wallet.Energy);
            game.RequestPause();yield return null;var quit=game.ActivePopup.GetComponent<SortPauseSettingsView>().QuitButton;quit.onClick.Invoke();quit.onClick.Invoke();game.ActivePopup.Primary.onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);home=Object.FindObjectOfType<SortHomeViewController>();Assert.AreEqual(3,Wallet.Energy);Assert.AreEqual("15:00",Field<Text>("EnergyRecoveryLabel").text);
            Capture("wallet-low-phone.png",720,1280);Capture("wallet-low-ipad.png",768,1024);
            WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddSeconds(899);AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());yield return new WaitForSecondsRealtime(1.1f);Assert.AreEqual("00:01",Field<Text>("EnergyRecoveryLabel").text);
            PointerClick(Field<Button>("EnergyPlus"),home.HomeCamera);Assert.AreEqual(4,Wallet.Energy);WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddSeconds(1);AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());yield return null;
            Assert.AreEqual(5,Wallet.Energy);Assert.AreEqual("Full",Field<Text>("EnergyRecoveryLabel").text);
        }
        [UnityTest] public IEnumerator VisibleHomeResultActionKeepsItsMeaningAfterRecovery()
        {
            for(int i=0;i<4;i++){AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));}
            Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;yield return null;
            var game=Object.FindObjectOfType<SortGameViewController>();Assert.AreEqual(0,Wallet.Energy);Assert.IsTrue(game.ActivePopup.Secondary.gameObject.activeSelf);
            WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddSeconds(900);AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());Assert.AreEqual(1,Wallet.Energy);
            game.ActivePopup.Secondary.onClick.Invoke();yield return null;yield return null;Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(1,Wallet.Energy);
        }
        [UnityTest] public IEnumerator LastEnergyRestartCanPlayAtZeroThenRetryIsBlockedAndFailureCanReturnHome()
        {
            for(int i=0;i<4;i++){AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));}
            Assert.AreEqual(1,Wallet.Energy);Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();
            var game=Object.FindObjectOfType<SortGameViewController>();game.RestartLevel();Assert.AreEqual(0,Wallet.Energy);game.RequestPause();yield return null;
            var popup=game.ActivePopup;var board=AllSortedApp.Interface.GetModel<SortBoardModel>().Board;popup.Secondary.onClick.Invoke();Assert.AreSame(popup,game.ActivePopup);Assert.AreSame(board,AllSortedApp.Interface.GetModel<SortBoardModel>().Board);
            game.ResumeLevel();AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;yield return null;
            Assert.AreEqual(0,Wallet.Energy);Assert.IsTrue(game.ActivePopup.Secondary.gameObject.activeSelf,"Zero energy terminal screen must allow returning to the refill HUD.");
            game.ActivePopup.Secondary.onClick.Invoke();yield return null;yield return null;Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);home=Object.FindObjectOfType<SortHomeViewController>();
            Assert.IsFalse(home.TryStartGame());Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(0,AllSortedApp.Interface.GetModel<SortHomeNavigationModel>().PendingLevel);
            PointerClick(Field<Button>("EnergyPlus"),home.HomeCamera);Assert.AreEqual(1,Wallet.Energy);Assert.IsTrue(home.TryStartGame());yield return null;
        }
    }
}
