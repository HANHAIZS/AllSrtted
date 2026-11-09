using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace AllSorted.PlayTests
{
    public sealed class PrelevelPopupPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private SortWalletModel Wallet {get{return AllSortedApp.Interface.GetModel<SortWalletModel>();}}
        [UnitySetUp] public IEnumerator Load(){yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();}
        private Button Item(int i){return home.ActiveEntryPopup.transform.Find("PopupContent/Item"+i).GetComponent<Button>();}
        private Button Plus(int i){return Item(i).transform.Find("Plus").GetComponent<Button>();}
        private void Click(Button b){Canvas.ForceUpdateCanvases();var d=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(home.HomeCamera,b.transform.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);Assert.Greater(hits.Count,0);Assert.AreSame(b.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));ExecuteEvents.Execute(b.gameObject,d,ExecuteEvents.pointerClickHandler);}
        private void Capture(string n,int w,int h){var f=new HomeNavigationPlayModeTests();typeof(HomeNavigationPlayModeTests).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f,home);typeof(HomeNavigationPlayModeTests).GetMethod("Capture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{n,w,h});}
        [UnityTest] public IEnumerator BlueAndGreenCheckedStatesToggleWithoutSpendingAndResetOnReopen()
        {
            Click(home.StartButton);yield return null;
            for(int i=0;i<2;i++){Assert.AreEqual("BluePlate",Item(i).image.sprite.name);Assert.IsFalse(Item(i).transform.Find("Selected").gameObject.activeSelf);Assert.AreEqual("RemainingChargeBadge",Plus(i).image.sprite.name);}
            Click(Item(0));Assert.IsTrue(Item(0).transform.Find("Selected").gameObject.activeSelf);var check=Item(0).transform.Find("CheckBadge");Assert.IsNotNull(check);Assert.IsTrue(check.gameObject.activeSelf);Assert.IsFalse(Plus(0).gameObject.activeSelf);Assert.IsFalse(Item(0).transform.Find("Count").gameObject.activeSelf);Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.IsFalse(Item(1).transform.Find("Selected").gameObject.activeSelf);
            Capture("prelevel-states-phone.png",720,1280);Capture("prelevel-states-ipad.png",768,1024);
            Click(Item(0));Assert.IsFalse(check.gameObject.activeSelf);Assert.IsTrue(Plus(0).gameObject.activeSelf);Assert.IsTrue(Item(0).transform.Find("Count").gameObject.activeSelf);Assert.AreEqual(3,Wallet.GetEntryCharges(0));
            Click(Item(0));Click(home.ActiveEntryPopup.Secondary);Click(home.StartButton);Assert.IsFalse(Item(0).transform.Find("Selected").gameObject.activeSelf);Assert.IsFalse(check.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator RealPointersSelectGrantCancelReuseAndStartBothEffectsOnce()
        {
            Assert.IsNull(home.ActiveEntryPopup);Assert.IsNull(Object.FindObjectOfType<SortPopupView>());Click(home.StartButton);yield return null;var popup=home.ActiveEntryPopup;Assert.IsNotNull(popup);Assert.IsTrue(home.Layers.GameplayBlocked);Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual("LEVEL 01",popup.Title.text);
            Assert.IsFalse(Plus(0).interactable);Plus(0).onClick.Invoke();Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.AreEqual(3,Wallet.GetEntryCharges(1));Click(Item(0));Click(Item(1));Assert.AreEqual(3,Wallet.GetEntryCharges(0));
            Capture("prelevel-selected-phone.png",720,1280);Capture("prelevel-selected-ipad.png",768,1024);
            home.TabButtons[0].onClick.Invoke();Assert.AreEqual(SortHomeTab.Home,home.SelectedTab);Assert.IsFalse(home.TryStartGame());int coins=Wallet.Coins;home.CoinPlus.onClick.Invoke();Assert.AreEqual(coins,Wallet.Coins);
            var oldPlus=Plus(0);Click(popup.Secondary);Assert.IsNull(home.ActiveEntryPopup);Assert.IsFalse(home.Layers.GameplayBlocked);oldPlus.onClick.Invoke();Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.AreEqual(5,Wallet.Energy);
            Click(home.StartButton);Assert.AreSame(popup,home.ActiveEntryPopup);Assert.IsFalse(Item(0).transform.Find("Selected").gameObject.activeSelf);Click(Item(0));Click(Item(1));Click(popup.Primary);popup.Primary.onClick.Invoke();yield return SceneEntryTestWait.Complete();
            Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name);var model=AllSortedApp.Interface.GetModel<SortBoardModel>();var normal=PrototypeLevel.Create(1);Assert.Greater(model.Board.RemainingSeconds,209);Assert.GreaterOrEqual(normal.RemainingCount-model.Board.RemainingCount,9);Assert.GreaterOrEqual(model.Board.Matches,3);Assert.AreEqual(2,Wallet.GetEntryCharges(0));Assert.AreEqual(2,Wallet.GetEntryCharges(1));Assert.AreEqual(5,Wallet.Energy);
            Object.FindObjectOfType<SortGameViewController>().RestartLevel();Assert.LessOrEqual(model.Board.RemainingSeconds,180);Assert.AreEqual(normal.RemainingCount,model.Board.RemainingCount);Assert.AreEqual(2,Wallet.GetEntryCharges(0));Assert.AreEqual(2,Wallet.GetEntryCharges(1));
        }
        [UnityTest] public IEnumerator PurchaseOffersUseExactContentPreserveSelectionsAndReturnToEntry()
        {
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,1));
            Click(home.StartButton);yield return null;var parent=home.ActiveEntryPopup;Click(Item(1));var green=Plus(0);Click(green);yield return null;var offer=home.ActiveEntryPurchasePopup;
            Assert.IsNotNull(offer);Assert.IsNull(home.ActiveEntryPopup);Assert.AreEqual("Vacuum Box",offer.Title.text);Assert.AreEqual("Vacuums up three identical items and clears them in one go",offer.Body.text);Assert.AreEqual("1600",offer.PrimaryCaption.text);Assert.IsTrue(offer.Primary.interactable);Assert.AreEqual("StrongVacuum",offer.transform.Find("PopupContent/Inset/BoosterIcon").GetComponent<Image>().sprite.name);
            yield return new WaitForSecondsRealtime(.3f);Capture("vacuum-box-purchase-phone.png",720,1280);Capture("vacuum-box-purchase-ipad.png",768,1024);
            parent.Primary.onClick.Invoke();green.onClick.Invoke();home.TabButtons[0].onClick.Invoke();Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(SortHomeTab.Home,home.SelectedTab);Assert.AreEqual(1600,Wallet.Coins);Assert.AreEqual(0,Wallet.GetEntryCharges(0));
            Click(offer.Secondary);yield return null;Assert.AreSame(parent,home.ActiveEntryPopup);Assert.IsTrue(Item(1).transform.Find("Selected").gameObject.activeSelf);Assert.AreEqual(1600,Wallet.Coins);
            Click(Plus(0));yield return null;Assert.AreSame(offer,home.ActiveEntryPurchasePopup);Click(offer.Primary);yield return null;offer.Primary.onClick.Invoke();Assert.AreSame(parent,home.ActiveEntryPopup);Assert.AreEqual(0,Wallet.Coins);Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.IsFalse(Item(0).transform.Find("Selected").gameObject.activeSelf);Assert.IsTrue(Item(1).transform.Find("Selected").gameObject.activeSelf);
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,2));yield return null;Click(Plus(1));Assert.AreSame(offer,home.ActiveEntryPurchasePopup);Assert.AreEqual("Time Alarm",offer.Title.text);Assert.AreEqual("Adds 30 seconds to the timer",offer.Body.text);Assert.AreEqual("ExtraTime",offer.transform.Find("PopupContent/Inset/BoosterIcon").GetComponent<Image>().sprite.name);Assert.IsFalse(offer.Primary.interactable);StringAssert.Contains("Not enough coins",offer.transform.Find("PopupContent/Notice").GetComponent<Text>().text);offer.Primary.onClick.Invoke();Assert.AreEqual(0,Wallet.Coins);Assert.AreEqual(0,Wallet.GetEntryCharges(1));
            AllSortedApp.Interface.SendCommand(new AddSortCoinsCommand());AllSortedApp.Interface.SendCommand(new AddSortCoinsCommand());Assert.IsTrue(offer.Primary.interactable);yield return new WaitForSecondsRealtime(.3f);Capture("time-alarm-purchase-phone.png",720,1280);Capture("time-alarm-purchase-ipad.png",768,1024);Click(offer.Secondary);yield return null;Assert.AreSame(parent,home.ActiveEntryPopup);Assert.AreEqual(1600,Wallet.Coins);
        }
        [UnityTest] public IEnumerator MissingPurchaseResourceKeepsParentAndReloadCleansBothViews()
        {
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,1));Assert.IsTrue(home.OpenEntryPopup());yield return null;var parent=home.ActiveEntryPopup;string key=home.EntryPurchasePopupKey;home.EntryPurchasePopupKey="MissingEntryPurchase";LogAssert.Expect(LogType.Error,new Regex("Entry purchase popup resource missing"));Assert.IsFalse(home.TryOpenEntryPurchase(0));Assert.AreSame(parent,home.ActiveEntryPopup);Assert.IsTrue(home.Layers.GameplayBlocked);Assert.AreEqual(1600,Wallet.Coins);home.EntryPurchasePopupKey=key;Assert.IsTrue(home.TryOpenEntryPurchase(0));
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsNull(home.ActiveEntryPurchasePopup);Assert.IsNull(home.ActiveEntryPopup);Assert.IsFalse(home.Layers.GameplayBlocked);Assert.AreEqual(1600,Wallet.Coins);Assert.IsTrue(home.OpenEntryPopup());
        }
        [UnityTest] public IEnumerator EntryPurchaseSharesWalletAndClosedBarCannotGrant()
        {
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,3));Assert.IsTrue(home.OpenEntryPopup());yield return null;Assert.IsTrue(home.TryOpenEntryPurchase(0));yield return new WaitForSecondsRealtime(.3f);
            var offer=home.ActiveEntryPurchasePopup;Assert.AreEqual("1600",offer.PurchaseCoinBalance.text);Click(offer.PurchaseCoinPlus);Assert.AreEqual(2400,Wallet.Coins);Assert.AreEqual("2400",offer.PurchaseCoinBalance.text);Assert.AreEqual("2400",home.CoinValue.text);
            Capture("purchase-wallet-home-phone.png",720,1280);Capture("purchase-wallet-home-ipad.png",768,1024);
            Click(offer.Secondary);offer.PurchaseCoinPlus.onClick.Invoke();Assert.AreEqual(2400,Wallet.Coins);yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(home.TryOpenEntryPurchase(1));yield return new WaitForSecondsRealtime(.3f);Assert.AreSame(offer,home.ActiveEntryPurchasePopup);Click(offer.PurchaseCoinPlus);Assert.AreEqual(3200,Wallet.Coins);
        }
        [UnityTest] public IEnumerator CoinPlusFailedSaveGrantsNothingAndSamePopupCanRetry()
        {
            var store=new FailingStore();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(store,WalletTestScope.Clock),null);
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,1));Assert.IsTrue(home.OpenEntryPopup());yield return null;Assert.IsTrue(home.TryOpenEntryPurchase(0));yield return new WaitForSecondsRealtime(.3f);var offer=home.ActiveEntryPurchasePopup;
            store.Fail=true;Click(offer.PurchaseCoinPlus);Assert.AreEqual(1600,Wallet.Coins);Assert.AreEqual("1600",offer.PurchaseCoinBalance.text);StringAssert.Contains("Unable to save",offer.transform.Find("PopupContent/Notice").GetComponent<Text>().text);
            store.Fail=false;Click(offer.PurchaseCoinPlus);Assert.AreEqual(2400,Wallet.Coins);Assert.AreEqual("2400",offer.PurchaseCoinBalance.text);Assert.IsTrue(offer.Primary.IsInteractable());
        }
        [UnityTest] public IEnumerator MissingPopupAndZeroEnergyDoNotTrapHome()
        {
            string key=home.EntryPopupKey;home.EntryPopupKey="Prefabs/UI/Popups/MissingEntry";LogAssert.Expect(LogType.Error,new Regex("Level entry popup resource missing"));Assert.IsFalse(home.OpenEntryPopup());Assert.IsFalse(home.Layers.GameplayBlocked);home.EntryPopupKey=key;Assert.IsTrue(home.OpenEntryPopup());home.CloseEntryPopup();
            for(int i=0;i<5;i++){AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));}Assert.AreEqual(0,Wallet.Energy);Assert.IsFalse(home.OpenEntryPopup());Assert.IsFalse(home.Layers.GameplayBlocked);yield return null;
        }
        private sealed class FailingStore : ISaveStore {
            private readonly MemorySaveStore inner=new MemorySaveStore();public bool Fail;
            public bool TryRead(string k,out string v){return inner.TryRead(k,out v);}public bool TryReadBackup(string k,out string v){return inner.TryReadBackup(k,out v);}public void Write(string k,string v){if(Fail)throw new System.IO.IOException("entry test save failure");inner.Write(k,v);}public void Restore(string k,string v){inner.Restore(k,v);}public void Delete(string k){inner.Delete(k);}
        }
        [UnityTest] public IEnumerator SaveFailureAndMissingSceneKeepCountsAndPermitRetry()
        {
            var store=new FailingStore();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(store,WalletTestScope.Clock),null);
            for(int i=0;i<3;i++)AllSortedApp.Interface.SendCommand(new StartSortEntryCommand(1,1));
            Assert.IsTrue(home.OpenEntryPopup());yield return null;Click(Plus(0));yield return null;var offer=home.ActiveEntryPurchasePopup;store.Fail=true;Click(offer.Primary);yield return null;Assert.AreEqual(0,Wallet.GetEntryCharges(0));Assert.AreEqual(1600,Wallet.Coins);Assert.IsNotNull(home.ActiveEntryPurchasePopup);store.Fail=false;AllSortedApp.Interface.SendCommand(new RefreshSortWalletCommand());Click(offer.Primary);yield return null;Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.AreEqual(0,Wallet.Coins);Click(Item(0));Click(Item(1));
            string scene=home.GameSceneName;home.GameSceneName="MissingEntryGame";LogAssert.Expect(LogType.Error,new Regex("Game scene is unavailable"));home.ActiveEntryPopup.Primary.onClick.Invoke();Assert.IsNotNull(home.ActiveEntryPopup);Assert.AreEqual(3,Wallet.GetEntryCharges(0));home.GameSceneName=scene;home.ActiveEntryPopup.Primary.onClick.Invoke();store.Fail=true;yield return SceneEntryTestWait.Complete();yield return null;yield return null;
            Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(3,Wallet.GetEntryCharges(0));Assert.AreEqual(3,Wallet.GetEntryCharges(1));home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsFalse(home.Layers.GameplayBlocked);Assert.IsTrue(home.ErrorLabel.gameObject.activeSelf);StringAssert.Contains("save",home.ErrorLabel.text);store.Fail=false;
        }
        [UnityTest] public IEnumerator EmptyChargesCanBeRefilledAndNoSelectionStartsNormally()
        {
            for(int i=0;i<3;i++){var use=new StartSortEntryCommand(1,3);AllSortedApp.Interface.SendCommand(use);Assert.IsTrue(use.Accepted);}Assert.IsTrue(home.OpenEntryPopup());yield return null;Assert.IsFalse(Item(0).interactable);Assert.IsFalse(Item(1).interactable);Item(0).onClick.Invoke();Click(Plus(0));yield return null;Click(home.ActiveEntryPurchasePopup.Primary);yield return null;Assert.IsTrue(Item(0).interactable);Assert.AreEqual(3,Wallet.GetEntryCharges(0));Click(home.ActiveEntryPopup.Primary);yield return SceneEntryTestWait.Complete();var b=AllSortedApp.Interface.GetModel<SortBoardModel>().Board;Assert.AreEqual(PrototypeLevel.Create(1).RemainingCount,b.RemainingCount);Assert.LessOrEqual(b.RemainingSeconds,180);Assert.AreEqual(3,Wallet.GetEntryCharges(0));
        }
    }
}
