using System.Collections;
using System.Reflection;
using GameFoundation.UI;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class PopupMotionPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private readonly System.Collections.Generic.List<SortPopupView> owned=new System.Collections.Generic.List<SortPopupView>();
        [UnitySetUp] public IEnumerator Load(){yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();}
        [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;var router=AllSortedApp.Interface.GetUtility<UiPopupRouter>();foreach(var v in owned){router.Unregister(v);if(v)Object.Destroy(v.gameObject);}owned.Clear();yield return null;}
        private SortPopupView Make(string key){var prefab=Resources.Load<GameObject>("Prefabs/UI/Popups/"+key);Assert.IsNotNull(prefab);var go=Object.Instantiate(prefab,home.Layers.Hosts[2].transform,false);go.SetActive(false);var v=go.GetComponent<SortPopupView>();owned.Add(v);return v;}
        private static RectTransform Content(SortPopupView v){return (RectTransform)v.transform.Find("PopupContent");}
        private static CanvasGroup Fade(SortPopupView v,string child){var g=v.transform.Find(child).GetComponent<CanvasGroup>();Assert.IsNotNull(g,"Motion must fade "+child);return g;}
        private static void Step(SortPopupView v,float seconds){var method=typeof(SortPopupView).GetMethod("AdvanceMotion",BindingFlags.Instance|BindingFlags.NonPublic);Assert.IsNotNull(method);method.Invoke(v,new object[]{seconds});}
        [UnityTest] public IEnumerator RecordsNativeOpeningAndClosingFrames()
        {
            System.IO.Directory.CreateDirectory("TestResults/popup-motion");Time.timeScale=0;Assert.IsTrue(home.OpenEntryPopup());var v=home.ActiveEntryPopup;
            var fixture=new HomeNavigationPlayModeTests();typeof(HomeNavigationPlayModeTests).GetField("home",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fixture,home);var capture=typeof(HomeNavigationPlayModeTests).GetMethod("Capture",BindingFlags.Instance|BindingFlags.NonPublic);
            var samples=new System.Text.StringBuilder("phase,time,scale,panelAlpha,dimAlpha\n");int index=0;
            capture.Invoke(fixture,new object[]{"popup-motion/frame"+(index++).ToString("000")+".png",360,640});samples.AppendLine("open,0,"+Content(v).localScale.x+","+Fade(v,"PopupContent").alpha+","+Fade(v,"DimBackground").alpha);
            for(int i=1;i<=10;i++){Step(v,.025f);capture.Invoke(fixture,new object[]{"popup-motion/frame"+(index++).ToString("000")+".png",360,640});samples.AppendLine("open,"+(i*.025f)+","+Content(v).localScale.x+","+Fade(v,"PopupContent").alpha+","+Fade(v,"DimBackground").alpha);}
            home.CloseEntryPopup();for(int i=1;i<=6;i++){Step(v,.025f);capture.Invoke(fixture,new object[]{"popup-motion/frame"+(index++).ToString("000")+".png",360,640});samples.AppendLine("close,"+(i*.025f)+","+Content(v).localScale.x+","+Fade(v,"PopupContent").alpha+","+Fade(v,"DimBackground").alpha);}
            System.IO.File.WriteAllText("TestResults/popup-motion/samples.csv",samples.ToString());Assert.IsFalse(v.gameObject.activeSelf);yield return null;
        }
        [UnityTest] public IEnumerator EveryModalUsesAuthoredScaleAndExactOpenCloseMilestones()
        {
            foreach(string key in new[]{"PausePopup","LeavingPopup","BoosterPurchasePopup","PrelevelPopup","ResultPopup","VictoryPopup"}){
                var v=Make(key);var scale=Content(v).localScale;int closed=0;v.Configure(()=>{},()=>{},()=>closed++);v.Open(null);
                Assert.AreEqual(scale.x*.85f,Content(v).localScale.x,.0001f,key);var panel=Fade(v,"PopupContent");var dim=Fade(v,"DimBackground");Assert.Less(panel.alpha,.01f);Assert.AreEqual(0,dim.alpha,.001f);
                Step(v,.175f);Assert.AreEqual(scale.x*1.03f,Content(v).localScale.x,.0001f);Assert.Greater(panel.alpha,.5f);Assert.Greater(dim.alpha,.5f);
                Step(v,.075f);Assert.AreEqual(scale,Content(v).localScale);Assert.AreEqual(1,panel.alpha,.001f);Assert.AreEqual(1,dim.alpha,.001f);
                v.Close();Assert.IsFalse(v.IsOpen);Assert.IsTrue(v.gameObject.activeSelf);Assert.AreEqual(1,closed);Assert.IsFalse(panel.blocksRaycasts);Assert.IsFalse(dim.blocksRaycasts);Step(v,.075f);Assert.Greater(panel.alpha,0);Assert.Less(panel.alpha,1);Step(v,.075f);Assert.IsFalse(v.gameObject.activeSelf);Assert.AreEqual(scale,Content(v).localScale);Assert.AreEqual(1,closed);
            }
            yield return null;
        }
        [UnityTest] public IEnumerator UnscaledMotionCompletesAndClosingButtonsCannotAct()
        {
            var v=Make("PrelevelPopup");var scale=Content(v).localScale;int clicks=0,closed=0;v.Configure(()=>clicks++,()=>clicks++,()=>closed++);Time.timeScale=0;v.Open(null);yield return new WaitForSecondsRealtime(.29f);Assert.AreEqual(scale,Content(v).localScale);Assert.AreEqual(1,Fade(v,"PopupContent").alpha,.001f);
            v.Close();v.Close();v.Primary.onClick.Invoke();v.Secondary.onClick.Invoke();Assert.AreEqual(0,clicks);Assert.AreEqual(1,closed);yield return new WaitForSecondsRealtime(.18f);Assert.IsFalse(v.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator ReplaceTransfersDimAndReopenCancelsOldCloseWithoutScaleDrift()
        {
            var a=Make("PrelevelPopup");var b=Make("BoosterPurchasePopup");a.Id="MotionA";b.Id="MotionB";var router=AllSortedApp.Interface.GetUtility<UiPopupRouter>();int closedA=0;var scale=Content(a).localScale;
            a.Configure(()=>{},()=>{},()=>{closedA++;router.NotifyClosed(a);});b.Configure(()=>{},()=>{},()=>router.NotifyClosed(b));router.Register(a);router.Register(b);router.Show(a.PopupId,UiPopupOpenMode.Replace);Step(a,.25f);router.Show(b.PopupId,UiPopupOpenMode.Replace);
            Assert.AreEqual(0,Fade(a,"DimBackground").alpha,.001f);Assert.AreEqual(1,Fade(b,"DimBackground").alpha,.001f);Assert.AreEqual(b.transform.parent.childCount-1,b.transform.GetSiblingIndex());Step(a,.075f);
            router.Show(a.PopupId,UiPopupOpenMode.Replace);Step(a,.25f);Step(b,.15f);Assert.AreSame(a,router.ActiveView);Assert.IsTrue(a.IsOpen);Assert.IsTrue(a.gameObject.activeSelf);Assert.AreEqual(scale,Content(a).localScale);Assert.AreEqual(1,closedA);
            a.gameObject.SetActive(false);Assert.AreEqual(scale,Content(a).localScale);a.Open(null);Step(a,.25f);Assert.AreEqual(scale,Content(a).localScale);yield return null;
        }
    }
}
