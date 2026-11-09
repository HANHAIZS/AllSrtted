using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using GameFoundation.UI;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class PrototypePlayModeTests : WalletTestBase
    {
        private SortGameViewController view;
        private SortBoardModel model;

        /// <summary>Loads the real authored scene before every runtime boundary test.</summary>
        [UnitySetUp] public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
            yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            Assert.IsNotNull(view);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
        }

        /// <summary>Checks real geometry and authored layers, with no eager modal residency.</summary>
        [UnityTest] public IEnumerator SceneRendersNativePiecesAndDefersPopupLoading()
        {
            Assert.AreEqual(24,view.RenderedPieceCount);Assert.AreEqual(0,view.CachedPopupCount);
            Assert.AreEqual(7,view.Layers.Hosts.Length);
            Assert.IsFalse(view.Layers.Hosts[4].blocksRaycasts);Assert.IsFalse(view.Layers.Hosts[5].blocksRaycasts);
            for(int i=0;i<4;i++) Assert.AreNotEqual("Hidden/InternalErrorShader",view.Palette[i].shader.name);
            yield return null;
            Capture(view.BoardCamera,"prototype-gameplay.png");
            Capture(view.BoardCamera,"prototype-ipad.png",768,1024);
        }

        /// <summary>Drives actual screen-space input into the same commands as user dragging.</summary>
        [UnityTest] public IEnumerator WorldDragRejectsOccupiedAndAcceptsMatchingDrop()
        {
            var camera=view.BoardCamera;
            var source=(Vector2)camera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0));
            Assert.IsTrue(view.TryBeginDrag(source));
            Assert.IsFalse(view.ReleaseDrag(camera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,0))));
            Assert.AreEqual(24,model.Board.RemainingCount);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(view.TryBeginDrag(source));
            Assert.IsTrue(view.ReleaseDrag(camera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));
            Assert.AreEqual(21,model.Board.RemainingCount);Assert.AreEqual(1,model.Board.Matches);
            yield return null;
        }

        /// <summary>Confirms lazy reuse, modal gate, pause clock, resume and owner cleanup.</summary>
        [UnityTest] public IEnumerator PausePopupReusesInstanceAndBlocksWorldInput()
        {
            Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0))));
            view.RequestPause();var popup=view.ActivePopup;Assert.IsNotNull(popup);
            Assert.IsFalse(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));
            int id=popup.GetInstanceID();float seconds=model.Board.RemainingSeconds;
            Assert.IsTrue(view.Layers.GameplayBlocked);Assert.IsFalse(view.Layers.Hosts[0].interactable);
            Assert.IsFalse(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,0))));
            yield return null;Assert.AreEqual(seconds,model.Board.RemainingSeconds);
            view.ResumeLevel();Assert.IsFalse(view.Layers.GameplayBlocked);
            view.RequestPause();Assert.AreEqual(id,view.ActivePopup.GetInstanceID());Assert.AreEqual(1,view.CachedPopupCount);
            view.RestartLevel();yield return null;Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>Off-board release and explicit cancellation preserve both slot identities and revision.</summary>
        [UnityTest] public IEnumerator AbortedDragNeverRevealsOrMutatesTheBoard()
        {
            int front=model.Board.GetFront(4,0).InstanceId,behind=model.Board.GetBehind(4,0).InstanceId;
            int revision=model.BoardRevision.Value;
            var source=view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0));
            Assert.IsTrue(view.TryBeginDrag(source));Assert.IsFalse(view.ReleaseDrag(new Vector2(-1000,-1000)));
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(view.TryBeginDrag(source));view.CancelDrag();
            Assert.IsFalse(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));
            Assert.AreEqual(front,model.Board.GetFront(4,0).InstanceId);Assert.AreEqual(behind,model.Board.GetBehind(4,0).InstanceId);
            Assert.AreEqual(revision,model.BoardRevision.Value);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>A missing resource preserves the playable state and can be repaired and retried.</summary>
        [UnityTest] public IEnumerator MissingPauseResourceDoesNotLeaveAnInvisibleGate()
        {
            string key=view.PausePopupKey;view.PausePopupKey="Prefabs/UI/Popups/MissingTestPopup";
            LogAssert.Expect(LogType.Error,new Regex("Sorting popup resource missing:"));view.RequestPause();
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.IsFalse(view.Layers.GameplayBlocked);
            view.PausePopupKey=key;view.RequestPause();Assert.IsNotNull(view.ActivePopup);
            view.ResumeLevel();yield return null;
        }

        /// <summary>Suspension freezes the clock and cancels input even if optional pause UI fails.</summary>
        [UnityTest] public IEnumerator SuspensionDuringDragSurvivesMissingPauseResource()
        {
            var source=view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0));
            Assert.IsTrue(view.TryBeginDrag(source));
            view.PausePopupKey="Prefabs/UI/Popups/MissingSuspensionPopup";
            view.SendMessage("OnApplicationPause",true);
            Assert.AreEqual(SortPhase.Paused,model.Board.Phase);
            Assert.IsNull(view.ActivePopup);Assert.AreEqual(0,view.CachedPopupCount);
            float seconds=model.Board.RemainingSeconds;
            Assert.IsFalse(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));
            yield return null;Assert.AreEqual(seconds,model.Board.RemainingSeconds);
            view.SendMessage("OnApplicationPause",false);yield return null;
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        [UnityTest] public IEnumerator AppSuspensionKeepsPopupAbsentAndReturnsToPlaying()
        {
            view.SendMessage("OnApplicationPause",true);
            Assert.IsNull(view.ActivePopup,"Backgrounding must not open the pause UI");Assert.AreEqual(0,view.CachedPopupCount);
            float seconds=model.Board.RemainingSeconds;AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=10});Assert.AreEqual(seconds,model.Board.RemainingSeconds);
            view.SendMessage("OnApplicationPause",false);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.IsFalse(view.Layers.GameplayBlocked);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=1});Assert.Less(model.Board.RemainingSeconds,seconds);
            view.PauseButton.onClick.Invoke();Assert.IsNotNull(view.ActivePopup.GetComponent<SortPauseSettingsView>());
            view.ActivePopup.Primary.onClick.Invoke();Assert.AreEqual(SortPhase.Playing,model.Board.Phase);yield return null;
        }

        [UnityTest] public IEnumerator FocusChangeDoesNotOpenPopupOrDismissManualPause()
        {
            view.SendMessage("OnApplicationFocus",false);Assert.IsNull(view.ActivePopup);Assert.AreEqual(0,view.CachedPopupCount);
            view.SendMessage("OnApplicationFocus",true);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
            view.PauseButton.onClick.Invoke();var popup=view.ActivePopup;Assert.IsNotNull(popup);
            view.SendMessage("OnApplicationFocus",false);view.SendMessage("OnApplicationPause",true);
            view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
            Assert.AreSame(popup,view.ActivePopup);Assert.AreEqual(SortPhase.Paused,model.Board.Phase);
            popup.Primary.onClick.Invoke();Assert.AreEqual(SortPhase.Playing,model.Board.Phase);yield return null;
        }

        [UnityTest] public IEnumerator OverlappingSuspensionsWaitForBothReturnNotifications()
        {
            if(Application.isBatchMode)yield break;
            view.SendMessage("OnApplicationFocus",false);view.SendMessage("OnApplicationPause",true);
            view.SendMessage("OnApplicationPause",false);
            Assert.AreEqual(SortPhase.Paused,model.Board.Phase,"Focus is still lost after app resume");Assert.IsNull(view.ActivePopup);
            view.SendMessage("OnApplicationFocus",true);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
            view.SendMessage("OnApplicationPause",true);view.SendMessage("OnApplicationFocus",false);
            view.SendMessage("OnApplicationFocus",true);
            Assert.AreEqual(SortPhase.Paused,model.Board.Phase,"App is still suspended after focus return");Assert.IsNull(view.ActivePopup);
            view.SendMessage("OnApplicationPause",false);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
        }

        /// <summary>A missing terminal resource retains a replay route and retries after key repair.</summary>
        [UnityTest] public IEnumerator MissingResultResourceCanRecoverAndReplay()
        {
            string key=view.ResultPopupKey;view.ResultPopupKey="Prefabs/UI/Popups/MissingResultPopup";
            LogAssert.Expect(LogType.Error,new Regex("Sorting popup resource missing:"));
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            Assert.AreEqual(SortPhase.Lost,model.Board.Phase);Assert.IsNull(view.ActivePopup);
            view.PauseButton.onClick.Invoke();yield return null;
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            view.ResultPopupKey=key;yield return null;yield return null;
            Assert.IsNotNull(view.ActivePopup);view.ActivePopup.Primary.onClick.Invoke();yield return null;
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
        }

        /// <summary>Completes a real scene through Commands, observes result UI, and replays the same level.</summary>
        [UnityTest] public IEnumerator CompleteLevelShowsWinAndReplayResetsEverything()
        {
            int[,] moves={{4,0,0,2},{0,0,3,2},{0,1,2,2},{2,0,1,2},{3,0,5,2},{4,1,3,0},{4,0,3,2},{1,1,4,0},{5,1,4,2},{5,0,0,0},{5,2,0,1},{5,1,0,2},{1,0,2,0},{5,0,2,2}};
            for(int i=0;i<moves.GetLength(0);i++) {
                var command=new MoveSortPieceCommand(moves[i,0],moves[i,1],moves[i,2],moves[i,3]);
                AllSortedApp.Interface.SendCommand(command);Assert.IsTrue(command.Accepted);yield return null;
            }
            yield return null;Assert.AreEqual(SortPhase.Won,model.Board.Phase);Assert.AreEqual(0,model.Board.RemainingCount);
            Assert.AreEqual("ALL SORTED!",view.ActivePopup.Title.text);
            yield return new WaitForSecondsRealtime(.30f);Capture(view.BoardCamera,"prototype-win.png");
            var finishedPopup=view.ActivePopup;finishedPopup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(1.4f);
            Assert.AreEqual(24,model.Board.RemainingCount);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
            Assert.IsFalse(view.Layers.GameplayBlocked);
            int revision=model.BoardRevision.Value;finishedPopup.Primary.onClick.Invoke();
            Assert.AreEqual(revision,model.BoardRevision.Value);
        }

        /// <summary>Checks actual timeout presentation and replay rather than domain state alone.</summary>
        [UnityTest] public IEnumerator TimeoutShowsLossAndReplayRecovers()
        {
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            Assert.AreEqual(SortPhase.Lost,model.Board.Phase);Assert.IsNotNull(view.ActivePopup);
            view.ActivePopup.Primary.onClick.Invoke();yield return null;
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>Unloads real UI before its controller and checks the router has no stale modal.</summary>
        [UnityTest] public IEnumerator SceneUnloadClearsModalWithoutAccessingDestroyedHosts()
        {
            view.RequestPause();var router=AllSortedApp.Interface.GetUtility<UiPopupRouter>();
            var oldScene=view.gameObject.scene;
            Object.Destroy(view.Layers.gameObject);yield return null;
            Object.Destroy(view.gameObject);yield return null;
            Assert.IsNull(router.ActiveView);
            SceneManager.CreateScene("EmptyTeardownTarget");
            yield return SceneManager.UnloadSceneAsync(oldScene);yield return null;
            Assert.IsNull(router.ActiveView);
        }

        /// <summary>Renders the actual scene camera, including its camera-space UI, for visual inspection.</summary>
        private static void Capture(Camera camera,string name,int width=720,int height=1280)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var target=new RenderTexture(width,height,24);var previous=camera.targetTexture;float oldSize=camera.orthographicSize;
            var active=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.orthographicSize=Mathf.Max(6.4f,4.3f/((float)width/height));
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            string folder=Path.Combine(Application.dataPath,"../TestResults");Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());
            camera.targetTexture=previous;camera.orthographicSize=oldSize;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
