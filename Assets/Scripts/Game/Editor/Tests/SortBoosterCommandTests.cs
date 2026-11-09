using NUnit.Framework;
using QFramework;
using System.Reflection;

namespace AllSorted.Tests
{
    public sealed class SortBoosterCommandTests : WalletTestBase
    {
        /// <summary>Isolates tests without adding a production inventory reset to level starts.</summary>
        [SetUp] public void ResetSessionInventory()
        {
            var inventory=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            var charges=WalletTestScope.Charges();
            for(int index=0;index<charges.Length;index++) charges[index]=3;
            inventory.Revision.Value++;
        }

        /// <summary>Accepted actions publish once, and repeated active freeze cannot publish another revision.</summary>
        [Test] public void FreezeCommandPublishesOnceAndTickChargesOnlyUnfrozenTime()
        {
            var command=NewCommand(SortBooster.Freeze);var app=AllSortedApp.Interface;
            app.SendCommand(new StartSortLevelCommand(1));var model=app.GetModel<SortBoardModel>();
            int revision=model.BoardRevision.Value;app.SendCommand(command);
            Assert.IsTrue(Accepted(command));Assert.AreEqual(revision+1,model.BoardRevision.Value);
            app.SendCommand(NewCommand(SortBooster.Freeze));Assert.AreEqual(revision+1,model.BoardRevision.Value);
            app.SendCommand(new TickSortCommand{DeltaTime=17});Assert.AreEqual(178,model.Seconds.Value);
            Assert.AreEqual(0f,model.Board.FreezeSeconds);
        }

        /// <summary>Selection results are owned snapshots that survive later board resets and other actions.</summary>
        [Test] public void GoldenCommandCopiesNineAffectedIdsRatherThanReturningSharedState()
        {
            var command=NewCommand(SortBooster.Golden);var app=AllSortedApp.Interface;
            app.SendCommand(new StartSortLevelCommand(1));var model=app.GetModel<SortBoardModel>();
            int revision=model.BoardRevision.Value;app.SendCommand(command);int[] ids=Affected(command);
            Assert.IsTrue(Accepted(command));Assert.AreEqual(9,ids.Length);Assert.AreEqual(revision+1,model.BoardRevision.Value);
            CollectionAssert.AreEqual(model.Board.BoosterPieceIds,ids);
            app.SendCommand(NewCommand(SortBooster.Freeze));Assert.AreEqual(0,model.Board.BoosterPieceIds.Count);
            Assert.AreEqual(9,Affected(command).Length);app.SendCommand(new StartSortLevelCommand(1));
            Assert.AreEqual(9,Affected(command).Length);
        }

        /// <summary>Vacuum must publish the authoritative removal and match count through the existing model.</summary>
        [Test] public void VacuumCommandPublishesRemovalAndMatchCount()
        {
            var command=NewCommand(SortBooster.Vacuum);var app=AllSortedApp.Interface;
            app.SendCommand(new StartSortLevelCommand(1));var model=app.GetModel<SortBoardModel>();
            int revision=model.BoardRevision.Value;app.SendCommand(command);
            Assert.IsTrue(Accepted(command));Assert.AreEqual(3,Affected(command).Length);
            Assert.AreEqual(21,model.Board.RemainingCount);Assert.AreEqual(1,model.MatchCount.Value);
            Assert.AreEqual(revision+1,model.BoardRevision.Value);
        }

        /// <summary>Paused or lost requests cannot publish or overwrite the board's previous successful draw.</summary>
        [Test] public void RejectedCommandDoesNotPublishAndClearsItsOwnStaleResult()
        {
            var command=NewCommand(SortBooster.Golden);var app=AllSortedApp.Interface;
            app.SendCommand(new StartSortLevelCommand(1));var model=app.GetModel<SortBoardModel>();
            app.SendCommand(command);int[] previous=Affected(command);
            app.SendCommand(new PauseSortCommand(true));int revision=model.BoardRevision.Value;app.SendCommand(command);
            Assert.IsFalse(Accepted(command));Assert.AreEqual(0,Affected(command).Length);
            CollectionAssert.AreEqual(previous,model.Board.BoosterPieceIds);Assert.AreEqual(revision,model.BoardRevision.Value);
            app.SendCommand(new PauseSortCommand(false));app.SendCommand(new TickSortCommand{DeltaTime=180});
            app.SendCommand(NewCommand(SortBooster.Freeze));Assert.AreEqual(revision,model.BoardRevision.Value);
            Assert.AreEqual(SortPhase.Lost,model.Phase.Value);
        }

        /// <summary>Retry and explicit level selection both replace ongoing temporary effects.</summary>
        [Test] public void RestartAndLevelSelectionClearFreezeAndSelectedIds()
        {
            var command=NewCommand(SortBooster.Golden);var app=AllSortedApp.Interface;
            app.SendCommand(new StartSortLevelCommand(2));var model=app.GetModel<SortBoardModel>();
            app.SendCommand(command);app.SendCommand(NewCommand(SortBooster.Freeze));
            app.SendCommand(new StartSortLevelCommand());Assert.AreEqual(2,model.LevelNumber.Value);
            Assert.AreEqual(0f,model.Board.FreezeSeconds);Assert.AreEqual(0,model.Board.BoosterPieceIds.Count);
            app.SendCommand(NewCommand(SortBooster.Freeze));app.SendCommand(new StartSortLevelCommand(3));
            Assert.AreEqual(0f,model.Board.FreezeSeconds);Assert.AreEqual(30,model.Board.RemainingCount);
        }

        private static UseSortBoosterCommand NewCommand(SortBooster booster) {return new UseSortBoosterCommand(booster);}
        private static bool Accepted(UseSortBoosterCommand command) {return command.Accepted;}
        private static int[] Affected(UseSortBoosterCommand command) {return command.AffectedPieceIds;}
    }
}
