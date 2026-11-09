using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace AllSorted.Tests
{
    public sealed class SortBoosterRulesTests : WalletTestBase
    {
        /// <summary>Front-only suction must leave stock behind partly occupied shelves.</summary>
        [Test] public void VacuumRemovesThreeMatchingFrontsWithoutRevealingPartialRows()
        {
            var board=new SortBoardState(new[] {
                new[]{new[]{0,7},new[]{1},new[]{2}},
                new[]{new[]{0,8},new[]{3},new[]{2}},
                new[]{new[]{0,9},new[]{1},new[]{3}}},180);
            int revision=board.Revision;
            int[] expected={board.GetFront(0,0).InstanceId,board.GetFront(1,0).InstanceId,board.GetFront(2,0).InstanceId};
            Assert.IsTrue(CanUse(board,"Vacuum"));Assert.IsTrue(Use(board,"Vacuum",8));
            CollectionAssert.AreEquivalent(expected,Affected(board));
            Assert.AreEqual(9,board.RemainingCount);Assert.AreEqual(1,board.Matches);Assert.AreEqual(revision+1,board.Revision);
            for(int shelf=0;shelf<3;shelf++) {
                Assert.IsTrue(board.GetFront(shelf,0).IsEmpty);
                Assert.AreEqual(7+shelf,board.GetBehind(shelf,0).Kind);
            }
        }

        /// <summary>One transaction must include shared-layer refill and every resulting triple.</summary>
        [Test] public void VacuumSettlesRearCascadeAndWinsWithOneRevision()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,1},new[]{0,1},new[]{0,1}}},180);
            int revision=board.Revision;
            Assert.IsTrue(Use(board,"Vacuum",2));Assert.AreEqual(0,board.RemainingCount);
            Assert.AreEqual(2,board.Matches);Assert.AreEqual(SortPhase.Won,board.Phase);
            Assert.AreEqual(revision+1,board.Revision);Assert.AreEqual(3,Affected(board).Length);
            CollectionAssert.AreEquivalent(new[]{1,3,5},Affected(board));
        }

        /// <summary>Eligible kinds and individual same-kind fronts must both vary with the random draw.</summary>
        [Test] public void VacuumRandomizesKindAndSelectionAcrossSeeds()
        {
            var kinds=new HashSet<int>();var selections=new HashSet<string>();
            for(int seed=0;seed<40;seed++) {
                var board=VacuumChoiceBoard();var before=RealPieces(board).ToDictionary(piece=>piece.InstanceId,piece=>piece.Kind);
                Assert.IsTrue(Use(board,"Vacuum",seed));int[] ids=Affected(board);
                Assert.AreEqual(3,ids.Length);Assert.AreEqual(3,ids.Distinct().Count());
                int kind=before[ids[0]];Assert.IsTrue(ids.All(id=>before[id]==kind));
                Assert.IsTrue(kind==0 || kind==1);Assert.AreEqual(9,board.RemainingCount);
                kinds.Add(kind);selections.Add(string.Join(",",ids.OrderBy(id=>id)));
            }
            CollectionAssert.AreEquivalent(new[]{0,1},kinds);Assert.Greater(selections.Count,2);
        }

        /// <summary>Three hidden matching pieces alone cannot make suction legal.</summary>
        [Test] public void VacuumRejectsHiddenOnlyTripleWithoutMutation()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,1},new[]{2,1},new[]{3,1}}},180);
            string before=Snapshot(board);Assert.IsFalse(CanUse(board,"Vacuum"));
            Assert.IsFalse(Use(board,"Vacuum",1));Assert.AreEqual(before,Snapshot(board));
        }

        /// <summary>Shuffling must preserve each occupied depth and vacancy, including default front holes.</summary>
        [Test] public void ShufflePreservesIdentitiesKindsVacanciesAndStackDepths()
        {
            var board=LayeredBoard();Assert.IsTrue(board.TryMove(0,0,2,2));
            var before=Stacks(board).Select(row=>row.Select(stack=>stack.ToArray()).ToArray()).ToArray();
            string pieces=Inventory(board),arrangement=Arrangement(board);int revision=board.Revision;
            Assert.IsTrue(Use(board,"Shuffle",19));Assert.AreEqual(pieces,Inventory(board));
            Assert.AreNotEqual(arrangement,Arrangement(board));Assert.AreEqual(revision+1,board.Revision);
            var after=Stacks(board);
            for(int shelf=0;shelf<before.Length;shelf++) for(int slot=0;slot<3;slot++) {
                Assert.AreEqual(before[shelf][slot].Length,after[shelf][slot].Count);
                for(int depth=0;depth<before[shelf][slot].Length;depth++)
                    Assert.AreEqual(before[shelf][slot][depth].IsEmpty,after[shelf][slot][depth].IsEmpty);
            }
        }

        /// <summary>An identity permutation must still produce a different kind arrangement.</summary>
        [Test] public void ShuffleAvoidsNoOpEvenWhenRandomDrawKeepsEveryPosition()
        {
            var board=LayeredBoard();string before=KindArrangement(board);
            Assert.IsTrue(Use(board,"Shuffle",new KeepOrderRandom()));
            Assert.AreNotEqual(before,KindArrangement(board));
        }

        /// <summary>Reordering hidden stock alone cannot satisfy the dice's visible rearrangement guarantee.</summary>
        [Test] public void ShuffleChangesAFrontKindWhenRandomOnlyPermutesHiddenPieces()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,1},new[]{0,2},new int[0]}},180);
            string inventory=Inventory(board);
            Assert.IsTrue(Use(board,"Shuffle",new HiddenOnlyShuffleRandom()));
            Assert.IsTrue(board.GetFront(0,0).Kind!=0 || board.GetFront(0,1).Kind!=0,
                "An accepted shuffle must change at least one occupied front kind.");
            Assert.AreEqual(inventory,Inventory(board));Assert.IsTrue(board.GetFront(0,2).IsEmpty);
            Assert.AreEqual(2,Stacks(board)[0][0].Count);Assert.AreEqual(2,Stacks(board)[0][1].Count);
        }

        /// <summary>A board with only one kind has no meaningful permutation, even with distinct IDs.</summary>
        [Test] public void ShuffleRejectsSingleKindWithoutMutation()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,0},new[]{0},new int[0]}},180);
            string before=Snapshot(board);Assert.IsFalse(CanUse(board,"Shuffle"));
            Assert.IsFalse(Use(board,"Shuffle",4));Assert.AreEqual(before,Snapshot(board));
        }

        /// <summary>A shuffle-created front triple must settle as part of that accepted action.</summary>
        [Test] public void ShuffleSettlesCreatedTriples()
        {
            bool found=false;
            for(int seed=0;seed<100;seed++) {
                var board=new SortBoardState(new[]{new[]{new[]{0},new[]{1},new[]{0}},new[]{new[]{1},new[]{0},new[]{1}}},180);
                int revision=board.Revision;Assert.IsTrue(Use(board,"Shuffle",seed));
                Assert.AreEqual(revision+1,board.Revision);
                if(board.Matches==0) continue;
                found=true;Assert.AreEqual(2,board.Matches);Assert.AreEqual(0,board.RemainingCount);
                Assert.AreEqual(SortPhase.Won,board.Phase);break;
            }
            Assert.IsTrue(found,"Seeded permutations should sometimes create the two matching rows.");
        }

        /// <summary>The card preserves position/identity and can transform hidden pieces.</summary>
        [Test] public void GoldenTransformsNinePiecesFromThreeCompleteMatchingGroups()
        {
            var board=LayeredBoard();var before=RealPieces(board).ToDictionary(piece=>piece.InstanceId,piece=>piece.Kind);
            string positions=IdArrangement(board);int revision=board.Revision;
            Assert.IsTrue(Use(board,"Golden",11));int[] ids=Affected(board);
            Assert.AreEqual(9,ids.Length);Assert.AreEqual(9,ids.Distinct().Count());
            for(int group=0;group<3;group++) Assert.AreEqual(1,ids.Skip(group*3).Take(3).Select(id=>before[id]).Distinct().Count());
            Assert.AreEqual(positions,IdArrangement(board));Assert.AreEqual(revision+1,board.Revision);
            var after=RealPieces(board).ToDictionary(piece=>piece.InstanceId,piece=>piece.Kind);
            foreach(int id in before.Keys) Assert.AreEqual(ids.Contains(id)?4:before[id],after[id]);
            Assert.IsTrue(Stacks(board).SelectMany(row=>row).Any(stack=>stack.Skip(1).Any(piece=>ids.Contains(piece.InstanceId))));
        }

        /// <summary>Partial-kind draws would strand stock; every selected kind must lose a multiple of three.</summary>
        [Test] public void GoldenSelectsMatchingTriplesAndPreservesRemainingKindRemainders()
        {
            var selections=new HashSet<string>();
            for(int seed=0;seed<30;seed++) {
                var board=PrototypeLevel.Create(1);var before=RealPieces(board).ToDictionary(piece=>piece.InstanceId,piece=>piece.Kind);
                Assert.IsTrue(Use(board,"Golden",seed));var ids=Affected(board);
                for(int group=0;group<3;group++) Assert.AreEqual(1,ids.Skip(group*3).Take(3).Select(id=>before[id]).Distinct().Count(),"Every selected group must match before conversion.");
                foreach(var group in ids.GroupBy(id=>before[id])) Assert.AreEqual(0,group.Count()%3);
                selections.Add(string.Join(",",Affected(board).OrderBy(id=>id)));
                foreach(var kind in before.Values.Distinct()) Assert.AreEqual(before.Count(pair=>pair.Value==kind)%3,RealPieces(board).Count(piece=>piece.Kind==kind)%3);
            }
            Assert.Greater(selections.Count,1);
        }

        /// <summary>Nine individual items do not make three groups when their kinds cannot form triples.</summary>
        [Test] public void GoldenRejectsNineItemsWithoutThreeCompleteGroupsWithoutMutation()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,0,0,0,0},new[]{1,1},new[]{2,2}}},180);
            Assert.AreEqual(9,board.RemainingCount);string before=Snapshot(board);
            Assert.IsFalse(CanUse(board,"Golden"));Assert.IsFalse(Use(board,"Golden",0));Assert.AreEqual(before,Snapshot(board));
        }

        /// <summary>Three complete groups may share a kind and include deeper hidden stock.</summary>
        [Test] public void GoldenCanChooseThreeGroupsFromOneKindAndLeaveItsRemainder()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0,0,0,0,0,0,0,0,0,0,0},new int[0],new int[0]}},180);
            Assert.IsTrue(CanUse(board,"Golden"));Assert.IsTrue(Use(board,"Golden",17));
            Assert.AreEqual(9,Affected(board).Distinct().Count());Assert.AreEqual(9,RealPieces(board).Count(piece=>piece.Kind==4));
            Assert.AreEqual(2,RealPieces(board).Count(piece=>piece.Kind==0));Assert.AreEqual(11,board.RemainingCount);
        }

        /// <summary>Existing gold items never count toward the nine eligible transformations.</summary>
        [Test] public void GoldenExcludesExistingGoldAndRejectsFewerThanNineNonGold()
        {
            var board=new SortBoardState(new[]{
                new[]{new[]{4,0,1,2,3},new[]{4,0,1,2,3},new int[0]},
                new[]{new[]{4,0,1},new[]{4,2,3},new int[0]}},180);
            int[] gold=RealPieces(board).Where(piece=>piece.Kind==4).Select(piece=>piece.InstanceId).ToArray();
            Assert.IsTrue(Use(board,"Golden",5));Assert.IsFalse(Affected(board).Any(id=>gold.Contains(id)));
            string before=Snapshot(board);Assert.IsFalse(CanUse(board,"Golden"));
            Assert.IsFalse(Use(board,"Golden",6));Assert.AreEqual(before,Snapshot(board));
        }

        /// <summary>All nine converted fronts can settle to a terminal win in one revision.</summary>
        [Test] public void GoldenSettlesAllFrontTriplesAndRetainsSelectedIdsAfterRemoval()
        {
            var board=new SortBoardState(new[]{new[]{new[]{0},new[]{1},new[]{2}},new[]{new[]{0},new[]{1},new[]{2}},new[]{new[]{0},new[]{1},new[]{2}}},180);
            int revision=board.Revision;Assert.IsTrue(Use(board,"Golden",1));
            Assert.AreEqual(3,board.Matches);Assert.AreEqual(0,board.RemainingCount);
            Assert.AreEqual(SortPhase.Won,board.Phase);Assert.AreEqual(revision+1,board.Revision);
            CollectionAssert.AreEquivalent(Enumerable.Range(1,9),Affected(board));
        }

        /// <summary>A boundary tick charges exactly its unfrozen remainder to the level clock.</summary>
        [Test] public void FreezeProtectsExactlyFifteenActiveSecondsAcrossBoundaryTick()
        {
            var board=PrototypeLevel.Create();int revision=board.Revision;
            Assert.IsTrue(Use(board,"Freeze",0));Assert.AreEqual(15f,Frozen(board));
            Assert.AreEqual(revision+1,board.Revision);board.Tick(17f);
            Assert.AreEqual(0f,Frozen(board));Assert.AreEqual(178f,board.RemainingSeconds);
            Assert.IsTrue(CanUse(board,"Freeze"));
        }

        /// <summary>Partial and exact-expiry ticks must preserve every frozen fraction.</summary>
        [Test] public void FreezeHandlesFractionalTicksAndExactExpiry()
        {
            var board=PrototypeLevel.Create();Assert.IsTrue(Use(board,"Freeze",0));
            board.Tick(14.5f);Assert.AreEqual(.5f,Frozen(board));Assert.AreEqual(180f,board.RemainingSeconds);
            board.Tick(.5f);Assert.AreEqual(0f,Frozen(board));Assert.AreEqual(180f,board.RemainingSeconds);
            board.Tick(.25f);Assert.AreEqual(179.75f,board.RemainingSeconds);
        }

        /// <summary>The freeze guard rejects stacking without revising or resetting the remaining duration.</summary>
        [Test] public void FreezeRejectsStackingWithoutMutation()
        {
            var board=PrototypeLevel.Create();Assert.IsTrue(Use(board,"Freeze",0));board.Tick(3f);
            string before=Snapshot(board);Assert.IsFalse(CanUse(board,"Freeze"));
            Assert.IsFalse(Use(board,"Freeze",1));Assert.AreEqual(before,Snapshot(board));Assert.AreEqual(12f,Frozen(board));
        }

        /// <summary>Frozen time allows moves and other boosters, while pause suspends both clocks.</summary>
        [Test] public void FreezeAllowsDraggingAndOtherBoostersWhilePauseStopsBothClocks()
        {
            var board=PrototypeLevel.Create();Assert.IsTrue(Use(board,"Freeze",0));
            Assert.IsTrue(board.TryMove(4,0,0,2));Assert.IsTrue(Use(board,"Golden",2));
            board.Tick(2);board.SetPaused(true);string before=Snapshot(board);board.Tick(100);
            Assert.AreEqual(before,Snapshot(board));Assert.AreEqual(13f,Frozen(board));
            board.SetPaused(false);board.Tick(14);Assert.AreEqual(0f,Frozen(board));Assert.AreEqual(179f,board.RemainingSeconds);
        }

        /// <summary>Retry restores original identities, kinds and clocks and clears stale effect selections.</summary>
        [Test] public void ResetClearsFreezeAndAffectedIds()
        {
            var board=PrototypeLevel.Create();string original=Arrangement(board);
            Assert.IsTrue(Use(board,"Freeze",4));Assert.IsTrue(Use(board,"Golden",4));
            Assert.AreEqual(9,Affected(board).Length);board.Tick(2);board.Reset();
            Assert.AreEqual(original,Arrangement(board));Assert.AreEqual(0f,Frozen(board));
            Assert.AreEqual(0,Affected(board).Length);Assert.AreEqual(180f,board.RemainingSeconds);
        }

        /// <summary>Rejected requests in paused, lost and won phases must leave all state untouched.</summary>
        [Test] public void EveryBoosterRejectsPausedLostAndWonBoardsWithoutMutation()
        {
            var paused=PrototypeLevel.Create();Use(paused,"Golden",1);paused.SetPaused(true);
            var lost=PrototypeLevel.Create();lost.Tick(180);
            var won=new SortBoardState(new[]{new[]{new[]{0},new[]{0},new[]{0}}},180);Use(won,"Vacuum",1);
            foreach(var board in new[]{paused,lost,won}) foreach(string booster in new[]{"Vacuum","Shuffle","Golden","Freeze"}) {
                string before=Snapshot(board);Assert.IsFalse(CanUse(board,booster));
                Assert.IsFalse(Use(board,booster,8));Assert.AreEqual(before,Snapshot(board));
            }
        }

        /// <summary>Invalid enum requests cannot alter existing selections or the active clock.</summary>
        [Test] public void UnknownBoosterRejectsWithoutMutation()
        {
            var board=PrototypeLevel.Create();Use(board,"Golden",2);string before=Snapshot(board);
            Assert.IsFalse(CanUse(board,"999"));Assert.IsFalse(Use(board,"999",3));Assert.AreEqual(before,Snapshot(board));
        }

        /// <summary>Repeated seeded draws must produce reproducible board state and selected identities.</summary>
        [Test] public void EqualSeedsProduceEqualOutcomesForEveryBooster()
        {
            foreach(string booster in new[]{"Vacuum","Shuffle","Golden","Freeze"}) {
                var first=booster=="Vacuum"?VacuumChoiceBoard():PrototypeLevel.Create(3);
                var second=booster=="Vacuum"?VacuumChoiceBoard():PrototypeLevel.Create(3);
                Assert.IsTrue(Use(first,booster,91));Assert.IsTrue(Use(second,booster,91));
                Assert.AreEqual(Snapshot(first),Snapshot(second));
            }
        }

        /// <summary>Every level shares the same booster rules rather than a first-level-only path.</summary>
        [Test] public void AllThreeLevelsCanUseEachBooster()
        {
            for(int level=1;level<=3;level++) foreach(string booster in new[]{"Vacuum","Shuffle","Golden","Freeze"}) {
                var board=PrototypeLevel.Create(level);Assert.IsTrue(CanUse(board,booster),level+" "+booster);
                Assert.IsTrue(Use(board,booster,level));
            }
        }

        private static SortBoardState VacuumChoiceBoard()
        {
            return new SortBoardState(new[]{new[]{new[]{0},new[]{1},new[]{2}},new[]{new[]{0},new[]{1},new[]{2}},
                new[]{new[]{0},new[]{1},new[]{3}},new[]{new[]{0},new[]{1},new[]{3}}},180);
        }

        private static SortBoardState LayeredBoard()
        {
            return new SortBoardState(new[]{new[]{new[]{0,1,2},new[]{1,2},new int[0]},
                new[]{new[]{2,3},new[]{3,0},new int[0]},new[]{new[]{0,2},new[]{1,3},new int[0]}},180);
        }

        private static SortBooster Booster(string name) {return (SortBooster)Enum.Parse(typeof(SortBooster),name);}
        private static bool CanUse(SortBoardState board,string name) {return board.CanUseBooster(Booster(name));}
        private static bool Use(SortBoardState board,string name,int seed) {return Use(board,name,new Random(seed));}
        private static bool Use(SortBoardState board,string name,Random random) {return board.TryUseBooster(Booster(name),random);}
        private static float Frozen(SortBoardState board) {return board.FreezeSeconds;}
        private static int[] Affected(SortBoardState board) {return board.BoosterPieceIds.ToArray();}
        private static List<SortPiece>[][] Stacks(SortBoardState board) {return (List<SortPiece>[][])typeof(SortBoardState).GetField("slots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);}
        private static IEnumerable<SortPiece> RealPieces(SortBoardState board) {return Stacks(board).SelectMany(row=>row).SelectMany(stack=>stack).Where(piece=>!piece.IsEmpty);}
        private static string Inventory(SortBoardState board) {return string.Join(",",RealPieces(board).OrderBy(piece=>piece.InstanceId).Select(piece=>piece.InstanceId+":"+piece.Kind));}
        private static string Arrangement(SortBoardState board) {return string.Join("/",Stacks(board).SelectMany(row=>row).Select(stack=>string.Join(",",stack.Select(piece=>piece.InstanceId+":"+piece.Kind))));}
        private static string KindArrangement(SortBoardState board) {return string.Join("/",Stacks(board).SelectMany(row=>row).Select(stack=>string.Join(",",stack.Select(piece=>piece.IsEmpty?"_":piece.Kind.ToString()))));}
        private static string IdArrangement(SortBoardState board) {return string.Join("/",Stacks(board).SelectMany(row=>row).Select(stack=>string.Join(",",stack.Select(piece=>piece.InstanceId))));}
        private static string Snapshot(SortBoardState board) {return Arrangement(board)+"|"+board.Phase+"|"+board.Revision+"|"+board.Matches+"|"+board.RemainingCount+"|"+board.RemainingSeconds+"|"+Frozen(board)+"|"+string.Join(",",Affected(board))+"|"+string.Join(",",board.MatchedShelves);}

        private sealed class KeepOrderRandom : Random
        {
            public override int Next(int maxValue) {return maxValue-1;}
        }

        private sealed class HiddenOnlyShuffleRandom : Random
        {
            // The four positions are front, hidden, front, hidden. Swap only the two hidden pieces.
            public override int Next(int maxValue) {return maxValue==4?1:maxValue-1;}
        }
    }
}
