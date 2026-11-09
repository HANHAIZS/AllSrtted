using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace AllSorted.PlayTests
{
    public static class SceneEntryTestWait
    {
        public static IEnumerator Complete(){float deadline=Time.realtimeSinceStartup+10;do{yield return null;}while((SortLevelTransitionView.Active || (UnityEngine.Object.FindObjectOfType<SortGameViewController>() && UnityEngine.Object.FindObjectOfType<SortGameViewController>().IsEntering)) && Time.realtimeSinceStartup<deadline);Assert.IsNull(SortLevelTransitionView.Active,"Entry overlay did not finish.");var game=UnityEngine.Object.FindObjectOfType<SortGameViewController>();if(game)Assert.IsFalse(game.IsEntering,"Local entry did not finish.");yield return null;}
    }
    public sealed class LevelEntryTransitionPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        [UnitySetUp] public IEnumerator Load(){if(SortLevelTransitionView.Active)SortLevelTransitionView.Active.Finish();yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;home=Object.FindObjectOfType<SortHomeViewController>();}
        private static void Set(object o,string name,object value){typeof(SortLevelTransitionView).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);}
        [UnityTest] public IEnumerator ActualAsyncEntryHoldsTimerAndInputThenReleasesWithSelectedBoostersOnce()
        {
            var app=AllSortedApp.Interface;var wallet=app.GetModel<SortWalletModel>();var model=app.GetModel<SortBoardModel>();app.SendCommand(new StartSortLevelCommand(2));Assert.IsTrue(home.OpenEntryPopup());yield return null;
            home.ActiveEntryPopup.transform.Find("PopupContent/Item1").GetComponent<Button>().onClick.Invoke();home.ActiveEntryPopup.Primary.onClick.Invoke();Assert.IsTrue(home.IsLoading);Assert.IsFalse(home.TryStartGame());var bridge=SortLevelTransitionView.Active;Assert.IsNotNull(bridge);
            float deadline=Time.realtimeSinceStartup+5;SortGameViewController game=null;while(!game && Time.realtimeSinceStartup<deadline){yield return null;game=Object.FindObjectOfType<SortGameViewController>();}
            Assert.IsNotNull(game);Assert.IsTrue(game.IsEntering);Assert.IsTrue(model.IsEntering);Assert.AreEqual(210,model.Board.RemainingSeconds);Assert.AreEqual("03:30",game.TimerLabel.text);
            var endpoints=(System.Collections.Generic.Dictionary<Material,Color>)typeof(SortGameViewController).GetField("entryColors",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);foreach(var color in endpoints.Values)Assert.AreEqual(1,color.a);
            Assert.IsFalse(game.TryBeginDrag(Vector2.zero));game.RequestPause();Assert.IsNull(game.ActivePopup);
            app.SendCommand(new TickSortCommand{DeltaTime=999});Assert.AreEqual(210,model.Board.RemainingSeconds);Assert.AreEqual(2,wallet.GetEntryCharges(1));Assert.AreEqual(5,wallet.Energy);
            yield return SceneEntryTestWait.Complete();Assert.IsFalse(game.IsEntering,"Presentation still entering");Assert.IsFalse(model.IsEntering,"Domain entry gate held");Assert.IsFalse(game.Layers.GameplayBlocked,"World gate held; phase="+model.Board.Phase+", popup="+(game.ActivePopup?game.ActivePopup.PopupId:"none"));Assert.AreEqual(Vector3.zero,game.PieceRoot.position);Assert.LessOrEqual(model.Board.RemainingSeconds,210);Assert.Greater(model.Board.RemainingSeconds,209);
            Assert.IsTrue(game.Layers.Hosts[0].alpha>.99f);Assert.IsTrue(game.Layers.Hosts[1].alpha>.99f);app.SendCommand(new TickSortCommand{DeltaTime=1});Assert.Less(model.Board.RemainingSeconds,209.1f);
            foreach(var renderer in game.PieceRoot.GetComponentsInChildren<Renderer>())StringAssert.DoesNotContain("EntryFade",renderer.sharedMaterial.name);
        }
        [UnityTest] public IEnumerator DelayedReadinessShowsReferenceOnlyWhileWaitingAndHasNoMinimumDwell()
        {
            var bridge=SortLevelTransitionView.Prepare(home.LevelTransitionKey,home.HomeCamera);Assert.IsNotNull(bridge);Set(bridge,"started",Time.realtimeSinceStartup-.1f);Set(bridge,"running",true);bridge.gameObject.SetActive(true);yield return null;Assert.IsFalse(bridge.IsLoadingPageVisible);
            Set(bridge,"started",Time.realtimeSinceStartup-.3f);yield return null;Assert.IsTrue(bridge.IsLoadingPageVisible);Assert.AreEqual("Loading",bridge.LoadingPage.sprite.name);Capture(home.HomeCamera,bridge,"level-loading-phone.png",720,1280);Capture(home.HomeCamera,bridge,"level-loading-ipad.png",768,1024);
            bridge.MarkSceneReady();Assert.IsFalse(bridge.IsLoadingPageVisible);bridge.Finish();yield return null;Assert.IsNull(SortLevelTransitionView.Active);
        }
        [UnityTest] public IEnumerator MissingTransitionResourceKeepsHomeAndRequestUnchanged()
        {
            home.LevelTransitionKey="MissingLevelTransition";LogAssert.Expect(LogType.Error,"Level transition resource missing: MissingLevelTransition");Assert.IsFalse(home.TryStartGame());Assert.IsFalse(home.IsLoading);Assert.IsNull(SortLevelTransitionView.Active);Assert.AreEqual(0,AllSortedApp.Interface.GetModel<SortHomeNavigationModel>().PendingLevel);Assert.IsTrue(home.StartButton.IsInteractable());yield return null;
        }
        [UnityTest] public IEnumerator DisableDuringArrivalRestoresGateMaterialsAndOwnedOverlay()
        {
            Assert.IsTrue(home.TryStartGame());SortGameViewController game=null;while(!game){yield return null;game=Object.FindObjectOfType<SortGameViewController>();}Assert.IsTrue(game.IsEntering);game.enabled=false;yield return null;Assert.IsFalse(AllSortedApp.Interface.GetModel<SortBoardModel>().IsEntering);Assert.IsNull(SortLevelTransitionView.Active);Assert.AreEqual(Vector3.zero,game.PieceRoot.position);Assert.AreEqual(1,game.Layers.Hosts[0].alpha);game.enabled=true;
        }
        [UnityTest] public IEnumerator SuspensionDuringArrivalFinishesVisualsButKeepsBoardPaused()
        {
            Assert.IsTrue(home.TryStartGame());SortGameViewController game=null;while(!game){yield return null;game=Object.FindObjectOfType<SortGameViewController>();}Assert.IsTrue(game.IsEntering);game.SendMessage("OnApplicationPause",true);yield return null;
            var model=AllSortedApp.Interface.GetModel<SortBoardModel>();Assert.IsFalse(model.IsEntering);Assert.IsFalse(game.IsEntering);Assert.AreEqual(SortPhase.Paused,model.Board.Phase);Assert.AreEqual(180,model.Board.RemainingSeconds);AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=100});Assert.AreEqual(180,model.Board.RemainingSeconds);Assert.IsNull(SortLevelTransitionView.Active);
        }
        [UnityTest] public IEnumerator CaptureEntryFramesAndProtectPopupElasticSettings()
        {
            Assert.IsTrue(home.TryStartGame());SortGameViewController game=null;while(!game){yield return null;game=Object.FindObjectOfType<SortGameViewController>();}var bridge=SortLevelTransitionView.Active;Assert.IsNotNull(bridge);
            Set(bridge,"readyAt",.15f);Set(bridge,"started",Time.realtimeSinceStartup-.15f);game.SendMessage("AdvanceSceneEntrance");bridge.SendMessage("RefreshPresentation");Capture(game.BoardCamera,bridge,"entry-start-phone.png",720,1280);
            Set(bridge,"started",Time.realtimeSinceStartup-.26f);game.SendMessage("AdvanceSceneEntrance");bridge.SendMessage("RefreshPresentation");Capture(game.BoardCamera,bridge,"entry-middle-phone.png",720,1280);Capture(game.BoardCamera,bridge,"entry-middle-ipad.png",768,1024);
            var transforms=game.PieceRoot.GetComponentsInChildren<Renderer>();var positions=new Vector3[transforms.Length];for(int i=0;i<transforms.Length;i++)positions[i]=transforms[i].transform.position;
            var targets=(System.Collections.Generic.Dictionary<Transform,Vector3>)typeof(SortGameViewController).GetField("entryPieceScales",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            Set(bridge,"started",Time.realtimeSinceStartup-.33f);game.SendMessage("AdvanceSceneEntrance");foreach(var piece in targets)Assert.Less(Vector3.Distance(piece.Key.localScale,piece.Value*1.15f),.001f);
            for(int i=0;i<transforms.Length;i++)Assert.AreEqual(positions[i],transforms[i].transform.position);Assert.AreEqual(Vector3.zero,game.PieceRoot.position);
            Capture(game.BoardCamera,bridge,"entry-peak-phone.png",720,1280);Capture(game.BoardCamera,bridge,"entry-peak-ipad.png",768,1024);
            Set(bridge,"started",Time.realtimeSinceStartup-.43f);game.SendMessage("AdvanceSceneEntrance");yield return null;Capture(game.BoardCamera,null,"entry-end-phone.png",720,1280);
            game.RequestPause();Assert.IsNotNull(game.ActivePopup);Assert.AreEqual(.25f,game.ActivePopup.OpenSeconds);Assert.AreEqual(.15f,game.ActivePopup.CloseSeconds);
        }
        private static void Capture(Camera camera,SortLevelTransitionView bridge,string name,int width,int height)
        {
            var old=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);Canvas canvas=bridge?bridge.GetComponent<Canvas>():null;
            var handle=typeof(CanvasScaler).GetMethod("Handle",BindingFlags.NonPublic|BindingFlags.Instance);try{
                if(canvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=8;}
                camera.targetTexture=target;foreach(var scaler in Object.FindObjectsOfType<CanvasScaler>())handle.Invoke(scaler,null);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllBytes("TestResults/"+name,texture.EncodeToPNG());
            }finally{if(canvas){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(texture);target.Release();Object.Destroy(target);foreach(var scaler in Object.FindObjectsOfType<CanvasScaler>())handle.Invoke(scaler,null);Canvas.ForceUpdateCanvases();}
        }
    }
}
