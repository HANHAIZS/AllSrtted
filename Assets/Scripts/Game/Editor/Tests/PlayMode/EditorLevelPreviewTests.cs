#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using QFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class EditorLevelPreviewTests : WalletTestBase
    {
        private const string LaunchKey="AllSorted.EditorPreview.StartLevel";

        /// <summary>Uses real menu registrations and checks reset/recovery from paused and failed boards.</summary>
        [UnityTest] public IEnumerator EditorMenusSelectAllThreeLevelsAndReleaseOldModals()
        {
            SessionState.EraseInt(LaunchKey);
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            var view=Object.FindObjectOfType<SortGameViewController>();var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            view.RequestPause();Assert.IsNotNull(view.ActivePopup);
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("All Sorted/关卡选择/第 2 关"));yield return null;
            Assert.AreEqual(2,model.LevelNumber.Value);Assert.AreEqual("LEVEL 02",view.LevelLabel.text);
            Assert.AreEqual(24,model.Board.RemainingCount);Assert.AreEqual(180,model.Seconds.Value);
            Assert.IsNull(view.ActivePopup);Assert.IsFalse(view.Layers.GameplayBlocked);Assert.AreEqual(SortPhase.Playing,model.Board.Phase);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=181});yield return null;Assert.IsNotNull(view.ActivePopup);
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("All Sorted/关卡选择/第 3 关"));yield return null;
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual("LEVEL 03",view.LevelLabel.text);
            Assert.AreEqual(30,model.Board.RemainingCount);Assert.IsNull(view.ActivePopup);Assert.IsFalse(view.Layers.GameplayBlocked);
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("All Sorted/关卡选择/第 1 关"));yield return null;
            Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual("LEVEL 01",view.LevelLabel.text);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>A launch choice affects exactly one scene entry and cannot leak into later sessions.</summary>
        [UnityTest] public IEnumerator EditorLaunchChoiceIsConsumedOnceThenSceneDefaultsToFirstLevel()
        {
            SessionState.SetInt(LaunchKey,3);
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            var model=AllSortedApp.Interface.GetModel<SortBoardModel>();var view=Object.FindObjectOfType<SortGameViewController>();
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual("LEVEL 03",view.LevelLabel.text);Assert.AreEqual(30,model.Board.RemainingCount);
            Assert.AreEqual(0,SessionState.GetInt(LaunchKey,0));
            yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);yield return null;yield return null;
            Assert.AreEqual(1,model.LevelNumber.Value);Assert.AreEqual(24,model.Board.RemainingCount);
        }

        /// <summary>Always remove test-owned startup intent even after an assertion fails.</summary>
        [UnityTearDown] public IEnumerator ClearChoice() {SessionState.EraseInt(LaunchKey);yield return null;}
    }
}
#endif
