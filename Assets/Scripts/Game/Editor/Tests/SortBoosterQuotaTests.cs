using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QFramework;

namespace AllSorted.Tests
{
    public sealed class SortBoosterQuotaTests : WalletTestBase
    {
        [SetUp] public void ResetFixtureInventory()
        {
            ResetCharges();AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));
        }

        /// <summary>Defaults belong to each new application model, not a global or board state.</summary>
        [Test] public void FreshSessionModelStartsWithThreeIndependentCharges()
        {
            var model=new SortBoosterModel();
            foreach(SortBooster booster in Enum.GetValues(typeof(SortBooster)))
                Assert.AreEqual(3,model.GetCharges(booster));
            Assert.AreEqual(0,model.Revision.Value);
            Assert.AreEqual(0,model.GetCharges((SortBooster)(-1)));
            Assert.AreEqual(0,model.GetCharges((SortBooster)999));
        }

        /// <summary>The fourth use must be rejected even when a new board is eligible again.</summary>
        [Test] public void EachBoosterAcceptsThreeUsesAndRejectsFourthWithoutPublishing()
        {
            var app=AllSortedApp.Interface;var inventory=app.GetModel<SortBoosterModel>();
            var model=app.GetModel<SortBoardModel>();
            foreach(SortBooster booster in Enum.GetValues(typeof(SortBooster))) {
                int quotaRevision=inventory.Revision.Value;
                for(int use=0;use<3;use++) {
                    app.SendCommand(new StartSortLevelCommand(1));int boardRevision=model.BoardRevision.Value;
                    var command=new UseSortBoosterCommand(booster);app.SendCommand(command);
                    Assert.IsTrue(command.Accepted,booster+" use "+use);
                    Assert.AreEqual(2-use,inventory.GetCharges(booster));
                    Assert.AreEqual(quotaRevision+use+1,inventory.Revision.Value);
                    Assert.AreEqual(boardRevision+1,model.BoardRevision.Value);
                }
                app.SendCommand(new StartSortLevelCommand(1));string before=Snapshot(model);
                var rejected=new UseSortBoosterCommand(booster);app.SendCommand(rejected);
                Assert.IsFalse(rejected.Accepted);Assert.AreEqual(0,rejected.AffectedPieceIds.Length);
                Assert.AreEqual(before,Snapshot(model));Assert.AreEqual(quotaRevision+3,inventory.Revision.Value);
            }
        }

        /// <summary>Spending one allowance cannot touch any other booster.</summary>
        [Test] public void AcceptedUseChangesOnlyItsOwnChargeAndNotifiesOnce()
        {
            var app=AllSortedApp.Interface;var inventory=app.GetModel<SortBoosterModel>();int notifications=0;
            var registration=inventory.Revision.Register(value=>notifications++);
            try {
                var command=new UseSortBoosterCommand(SortBooster.Freeze);app.SendCommand(command);
                Assert.IsTrue(command.Accepted);Assert.AreEqual(2,inventory.GetCharges(SortBooster.Freeze));
                Assert.AreEqual(3,inventory.GetCharges(SortBooster.Vacuum));
                Assert.AreEqual(3,inventory.GetCharges(SortBooster.Shuffle));
                Assert.AreEqual(3,inventory.GetCharges(SortBooster.Golden));Assert.AreEqual(1,notifications);
                var fresh=new SortBoosterModel();Assert.AreEqual(3,fresh.GetCharges(SortBooster.Freeze));
            }
            finally {registration.UnRegister();}
        }

        /// <summary>Insufficient targets, active freeze and unknown enums cannot spend or publish.</summary>
        [Test] public void FailedEligibilityAndInvalidBoostersLeaveQuotaAndBoardUntouched()
        {
            var app=AllSortedApp.Interface;
            ReplaceBoard(new SortBoardState(new[]{new[]{new[]{0,1},new[]{2,1},new[]{3,1}}},180));
            AssertUseRejected(SortBooster.Vacuum);
            ReplaceBoard(new SortBoardState(new[]{new[]{new[]{0},new[]{0},new int[0]}},180));
            AssertUseRejected(SortBooster.Shuffle);AssertUseRejected(SortBooster.Golden);
            ReplaceBoard(new SortBoardState(new[]{new[]{new[]{0,0,0,0,0},new[]{1,1},new[]{2,2}}},180));
            Assert.AreEqual(9,app.GetModel<SortBoardModel>().Board.RemainingCount);AssertUseRejected(SortBooster.Golden);
            app.SendCommand(new StartSortLevelCommand(1));app.SendCommand(new UseSortBoosterCommand(SortBooster.Freeze));
            AssertUseRejected(SortBooster.Freeze);AssertUseRejected((SortBooster)(-1));AssertUseRejected((SortBooster)999);
        }

        /// <summary>All non-playing phases reject use and a zero-stock refill.</summary>
        [Test] public void PausedLostAndWonBoardsRejectUseAndRefillForEveryBooster()
        {
            var paused=PrototypeLevel.Create();paused.SetPaused(true);
            var lost=PrototypeLevel.Create();lost.Tick(180);
            var won=new SortBoardState(new[]{new[]{new[]{0},new[]{0},new[]{0}}},180);
            Assert.IsTrue(won.TryUseBooster(SortBooster.Vacuum,new Random(1)));
            foreach(var board in new[]{paused,lost,won}) {
                ReplaceBoard(board);
                foreach(SortBooster booster in Enum.GetValues(typeof(SortBooster))) {
                    ResetCharges();AssertUseRejected(booster);SetCharge(booster,0);AssertAddRejected(booster);
                }
            }
        }

        /// <summary>Refill must change only stock even when the depleted booster currently has no target.</summary>
        [Test] public void AddAtZeroRestoresOneWithoutEffectOrBoardPublicationAndRejectsStaleRepeat()
        {
            var app=AllSortedApp.Interface;var inventory=app.GetModel<SortBoosterModel>();
            var model=app.GetModel<SortBoardModel>();
            for(int use=0;use<3;use++) {
                app.SendCommand(new StartSortLevelCommand(1));app.SendCommand(new UseSortBoosterCommand(SortBooster.Freeze));
            }
            Assert.AreEqual(0,inventory.GetCharges(SortBooster.Freeze));
            Assert.IsFalse(model.Board.CanUseBooster(SortBooster.Freeze));
            var board=model.Board;string before=Snapshot(model);int revision=inventory.Revision.Value;
            Assert.IsTrue(Add(SortBooster.Freeze));Assert.AreSame(board,model.Board);
            Assert.AreEqual(1,inventory.GetCharges(SortBooster.Freeze));Assert.AreEqual(revision+1,inventory.Revision.Value);
            Assert.AreEqual(before,Snapshot(model));AssertAddRejected(SortBooster.Freeze);
            Assert.AreEqual(3,inventory.GetCharges(SortBooster.Vacuum));
            Assert.AreEqual(3,inventory.GetCharges(SortBooster.Shuffle));
            Assert.AreEqual(3,inventory.GetCharges(SortBooster.Golden));
        }

        /// <summary>Positive inventory and invalid values cannot be used as unlimited refill requests.</summary>
        [Test] public void RefillRejectsPositiveStockAndInvalidBooster()
        {
            foreach(SortBooster booster in Enum.GetValues(typeof(SortBooster))) AssertAddRejected(booster);
            AssertAddRejected((SortBooster)(-1));AssertAddRejected((SortBooster)999);
        }

        /// <summary>Board replacement and campaign progression retain the same application inventory.</summary>
        [Test] public void RetryExplicitSelectionAndNextLevelPreserveSessionInventory()
        {
            var app=AllSortedApp.Interface;var inventory=app.GetModel<SortBoosterModel>();
            app.SendCommand(new UseSortBoosterCommand(SortBooster.Freeze));int revision=inventory.Revision.Value;
            foreach(int level in new[]{0,2,3,1}) {
                app.SendCommand(new StartSortLevelCommand(level));
                Assert.AreSame(inventory,app.GetModel<SortBoosterModel>());
                Assert.AreEqual(2,inventory.GetCharges(SortBooster.Freeze));Assert.AreEqual(revision,inventory.Revision.Value);
            }
            CampaignCommandTests.ClearCurrent(1);var next=new AdvanceSortLevelCommand();app.SendCommand(next);
            Assert.IsTrue(next.Accepted);Assert.AreEqual(2,app.GetModel<SortBoardModel>().LevelNumber.Value);
            Assert.AreEqual(2,inventory.GetCharges(SortBooster.Freeze));Assert.AreEqual(revision,inventory.Revision.Value);
        }

        private static bool Add(SortBooster booster)
        {
            var command=new AddSortBoosterChargeCommand(booster);
            AllSortedApp.Interface.SendCommand(command);return command.Accepted;
        }

        private static void AssertUseRejected(SortBooster booster)
        {
            var app=AllSortedApp.Interface;var inventory=app.GetModel<SortBoosterModel>();
            var model=app.GetModel<SortBoardModel>();string before=Snapshot(model);
            int charges=inventory.GetCharges(booster),revision=inventory.Revision.Value;
            var command=new UseSortBoosterCommand(booster);app.SendCommand(command);Assert.IsFalse(command.Accepted);
            Assert.AreEqual(charges,inventory.GetCharges(booster));Assert.AreEqual(revision,inventory.Revision.Value);
            Assert.AreEqual(before,Snapshot(model));
        }

        private static void AssertAddRejected(SortBooster booster)
        {
            var inventory=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            var model=AllSortedApp.Interface.GetModel<SortBoardModel>();string before=Snapshot(model);
            int charges=inventory.GetCharges(booster),revision=inventory.Revision.Value;
            Assert.IsFalse(Add(booster));Assert.AreEqual(charges,inventory.GetCharges(booster));
            Assert.AreEqual(revision,inventory.Revision.Value);Assert.AreEqual(before,Snapshot(model));
        }

        // Test-only isolation: production level resets deliberately preserve the session allowance.
        private static void ResetCharges()
        {
            var model=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            Assert.IsNotNull(model,"Architecture must register the session inventory before its System.");
            var charges=WalletTestScope.Charges();
            for(int index=0;index<charges.Length;index++) charges[index]=3;
            model.Revision.Value++;
        }

        private static void SetCharge(SortBooster booster,int value)
        {
            var model=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            var charges=WalletTestScope.Charges();
            charges[(int)booster]=value;model.Revision.Value++;
        }

        private static void ReplaceBoard(SortBoardState board)
        {
            var model=AllSortedApp.Interface.GetModel<SortBoardModel>();
            typeof(SortBoardModel).GetProperty("Board").SetValue(model,board,null);
            AllSortedApp.Interface.SendCommand(new TickSortCommand{DeltaTime=0});
        }

        private static string Snapshot(SortBoardModel model)
        {
            var board=model.Board;
            var slots=(List<SortPiece>[][])typeof(SortBoardState).GetField("slots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
            return string.Join("/",slots.SelectMany(row=>row).Select(stack=>string.Join(",",stack.Select(piece=>piece.InstanceId+":"+piece.Kind))))+
                "|"+board.Revision+"|"+board.Phase+"|"+board.RemainingCount+"|"+board.RemainingSeconds+"|"+board.FreezeSeconds+
                "|"+board.Matches+"|"+string.Join(",",board.BoosterPieceIds)+"|"+string.Join(",",board.MatchedShelves)+
                "|"+model.BoardRevision.Value+"|"+model.Seconds.Value+"|"+model.Phase.Value+"|"+model.MatchCount.Value;
        }
    }
}
