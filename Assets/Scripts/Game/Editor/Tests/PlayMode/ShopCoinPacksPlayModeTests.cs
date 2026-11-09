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
namespace AllSorted.PlayTests
{
    public sealed class ShopCoinPacksPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private SortWalletModel Wallet {get{return AllSortedApp.Interface.GetModel<SortWalletModel>();}}
        [UnitySetUp] public IEnumerator Load(){yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();}
        private T Field<T>(string name) where T:class {var f=typeof(SortHomeViewController).GetField(name);Assert.IsNotNull(f,"Missing Shop binding: "+name);var value=f.GetValue(home) as T;Assert.IsNotNull(value);return value;}
        private IEnumerator Click(Button b){yield return ShopBundlePlayModeTests.Reveal(home,b);Canvas.ForceUpdateCanvases();var d=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(home.HomeCamera,b.transform.TransformPoint(((RectTransform)b.transform).rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);Assert.Greater(hits.Count,0);Assert.AreSame(b.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));ExecuteEvents.Execute(b.gameObject,d,ExecuteEvents.pointerClickHandler);yield return null;}
        private void Capture(string name,int w,int h){var fixture=new HomeNavigationPlayModeTests();typeof(HomeNavigationPlayModeTests).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture,home);typeof(HomeNavigationPlayModeTests).GetMethod("Capture",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture,new object[]{name,w,h});}
        [UnityTest] public IEnumerator SixPricesAndRealPointerGrantsShareHudAndSurviveSceneReload()
        {
            var buttons=Field<Button[]>("CoinPackButtons");Assert.AreEqual(6,buttons.Length);foreach(var b in buttons)b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Capture("shop-coins-phone.png",720,1280);Capture("shop-coins-ipad.png",768,1024);
            int[] amounts={1000,5000,10000,25000,50000,100000};string[] labels={"1.000","5.000","10.000","25.000","50.000","100.000"};string[] prices={"$1.99","$8.99","$16.99","$34.99","$64.99","$99.99"};int expected=1600;
            for(int i=0;i<6;i++){Assert.AreEqual(prices[i],buttons[i].transform.Find("Price").GetComponent<Text>().text);Assert.AreEqual(labels[i],buttons[i].transform.parent.Find("Amount").GetComponent<Text>().text);yield return Click(buttons[i]);expected+=amounts[i];Assert.AreEqual(expected,Wallet.Coins);Assert.AreEqual(expected.ToString(),home.CoinValue.text);}
            home.TabButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.AreEqual(expected.ToString(),home.CoinValue.text);buttons[0].onClick.Invoke();Assert.AreEqual(expected,Wallet.Coins);
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();Assert.AreEqual(expected.ToString(),home.CoinValue.text);
        }
        [UnityTest] public IEnumerator TransitionsHiddenPagesAndDisableCannotGrantOrDuplicateListeners()
        {
            var b=Field<Button[]>("CoinPackButtons")[0];home.TabButtons[0].onClick.Invoke();b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);yield return new WaitForSecondsRealtime(.35f);
            for(int i=0;i<3;i++){home.enabled=false;b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);home.enabled=true;yield return null;}
            var root=Field<RectTransform>("CoinShopRoot");root.gameObject.SetActive(false);b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);root.gameObject.SetActive(true);
            b.gameObject.SetActive(false);b.onClick.Invoke();Assert.AreEqual(1600,Wallet.Coins);b.gameObject.SetActive(true);
            yield return Click(b);Assert.AreEqual(2600,Wallet.Coins);home.TabButtons[2].onClick.Invoke();b.onClick.Invoke();Assert.AreEqual(2600,Wallet.Coins);yield return new WaitForSecondsRealtime(.35f);b.onClick.Invoke();Assert.AreEqual(2600,Wallet.Coins);
        }
        private sealed class FailingStore : ISaveStore
        {
            private readonly MemorySaveStore memory=new MemorySaveStore();public bool Fail;
            public bool TryRead(string s,out string v){return memory.TryRead(s,out v);}public bool TryReadBackup(string s,out string v){return memory.TryReadBackup(s,out v);}
            public void Write(string s,string v){if(Fail)throw new System.IO.IOException("test coin-pack write failure");memory.Write(s,v);}public void Restore(string s,string v){memory.Restore(s,v);}public void Delete(string s){memory.Delete(s);}
        }
        [UnityTest] public IEnumerator FailedSaveGrantsNothingAndCanRetryFromSameCard()
        {
            var disk=new FailingStore();typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Wallet,new SortWalletLedger(disk,WalletTestScope.Clock),null);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);var b=Field<Button[]>("CoinPackButtons")[2];disk.Fail=true;yield return Click(b);Assert.AreEqual(1600,Wallet.Coins);StringAssert.Contains("Unable to save",Field<Text>("CoinShopNotice").text);
            disk.Fail=false;yield return Click(b);Assert.AreEqual(11600,Wallet.Coins);Assert.AreEqual("11600",home.CoinValue.text);
        }
    }
}
