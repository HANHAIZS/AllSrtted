using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace AllSorted.PlayTests
{
    public sealed class BoosterPlayModeTests : WalletTestBase
    {
        private SortGameViewController view;private SortBoardModel model;

        /// <summary>Tests against the user's renamed actual scene and authored buttons.</summary>
        [UnitySetUp] public IEnumerator Load()
        {
            BoosterSessionTestState.Reset();
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            view=Object.FindObjectOfType<SortGameViewController>();model=AllSortedApp.Interface.GetModel<SortBoardModel>();
        }

        /// <summary>Requires the supplied icon order inside the unchanged green button bases.</summary>
        [UnityTest] public IEnumerator FourButtonsContainTransparentIconsAndVacuumClearsOneFrontTriple()
        {
            string[] names={"Vacuum","Shuffle","GoldenCard","FreezeWatch"};
            for(int i=0;i<4;i++) {
                var button=Button(i);Assert.IsTrue(button.interactable);
                var icon=button.transform.Find("Icon").GetComponent<Image>();Assert.IsNotNull(icon.sprite);
                StringAssert.Contains(names[i],icon.sprite.name);Assert.IsTrue(icon.preserveAspect);Assert.IsFalse(icon.raycastTarget);
                var rect=(RectTransform)button.transform;Assert.AreEqual(128,rect.sizeDelta.x);Assert.AreEqual(128,rect.sizeDelta.y);
                Assert.AreEqual((i+.5f)/4f,rect.anchorMin.x);
            }
            Capture("boosters-phone.png",720,1280);Capture("boosters-ipad.png",768,1024);
            Button(0).onClick.Invoke();yield return new WaitForSecondsRealtime(.12f);Capture("boosters-vacuum.png",720,1280);
            Assert.LessOrEqual(model.Board.RemainingCount,21);Assert.GreaterOrEqual(model.Board.Matches,1);
            Assert.AreEqual(24,model.Board.RemainingCount+model.Board.Matches*3);
        }

        /// <summary>Dice changes positions without losing stock or filling front vacancies.</summary>
        [UnityTest] public IEnumerator ShuffleRearrangesTheStockAndPreservesInitialGaps()
        {
            string before=Layout();Assert.IsTrue(Button(1).interactable);Button(1).onClick.Invoke();yield return null;
            Assert.AreNotEqual(before,Layout());Assert.AreEqual(24,model.Board.RemainingCount);
            for(int s=0;s<6;s++) Assert.IsTrue(model.Board.GetFront(s,2).IsEmpty);
        }

        /// <summary>The actual card button draws matching groups and renders their nine IDs as playable cards.</summary>
        [UnityTest] public IEnumerator CardChangesNineItemsWithLockedInputAndRealCardPrefabs()
        {
            var originalKinds=new Dictionary<int,int>();
            for(int s=0;s<6;s++) for(int c=0;c<3;c++) for(int d=0;d<2;d++) {
                var p=d==0?model.Board.GetFront(s,c):model.Board.GetBehind(s,c);
                if(!p.IsEmpty) originalKinds.Add(p.InstanceId,p.Kind);
            }
            Assert.IsTrue(Button(2).interactable);Button(2).onClick.Invoke();yield return null;yield return null;
            Assert.IsTrue(view.IsCardTransformationPlaying);
            Assert.IsTrue(view.Layers.GameplayBlocked);
            Assert.IsTrue(model.Board.CardTransformationPending);
            Assert.IsFalse(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0))));
            view.RequestPause();Assert.IsNull(view.ActivePopup);
            var ids=model.Board.BoosterPieceIds;
            for(int group=0;group<3;group++) Assert.AreEqual(1,ids.Skip(group*3).Take(3).Select(id=>originalKinds[id]).Distinct().Count());
            Assert.AreEqual(2,BoosterSessionTestState.Charges(2));
            int gold=0;
            for(int s=0;s<6;s++) for(int c=0;c<3;c++) for(int d=0;d<2;d++) {
                var p=d==0?model.Board.GetFront(s,c):model.Board.GetBehind(s,c);
                if(p.IsEmpty || p.Kind!=4) continue;gold++;
                Assert.Contains(p.InstanceId,ids.ToArray());
            }
            Assert.AreEqual(9,gold);Assert.AreEqual(24,model.Board.RemainingCount);
            yield return new WaitForSecondsRealtime(.47f);
            Capture("card-in-flight-phone.png",720,1280);
            Capture("card-in-flight-tablet.png",768,1024);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.IsFalse(view.IsCardTransformationPlaying);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(view.Layers.GameplayBlocked);
            Assert.IsFalse(model.Board.CardTransformationPending);
            Assert.IsNull(GameObject.Find("Card Transformation VFX"));
            var cards=view.PieceRoot.Cast<Transform>().Where(piece=>piece.name.EndsWith("_Kind4")).ToArray();
            Assert.AreEqual(9,cards.Length);
            foreach(var card in cards)
            {
                Assert.IsNotNull(card.GetComponent<BoxCollider>());
                var renderer=card.Find("Visual").GetComponent<MeshRenderer>();
                Assert.AreEqual(6,renderer.sharedMaterials.Length);
                Assert.AreEqual("CardFrontMat",renderer.sharedMaterials[4].name);
                Assert.AreEqual("CardBackMat",renderer.sharedMaterials[5].name);
            }
            Capture("card-final-phone.png",720,1280);
            Capture("card-final-tablet.png",768,1024);
        }

        [UnityTest] public IEnumerator CardEffectInterruptedByDisableCommitsAndReleasesTheGate()
        {
            Button(2).onClick.Invoke();
            yield return new WaitForSecondsRealtime(.42f);
            Assert.IsTrue(model.Board.CardTransformationPending);
            view.enabled=false;
            Assert.IsFalse(model.Board.CardTransformationPending);
            Assert.IsFalse(view.Layers.GameplayBlocked);
            Assert.IsNull(GameObject.Find("Card Transformation VFX"));
            Assert.AreEqual(9,model.Board.BoosterPieceIds.Distinct().Count());
            view.enabled=true;
            yield return null;
            Assert.AreEqual(9,view.PieceRoot.Cast<Transform>().Count(piece=>piece.name.EndsWith("_Kind4")));
        }

        /// <summary>Watch leaves drag input active, obeys pause, precisely expires and clears on retry.</summary>
        [UnityTest] public IEnumerator WatchFreezesOnlyTheCountdownWhileDragAndPauseStillWork()
        {
            Assert.IsTrue(Button(3).interactable);float clock=model.Board.RemainingSeconds;
            Button(3).onClick.Invoke();Assert.AreEqual(0,Freeze(),"Freeze begins at the watch impact.");
            yield return new WaitForSecondsRealtime(.5f);
            view.enabled=false;clock=model.Board.RemainingSeconds;
            Assert.Greater(Freeze(),14.7f);Assert.LessOrEqual(Freeze(),15);Assert.IsFalse(Button(3).interactable);
            Capture("boosters-freeze.png",720,1280);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=4});Assert.AreEqual(clock,model.Board.RemainingSeconds);
            Assert.IsTrue(view.TryBeginDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(4,0))));
            Assert.IsTrue(view.ReleaseDrag(view.BoardCamera.WorldToScreenPoint(SortGameViewController.SlotWorld(0,2))));
            Assert.AreEqual(21,model.Board.RemainingCount);
            view.RequestPause();float frozen=Freeze();AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=30});
            Assert.AreEqual(frozen,Freeze());Assert.AreEqual(clock,model.Board.RemainingSeconds);
            view.ResumeLevel();AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=frozen+.5f});
            Assert.AreEqual(0,Freeze());Assert.AreEqual(clock-.5f,model.Board.RemainingSeconds,.001f);
            view.enabled=true;Button(3).onClick.Invoke();view.RestartLevel();Assert.AreEqual(0,Freeze());Assert.AreEqual(180,model.Board.RemainingSeconds);
            yield return null;
        }

        /// <summary>Hidden/direct callbacks cannot bypass pause, terminal or owner destruction gates.</summary>
        [UnityTest] public IEnumerator BoosterCallbacksCannotMutateAPausedOrFailedBoard()
        {
            view.RequestPause();int revision=model.BoardRevision.Value;
            for(int i=0;i<4;i++) Button(i).onClick.Invoke();Assert.AreEqual(revision,model.BoardRevision.Value);
            view.ResumeLevel();AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;
            revision=model.BoardRevision.Value;
            for(int i=0;i<4;i++) Button(i).onClick.Invoke();Assert.AreEqual(revision,model.BoardRevision.Value);
        }

        /// <summary>Finds actual authored button instances rather than mock callbacks.</summary>
        private Button Button(int index) {return view.Layers.Hosts[0].transform.Find("SafeArea/BottomButtons/FunctionButton_"+(index+1)).GetComponent<Button>();}
        /// <summary>Reads the countdown effect through its public domain property.</summary>
        private float Freeze() {var p=typeof(SortBoardState).GetProperty("FreezeSeconds");Assert.IsNotNull(p);return (float)p.GetValue(model.Board);}
        /// <summary>Captures the visible front/rear kind arrangement for a real shuffle assertion.</summary>
        private string Layout()
        {
            var result=new System.Text.StringBuilder();
            for(int s=0;s<6;s++) for(int c=0;c<3;c++) {result.Append(model.Board.GetFront(s,c).Kind);result.Append(':');result.Append(model.Board.GetBehind(s,c).Kind);result.Append(';');}
            return result.ToString();
        }

        /// <summary>Renders the production camera and its camera-space HUD at the target device aspect.</summary>
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
