using System.Collections;
using System.IO;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class ShelfRowRefillPlayModeTests : WalletTestBase
    {
        /// <summary>Exercises real drag input, rear input rejection and grouped visual promotion in all levels.</summary>
        [UnityTest] public IEnumerator AllThreeLevelsKeepRearStockBehindUntilTheFrontRowIsEmpty()
        {
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            for(int level=1;level<=3;level++) {
                AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(level));yield return null;yield return null;
                yield return new WaitForSecondsRealtime(.25f);
                var left=model.Board.GetBehind(4,0);var right=model.Board.GetBehind(4,1);
                var rear=view.PieceRoot.Find("Piece_"+left.InstanceId+"_Kind"+left.Kind);Assert.IsNotNull(rear);
                Vector3 rearPosition=rear.position;
                Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0))));
                Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(5,2))));
                yield return new WaitForSecondsRealtime(.25f);
                Assert.IsTrue(model.Board.GetFront(4,0).IsEmpty);Assert.AreEqual(left.InstanceId,model.Board.GetBehind(4,0).InstanceId);
                Assert.Less(Vector3.Distance(rearPosition,rear.position),.001f);
                Assert.AreSame(view.OriginalRearMaterials[left.Kind],rear.Find("OriginalArtwork").GetComponentInChildren<Renderer>().sharedMaterial);
                Assert.IsFalse(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(rear.position)),"Rear stock must not be draggable");
                Assert.IsFalse(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0))));
                if(level==1) Capture(view,"row-refill-waiting.png");
                Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,1))));
                Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(level==3?1:0,2))));
                Assert.AreEqual(left.InstanceId,model.Board.GetFront(4,0).InstanceId);
                Assert.AreEqual(right.InstanceId,model.Board.GetFront(4,1).InstanceId);
                Assert.AreEqual(level==3?30:24,model.Board.RemainingCount);
                yield return new WaitForSecondsRealtime(.35f);
                Assert.Less(Vector3.Distance(SortGameViewController.SlotWorld(4,0),rear.position),.01f);
                Assert.AreSame(view.OriginalFrontMaterials[left.Kind],rear.Find("OriginalArtwork").GetComponentInChildren<Renderer>().sharedMaterial);
                if(level==1) Capture(view,"row-refill-promoted.png");
            }
        }

        /// <summary>Stores production camera evidence of a vacancy and its eventual whole-row refill.</summary>
        private static void Capture(SortGameViewController view,string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera=view.BoardCamera;var target=new RenderTexture(720,1280,24);var old=camera.targetTexture;
            var active=RenderTexture.active;float size=camera.orthographicSize;var scale=view.Background.transform.localScale;
            var image=new Texture2D(720,1280,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.orthographicSize=Mathf.Max(6.4f,4.3f/(720f/1280f));view.SendMessage("FitBackground",720f/1280f);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,720,1280),0,0);image.Apply();
            Directory.CreateDirectory("TestResults");File.WriteAllBytes("TestResults/"+name,image.EncodeToPNG());
            camera.targetTexture=old;camera.orthographicSize=size;view.Background.transform.localScale=scale;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
