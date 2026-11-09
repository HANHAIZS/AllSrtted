using NUnit.Framework;
namespace AllSorted.Tests
{
    public sealed class ShelfRowRefillTests : WalletTestBase
    {
        /// <summary>Removing one or two fronts must not expose their rear stock.</summary>
        [Test] public void PartialRowStaysVacantUntilTheLastFrontLeaves()
        {
            var b=CreateBoard();
            int a=b.GetBehind(0,0).InstanceId,c=b.GetBehind(0,2).InstanceId;
            Assert.IsTrue(b.TryMove(0,0,1,0));
            Assert.IsTrue(b.GetFront(0,0).IsEmpty);Assert.AreEqual(a,b.GetBehind(0,0).InstanceId);
            Assert.IsFalse(b.TryMove(0,0,1,1));
            Assert.IsTrue(b.TryMove(0,1,1,1));
            Assert.IsTrue(b.GetFront(0,0).IsEmpty);Assert.IsTrue(b.GetFront(0,1).IsEmpty);
            Assert.AreEqual(c,b.GetBehind(0,2).InstanceId);Assert.AreEqual(9,b.RemainingCount);
        }

        /// <summary>The final vacancy advances exactly one shared layer, retaining deeper previews.</summary>
        [Test] public void LastFrontLeavingRefillsTheWholeShelfTogether()
        {
            var b=CreateBoard();int a=b.GetBehind(0,0).InstanceId,z=b.GetBehind(0,2).InstanceId;
            Assert.IsTrue(b.TryMove(0,0,1,0));Assert.IsTrue(b.TryMove(0,1,1,1));Assert.IsTrue(b.TryMove(0,2,1,2));
            Assert.AreEqual(a,b.GetFront(0,0).InstanceId);Assert.AreEqual(2,b.GetFront(0,0).Kind);
            Assert.AreEqual(3,b.GetFront(0,1).Kind);Assert.AreEqual(z,b.GetFront(0,2).InstanceId);
            Assert.AreEqual(1,b.GetBehind(0,0).Kind);Assert.AreEqual(9,b.RemainingCount);Assert.AreEqual(0,b.Matches);
        }

        /// <summary>Hidden stock does not occupy an otherwise empty front drop target.</summary>
        [Test] public void DroppingIntoVacancyKeepsItsRearStockAndDelaysRefill()
        {
            var b=CreateBoard();int behind=b.GetBehind(0,0).InstanceId,original=b.GetFront(0,0).InstanceId;
            Assert.IsTrue(b.TryMove(0,0,1,0));Assert.IsTrue(b.TryMove(1,0,0,0));
            Assert.AreEqual(original,b.GetFront(0,0).InstanceId);Assert.AreEqual(behind,b.GetBehind(0,0).InstanceId);
            Assert.IsTrue(b.TryMove(0,1,1,1));Assert.IsTrue(b.TryMove(0,2,1,2));
            Assert.AreEqual(original,b.GetFront(0,0).InstanceId);Assert.IsTrue(b.GetFront(0,1).IsEmpty);
            Assert.AreEqual(behind,b.GetBehind(0,0).InstanceId);
        }

        /// <summary>Two initial gaps count as empty, without suppressing a stock-bearing column's refill.</summary>
        [Test] public void InitiallyEmptyColumnsAllowRefillAfterTheOnlyFrontLeaves()
        {
            var b=new SortBoardState(new[]{new[]{new[]{0,1,2},new int[0],new int[0]},
                new[]{new int[0],new int[0],new int[0]}},180);
            int next=b.GetBehind(0,0).InstanceId;
            Assert.IsTrue(b.TryMove(0,0,1,0));Assert.AreEqual(next,b.GetFront(0,0).InstanceId);
            Assert.AreEqual(2,b.GetBehind(0,0).Kind);Assert.IsTrue(b.GetFront(0,1).IsEmpty);Assert.AreEqual(3,b.RemainingCount);
        }

        /// <summary>A paused vacancy and a failed hidden pickup cannot reveal stock; retry restores it.</summary>
        [Test] public void PauseInvalidMovesAndResetPreserveLayerBoundaries()
        {
            var b=CreateBoard();int front=b.GetFront(0,0).InstanceId,behind=b.GetBehind(0,0).InstanceId;
            b.TryMove(0,0,1,0);b.SetPaused(true);int revision=b.Revision;
            Assert.IsFalse(b.TryMove(0,1,1,1));Assert.IsTrue(b.GetFront(0,0).IsEmpty);Assert.AreEqual(revision,b.Revision);
            b.SetPaused(false);Assert.IsFalse(b.TryMove(0,0,1,2));Assert.AreEqual(behind,b.GetBehind(0,0).InstanceId);
            b.Tick(181);b.Reset();Assert.AreEqual(front,b.GetFront(0,0).InstanceId);
            Assert.AreEqual(behind,b.GetBehind(0,0).InstanceId);Assert.AreEqual(9,b.RemainingCount);
            Assert.AreEqual(180,b.RemainingSeconds);Assert.AreEqual(SortPhase.Playing,b.Phase);
        }

        /// <summary>A physical front/rear fixture with unique rear identities and no automatic triples.</summary>
        private static SortBoardState CreateBoard()
        {
            return new SortBoardState(new[]{new[]{new[]{0,2,1},new[]{1,3,2},new[]{2,0,3}},
                new[]{new int[0],new int[0],new int[0]}},180);
        }
    }
}
