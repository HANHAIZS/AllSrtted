using NUnit.Framework;
namespace AllSorted.Tests
{
    public sealed class ThreeLevelContentTests : WalletTestBase
    {
        public static readonly int[,] Level2Solution={{0,0,1,2},{1,0,0,0},{2,0,0,2},{0,0,1,0},{1,0,0,0},{4,0,0,2},{3,0,1,0},{5,1,1,2},{4,1,2,0},{4,0,2,2},{3,1,4,0},{5,0,4,2},{2,0,3,2},{3,0,2,0},{5,0,3,0},{5,1,2,2}};
        public static readonly int[,] Level3Solution={{0,1,1,2},{1,1,0,1},{2,0,1,1},{2,1,4,2},{1,0,2,2},{0,0,1,0},{3,0,1,2},{0,2,3,0},{3,1,0,0},{5,0,0,2},{4,1,1,1},{3,2,4,1},{5,1,4,2},{5,0,1,2},{3,0,4,0},{5,1,4,2}};
        /// <summary>Rejects missing/incorrect level-two data by replaying a complete legal solution.</summary>
        [Test] public void SecondLevelCanBeClearedWithoutBoosters()
        {
            var board=PrototypeLevel.Create(2);Assert.AreEqual(24,board.RemainingCount);
            Replay(board,Level2Solution);Assert.AreEqual(8,board.Matches);Assert.AreEqual(SortPhase.Won,board.Phase);
        }

        /// <summary>Exercises thirty pieces, including deeper hidden stacks, to complete level three.</summary>
        [Test] public void ThirdLevelCanBeClearedWithoutBoosters()
        {
            var board=PrototypeLevel.Create(3);Assert.AreEqual(30,board.RemainingCount);
            Replay(board,Level3Solution);Assert.AreEqual(10,board.Matches);Assert.AreEqual(0,board.RemainingCount);
            Assert.AreEqual(SortPhase.Won,board.Phase);
        }

        /// <summary>Moving one board cannot mutate another instance of its independently authored content.</summary>
        [Test] public void NewLevelInstancesDoNotShareMutableStacks()
        {
            var first=PrototypeLevel.Create(3);var second=PrototypeLevel.Create(3);
            int id=second.GetFront(1,0).InstanceId,kind=second.GetFront(1,0).Kind;
            Assert.IsTrue(first.TryMove(1,0,2,2));Assert.AreEqual(id,second.GetFront(1,0).InstanceId);
            Assert.AreEqual(kind,second.GetFront(1,0).Kind);Assert.AreEqual(30,second.RemainingCount);
        }

        /// <summary>Out-of-range content must fail explicitly instead of silently substituting another level.</summary>
        [Test] public void UnknownLevelIsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>PrototypeLevel.Create(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>PrototypeLevel.Create(4));
        }

        /// <summary>Replays explicit fixture actions against the production rules, checking every commit.</summary>
        public static void Replay(SortBoardState board,int[,] moves)
        {
            for(int i=0;i<moves.GetLength(0);i++) Assert.IsTrue(board.TryMove(moves[i,0],moves[i,1],moves[i,2],moves[i,3]),"Move "+i);
        }
    }
}
