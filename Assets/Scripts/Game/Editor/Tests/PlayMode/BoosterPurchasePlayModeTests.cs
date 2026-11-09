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
    public sealed class BoosterPurchasePlayModeTests : WalletTestBase
    {
        private SortGameViewController game;
        private IArchitecture app;
        private SortWalletModel wallet;
        private SortBoardModel board;
        private SortBoosterModel stock;
        [UnitySetUp] public IEnumerator Load(){BoosterSessionTestState.Reset();yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;game=Object.FindObjectOfType<SortGameViewController>();app=AllSortedApp.Interface;wallet=app.GetModel<SortWalletModel>();board=app.GetModel<SortBoardModel>();stock=app.GetModel<SortBoosterModel>();}
        private void Deplete(int index){for(int i=0;i<3;i++){app.SendCommand(new StartSortLevelCommand(1));var c=new UseSortBoosterCommand((SortBooster)index);app.SendCommand(c);Assert.IsTrue(c.Accepted);}Assert.AreEqual(0,stock.GetCharges((SortBooster)index));game.SendMessage("ReconcilePieces");game.SendMessage("RefreshBoosterUi",true);}
        private void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();var d=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(game.BoardCamera,button.transform.TransformPoint(((RectTransform)button.transform).rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);Assert.Greater(hits.Count,0,"No pointer target for "+button.name+" at "+d.position+", phase="+board.Board.Phase+", modal="+game.Layers.GameplayBlocked+", popup="+(game.ActivePopup?game.ActivePopup.PopupId:"none"));Assert.AreSame(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));ExecuteEvents.Execute(button.gameObject,d,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator FourVariantsBuyThreeFor1600WithoutTriggeringTheirEffects()
        {
            string[] titles={"Vacuum","Shuffle Dice","Wildcard","Freeze Watch"};
            string[] bodies={"Vacuums up a set of three identical items","Shuffles all remaining items into a new layout","Turns three groups of three items into identical wildcards","Stops the timer for 15 seconds while you keep sorting"};
            SortPopupView cached=null;
            for(int i=0;i<4;i++) {
                if(i>0){app.SendCommand(new AddSortCoinsCommand());app.SendCommand(new AddSortCoinsCommand());}
                Deplete(i);int revision=board.BoardRevision.Value;float seconds=board.Board.RemainingSeconds,freeze=board.Board.FreezeSeconds;Click(game.BoosterBadges[i]);yield return null;
                Assert.IsNotNull(game.ActivePopup,"Green badge must open purchase instead of granting a free use.");var popup=game.ActivePopup;if(cached)Assert.AreSame(cached,popup);cached=popup;
                Capture(i,720,1280);if(i==1)Capture(i,768,1024);
                Assert.AreEqual(titles[i],popup.Title.text);Assert.AreEqual(bodies[i],popup.Body.text);Assert.AreEqual("1600",popup.PrimaryCaption.text);Assert.AreEqual(0,stock.GetCharges((SortBooster)i));Assert.AreEqual(1600,wallet.Coins);Assert.AreEqual(SortPhase.Paused,board.Board.Phase);
                app.SendCommand(new TickSortCommand{DeltaTime=10});Assert.AreEqual(seconds,board.Board.RemainingSeconds);Assert.AreEqual(freeze,board.Board.FreezeSeconds);
                game.BoosterButtons[i].onClick.Invoke();Assert.AreEqual(revision,board.BoardRevision.Value);
                Click(popup.Primary);popup.Primary.onClick.Invoke();yield return null;
                Assert.AreEqual(0,wallet.Coins);Assert.AreEqual(3,stock.GetCharges((SortBooster)i));Assert.IsNull(game.ActivePopup);Assert.AreEqual(SortPhase.Playing,board.Board.Phase);Assert.AreEqual(revision,board.BoardRevision.Value);
            }
        }
        private void Capture(int index,int width,int height)
        {
            var camera=game.BoardCamera;var old=camera.targetTexture;var active=RenderTexture.active;float oldSize=camera.orthographicSize;var oldScale=game.Background.transform.localScale;
            var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);var handle=typeof(CanvasScaler).GetMethod("Handle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            try {
                camera.targetTexture=target;float aspect=(float)width/height;camera.orthographicSize=Mathf.Max(6.4f,4.3f/aspect);game.SendMessage("FitBackground",aspect);foreach(var scaler in Object.FindObjectsOfType<CanvasScaler>())handle.Invoke(scaler,null);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllBytes("TestResults/purchase-"+index+"-"+width+".png",image.EncodeToPNG());
            } finally {
                camera.targetTexture=old;camera.orthographicSize=oldSize;game.Background.transform.localScale=oldScale;RenderTexture.active=active;Object.Destroy(image);target.Release();Object.Destroy(target);foreach(var scaler in Object.FindObjectsOfType<CanvasScaler>())handle.Invoke(scaler,null);Canvas.ForceUpdateCanvases();
            }
        }
        [UnityTest] public IEnumerator InsufficientCoinsRetainsOfferAndCloseResumesWithoutCharging()
        {
            Deplete(0);Click(game.BoosterBadges[0]);yield return null;Click(game.ActivePopup.Primary);yield return null;Assert.AreEqual(0,wallet.Coins);
            Deplete(1);Click(game.BoosterBadges[1]);yield return null;var popup=game.ActivePopup;Assert.IsNotNull(popup);Assert.IsFalse(popup.Primary.IsInteractable());popup.Primary.onClick.Invoke();Assert.AreEqual(0,wallet.Coins);Assert.AreEqual(0,stock.GetCharges(SortBooster.Shuffle));
            Click(popup.Secondary);yield return null;Assert.IsNull(game.ActivePopup);Assert.AreEqual(SortPhase.Playing,board.Board.Phase);Assert.AreEqual(0,stock.GetCharges(SortBooster.Shuffle));
        }
        [UnityTest] public IEnumerator PurchaseWalletAdds800UpdatesEligibilityAndRejectsClosedAndStaleCallbacks()
        {
            Deplete(0);Assert.IsTrue(game.TryOpenBoosterPurchase(SortBooster.Vacuum));yield return new WaitForSecondsRealtime(.3f);var first=game.ActivePopup;Click(first.Primary);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(0,wallet.Coins);
            Deplete(1);Assert.IsTrue(game.TryOpenBoosterPurchase(SortBooster.Shuffle));yield return new WaitForSecondsRealtime(.3f);var popup=game.ActivePopup;Assert.AreSame(first,popup);Assert.AreEqual("0",popup.PurchaseCoinBalance.text);Assert.IsFalse(popup.Primary.IsInteractable());
            Click(popup.PurchaseCoinPlus);Assert.AreEqual(800,wallet.Coins);Assert.AreEqual("800",popup.PurchaseCoinBalance.text);Assert.IsFalse(popup.Primary.IsInteractable());
            Click(popup.PurchaseCoinPlus);Assert.AreEqual(1600,wallet.Coins);Assert.AreEqual("1600",popup.PurchaseCoinBalance.text);Assert.IsTrue(popup.Primary.IsInteractable());
            yield return new WaitForSecondsRealtime(.15f);Capture(8,720,1280);Capture(8,768,1024);
            Click(popup.Secondary);popup.PurchaseCoinPlus.onClick.Invoke();Assert.AreEqual(1600,wallet.Coins);yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(game.TryOpenBoosterPurchase(SortBooster.Shuffle));yield return new WaitForSecondsRealtime(.3f);Click(popup.PurchaseCoinPlus);Assert.AreEqual(2400,wallet.Coins);
            app.SendCommand(new StartSortLevelCommand(2));popup.PurchaseCoinPlus.onClick.Invoke();Assert.AreEqual(2400,wallet.Coins);yield return null;
        }
        [UnityTest] public IEnumerator MissingResourceDoesNotPauseAndStaleAttemptCannotPurchase()
        {
            Deplete(1);var field=typeof(SortGameViewController).GetField("BoosterPurchasePopupKey");Assert.IsNotNull(field);string key=(string)field.GetValue(game);field.SetValue(game,"Prefabs/UI/Popups/MissingPurchase");
            LogAssert.Expect(LogType.Error,new Regex("Booster purchase popup resource missing"));game.BoosterBadges[1].onClick.Invoke();Assert.IsNull(game.ActivePopup);Assert.AreEqual(SortPhase.Playing,board.Board.Phase);Assert.AreEqual(1600,wallet.Coins);
            field.SetValue(game,key);Click(game.BoosterBadges[1]);yield return null;var popup=game.ActivePopup;Assert.IsNotNull(popup);app.SendCommand(new StartSortLevelCommand(2));yield return null;
            popup.Primary.onClick.Invoke();Assert.AreEqual(1600,wallet.Coins);Assert.AreEqual(0,stock.GetCharges(SortBooster.Shuffle));Assert.IsNull(game.ActivePopup);Assert.AreEqual(SortPhase.Playing,board.Board.Phase);
        }
    }
}
