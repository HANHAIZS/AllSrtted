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
    // Test-only isolation: production scene/retry operations intentionally retain the inventory.
    public static class BoosterSessionTestState
    {
        public static object Stock()
        {
            var type=typeof(SortBoardModel).Assembly.GetType("AllSorted.SortBoosterModel");
            if(type==null) return null;
            return typeof(IArchitecture).GetMethod("GetModel").MakeGenericMethod(type).Invoke(AllSortedApp.Interface,null);
        }

        public static void Reset()
        {
            var stock=Stock();if(stock==null) return;
            var charges=WalletTestScope.Charges();
            for(int i=0;i<4;i++) charges[i]=3;
            ((BindableProperty<int>)stock.GetType().GetField("Revision").GetValue(stock)).Value++;
        }

        public static int Charges(int index)
        {
            var stock=Stock();Assert.IsNotNull(stock);
            return (int)stock.GetType().GetMethod("GetCharges").Invoke(stock,new object[]{(SortBooster)index});
        }
    }

    public sealed class BoosterQuotaPlayModeTests : WalletTestBase
    {
        private SortGameViewController view;private SortBoardModel model;

        /// <summary>Each test owns a fresh quota fixture without changing the production restart policy.</summary>
        [UnitySetUp] public IEnumerator Load()
        {
            BoosterSessionTestState.Reset();
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();model=AllSortedApp.Interface.GetModel<SortBoardModel>();
        }

        /// <summary>The actual child badge receives the pointer and must not also use the parent booster.</summary>
        [UnityTest] public IEnumerator RedThreeBecomesZeroPlusThenPurchaseThreeWithoutParentActivation()
        {
            for(int i=0;i<4;i++) {
                var badge=Badge(i);Assert.IsNotNull(badge);
                Assert.AreEqual("3",badge.transform.Find("Count").GetComponent<Text>().text);
                StringAssert.Contains("RemainingChargeBadge",badge.image.sprite.name);
            }
            Capture("quota-phone.png",720,1280);Capture("quota-ipad.png",768,1024);
            for(int i=0;i<3;i++) Main(1).onClick.Invoke();
            Assert.AreEqual(0,BoosterSessionTestState.Charges(1));Assert.IsFalse(Main(1).interactable);
            Assert.IsFalse(Badge(1).transform.Find("Count").gameObject.activeSelf);StringAssert.Contains("AddChargeBadge",Badge(1).image.sprite.name);
            Capture("quota-empty.png",720,1280);
            int revision=model.BoardRevision.Value;var target=Badge(1);
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(view.BoardCamera,target.transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.Greater(hits.Count,0);Assert.AreSame(target.gameObject,hits[0].gameObject);
            var press=ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.07f);
            ExecuteEvents.Execute(press,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(press,pointer,ExecuteEvents.pointerClickHandler);
            Assert.IsNotNull(view.ActivePopup);Assert.AreEqual(0,BoosterSessionTestState.Charges(1));Assert.AreEqual(revision,model.BoardRevision.Value);
            view.ActivePopup.Primary.onClick.Invoke();Assert.AreEqual(3,BoosterSessionTestState.Charges(1));Assert.AreEqual(revision,model.BoardRevision.Value);
            Assert.AreEqual("3",target.transform.Find("Count").GetComponent<Text>().text);StringAssert.Contains("RemainingChargeBadge",target.image.sprite.name);
            target.onClick.Invoke();Assert.AreEqual(3,BoosterSessionTestState.Charges(1));Assert.AreEqual(revision,model.BoardRevision.Value);
            for(int i=0;i<4;i++) if(i!=1) Assert.AreEqual(3,BoosterSessionTestState.Charges(i));
            yield return new WaitForSecondsRealtime(.3f);Capture("quota-refilled.png",720,1280);
        }

        /// <summary>Replacing boards and scene controllers must not replace the session allowance model.</summary>
        [UnityTest] public IEnumerator RetryLevelSelectionAndSceneReloadKeepSpentAllowances()
        {
            Assert.IsNotNull(Badge(1));Main(1).onClick.Invoke();Assert.AreEqual(2,BoosterSessionTestState.Charges(1));
            view.RestartLevel();Assert.AreEqual(2,BoosterSessionTestState.Charges(1));
#if UNITY_EDITOR
            Assert.IsTrue(UnityEditor.EditorApplication.ExecuteMenuItem("All Sorted/关卡选择/第 3 关"));yield return null;
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual(2,BoosterSessionTestState.Charges(1));
#endif
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();
            Assert.AreEqual(2,BoosterSessionTestState.Charges(1));Assert.AreEqual("2",Badge(1).transform.Find("Count").GetComponent<Text>().text);
        }

        /// <summary>Press, release, exit, cancel and disable restore exact authored scales without drift.</summary>
        [UnityTest] public IEnumerator MainAndBadgePressFeedbackRestoresAuthoredScale()
        {
            foreach(var button in new[]{Main(1),Badge(1)}) {
                Assert.IsNotNull(button.GetComponent("SortButtonPressFeedback"));Vector3 scale=button.transform.localScale;
                var pointer=new PointerEventData(EventSystem.current){pointerId=7,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.08f);
                Assert.Less(button.transform.localScale.x,scale.x);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(.35f);
                Assert.Less(Vector3.Distance(scale,button.transform.localScale),.001f);
                for(int i=0;i<8;i++) {ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);}
                yield return new WaitForSecondsRealtime(.35f);Assert.Less(Vector3.Distance(scale,button.transform.localScale),.001f);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.25f);
                Assert.Less(Vector3.Distance(scale,button.transform.localScale),.001f);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
                ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.cancelHandler);
                Assert.AreEqual(scale,button.transform.localScale);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
                button.gameObject.SetActive(false);Assert.AreEqual(scale,button.transform.localScale);button.gameObject.SetActive(true);
            }
        }

        /// <summary>Direct stale callbacks cannot bypass modal/terminal gates to refill or use a booster.</summary>
        [UnityTest] public IEnumerator EmptyBadgeCannotRefillDuringPauseOrAfterTimeout()
        {
            Assert.IsNotNull(Badge(1));for(int i=0;i<3;i++) Main(1).onClick.Invoke();Assert.AreEqual(0,BoosterSessionTestState.Charges(1));
            view.RequestPause();int revision=model.BoardRevision.Value;Badge(1).onClick.Invoke();Main(1).onClick.Invoke();
            Assert.AreEqual(0,BoosterSessionTestState.Charges(1));Assert.AreEqual(revision,model.BoardRevision.Value);
            view.ResumeLevel();AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            Badge(1).onClick.Invoke();Assert.AreEqual(0,BoosterSessionTestState.Charges(1));Assert.AreEqual(revision,model.BoardRevision.Value);
        }

        private Button Main(int index) {return view.Layers.Hosts[0].transform.Find("SafeArea/BottomButtons/FunctionButton_"+(index+1)).GetComponent<Button>();}
        private Button Badge(int index) {var rect=Main(index).transform.Find("QuotaBadge");return rect?rect.GetComponent<Button>():null;}

        /// <summary>Captures actual camera-space HUD at the supported phone and tablet aspects.</summary>
        private void Capture(string name,int width,int height)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera=view.BoardCamera;var target=new RenderTexture(width,height,24);var old=camera.targetTexture;
            var active=RenderTexture.active;float size=camera.orthographicSize;var scale=view.Background.transform.localScale;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);float aspect=(float)width/height;
            camera.targetTexture=target;camera.orthographicSize=Mathf.Max(6.4f,4.3f/aspect);view.SendMessage("FitBackground",aspect);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllBytes("TestResults/"+name,image.EncodeToPNG());
            camera.targetTexture=old;camera.orthographicSize=size;view.Background.transform.localScale=scale;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
