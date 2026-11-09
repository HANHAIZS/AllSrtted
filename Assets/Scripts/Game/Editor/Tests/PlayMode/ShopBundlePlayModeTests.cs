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
    public sealed class ShopBundlePlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private SortWalletModel Wallet {get{return AllSortedApp.Interface.GetModel<SortWalletModel>();}}
        [UnitySetUp] public IEnumerator Load(){yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();}
        public static IEnumerator Reveal(SortHomeViewController home,Button button)
        {
            var scroll=home.ShopScroll;if(!scroll)yield break;
            scroll.StopMovement();Canvas.ForceUpdateCanvases();var viewport=scroll.viewport;var rect=(RectTransform)button.transform;
            float y=viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
            scroll.content.anchoredPosition+=new Vector2(0,viewport.rect.center.y-y);Canvas.ForceUpdateCanvases();yield return null;yield return null;
        }
        private void PointerClick(Button button)
        {
            Canvas.ForceUpdateCanvases();var r=(RectTransform)button.transform;var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(home.HomeCamera,r.TransformPoint(r.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Assert.Greater(hits.Count,0);Assert.AreSame(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private void Capture(string name,int width,int height){var fixture=new HomeNavigationPlayModeTests();typeof(HomeNavigationPlayModeTests).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture,home);typeof(HomeNavigationPlayModeTests).GetMethod("Capture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture,new object[]{name,width,height});}
        [UnityTest] public IEnumerator ProductsRenderAboveCoinsPointerPurchasesGrantAllAndKeepHudOnReload()
        {
            Assert.AreEqual(3,home.BundleButtons.Length);foreach(var b in home.BundleButtons)b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);home.ShopScroll.verticalNormalizedPosition=1;yield return null;
            Capture("shop-bundles-phone.png",720,1280);Capture("shop-bundles-ipad.png",768,1024);
            string[] prices={"US$9.99","US$18.99","US$36.99"};string[] coins={"5000","10000","25000"};string[] durations={"1h","3h","12h"};int[] quantities={1,3,6};int expected=1600;
            for(int i=0;i<3;i++) {
                var button=home.BundleButtons[i];var row=button.transform.parent;
                Assert.AreEqual(prices[i],button.transform.Find("Price").GetComponent<Text>().text);Assert.AreEqual(coins[i],row.Find("Coins").GetComponent<Text>().text);Assert.AreEqual(durations[i],row.Find("UnlimitedLives/Duration").GetComponent<Text>().text);
                for(int j=0;j<4;j++)Assert.AreEqual("×"+quantities[i],row.Find("Booster"+j+"/Count").GetComponent<Text>().text);
                Assert.AreEqual(Color.white,row.Find("Name").GetComponent<Text>().color);Assert.AreEqual(home.CoinValue.font,row.Find("Name").GetComponent<Text>().font);Assert.AreEqual(home.CoinPackButtons[0].transform.Find("Price").GetComponent<Text>().font,row.Find("Coins").GetComponent<Text>().font);
                yield return Reveal(home,button);PointerClick(button);expected+=SortBundleCatalog.GetCoins(i);Assert.AreEqual(expected,Wallet.Coins);Assert.AreEqual(expected.ToString(),home.CoinValue.text);Assert.IsTrue(Wallet.HasUnlimitedLives);Assert.AreEqual("∞",home.EnergyValue.text);Assert.LessOrEqual(home.EnergyRecoveryLabel.preferredWidth,home.EnergyRecoveryLabel.rectTransform.rect.width);Assert.IsFalse(home.EnergyPlus.gameObject.activeSelf);
            }
            Assert.AreEqual(57600,Wallet.UnlimitedSeconds);Assert.AreEqual(13,AllSortedApp.Interface.GetModel<SortBoosterModel>().GetCharges(SortBooster.Vacuum));Assert.AreEqual(13,Wallet.GetEntryCharges(0));
            yield return Reveal(home,home.CoinPackButtons[5]);PointerClick(home.CoinPackButtons[5]);Assert.AreEqual(expected+100000,Wallet.Coins);home.ShopScroll.verticalNormalizedPosition=0;yield return null;Capture("shop-bundles-coins-phone.png",720,1280);
            home.TabButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Capture("shop-bundles-unlimited-home.png",720,1280);
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();Assert.AreEqual(expected+100000,Wallet.Coins);Assert.AreEqual("∞",home.EnergyValue.text);Assert.LessOrEqual(home.EnergyRecoveryLabel.preferredWidth,home.EnergyRecoveryLabel.rectTransform.rect.width);Assert.IsFalse(home.EnergyPlus.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator HiddenTransitionDisabledAndModalCallbacksCannotGrantAndReuseDoesNotDuplicate()
        {
            var b=home.BundleButtons[0];home.TabButtons[0].onClick.Invoke();b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);yield return new WaitForSecondsRealtime(.35f);
            for(int i=0;i<3;i++){home.enabled=false;b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);home.enabled=true;yield return null;}
            home.BundleRoot.gameObject.SetActive(false);b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);home.BundleRoot.gameObject.SetActive(true);b.interactable=false;b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);b.interactable=true;
            home.Layers.SetModal(true);b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);home.Layers.SetModal(false);
            yield return Reveal(home,b);PointerClick(b);Assert.AreEqual(6600,Wallet.Coins);home.TabButtons[2].onClick.Invoke();b.onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);b.onClick.Invoke();Assert.AreEqual(6600,Wallet.Coins);
        }
        [UnityTest] public IEnumerator MissingScrollContentRestoresReachableExistingCoinShop(){return MissingBinding(0);}
        [UnityTest] public IEnumerator MissingScrollReferenceRestoresReachableExistingCoinShop(){return MissingBinding(1);}
        [UnityTest] public IEnumerator MissingBundleNoticeRestoresReachableExistingCoinShop(){return MissingBinding(2);}
        private IEnumerator MissingBinding(int kind)
        {
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);var scroll=home.ShopScroll;
            if(kind==0)scroll.content=null;else if(kind==1)home.ShopScroll=null;else home.BundleNotice=null;
            LogAssert.Expect(LogType.Error,"Shop bundle scroll bindings are incomplete; coin packs restored to fixed layout.");home.RefreshLayout();yield return null;
            Assert.IsFalse(home.BundleRoot.gameObject.activeSelf);Assert.AreSame(home.Pages[0].transform,home.CoinShopRoot.parent);Assert.IsFalse(scroll.gameObject.activeSelf);
            home.BundleButtons[0].onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);PointerClick(home.CoinPackButtons[0]);Assert.AreEqual(2600,Wallet.Coins);
        }
        private sealed class Disk : ISaveStore
        {
            private readonly MemorySaveStore memory=new MemorySaveStore();public bool Fail;
            public bool TryRead(string k,out string v){return memory.TryRead(k,out v);}public bool TryReadBackup(string k,out string v){return memory.TryReadBackup(k,out v);}public void Write(string k,string v){if(Fail)throw new System.IO.IOException("bundle test failure");memory.Write(k,v);}public void Restore(string k,string v){memory.Restore(k,v);}public void Delete(string k){memory.Delete(k);}
        }
        [UnityTest] public IEnumerator FailedPurchaseCanRetryAndExpirationRestoresHudAndRecovery()
        {
            var disk=new Disk();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(disk,WalletTestScope.Clock),null);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);var b=home.BundleButtons[0];yield return Reveal(home,b);disk.Fail=true;PointerClick(b);Assert.AreEqual(1600,Wallet.Coins);StringAssert.Contains("Unable to save",home.BundleNotice.text);
            disk.Fail=false;PointerClick(b);Assert.AreEqual(6600,Wallet.Coins);WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddHours(1);yield return new WaitForSecondsRealtime(1.1f);Assert.AreEqual("5",home.EnergyValue.text);Assert.AreEqual("Full",home.EnergyRecoveryLabel.text);Assert.IsFalse(home.EnergyPlus.gameObject.activeSelf);
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));AllSortedApp.Interface.SendCommand(new EndSortAttemptCommand(Wallet.AttemptId));yield return null;Assert.AreEqual("4",home.EnergyValue.text);Assert.IsTrue(home.EnergyPlus.gameObject.activeSelf);
        }
    }
}
