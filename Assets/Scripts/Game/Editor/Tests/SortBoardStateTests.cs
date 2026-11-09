using NUnit.Framework;

namespace AllSorted.Tests
{
    public sealed class SortBoardStateTests : WalletTestBase
    {
        // Own test data: fourteen legal moves clear the independently authored level.
        public static readonly int[,] Solution = {{4,0,0,2},{0,0,3,2},{0,1,2,2},{2,0,1,2},{3,0,5,2},{4,1,3,0},{4,0,3,2},{1,1,4,0},{5,1,4,2},{5,0,0,0},{5,2,0,1},{5,1,0,2},{1,0,2,0},{5,0,2,2}};

        /// <summary>Checks the authored count, available room and starting clock.</summary>
        [Test] public void InitialLevelHasTwentyFourPiecesAndSixEmptyFrontSlots()
        {
            var b = PrototypeLevel.Create();
            Assert.AreEqual(24, b.RemainingCount);
            Assert.AreEqual(180f, b.RemainingSeconds);
            Assert.AreEqual(SortPhase.Playing, b.Phase);
            for (int s=0; s<6; s++) Assert.IsTrue(b.GetFront(s,2).IsEmpty);
        }

        /// <summary>Rejecting an occupied target must not pop the source stack.</summary>
        [Test] public void InvalidMoveDoesNotRevealOrLosePieces()
        {
            var b=PrototypeLevel.Create(); var piece=b.GetFront(0,0);
            Assert.IsFalse(b.TryMove(0,0,1,0));
            Assert.AreEqual(piece.InstanceId,b.GetFront(0,0).InstanceId);
            Assert.AreEqual(24,b.RemainingCount);
        }

        /// <summary>A removed front stays empty while a neighboring front still occupies the shelf.</summary>
        [Test] public void ValidMoveLeavesVacancyUntilTheOtherFrontsLeave()
        {
            var b=PrototypeLevel.Create(); var neighbor=b.GetFront(4,1);
            Assert.IsTrue(b.TryMove(4,0,5,2));
            Assert.IsTrue(b.GetFront(4,0).IsEmpty);Assert.AreEqual(1,b.GetBehind(4,0).Kind);
            Assert.AreEqual(neighbor.InstanceId,b.GetFront(4,1).InstanceId);
            Assert.AreEqual(0,b.GetFront(5,2).Kind);
            Assert.AreEqual(24,b.RemainingCount);
        }

        /// <summary>One matching triple removes exactly three pieces.</summary>
        [Test] public void ThirdMatchingPieceClearsOneTripleAndRevealsBehind()
        {
            var b=PrototypeLevel.Create(); Assert.IsTrue(b.TryMove(4,0,0,2));
            Assert.AreEqual(21,b.RemainingCount); Assert.AreEqual(1,b.Matches);
            Assert.AreEqual(3,b.GetFront(0,0).Kind); Assert.AreEqual(2,b.GetFront(0,1).Kind);
            Assert.IsTrue(b.GetFront(0,2).IsEmpty);
        }

        /// <summary>A shelf containing different keys never matches by appearance.</summary>
        [Test] public void MixedShelfDoesNotMatch()
        {
            var b=PrototypeLevel.Create();Assert.IsTrue(b.TryMove(4,1,0,2));
            Assert.AreEqual(24,b.RemainingCount);Assert.AreEqual(0,b.Matches);
        }

        /// <summary>Invalid coordinates and empty or identical source targets are non-mutating.</summary>
        [Test] public void InvalidIndicesEmptySourceAndSameSlotAreRejected()
        {
            var b=PrototypeLevel.Create();
            Assert.IsFalse(b.TryMove(-1,0,0,2)); Assert.IsFalse(b.TryMove(0,9,0,2));
            Assert.IsFalse(b.TryMove(0,2,1,2)); Assert.IsFalse(b.TryMove(0,0,0,0));
            Assert.AreEqual(24,b.RemainingCount);
        }

        /// <summary>Newly exposed triples settle until stable in one accepted move.</summary>
        [Test] public void RevealedMatchingTriplesCascadeAtomically()
        {
            var b=new SortBoardState(new[]{new[]{new[]{0,1},new[]{0,1},new[]{2,1}},
                new[]{new[]{2},new[]{2},new int[0]},new[]{new[]{0},new int[0],new int[0]}},180f);
            Assert.IsTrue(b.TryMove(0,2,1,2));Assert.AreEqual(1,b.Matches);
            Assert.IsTrue(b.GetFront(0,2).IsEmpty);Assert.AreEqual(1,b.GetBehind(0,2).Kind);
            Assert.IsTrue(b.TryMove(2,0,0,2));Assert.AreEqual(3,b.Matches);
            Assert.AreEqual(0,b.RemainingCount);Assert.AreEqual(SortPhase.Won,b.Phase);
        }

        /// <summary>A destination match cannot expose a source rear until the source front row empties.</summary>
        [Test] public void DestinationMatchDoesNotRevealPartiallyOccupiedSource()
        {
            var b=new SortBoardState(new[]{new[]{new[]{0,1},new[]{1},new[]{1}},
                new[]{new[]{0},new[]{0},new int[0]}},180f);
            Assert.IsTrue(b.TryMove(0,0,1,2));
            Assert.AreEqual(1,b.Matches);Assert.AreEqual(3,b.RemainingCount);
            Assert.IsTrue(b.GetFront(0,0).IsEmpty);Assert.AreEqual(1,b.GetBehind(0,0).Kind);
            Assert.AreEqual(SortPhase.Playing,b.Phase);
        }

        /// <summary>Pause is a domain gate for both time and movement.</summary>
        [Test] public void PauseFreezesTimeAndRejectsMoves()
        {
            var b=PrototypeLevel.Create();b.SetPaused(true);b.Tick(30f);
            Assert.AreEqual(180f,b.RemainingSeconds);Assert.IsFalse(b.TryMove(4,0,0,2));
            b.SetPaused(false);b.Tick(1f);Assert.AreEqual(179f,b.RemainingSeconds);
        }

        /// <summary>Expiry is clamped and no later input can revive a lost board.</summary>
        [Test] public void DeadlineLosesAndBlocksFurtherMoves()
        {
            var b=PrototypeLevel.Create();b.Tick(181f);
            Assert.AreEqual(0f,b.RemainingSeconds);Assert.AreEqual(SortPhase.Lost,b.Phase);
            Assert.IsFalse(b.TryMove(4,0,0,2));b.SetPaused(false);
            Assert.AreEqual(SortPhase.Lost,b.Phase);
        }

        /// <summary>Expiry wins when its tick precedes the otherwise winning final input.</summary>
        [Test] public void ExpiryBeforeFinalMoveProducesOnlyLoss()
        {
            var b=PrototypeLevel.Create();
            for(int i=0;i<Solution.GetLength(0)-1;i++) b.TryMove(Solution[i,0],Solution[i,1],Solution[i,2],Solution[i,3]);
            b.Tick(180f);int last=Solution.GetLength(0)-1;Assert.IsFalse(b.TryMove(Solution[last,0],Solution[last,1],Solution[last,2],Solution[last,3]));
            Assert.AreEqual(SortPhase.Lost,b.Phase);Assert.AreEqual(3,b.RemainingCount);Assert.AreEqual(7,b.Matches);
        }

        /// <summary>A completed final move remains a win when an expiry tick arrives afterward.</summary>
        [Test] public void FinalMoveBeforeExpiryProducesOnlyWin()
        {
            var b=PrototypeLevel.Create();b.Tick(179.99f);
            for(int i=0;i<Solution.GetLength(0);i++) Assert.IsTrue(b.TryMove(Solution[i,0],Solution[i,1],Solution[i,2],Solution[i,3]));
            b.Tick(1f);Assert.AreEqual(SortPhase.Won,b.Phase);Assert.AreEqual(8,b.Matches);
        }

        /// <summary>Replaying the declared solution proves actual level solvability.</summary>
        [Test] public void AuthoredLevelHasVerifiedFourteenMoveSolution()
        {
            var b=PrototypeLevel.Create();
            for(int i=0;i<Solution.GetLength(0);i++)
                Assert.IsTrue(b.TryMove(Solution[i,0],Solution[i,1],Solution[i,2],Solution[i,3]),"Move "+i);
            Assert.AreEqual(0,b.RemainingCount);Assert.AreEqual(8,b.Matches);
            Assert.AreEqual(SortPhase.Won,b.Phase);b.Tick(999f);
            Assert.AreEqual(SortPhase.Won,b.Phase);
        }

        /// <summary>Reset recreates stacks and IDs without retaining prior mutations.</summary>
        [Test] public void ResetRestoresInitialStateAfterMutationAndFailure()
        {
            var b=PrototypeLevel.Create();int id=b.GetFront(0,0).InstanceId;
            b.TryMove(4,0,0,2);b.Tick(999f);b.Reset();
            Assert.AreEqual(id,b.GetFront(0,0).InstanceId);Assert.AreEqual(24,b.RemainingCount);
            Assert.AreEqual(0,b.Matches);Assert.AreEqual(180f,b.RemainingSeconds);
            Assert.AreEqual(SortPhase.Playing,b.Phase);
        }
    }
}
