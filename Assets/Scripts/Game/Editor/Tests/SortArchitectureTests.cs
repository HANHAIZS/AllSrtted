using NUnit.Framework;
using QFramework;
namespace AllSorted.Tests
{
    public sealed class SortArchitectureTests : WalletTestBase
    {
        /// <summary>Proves the generated project architecture really owns the game model.</summary>
        [Test] public void ProjectArchitectureRegistersSortBoardModel()
        {
            Assert.IsNotNull(AllSortedApp.Interface.GetModel<SortBoardModel>());
        }

        /// <summary>Exercises real start/move commands and the observable revision.</summary>
        [Test] public void CommandsMoveAndPublishAuthoritativeState()
        {
            var app=AllSortedApp.Interface;app.SendCommand(new StartSortLevelCommand(1));
            var model=app.GetModel<SortBoardModel>();int revision=model.BoardRevision.Value;
            var move=new MoveSortPieceCommand(4,0,0,2);app.SendCommand(move);
            Assert.IsTrue(move.Accepted);Assert.AreEqual(21,model.Board.RemainingCount);
            Assert.AreEqual(1,model.MatchCount.Value);Assert.Greater(model.BoardRevision.Value,revision);
        }

        /// <summary>Verifies the clock command obeys pause and replay restores the session.</summary>
        [Test] public void PauseAndStartCommandsControlClockAndReset()
        {
            var app=AllSortedApp.Interface;app.SendCommand(new StartSortLevelCommand(1));
            var model=app.GetModel<SortBoardModel>();app.SendCommand(new PauseSortCommand(true));
            app.SendCommand(new TickSortCommand{DeltaTime=40});Assert.AreEqual(180,model.Seconds.Value);
            app.SendCommand(new PauseSortCommand(false));app.SendCommand(new TickSortCommand{DeltaTime=181});
            Assert.AreEqual(SortPhase.Lost,model.Phase.Value);
            app.SendCommand(new StartSortLevelCommand(1));Assert.AreEqual(24,model.Board.RemainingCount);
            Assert.AreEqual(SortPhase.Playing,model.Phase.Value);
        }
    }
}
