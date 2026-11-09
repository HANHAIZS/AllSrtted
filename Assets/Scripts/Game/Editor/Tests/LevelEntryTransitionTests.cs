using NUnit.Framework;
using QFramework;
namespace AllSorted.Tests
{
    public sealed class LevelEntryTransitionTests : WalletTestBase
    {
        [Test] public void FastEntryFadesHomeBeforeElasticArrivalAndNeverShowsLoading()
        {
            Assert.AreEqual(1,SortLevelTransitionTiming.HomeAlpha(0));Assert.AreEqual(0,SortLevelTransitionTiming.HomeAlpha(.15f));
            Assert.IsFalse(SortLevelTransitionTiming.ShowLoading(.4f,true));Assert.IsFalse(SortLevelTransitionTiming.ShowLoading(.24f,false));Assert.IsTrue(SortLevelTransitionTiming.ShowLoading(.26f,false));
            Assert.AreEqual(0,SortLevelTransitionTiming.ArrivalProgress(.15f,.05f));Assert.AreEqual(1,SortLevelTransitionTiming.ArrivalProgress(.43f,.05f));
        }
        [Test] public void SlowReadinessHasNoExtraHomeFadeOrMinimumLoadingDwell()
        {
            Assert.IsTrue(SortLevelTransitionTiming.ShowLoading(.6f,false));Assert.IsFalse(SortLevelTransitionTiming.ShowLoading(.6f,true));
            Assert.AreEqual(0,SortLevelTransitionTiming.ArrivalProgress(.6f,.6f));Assert.AreEqual(1,SortLevelTransitionTiming.ArrivalProgress(.88f,.6f));
            Assert.Greater(SortLevelTransitionTiming.ArrivalProgress(.725f,.6f),.8f);
        }
        [Test] public void MerchandiseExpandsTo115PercentThenSettlesAtTheFullDuration()
        {
            Assert.AreEqual(0,SortLevelTransitionTiming.MerchandiseScale(.15f,.05f));
            Assert.AreEqual(1.15f,SortLevelTransitionTiming.MerchandiseScale(.33f,.05f),.0001f);
            Assert.Greater(SortLevelTransitionTiming.MerchandiseScale(.38f,.05f),1);Assert.Less(SortLevelTransitionTiming.MerchandiseScale(.38f,.05f),1.15f);
            Assert.AreEqual(1,SortLevelTransitionTiming.MerchandiseScale(.43f,.05f),.0001f);Assert.AreEqual(1,SortLevelTransitionTiming.ArrivalFraction(.43f,.05f));
            Assert.Less(SortLevelTransitionTiming.ArrivalFraction(.42f,.05f),1);Assert.AreEqual(0,SortLevelTransitionTiming.MerchandiseScale(.5f,.6f));
        }
        [Test] public void EntryGateBlocksTimerGameplayAndPauseButResumesWithoutCatchup()
        {
            var app=AllSortedApp.Interface;var board=app.GetModel<SortBoardModel>();app.SendCommand(new StartSortLevelCommand(1));int revision=board.BoardRevision.Value;
            app.SendCommand(new SetSortEntryPresentationCommand(true));Assert.IsTrue(board.IsEntering);app.SendCommand(new TickSortCommand{DeltaTime=999});Assert.AreEqual(180,board.Board.RemainingSeconds);
            var use=new UseSortBoosterCommand(SortBooster.Shuffle);app.SendCommand(use);Assert.IsFalse(use.Accepted);app.SendCommand(new PauseSortCommand(true));Assert.AreEqual(SortPhase.Playing,board.Board.Phase);Assert.AreEqual(revision,board.BoardRevision.Value);
            app.SendCommand(new SetSortEntryPresentationCommand(false));app.SendCommand(new TickSortCommand{DeltaTime=1});Assert.AreEqual(179,board.Board.RemainingSeconds);
        }
        [Test] public void ANewDirectLevelClearsAnyInterruptedEntryGate()
        {
            var app=AllSortedApp.Interface;var model=app.GetModel<SortBoardModel>();app.SendCommand(new SetSortEntryPresentationCommand(true));app.SendCommand(new StartSortLevelCommand(2));Assert.IsFalse(model.IsEntering);
        }
    }
}
