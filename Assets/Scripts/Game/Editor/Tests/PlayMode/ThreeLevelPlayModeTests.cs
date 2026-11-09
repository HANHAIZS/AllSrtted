using System.Collections;
using System.IO;
using GameFoundation.UI;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class ThreeLevelPlayModeTests : WalletTestBase
    {
        private SortGameViewController view;private SortBoardModel model;
        private static readonly int[,] Moves1={{4,0,0,2},{0,0,3,2},{0,1,2,2},{2,0,1,2},{3,0,5,2},{4,1,3,0},{4,0,3,2},{1,1,4,0},{5,1,4,2},{5,0,0,0},{5,2,0,1},{5,1,0,2},{1,0,2,0},{5,0,2,2}};
        private static readonly int[,] Moves2={{0,0,1,2},{1,0,0,0},{2,0,0,2},{0,0,1,0},{1,0,0,0},{4,0,0,2},{3,0,1,0},{5,1,1,2},{4,1,2,0},{4,0,2,2},{3,1,4,0},{5,0,4,2},{2,0,3,2},{3,0,2,0},{5,0,3,0},{5,1,2,2}};
        private static readonly int[,] Moves3={{0,1,1,2},{1,1,0,1},{2,0,1,1},{2,1,4,2},{1,0,2,2},{0,0,1,0},{3,0,1,2},{0,2,3,0},{3,1,0,0},{5,0,0,2},{4,1,1,1},{3,2,4,1},{5,1,4,2},{5,0,1,2},{3,0,4,0},{5,1,4,2}};
        /// <summary>Checks every scenario against the actual existing scene and its selected UI assets.</summary>
        [UnitySetUp] public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
        }

        /// <summary>Catches wrong primary actions, stale visual IDs, missing labels and skipped levels.</summary>
        [UnityTest] public IEnumerator ThreeWinsAdvanceLabelsThenStartOverWithoutStaleCallbacks()
        {
            for(int level=1;level<=3;level++) {
                Assert.AreEqual(level,model.LevelNumber.Value);Assert.AreEqual("LEVEL "+level.ToString("00"),view.LevelLabel.text);
                var front=model.Board.GetFront(0,0);
                Assert.IsNotNull(view.PieceRoot.Find("Piece_"+front.InstanceId+"_Kind"+front.Kind));
                Assert.AreEqual(level==3?30:24,model.Board.RemainingCount);
                Assert.AreEqual(level==3?28:24,view.RenderedPieceCount);
                if(level>1) Capture(view,"prototype-level-0"+level+".png");
                var moves=Moves(level);
                for(int i=0;i<moves.GetLength(0);i++) {
                    var move=new MoveSortPieceCommand(moves[i,0],moves[i,1],moves[i,2],moves[i,3]);
                    AllSortedApp.Interface.SendCommand(move);Assert.IsTrue(move.Accepted,"Move "+i);yield return null;
                }
                yield return null;var popup=view.ActivePopup;Assert.IsNotNull(popup);
                Assert.AreEqual("Continue",popup.PrimaryCaption.text);
                Assert.AreEqual("VictoryPopup",popup.PopupId);
                int coins=AllSortedApp.Interface.GetModel<SortWalletModel>().Coins;
                popup.Primary.onClick.Invoke();popup.Primary.onClick.Invoke();yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(level,model.LevelNumber.Value,"Reward flight must finish before advancing");
                Assert.AreEqual(coins,AllSortedApp.Interface.GetModel<SortWalletModel>().Coins);
                yield return new WaitForSecondsRealtime(1.3f);
                Assert.AreEqual(coins+30,AllSortedApp.Interface.GetModel<SortWalletModel>().Coins);
                Assert.AreEqual(level<3?level+1:1,model.LevelNumber.Value);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
                int revision=model.BoardRevision.Value;popup.Primary.onClick.Invoke();Assert.AreEqual(revision,model.BoardRevision.Value);
                Assert.IsFalse(view.Layers.GameplayBlocked);
            }
        }

        /// <summary>Both pause restart and timeout replay on level three must retain its thirty-piece layout.</summary>
        [UnityTest] public IEnumerator ThirdLevelPauseRestartAndTimeoutReplayKeepTheLevel()
        {
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(3));yield return null;yield return null;
            Assert.AreEqual("LEVEL 03",view.LevelLabel.text);view.PauseButton.onClick.Invoke();
            Assert.AreEqual(SortPhase.Paused,model.Board.Phase);view.ActivePopup.Secondary.onClick.Invoke();yield return SceneEntryTestWait.Complete();
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual(30,model.Board.RemainingCount);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            Assert.AreEqual(SortPhase.Lost,model.Board.Phase);view.ActivePopup.Primary.onClick.Invoke();yield return null;
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual(30,model.Board.RemainingCount);
            Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.IsFalse(view.Layers.GameplayBlocked);
        }

        /// <summary>Fresh scene entry starts level one instead of retaining a previous scene's progression.</summary>
        [UnityTest] public IEnumerator ReloadingGameplaySceneStartsAtFirstLevel()
        {
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(3));yield return null;
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();
            Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual("LEVEL 01",view.LevelLabel.text);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>Selects test-only explicit solutions without deriving them from the production content.</summary>
        private static int[,] Moves(int level) {return level==1?Moves1:level==2?Moves2:Moves3;}

        /// <summary>Captures the real scene using its production backdrop fit and authored HUD.</summary>
        private static void Capture(SortGameViewController view,string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera=view.BoardCamera;var target=new RenderTexture(720,1280,24);var previous=camera.targetTexture;
            var active=RenderTexture.active;float oldSize=camera.orthographicSize;var oldScale=view.Background.transform.localScale;
            var image=new Texture2D(720,1280,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.orthographicSize=Mathf.Max(6.4f,4.3f/(720f/1280f));view.SendMessage("FitBackground",720f/1280f);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,720,1280),0,0);image.Apply();Directory.CreateDirectory("TestResults");
            File.WriteAllBytes("TestResults/"+name,image.EncodeToPNG());
            camera.targetTexture=previous;camera.orthographicSize=oldSize;view.Background.transform.localScale=oldScale;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
