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
    public sealed class PauseSettingsPlayModeTests : WalletTestBase
    {
        private SortGameViewController view;private SortBoardModel model;

        /// <summary>Only the test fixture resets session preferences and charges.</summary>
        [UnitySetUp] public IEnumerator Load()
        {
            BoosterSessionTestState.Reset();var settings=Preferences();
            if(settings!=null) {
                var values=(bool[])settings.GetType().GetField("enabled",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(settings);
                for(int i=0;i<values.Length;i++) values[i]=true;
                ((BindableProperty<int>)settings.GetType().GetField("Revision").GetValue(settings)).Value++;
            }
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();model=AllSortedApp.Interface.GetModel<SortBoardModel>();
        }

        /// <summary>The real gear opens a paused, illustrated modal and X resumes the same board.</summary>
        [UnityTest] public IEnumerator IllustratedPauseOpensLazilyAndCloseResumesWithoutChangingBoard()
        {
            Assert.AreEqual(0,view.CachedPopupCount);view.PauseButton.onClick.Invoke();yield return null;
            var popup=view.ActivePopup;Assert.IsNotNull(Settings(popup));Assert.AreEqual(SortPhase.Paused,model.Board.Phase);
            Assert.IsTrue(view.Layers.GameplayBlocked);Assert.IsFalse(popup.Title.gameObject.activeSelf);
            StringAssert.Contains("Background",popup.transform.Find("PopupContent").GetComponent<Image>().sprite.name);
            StringAssert.Contains("Close",popup.Primary.image.sprite.name);StringAssert.Contains("Restart",popup.Secondary.image.sprite.name);
            var board=model.Board;float seconds=board.RemainingSeconds;AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=20});
            Assert.AreEqual(seconds,board.RemainingSeconds);Capture("pause-settings-phone.png",720,1280);Capture("pause-settings-ipad.png",768,1024);
            popup.Primary.onClick.Invoke();yield return null;
            Assert.IsNull(view.ActivePopup);Assert.IsFalse(view.Layers.GameplayBlocked);Assert.AreSame(board,model.Board);Assert.AreEqual(SortPhase.Playing,board.Phase);
        }

        /// <summary>Each toggle owns its pointer, changes once and survives reuse without duplicate listeners.</summary>
        [UnityTest] public IEnumerator IndependentTogglesRetainStateAndHiddenCallbacksDoNothing()
        {
            view.RequestPause();yield return null;Canvas.ForceUpdateCanvases();var popup=view.ActivePopup;var settings=Settings(popup);Assert.IsNotNull(settings);
            var buttons=Field<Button[]>(settings,"ToggleButtons");var marks=Field<GameObject[]>(settings,"DisabledMarks");
            Assert.AreEqual(3,buttons.Length);for(int i=0;i<3;i++) Assert.IsFalse(marks[i].activeSelf);
            var revision=(BindableProperty<int>)Preferences().GetType().GetField("Revision").GetValue(Preferences());int start=revision.Value;
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(view.BoardCamera,buttons[1].transform.position)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.Greater(hits.Count,0);
            var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);Assert.AreSame(buttons[1].gameObject,target);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.08f);
            Assert.Less(buttons[1].transform.localScale.x,1);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(start+1,revision.Value);Assert.IsTrue(marks[1].activeSelf);Assert.IsFalse(marks[0].activeSelf);Assert.IsFalse(marks[2].activeSelf);
            Canvas.ForceUpdateCanvases();Assert.Greater(marks[1].GetComponent<Image>().color.a,0,"OFF overlay must be visible.");
            popup.Primary.onClick.Invoke();buttons[1].onClick.Invoke();Assert.AreEqual(start+1,revision.Value);
            view.RequestPause();Assert.AreSame(popup,view.ActivePopup);Assert.IsTrue(marks[1].activeSelf);
            buttons[0].onClick.Invoke();buttons[2].onClick.Invoke();Assert.AreEqual(start+3,revision.Value);
            Assert.IsTrue(marks[0].activeSelf);Assert.IsTrue(marks[2].activeSelf);Capture("pause-settings-off.png",720,1280);
            buttons[1].onClick.Invoke();Assert.AreEqual(start+4,revision.Value);Assert.IsFalse(marks[1].activeSelf);
        }

        /// <summary>Restart resets this level while keeping spent booster allowances; Home tests cover the new Quit flow.</summary>
        [UnityTest] public IEnumerator RestartKeepsCurrentLevelAndSpentBoosters()
        {
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(3));yield return null;
            view.BoosterButtons[1].onClick.Invoke();view.BoosterButtons[3].onClick.Invoke();yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(2,BoosterSessionTestState.Charges(1));Assert.AreEqual(2,BoosterSessionTestState.Charges(3));
            view.RequestPause();var popup=view.ActivePopup;var settings=Settings(popup);Assert.IsNotNull(settings);
            var toggles=Field<Button[]>(settings,"ToggleButtons");toggles[0].onClick.Invoke();
            popup.Secondary.onClick.Invoke();yield return SceneEntryTestWait.Complete();
            Assert.IsNull(view.ActivePopup);Assert.IsFalse(view.Layers.GameplayBlocked);Assert.AreEqual(3,model.LevelNumber.Value);
            Assert.AreEqual(30,model.Board.RemainingCount);Assert.Greater(model.Board.RemainingSeconds,179);Assert.AreEqual(0,model.Board.FreezeSeconds);
            Assert.AreEqual(2,BoosterSessionTestState.Charges(1));Assert.AreEqual(2,BoosterSessionTestState.Charges(3));
            view.RequestPause();Assert.IsTrue(Field<GameObject[]>(Settings(view.ActivePopup),"DisabledMarks")[0].activeSelf);
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();view.RequestPause();Assert.IsTrue(Field<GameObject[]>(Settings(view.ActivePopup),"DisabledMarks")[0].activeSelf);
            Assert.AreEqual(2,BoosterSessionTestState.Charges(1));Assert.AreEqual(2,BoosterSessionTestState.Charges(3));
        }

        private static Component Settings(SortPopupView popup) {return popup?popup.GetComponent("SortPauseSettingsView"):null;}
        private static T Field<T>(Component settings,string name) {return (T)settings.GetType().GetField(name).GetValue(settings);}
        private static object Preferences()
        {
            var type=typeof(SortBoardModel).Assembly.GetType("AllSorted.SortSettingsModel");
            return type==null?null:typeof(IArchitecture).GetMethod("GetModel").MakeGenericMethod(type).Invoke(AllSortedApp.Interface,null);
        }

        /// <summary>Renders the actual camera UI at phone/tablet aspects.</summary>
        private void Capture(string name,int width,int height)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera=view.BoardCamera;var old=camera.targetTexture;var active=RenderTexture.active;float size=camera.orthographicSize;var scale=view.Background.transform.localScale;
            var target=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);float aspect=(float)width/height;
            camera.targetTexture=target;camera.orthographicSize=Mathf.Max(6.4f,4.3f/aspect);view.SendMessage("FitBackground",aspect);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllBytes("TestResults/"+name,image.EncodeToPNG());
            camera.targetTexture=old;camera.orthographicSize=size;view.Background.transform.localScale=scale;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
