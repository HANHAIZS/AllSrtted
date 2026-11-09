using NUnit.Framework;
using QFramework;
namespace AllSorted.Tests
{
    public sealed class CampaignCommandTests : WalletTestBase
    {
        /// <summary>Starts each command scenario from the first authored level.</summary>
        [SetUp] public void Reset() {AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));}

        /// <summary>Catches starting the wrong content or publishing a stale match target.</summary>
        [Test] public void ExplicitStartSelectsThirdLevelAndItsTenMatches()
        {
            var app=AllSortedApp.Interface;app.SendCommand(new StartSortLevelCommand(3));var model=app.GetModel<SortBoardModel>();
            Assert.AreEqual(3,model.LevelNumber.Value);Assert.AreEqual(30,model.Board.RemainingCount);Assert.AreEqual(10,model.TotalMatches);
        }

        /// <summary>Timeout replay must not silently return the player to level one.</summary>
        [Test] public void RetryAfterTimeoutKeepsTheCurrentLevel()
        {
            var app=AllSortedApp.Interface;app.SendCommand(new StartSortLevelCommand(2));app.SendCommand(new TickSortCommand{DeltaTime=181});
            Assert.AreEqual(SortPhase.Lost,app.GetModel<SortBoardModel>().Phase.Value);
            app.SendCommand(new StartSortLevelCommand());var model=app.GetModel<SortBoardModel>();
            Assert.AreEqual(2,model.LevelNumber.Value);Assert.AreEqual(24,model.Board.RemainingCount);Assert.AreEqual(180,model.Seconds.Value);
        }

        /// <summary>Playing, paused and failed boards cannot use the next-level command.</summary>
        [Test] public void AdvanceRejectsEveryNonWinningPhase()
        {
            var app=AllSortedApp.Interface;var model=app.GetModel<SortBoardModel>();
            var playing=new AdvanceSortLevelCommand();app.SendCommand(playing);Assert.IsFalse(playing.Accepted);
            app.SendCommand(new PauseSortCommand(true));var paused=new AdvanceSortLevelCommand();app.SendCommand(paused);Assert.IsFalse(paused.Accepted);
            app.SendCommand(new PauseSortCommand(false));app.SendCommand(new TickSortCommand{DeltaTime=181});
            var lost=new AdvanceSortLevelCommand();app.SendCommand(lost);Assert.IsFalse(lost.Accepted);Assert.AreEqual(1,model.LevelNumber.Value);
        }

        /// <summary>Completes production command transitions 1, 2, 3 and explicit final start-over.</summary>
        [Test] public void WinningAllThreeLevelsAdvancesAndWrapsToFirst()
        {
            var app=AllSortedApp.Interface;var model=app.GetModel<SortBoardModel>();
            for(int level=1;level<=3;level++) {
                Assert.AreEqual(level,model.LevelNumber.Value);ClearCurrent(level);
                Assert.AreEqual(SortPhase.Won,model.Board.Phase);var next=new AdvanceSortLevelCommand();app.SendCommand(next);
                Assert.IsTrue(next.Accepted);Assert.AreEqual(level==3?1:level+1,model.LevelNumber.Value);
                Assert.AreEqual(SortPhase.Playing,model.Board.Phase);Assert.AreEqual(180,model.Seconds.Value);
            }
        }

        /// <summary>Rejects an invalid level before replacing the model or its current selection.</summary>
        [Test] public void InvalidStartCannotReplaceTheActiveBoard()
        {
            var app=AllSortedApp.Interface;app.SendCommand(new StartSortLevelCommand(2));var model=app.GetModel<SortBoardModel>();var board=model.Board;
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>app.SendCommand(new StartSortLevelCommand(4)));
            Assert.AreSame(board,model.Board);Assert.AreEqual(2,model.LevelNumber.Value);
        }

        /// <summary>A second next-level intent on the freshly playing board cannot skip content.</summary>
        [Test] public void RepeatedAdvanceDoesNotSkipTheNextPlayingLevel()
        {
            var app=AllSortedApp.Interface;ClearCurrent(1);var first=new AdvanceSortLevelCommand();app.SendCommand(first);Assert.IsTrue(first.Accepted);
            var second=new AdvanceSortLevelCommand();app.SendCommand(second);Assert.IsFalse(second.Accepted);
            Assert.AreEqual(2,app.GetModel<SortBoardModel>().LevelNumber.Value);
        }

        /// <summary>Uses explicit known moves against the real Command/System/Model path.</summary>
        public static void ClearCurrent(int level)
        {
            var moves=level==1?SortBoardStateTests.Solution:level==2?ThreeLevelContentTests.Level2Solution:ThreeLevelContentTests.Level3Solution;
            for(int i=0;i<moves.GetLength(0);i++) {
                var move=new MoveSortPieceCommand(moves[i,0],moves[i,1],moves[i,2],moves[i,3]);
                AllSortedApp.Interface.SendCommand(move);Assert.IsTrue(move.Accepted,"Move "+i);
            }
        }
    }
}
