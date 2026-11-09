using System;
using System.IO;
using System.Reflection;
using GameFoundation.Core;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;

namespace AllSorted.Tests
{
    // Reflection keeps the RED suite compilable before the new transaction API exists.
    public sealed class BoosterPurchaseTests : WalletTestBase
    {
        private sealed class Clock : IFoundationClock
        {
            public DateTimeOffset UtcNow {get{return new DateTimeOffset(2026,10,28,0,0,0,TimeSpan.Zero);}}
            public float RealtimeSinceStartup {get{return 0;}}
        }
        private sealed class Disk : ISaveStore
        {
            public readonly MemorySaveStore Memory=new MemorySaveStore();
            public bool Fail;
            public bool TryRead(string key,out string value){return Memory.TryRead(key,out value);}
            public bool TryReadBackup(string key,out string value){return Memory.TryReadBackup(key,out value);}
            public void Write(string key,string value){if(Fail)throw new IOException("test disk full");Memory.Write(key,value);}
            public void Restore(string key,string value){Memory.Restore(key,value);}
            public void Delete(string key){Memory.Delete(key);}
        }
        private Disk disk;
        private Clock clock;
        [SetUp] public void Fresh(){disk=new Disk();clock=new Clock();}
        private SortWalletLedger Load(){return new SortWalletLedger(disk,clock);}
        private static T Invoke<T>(SortWalletLedger ledger,string method,SortBooster booster)
        {
            var member=typeof(SortWalletLedger).GetMethod(method,new[]{typeof(SortBooster)});
            Assert.IsNotNull(member,"Missing ledger capability: "+method);
            return (T)member.Invoke(ledger,new object[]{booster});
        }
        private static int Count(SortWalletLedger ledger,SortBooster booster){return Invoke<int>(ledger,"GetCharges",booster);}
        private static void Empty(SortWalletLedger ledger,SortBooster booster)
        {
            for(int i=0;i<3;i++)Assert.IsTrue(Invoke<bool>(ledger,"TrySpendBooster",booster));
            Assert.AreEqual(0,Count(ledger,booster));
        }
        private string Raw(){string value;Assert.IsTrue(disk.TryRead(SortWalletLedger.SaveSlot,out value));return value;}
        private void SeedV1(int coins,int energy,long due,int version=1)
        {
            string payload="{\"Coins\":"+coins+",\"Energy\":"+energy+",\"NextRecoveryUtcTicks\":"+due+",\"LastSeenUtcTicks\":"+clock.UtcNow.UtcTicks+"}";
            disk.Write(SortWalletLedger.SaveSlot,JsonUtility.ToJson(new SaveEnvelope{Version=version,Payload=payload,SavedAtUtc=clock.UtcNow.ToString("O"),Checksum=SaveChecksum.Compute(version,payload)}));
        }

        [TestCase(SortBooster.Vacuum)] [TestCase(SortBooster.Shuffle)]
        [TestCase(SortBooster.Golden)] [TestCase(SortBooster.Freeze)]
        public void PurchaseAtomicallyDebits1600AndGrantsOnlySelectedThreeAcrossReload(SortBooster booster)
        {
            var ledger=Load();Empty(ledger,booster);
            foreach(SortBooster other in Enum.GetValues(typeof(SortBooster)))
                if(other!=booster)Assert.IsTrue(Invoke<bool>(ledger,"TrySpendBooster",other));
            Assert.IsTrue(Invoke<bool>(ledger,"BuyBooster",booster));
            var reloaded=Load();Assert.AreEqual(0,reloaded.Coins);Assert.AreEqual(5,reloaded.Energy);
            foreach(SortBooster item in Enum.GetValues(typeof(SortBooster)))Assert.AreEqual(item==booster?3:2,Count(reloaded,item));
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",booster));Assert.AreEqual(0,ledger.Coins);
        }

        [Test] public void SpentInventorySurvivesReloadIndependentlyForEachBooster()
        {
            var ledger=Load();
            Assert.IsTrue(Invoke<bool>(ledger,"TrySpendBooster",SortBooster.Vacuum));
            Assert.IsTrue(Invoke<bool>(ledger,"TrySpendBooster",SortBooster.Freeze));
            Assert.IsTrue(Invoke<bool>(ledger,"TrySpendBooster",SortBooster.Freeze));
            var loaded=Load();Assert.AreEqual(2,Count(loaded,SortBooster.Vacuum));Assert.AreEqual(1,Count(loaded,SortBooster.Freeze));
            Assert.AreEqual(3,Count(loaded,SortBooster.Shuffle));Assert.AreEqual(3,Count(loaded,SortBooster.Golden));Assert.AreEqual(1600,loaded.Coins);
        }

        [Test] public void V1MigrationRetainsBalancesAndRecoveryTimestampsAndDefaultsEveryCountToThree()
        {
            long due=clock.UtcNow.UtcTicks+TimeSpan.FromMinutes(10).Ticks;SeedV1(321,4,due);
            var ledger=Load();Assert.AreEqual(321,ledger.Coins);Assert.AreEqual(4,ledger.Energy);Assert.AreEqual(600,ledger.RecoverySeconds);
            foreach(SortBooster item in Enum.GetValues(typeof(SortBooster)))Assert.AreEqual(3,Count(ledger,item));
            var envelope=JsonUtility.FromJson<SaveEnvelope>(Raw());Assert.AreEqual(4,envelope.Version);
            var saved=JsonUtility.FromJson<SortWalletSave>(envelope.Payload);
            Assert.AreEqual(due,saved.NextRecoveryUtcTicks);Assert.AreEqual(clock.UtcNow.UtcTicks,saved.LastSeenUtcTicks);
        }

        [Test] public void InsufficientCoinsLeavesBothInventoryAndSaveUnchanged()
        {
            SeedV1(1599,5,0);var ledger=Load();Empty(ledger,SortBooster.Vacuum);string before=Raw();
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",SortBooster.Vacuum));
            Assert.AreEqual(1599,ledger.Coins);Assert.AreEqual(0,Count(ledger,SortBooster.Vacuum));Assert.AreEqual(before,Raw());
        }

        [Test] public void FailedPurchaseWriteLeavesCoinsAndInventoryUnchangedAndCanRetry()
        {
            var ledger=Load();Empty(ledger,SortBooster.Vacuum);string before=Raw();disk.Fail=true;
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",SortBooster.Vacuum));
            Assert.AreEqual(1600,ledger.Coins);Assert.AreEqual(0,Count(ledger,SortBooster.Vacuum));Assert.AreEqual(before,Raw());
            Assert.IsNotEmpty(ledger.Error);disk.Fail=false;
            Assert.IsTrue(Invoke<bool>(ledger,"BuyBooster",SortBooster.Vacuum));Assert.AreEqual(0,Load().Coins);Assert.AreEqual(3,Count(Load(),SortBooster.Vacuum));
        }

        [Test] public void FailedSpendWriteDoesNotConsumeStockOrPublishInventory()
        {
            var ledger=Load();string before=Raw();disk.Fail=true;
            Assert.IsFalse(Invoke<bool>(ledger,"TrySpendBooster",SortBooster.Freeze));
            Assert.AreEqual(3,Count(ledger,SortBooster.Freeze));Assert.AreEqual(before,Raw());Assert.AreEqual(3,Count(Load(),SortBooster.Freeze));
        }

        [Test] public void InvalidOrNonemptyBoosterCannotBePurchasedAndEmptyCannotBeSpent()
        {
            var ledger=Load();string before=Raw();
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",SortBooster.Freeze));
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",(SortBooster)99));
            Assert.IsFalse(Invoke<bool>(ledger,"TrySpendBooster",(SortBooster)(-1)));Assert.AreEqual(before,Raw());
            Empty(ledger,SortBooster.Freeze);before=Raw();Assert.IsFalse(Invoke<bool>(ledger,"TrySpendBooster",SortBooster.Freeze));Assert.AreEqual(before,Raw());
        }

        private SortWalletLedger InstallDisk()
        {
            var app=AllSortedApp.Interface;var ledger=Load();
            typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app.GetModel<SortWalletModel>(),ledger,null);
            app.SendCommand(new StartSortLevelCommand(1));return ledger;
        }
        private static bool BuyCommand(SortBooster booster,int attempt)
        {
            var type=typeof(SortWalletLedger).Assembly.GetType("AllSorted.BuySortBoosterCommand");
            Assert.IsNotNull(type,"Missing purchase command capability.");
            var command=Activator.CreateInstance(type,new object[]{booster,attempt});
            AllSortedApp.Interface.SendCommand((ICommand)command);
            return (bool)type.GetProperty("Accepted").GetValue(command,null);
        }

        [Test] public void PurchaseCommandRequiresCurrentPausedAttemptAndDepletedStockWithoutApplyingEffect()
        {
            var ledger=InstallDisk();Empty(ledger,SortBooster.Freeze);var app=AllSortedApp.Interface;
            var model=app.GetModel<SortBoardModel>();var inventory=app.GetModel<SortBoosterModel>();
            Assert.IsFalse(BuyCommand(SortBooster.Freeze,ledger.AttemptId));
            app.SendCommand(new PauseSortCommand(true));
            Assert.IsFalse(BuyCommand(SortBooster.Freeze,ledger.AttemptId+1));Assert.IsFalse(BuyCommand(SortBooster.Freeze,0));
            Assert.IsFalse(BuyCommand(SortBooster.Vacuum,ledger.AttemptId));
            var board=model.Board;int revision=model.BoardRevision.Value,stockRevision=inventory.Revision.Value;
            float seconds=board.RemainingSeconds;
            Assert.IsTrue(BuyCommand(SortBooster.Freeze,ledger.AttemptId));
            Assert.AreSame(board,model.Board);Assert.AreEqual(SortPhase.Paused,board.Phase);Assert.AreEqual(seconds,board.RemainingSeconds);
            Assert.AreEqual(0,board.FreezeSeconds);Assert.AreEqual(revision,model.BoardRevision.Value);Assert.AreEqual(stockRevision+1,inventory.Revision.Value);
            Assert.AreEqual(3,inventory.GetCharges(SortBooster.Freeze));Assert.AreEqual(0,ledger.Coins);
            Assert.IsFalse(BuyCommand(SortBooster.Freeze,ledger.AttemptId));
        }

        [Test] public void FailedUseSaveLeavesBoardAndObservableInventoryUnchanged()
        {
            var ledger=InstallDisk();var app=AllSortedApp.Interface;var model=app.GetModel<SortBoardModel>();
            var inventory=app.GetModel<SortBoosterModel>();int boardRevision=model.BoardRevision.Value,stockRevision=inventory.Revision.Value;
            int stateRevision=model.Board.Revision,remaining=model.Board.RemainingCount;disk.Fail=true;
            var command=new UseSortBoosterCommand(SortBooster.Freeze);app.SendCommand(command);
            Assert.IsFalse(command.Accepted);Assert.AreEqual(0,command.AffectedPieceIds.Length);Assert.AreEqual(0,model.Board.FreezeSeconds);
            Assert.AreEqual(stateRevision,model.Board.Revision);Assert.AreEqual(remaining,model.Board.RemainingCount);
            Assert.AreEqual(boardRevision,model.BoardRevision.Value);Assert.AreEqual(stockRevision,inventory.Revision.Value);
            Assert.AreEqual(3,inventory.GetCharges(SortBooster.Freeze));Assert.IsNotEmpty(ledger.Error);
        }

        [Test] public void InvalidV2InventoryIsProtectedBeforeAnyBackupRestoreOrTransaction()
        {
            string payload="{\"Coins\":1600,\"Energy\":5,\"NextRecoveryUtcTicks\":0,\"LastSeenUtcTicks\":"+clock.UtcNow.UtcTicks+",\"BoosterCharges\":[3,-1,3,3]}";
            disk.Write(SortWalletLedger.SaveSlot,JsonUtility.ToJson(new SaveEnvelope{Version=2,Payload=payload,SavedAtUtc=clock.UtcNow.ToString("O"),Checksum=SaveChecksum.Compute(2,payload)}));
            string before=Raw();var ledger=Load();Assert.IsFalse(ledger.Writable);Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",SortBooster.Shuffle));Assert.AreEqual(before,Raw());
        }

        [Test] public void FutureSaveCannotBePurchasedOrOverwritten()
        {
            SeedV1(9000,5,0,99);string before=Raw();var ledger=Load();
            Assert.IsFalse(Invoke<bool>(ledger,"BuyBooster",SortBooster.Vacuum));Assert.IsFalse(ledger.Writable);Assert.AreEqual(before,Raw());
        }
    }
}
