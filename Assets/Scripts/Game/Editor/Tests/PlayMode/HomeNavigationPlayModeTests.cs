using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public sealed class HomeNavigationPlayModeTests : WalletTestBase
    {
        private SortHomeViewController home;
        private readonly bool[] originalSettings=new bool[3];
        [UnitySetUp] public IEnumerator LoadHome()
        {
            var app=AllSortedApp.Interface;var settings=app.GetModel<SortSettingsModel>();
            for(int i=0;i<3;i++) originalSettings[i]=settings.GetEnabled((SortSetting)i);
            BoosterSessionTestState.Reset();app.SendCommand(new ClearSortHomeEntryCommand());app.SendCommand(new StartSortLevelCommand(1));
            Assert.IsTrue(File.Exists("Assets/Scenes/HomeScene.unity"));
            yield return SceneManager.LoadSceneAsync("HomeScene",LoadSceneMode.Single);yield return null;yield return null;
            home=Object.FindObjectOfType<SortHomeViewController>();Assert.IsNotNull(home);Canvas.ForceUpdateCanvases();home.RefreshLayout();
        }
        [UnityTearDown] public IEnumerator RestoreSession()
        {
            Time.timeScale=1;var app=AllSortedApp.Interface;app.SendCommand(new ClearSortHomeEntryCommand());var settings=app.GetModel<SortSettingsModel>();
            for(int i=0;i<3;i++) if(originalSettings[i]!=settings.GetEnabled((SortSetting)i)) app.SendCommand(new ToggleSortSettingCommand((SortSetting)i));
            yield return null;
        }
        [UnityTest] public IEnumerator DisabledSettingShadeCoversEachWholeButtonWithoutBlockingPointers()
        {
            home.TabButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);var settings=AllSortedApp.Interface.GetModel<SortSettingsModel>();
            for(int i=0;i<3;i++) {
                var b=home.SettingButtons[i];var shade=home.OffMarks[i].GetComponent<Image>();var r=(RectTransform)shade.transform;
                Assert.AreEqual(((RectTransform)b.transform).rect.size,r.rect.size,"Shade must cover entire button "+i);Assert.AreSame(b.image.sprite,shade.sprite);Assert.AreEqual(b.image.preserveAspect,shade.preserveAspect);Assert.IsFalse(shade.raycastTarget);
                if(settings.GetEnabled((SortSetting)i))b.onClick.Invoke();Assert.IsTrue(home.OffMarks[i].activeSelf);
                Assert.AreSame(b.gameObject,PointerTarget(RectTransformUtility.WorldToScreenPoint(home.HomeCamera,b.transform.position)));
            }
            Capture("home-settings-all-off-phone.png",720,1280);Capture("home-settings-all-off-ipad.png",768,1024);
            for(int i=0;i<3;i++){home.SettingButtons[i].onClick.Invoke();Assert.IsFalse(home.OffMarks[i].activeSelf);}
        }
        [UnityTest] public IEnumerator AuthoredHomeSceneContainsRealPagesAndStartsOnHome()
        {
            AssertPage(1);Assert.AreEqual("Level 1",home.LevelLabel.text);Assert.AreEqual(1,Object.FindObjectsOfType<EventSystem>().Length);
            Assert.AreEqual(1,Object.FindObjectsOfType<Camera>().Length);Assert.IsNull(Object.FindObjectOfType<SortGameViewController>());
            foreach(var popup in Resources.FindObjectsOfTypeAll<SortPopupView>()) Assert.IsFalse(popup.gameObject.scene.IsValid() && popup.gameObject.scene.isLoaded,"Menu must not eagerly instantiate popups.");
            Capture("home-phone.png",720,1280);Capture("home-ipad.png",768,1024);yield return null;
        }
        [UnityTest] public IEnumerator AllDirectionsAndRetargetsKeepStableHitAreasAndOnlyFinalPageInteractive()
        {
            var app=AllSortedApp.Interface;int revision=app.GetModel<SortSettingsModel>().Revision.Value;
            foreach(int next in new[]{0,2,1,2,0,1}) {
                var point=RectTransformUtility.WorldToScreenPoint(home.HomeCamera,home.TabButtons[next].transform.TransformPoint(new Vector3(0,70,0)));
                Assert.AreSame(home.TabButtons[next].gameObject,PointerTarget(point));home.TabButtons[next].onClick.Invoke();Assert.IsTrue(home.IsTransitioning);
                home.StartButton.onClick.Invoke();for(int i=0;i<3;i++) home.SettingButtons[i].onClick.Invoke();
                Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);Assert.AreEqual(revision,app.GetModel<SortSettingsModel>().Revision.Value);
                yield return new WaitForSecondsRealtime(.35f);AssertPage(next);
            }
            Time.timeScale=0;home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.08f);
            var position=home.SelectionPlate.anchoredPosition;home.TabButtons[2].onClick.Invoke();Assert.AreEqual(position,home.SelectionPlate.anchoredPosition);
            home.TabButtons[0].onClick.Invoke();home.TabButtons[1].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);AssertPage(1);
            Assert.IsFalse(home.SelectPage(SortHomeTab.Home));Assert.IsFalse(home.IsTransitioning);Time.timeScale=1;
        }
        [UnityTest] public IEnumerator SettingsUseRealPointerAndSharedStateWhileHiddenCallbacksAreRejected()
        {
            home.TabButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);AssertPage(2);Canvas.ForceUpdateCanvases();
            var model=AllSortedApp.Interface.GetModel<SortSettingsModel>();bool before=model.GetEnabled(SortSetting.Sound);int revision=model.Revision.Value;
            var button=home.SettingButtons[1];var data=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(home.HomeCamera,button.transform.position)};
            Assert.AreSame(button.gameObject,PointerTarget(data.position));ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.07f);
            Assert.Less(button.transform.localScale.x,1);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(!before,model.GetEnabled(SortSetting.Sound));Assert.AreEqual(revision+1,model.Revision.Value);Assert.AreEqual(before,home.OffMarks[1].activeSelf);
            yield return new WaitForSecondsRealtime(.25f);Capture("home-settings-phone.png",720,1280);Capture("home-settings-ipad.png",768,1024);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);button.onClick.Invoke();Assert.AreEqual(revision+1,model.Revision.Value);Capture("home-shop-phone.png",720,1280);Capture("home-shop-ipad.png",768,1024);
            home.TabButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.AreEqual(before,home.OffMarks[1].activeSelf);
        }
        [UnityTest] public IEnumerator StartAndQuitRoundTripAllLevelsWithSpentQuotaAndSharedSettings()
        {
            var app=AllSortedApp.Interface;var model=app.GetModel<SortBoardModel>();var settings=app.GetModel<SortSettingsModel>();
            app.SendCommand(new ToggleSortSettingCommand(SortSetting.Music));bool music=settings.GetEnabled(SortSetting.Music);
            for(int level=1;level<=3;level++) {
                app.SendCommand(new StartSortLevelCommand(level));Assert.AreEqual("Level "+level,home.LevelLabel.text);
                home.StartButton.onClick.Invoke();home.StartButton.onClick.Invoke();Assert.IsNotNull(home.ActiveEntryPopup);home.ActiveEntryPopup.Primary.onClick.Invoke();yield return SceneEntryTestWait.Complete();
                Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name);var game=Object.FindObjectOfType<SortGameViewController>();
                Assert.AreEqual(level,model.LevelNumber.Value);Assert.Greater(model.Board.RemainingSeconds,179);Assert.AreEqual(0,app.GetModel<SortHomeNavigationModel>().PendingLevel);
                game.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(3-level,BoosterSessionTestState.Charges(3));game.RequestPause();yield return null;
                var popup=game.ActivePopup;Assert.IsNotNull(popup);var prefs=popup.GetComponent<SortPauseSettingsView>();Assert.AreEqual(!music,prefs.DisabledMarks[0].activeSelf);
                var quit=prefs.QuitButton;quit.onClick.Invoke();quit.onClick.Invoke();game.ActivePopup.Primary.onClick.Invoke();yield return null;yield return null;
                Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);home=Object.FindObjectOfType<SortHomeViewController>();AssertPage(1);
                Assert.AreEqual("Level "+level,home.LevelLabel.text);Assert.AreEqual(3-level,BoosterSessionTestState.Charges(3));Assert.AreEqual(music,settings.GetEnabled(SortSetting.Music));
            }
            Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual(0,model.Board.FreezeSeconds);
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;Assert.AreEqual(1,model.LevelNumber.Value);
        }
        [UnityTest] public IEnumerator MissingScenesKeepTheCurrentPageOrModalAndPermitRecovery()
        {
            home.GameSceneName="MissingHomeGameScene";LogAssert.Expect(LogType.Error,new Regex("Game scene is unavailable"));Assert.IsFalse(home.TryStartGame());AssertPage(1);
            Assert.IsFalse(home.IsLoading);Assert.AreEqual(0,AllSortedApp.Interface.GetModel<SortHomeNavigationModel>().PendingLevel);Assert.IsTrue(home.ErrorLabel.gameObject.activeSelf);
            home.GameSceneName="GameScene";Assert.IsTrue(home.TryStartGame());yield return SceneEntryTestWait.Complete();
            var game=Object.FindObjectOfType<SortGameViewController>();game.RequestPause();var popup=game.ActivePopup;Assert.IsNotNull(popup);
            var field=typeof(SortGameViewController).GetField("HomeSceneName");Assert.IsNotNull(field,"Quit must expose its actual Home scene contract.");field.SetValue(game,"MissingHomeScene");
            popup.GetComponent<SortPauseSettingsView>().QuitButton.onClick.Invoke();popup=game.ActivePopup;
            LogAssert.Expect(LogType.Error,new Regex("Home scene is unavailable"));popup.Primary.onClick.Invoke();yield return null;
            Assert.AreSame(popup,game.ActivePopup);Assert.IsTrue(game.Layers.GameplayBlocked);Assert.AreEqual(SortPhase.Paused,AllSortedApp.Interface.GetModel<SortBoardModel>().Phase.Value);
            field.SetValue(game,"HomeScene");popup.Primary.onClick.Invoke();yield return null;yield return null;Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);
        }
        [UnityTest] public IEnumerator MissingPageAndHiddenStartCannotLeaveTheValidScreen()
        {
            var settings=home.Pages[2];home.Pages[2]=null;LogAssert.Expect(LogType.Error,new Regex("Menu page is unavailable"));Assert.IsFalse(home.SelectPage(SortHomeTab.Settings));Assert.AreEqual(SortHomeTab.Home,home.SelectedTab);
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);Assert.AreEqual(SortHomeTab.Shop,home.SelectedTab);Assert.IsTrue(home.Pages[0].interactable);
            home.Pages[2]=settings;var main=home.Pages[1];home.Pages[1]=null;home.TabButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(SortHomeTab.Settings,home.SelectedTab);Assert.IsTrue(home.Pages[2].interactable);home.Pages[1]=main;
            home.StartButton.onClick.Invoke();Assert.AreEqual("HomeScene",SceneManager.GetActiveScene().name);AssertPage(2);
        }
        private void AssertPage(int selected)
        {
            Assert.IsFalse(home.IsTransitioning);Assert.AreEqual((SortHomeTab)selected,home.SelectedTab);
            for(int i=0;i<3;i++) {Assert.AreEqual(i==selected,home.Pages[i].interactable);Assert.AreEqual(i==selected,home.Pages[i].blocksRaycasts);Assert.AreEqual(i==selected?1:0,home.Pages[i].alpha,.001f);Assert.AreEqual(i==selected?1:0,home.TabLabels[i].color.a,.001f);}
            Assert.AreEqual((selected-1)*home.NavigationArea.rect.width/3,home.SelectionPlate.anchoredPosition.x,.1f);
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator EditorLevelMenuCanEnterGameplayFromTheRunningHome()
        {
            Assert.IsTrue(UnityEditor.EditorApplication.ExecuteMenuItem("All Sorted/关卡选择/第 3 关"));yield return null;yield return null;
            Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name);Assert.AreEqual(3,AllSortedApp.Interface.GetModel<SortBoardModel>().LevelNumber.Value);
        }
#endif
        [UnityTest] public IEnumerator ResizeDuringRetargetRecomputesItsDestinationAndReenableRestoresInput()
        {
            home.TabButtons[0].onClick.Invoke();yield return new WaitForSecondsRealtime(.08f);
            home.NavigationArea.sizeDelta+=new Vector2(120,0);Canvas.ForceUpdateCanvases();home.RefreshLayout();
            home.TabButtons[2].onClick.Invoke();yield return new WaitForSecondsRealtime(.35f);AssertPage(2);
            int revision=AllSortedApp.Interface.GetModel<SortSettingsModel>().Revision.Value;home.enabled=false;home.SettingButtons[0].onClick.Invoke();Assert.AreEqual(revision,AllSortedApp.Interface.GetModel<SortSettingsModel>().Revision.Value);
            home.enabled=true;yield return null;AssertPage(2);
        }
        private static GameObject PointerTarget(Vector2 position)
        {
            var data=new PointerEventData(EventSystem.current){position=position};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Assert.Greater(hits.Count,0);
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        }
        private void Capture(string name,int width,int height)
        {
            var camera=home.HomeCamera;var old=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            camera.targetTexture=target;UpdateScalers();Canvas.ForceUpdateCanvases();home.RefreshLayout();
            foreach(var scale in Object.FindObjectsOfType<SortHomeArtworkScale>()) scale.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();Directory.CreateDirectory("TestResults");File.WriteAllBytes("TestResults/"+name,image.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;Object.Destroy(image);target.Release();Object.Destroy(target);
            UpdateScalers();Canvas.ForceUpdateCanvases();home.RefreshLayout();
        }
        private static void UpdateScalers() {var method=typeof(CanvasScaler).GetMethod("Handle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);foreach(var scaler in Object.FindObjectsOfType<CanvasScaler>()) method.Invoke(scaler,null);}
    }
}
