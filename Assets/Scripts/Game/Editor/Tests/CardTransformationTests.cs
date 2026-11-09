using System;
using System.Linq;
using NUnit.Framework;

namespace AllSorted
{
    public sealed class CardTransformationTests
    {
        [Test]
        public void DeferredCardConversionPreservesNineIdsUntilSettlement()
        {
            int[][][] shelves = Enumerable.Range(0, 3)
                .Select(kind => Enumerable.Range(0, 3).Select(_ => new[] { kind }).ToArray()).ToArray();
            var board = new SortBoardState(shelves, 180);
            var begin = typeof(SortBoardState).GetMethod("BeginCardTransformation");
            var complete = typeof(SortBoardState).GetMethod("CompleteCardTransformation");
            Assert.NotNull(begin, "The view needs a transaction that postpones matching.");
            Assert.NotNull(complete);
            Assert.IsTrue((bool)begin.Invoke(board, new object[] { new Random(17) }));
            Assert.AreEqual(9, board.BoosterPieceIds.Distinct().Count());
            Assert.AreEqual(9, board.RemainingCount);
            Assert.AreEqual(0, board.Matches);
            for (int shelf = 0; shelf < 3; shelf++)
                for (int slot = 0; slot < 3; slot++)
                    Assert.AreEqual(SortBoardState.GoldenKind, board.GetFront(shelf, slot).Kind);
            Assert.IsFalse(board.CanUseBooster(SortBooster.Freeze));
            Assert.IsTrue((bool)complete.Invoke(board, null));
            Assert.IsFalse((bool)complete.Invoke(board, null));
            Assert.AreEqual(3, board.Matches);
            Assert.AreEqual(SortPhase.Won, board.Phase);
        }
    }
}
