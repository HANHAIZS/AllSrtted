using System;
using System.IO;
using System.Reflection;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;
namespace AllSorted.Tests
{
    public sealed class ShopBundleTests : WalletTestBase
    {
        private readonly WalletTestClock clock=new WalletTestClock();
        private sealed class Disk : ISaveStore
        {
            public readonly MemorySaveStore Memory=new MemorySaveStore();public bool Fail;public int Writes;
            public bool TryRead(string k,out string v){return Memory.TryRead(k,out v);}public bool TryReadBackup(string k,out string v){return Memory.TryReadBackup(k,out v);}
            public void Write(string k,string v){Writes++;if(Fail)throw new IOException("bundle disk failure");Memory.Write(k,v);}public void Restore(string k,string v){Memory.Restore(k,v);}public void Delete(string k){Memory.Delete(k);}
        }
        private static void End(SortWalletLedger w){Assert.IsTrue(w.EndAttempt(w.BeginAttempt()));}
        private static SortWalletSave State(SortWalletLedger w){return (SortWalletSave)typeof(SortWalletLedger).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w);}
        private void Seed(ISaveStore disk,SortWalletSave state,int version){var payload=JsonUtility.ToJson(state);disk.Write(SortWalletLedger.SaveSlot,JsonUtility.ToJson(new SaveEnvelope{Version=version,Payload=payload,Checksum=SaveChecksum.Compute(version,payload)}));}
        [TestCase(0,5000,1,1,"US$9.99")][TestCase(1,10000,3,3,"US$18.99")][TestCase(2,25000,6,12,"US$36.99")]
        public void ProductAwardsExactSixInventoryCountsCoinsAndDurationInOneWrite(int index,int coins,int count,int hours,string price)
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);int writes=disk.Writes;
            Assert.AreEqual(price,SortBundleCatalog.GetPrice(index));Assert.IsTrue(w.ClaimBundle(index));Assert.AreEqual(writes+1,disk.Writes);
            var loaded=new SortWalletLedger(disk,clock);Assert.AreEqual(1600+coins,loaded.Coins);for(int i=0;i<4;i++)Assert.AreEqual(3+count,loaded.GetCharges((SortBooster)i));for(int i=0;i<2;i++)Assert.AreEqual(3+count,loaded.GetEntryCharges(i));Assert.AreEqual(hours*3600,loaded.UnlimitedSeconds);
            string raw;disk.TryRead(SortWalletLedger.SaveSlot,out raw);Assert.AreEqual(4,JsonUtility.FromJson<SaveEnvelope>(raw).Version);
        }
        [Test] public void DurationStacksOnRemainingTimeAndCountsOfflineWithExactExpiry()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);w.ClaimBundle(0);clock.Now=clock.Now.AddMinutes(10);w.ClaimBundle(1);Assert.AreEqual(13800,w.UnlimitedSeconds);
            clock.Now=clock.Now.AddSeconds(13799);var load=new SortWalletLedger(disk,clock);Assert.AreEqual(1,load.UnlimitedSeconds);Assert.IsTrue(load.HasUnlimitedLives);
            clock.Now=clock.Now.AddSeconds(1);Assert.IsFalse(load.HasUnlimitedLives);Assert.AreEqual(0,load.UnlimitedSeconds);Assert.IsTrue(load.ClaimBundle(0));Assert.AreEqual(3600,load.UnlimitedSeconds);
        }
        [Test] public void InfiniteAtZeroPermitsEntryAndPreventsDebitsWhileNormalRecoveryContinues()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);for(int i=0;i<5;i++)End(w);Assert.IsFalse(w.CanEnter);w.ClaimBundle(0);Assert.IsTrue(w.CanEnter);
            End(w);End(w);Assert.AreEqual(0,w.Energy);clock.Now=clock.Now.AddMinutes(15);w.Refresh();Assert.AreEqual(1,w.Energy);End(w);Assert.AreEqual(1,w.Energy);
            clock.Now=clock.Now.AddMinutes(45);w.Refresh();Assert.IsFalse(w.HasUnlimitedLives);Assert.AreEqual(4,w.Energy);End(w);Assert.AreEqual(3,w.Energy);
        }
        [Test] public void SettlementDuringUnlimitedCannotChargeAgainAfterExpiry()
        {
            var w=new SortWalletLedger(new Disk(),clock);w.ClaimBundle(0);int id=w.BeginAttempt();Assert.IsTrue(w.EndAttempt(id));clock.Now=clock.Now.AddHours(1);w.Refresh();Assert.IsTrue(w.EndAttempt(id));Assert.AreEqual(5,w.Energy);End(w);Assert.AreEqual(4,w.Energy);
        }
        [Test] public void FailedWriteGrantsNothingAndRetryDoesNotDuplicate()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);disk.Fail=true;Assert.IsFalse(w.ClaimBundle(2));Assert.AreEqual(1600,w.Coins);Assert.IsFalse(w.HasUnlimitedLives);Assert.AreEqual(3,w.GetCharges(SortBooster.Vacuum));Assert.AreEqual(3,w.GetEntryCharges(0));disk.Fail=false;Assert.IsTrue(w.ClaimBundle(2));Assert.AreEqual(26600,new SortWalletLedger(disk,clock).Coins);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void AnyOverflowRejectsWholeBundle(int target)
        {
            var disk=new Disk();var source=new SortWalletLedger(disk,clock);var s=State(source);
            if(target==0)s.Coins=int.MaxValue;else if(target==1)s.BoosterCharges[2]=int.MaxValue;else if(target==2)s.EntryCharges[1]=int.MaxValue;else s.UnlimitedUntilUtcTicks=DateTimeOffset.MaxValue.UtcTicks;
            Seed(disk,s,4);var w=new SortWalletLedger(disk,clock);int writes=disk.Writes,coins=w.Coins,charges=w.GetCharges(SortBooster.Vacuum);Assert.IsFalse(w.ClaimBundle(2));Assert.AreEqual(coins,w.Coins);Assert.AreEqual(charges,w.GetCharges(SortBooster.Vacuum));Assert.AreEqual(writes,disk.Writes);
        }
        [TestCase(-1)][TestCase(3)] public void InvalidProductHasNoWrites(int index){var disk=new Disk();var w=new SortWalletLedger(disk,clock);int count=disk.Writes;Assert.IsFalse(w.ClaimBundle(index));Assert.AreEqual(count,disk.Writes);}
        [Test] public void VersionThreePreservesAllCountsAndBalancesAndInitializesExpiry()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);End(w);var s=State(w);s.Coins=3210;s.BoosterCharges=new[]{0,1,2,3};s.EntryCharges=new[]{4,1};Seed(disk,s,3);
            var old=new SortWalletLedger(disk,clock);Assert.IsTrue(old.Writable);Assert.AreEqual(3210,old.Coins);Assert.AreEqual(4,old.Energy);Assert.AreEqual(900,old.RecoverySeconds);Assert.AreEqual(4,old.GetEntryCharges(0));Assert.AreEqual(1,old.GetCharges(SortBooster.Shuffle));Assert.IsFalse(old.HasUnlimitedLives);Assert.IsTrue(old.ClaimBundle(2));Assert.AreEqual(7,new SortWalletLedger(disk,clock).GetCharges(SortBooster.Shuffle));
        }
        [Test] public void FutureSaveAndInvalidDeadlineCannotGrantOrOverwrite()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);Seed(disk,State(w),99);int count=disk.Writes;Assert.IsFalse(new SortWalletLedger(disk,clock).ClaimBundle(0));Assert.AreEqual(count,disk.Writes);
            var bad=new Disk();var s=State(w);s.UnlimitedUntilUtcTicks=-1;Seed(bad,s,4);Seed(bad,s,4);Assert.IsFalse(new SortWalletLedger(bad,clock).Writable);
        }
        [Test] public void UnlimitedCountdownDoesNotWriteEachSecondAndRollbackCannotExtendIt()
        {
            var disk=new Disk();var w=new SortWalletLedger(disk,clock);w.ClaimBundle(0);int count=disk.Writes;for(int i=0;i<30;i++){clock.Now=clock.Now.AddSeconds(1);w.Refresh();}Assert.AreEqual(count,disk.Writes);w.Flush();clock.Now=clock.Now.AddMinutes(-5);var loaded=new SortWalletLedger(disk,clock);Assert.AreEqual(3570,loaded.UnlimitedSeconds);
        }
        [Test] public void CommandRefreshesBothInventoriesAndUnlimitedAllowsGameFailureRestartAtZero()
        {
            var app=AllSortedApp.Interface;var wallet=app.GetModel<SortWalletModel>();var boosters=app.GetModel<SortBoosterModel>();var board=app.GetModel<SortBoardModel>();
            for(int i=0;i<5;i++){app.SendCommand(new StartSortLevelCommand(1));app.SendCommand(new EndSortAttemptCommand(wallet.AttemptId));}Assert.AreEqual(0,wallet.Energy);int revision=boosters.Revision.Value;
            var cmd=new ClaimSortBundleCommand(0);app.SendCommand(cmd);Assert.IsTrue(cmd.Accepted);Assert.Greater(boosters.Revision.Value,revision);Assert.AreEqual(4,boosters.GetCharges(SortBooster.Vacuum));
            var entry=new RequestSortHomeEntryCommand(1);app.SendCommand(entry);Assert.IsTrue(entry.Accepted);app.SendCommand(new StartSortLevelCommand(1));app.SendCommand(new TickSortCommand{DeltaTime=181});Assert.AreEqual(0,wallet.Energy);app.SendCommand(new RestartSortLevelCommand());Assert.AreEqual(SortPhase.Playing,board.Phase.Value);Assert.AreEqual(0,wallet.Energy);
        }
    }
}
