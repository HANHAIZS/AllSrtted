using System;
using System.IO;
using System.Reflection;
using GameFoundation.Core;
using GameFoundation.Save;
using NUnit.Framework;
using UnityEngine;
namespace AllSorted.Tests
{
    public sealed class SortWalletLedgerTests : WalletTestBase
    {
        private const string Slot="all-sorted-wallet";
        private MemorySaveStore memory;private TestClock clock;
        private sealed class TestClock : IFoundationClock
        {
            public DateTimeOffset Now=new DateTimeOffset(2026,10,27,0,0,0,TimeSpan.Zero);
            public DateTimeOffset UtcNow {get{return Now;}} public float RealtimeSinceStartup {get{return 0;}}
            public void Advance(double seconds){Now=Now.AddSeconds(seconds);}
        }
        private sealed class FailingStore : ISaveStore
        {
            public readonly MemorySaveStore Memory=new MemorySaveStore();public bool Fail;
            public bool TryRead(string key,out string value){return Memory.TryRead(key,out value);}
            public bool TryReadBackup(string key,out string value){return Memory.TryReadBackup(key,out value);}
            public void Write(string key,string value){if(Fail)throw new IOException("test disk failure");Memory.Write(key,value);}
            public void Restore(string key,string value){Memory.Restore(key,value);}public void Delete(string key){Memory.Delete(key);}
        }
        [SetUp] public void Fresh(){memory=new MemorySaveStore();clock=new TestClock();}
        private object Ledger(ISaveStore store=null)
        {
            var type=typeof(SortBoardModel).Assembly.GetType("AllSorted.SortWalletLedger");
            Assert.IsNotNull(type,"Resource ledger capability is missing.");
            return Activator.CreateInstance(type,new object[]{store??memory,clock});
        }
        private static T Read<T>(object ledger,string name){return (T)ledger.GetType().GetProperty(name).GetValue(ledger,null);}
        private static T Call<T>(object ledger,string name,params object[] args){return (T)ledger.GetType().GetMethod(name).Invoke(ledger,args);}
        private static void Spend(object ledger){int id=Call<int>(ledger,"BeginAttempt");Assert.IsTrue(Call<bool>(ledger,"EndAttempt",id));}
        private void Seed(int coins,int energy,long due,long seen,int version=1)
        {
            string payload="{\"Coins\":"+coins+",\"Energy\":"+energy+",\"NextRecoveryUtcTicks\":"+due+",\"LastSeenUtcTicks\":"+seen+"}";
            memory.Write(Slot,JsonUtility.ToJson(new SaveEnvelope{Version=version,SavedAtUtc=clock.Now.ToString("O"),Payload=payload,Checksum=SaveChecksum.Compute(version,payload)}));
        }
        [Test] public void VictoryRewardIsThirtyOncePerAttemptAndPersists()
        {
            var w=Ledger();var method=w.GetType().GetMethod("ClaimVictoryReward");Assert.IsNotNull(method,"Victory reward transaction missing");
            int token=Call<int>(w,"BeginAttempt");Assert.IsTrue(Call<bool>(w,"ClaimVictoryReward",token));Assert.IsTrue(Call<bool>(w,"ClaimVictoryReward",token));
            Assert.AreEqual(1630,Read<int>(w,"Coins"));Assert.AreEqual(1630,Read<int>(Ledger(),"Coins"));
            int next=Call<int>(w,"BeginAttempt");Assert.IsFalse(Call<bool>(w,"ClaimVictoryReward",token));Assert.IsTrue(Call<bool>(w,"ClaimVictoryReward",next));Assert.AreEqual(1660,Read<int>(w,"Coins"));
        }
        [Test] public void VictoryRewardSaveFailureCanRetryWithoutDoubleGrant()
        {
            var disk=new FailingStore();var w=Ledger(disk);Assert.IsNotNull(w.GetType().GetMethod("ClaimVictoryReward"));int token=Call<int>(w,"BeginAttempt");disk.Fail=true;
            Assert.IsFalse(Call<bool>(w,"ClaimVictoryReward",token));Assert.AreEqual(1600,Read<int>(w,"Coins"));disk.Fail=false;
            Assert.IsTrue(Call<bool>(w,"ClaimVictoryReward",token));Assert.IsTrue(Call<bool>(w,"ClaimVictoryReward",token));Assert.AreEqual(1630,Read<int>(Ledger(disk),"Coins"));
        }
        [Test] public void MissingSaveStarts1600AndFiveFull(){var w=Ledger();Assert.AreEqual(1600,Read<int>(w,"Coins"));Assert.AreEqual(5,Read<int>(w,"Energy"));Assert.AreEqual(0,Read<int>(w,"RecoverySeconds"));}
        [Test] public void CoinsAdd800AndSurviveReload(){var w=Ledger();Assert.IsTrue(Call<bool>(w,"AddCoins"));Assert.AreEqual(2400,Read<int>(Ledger(),"Coins"));}
        [Test] public void EnergyPlusAddsOneButNeverExceedsFive(){var w=Ledger();Spend(w);Spend(w);Assert.IsTrue(Call<bool>(w,"AddEnergy"));Assert.AreEqual(4,Read<int>(w,"Energy"));Call<bool>(w,"AddEnergy");Call<bool>(w,"AddEnergy");Assert.AreEqual(5,Read<int>(Ledger(),"Energy"));}
        [Test] public void ExactFifteenMinuteBoundaryRecoversOne(){var w=Ledger();Spend(w);clock.Advance(899);Call<bool>(w,"Refresh");Assert.AreEqual(4,Read<int>(w,"Energy"));Assert.AreEqual(1,Read<int>(w,"RecoverySeconds"));clock.Advance(1);Call<bool>(w,"Refresh");Assert.AreEqual(5,Read<int>(w,"Energy"));Assert.AreEqual(0,Read<int>(w,"RecoverySeconds"));}
        [Test] public void ManualChangesBelowFullPreservePartialInterval(){var w=Ledger();Spend(w);Spend(w);clock.Advance(600);Call<bool>(w,"AddEnergy");Assert.AreEqual(4,Read<int>(w,"Energy"));Assert.AreEqual(300,Read<int>(w,"RecoverySeconds"));clock.Advance(300);Call<bool>(w,"Refresh");Assert.AreEqual(5,Read<int>(w,"Energy"));}
        [Test] public void FullDiscardsStoredRecoveryTime(){var w=Ledger();clock.Advance(90000);Spend(w);Assert.AreEqual(4,Read<int>(w,"Energy"));Assert.AreEqual(900,Read<int>(w,"RecoverySeconds"));clock.Advance(899);Call<bool>(w,"Refresh");Assert.AreEqual(4,Read<int>(w,"Energy"));}
        [Test] public void OfflineCatchupPreservesRemainderAndCapsAtFive(){var w=Ledger();for(int i=0;i<5;i++)Spend(w);clock.Advance(1900);var loaded=Ledger();Assert.AreEqual(2,Read<int>(loaded,"Energy"));Assert.AreEqual(800,Read<int>(loaded,"RecoverySeconds"));clock.Advance(86400000);Call<bool>(loaded,"Refresh");Assert.AreEqual(5,Read<int>(loaded,"Energy"));}
        [Test] public void ClockRollbackCannotAwardEnergyOrResetPartialProgress(){var w=Ledger();Spend(w);clock.Advance(600);Call<bool>(w,"Flush");clock.Advance(-1000);var loaded=Ledger();Assert.AreEqual(4,Read<int>(loaded,"Energy"));Assert.AreEqual(300,Read<int>(loaded,"RecoverySeconds"));clock.Advance(1299);Call<bool>(loaded,"Refresh");Assert.AreEqual(4,Read<int>(loaded,"Energy"));clock.Advance(1);Call<bool>(loaded,"Refresh");Assert.AreEqual(5,Read<int>(loaded,"Energy"));}
        [Test] public void OneDebitPerAttemptAndOldAttemptCannotChargeNewBoard(){var w=Ledger();int first=Call<int>(w,"BeginAttempt");Call<bool>(w,"EndAttempt",first);Call<bool>(w,"EndAttempt",first);Assert.AreEqual(4,Read<int>(w,"Energy"));int second=Call<int>(w,"BeginAttempt");Assert.IsFalse(Call<bool>(w,"EndAttempt",first));Assert.AreEqual(4,Read<int>(w,"Energy"));Call<bool>(w,"EndAttempt",second);Assert.AreEqual(3,Read<int>(w,"Energy"));}
        [Test] public void ZeroRemainsZeroWhenEndingAnActiveAttempt(){var w=Ledger();for(int i=0;i<7;i++)Spend(w);Assert.AreEqual(0,Read<int>(w,"Energy"));Assert.IsFalse(Read<bool>(w,"CanEnter"));}
        [Test] public void CorruptPrimaryRecoversPreviousValidBackup(){var w=Ledger();Call<bool>(w,"AddCoins");memory.Write(Slot,"corrupt");var loaded=Ledger();Assert.AreEqual(2400,Read<int>(loaded,"Coins"));Assert.IsTrue(Read<bool>(loaded,"Writable"));}
        [Test] public void CorruptBothCopiesAreNotOverwritten(){memory.Write(Slot,"bad");memory.Write(Slot,"still bad");var w=Ledger();Assert.IsFalse(Read<bool>(w,"Writable"));Assert.IsFalse(Call<bool>(w,"AddCoins"));string raw;memory.TryRead(Slot,out raw);Assert.AreEqual("still bad",raw);}
        [Test] public void FutureSaveIsReadOnlyAndUnmodified(){Seed(9999,4,clock.Now.UtcTicks+TimeSpan.FromMinutes(15).Ticks,clock.Now.UtcTicks,99);string raw;memory.TryRead(Slot,out raw);var w=Ledger();Assert.IsFalse(Read<bool>(w,"Writable"));Call<bool>(w,"AddEnergy");string after;memory.TryRead(Slot,out after);Assert.AreEqual(raw,after);}
        [Test] public void InvalidBusinessDataIsNotAccepted(){Seed(-1,6,0,clock.Now.UtcTicks);var w=Ledger();Assert.IsFalse(Read<bool>(w,"Writable"));}
        [Test] public void FailedWriteDoesNotGrantOrDebitAndCanRetry(){var disk=new FailingStore();var w=Ledger(disk);int id=Call<int>(w,"BeginAttempt");disk.Fail=true;Assert.IsFalse(Call<bool>(w,"AddCoins"));Assert.AreEqual(1600,Read<int>(w,"Coins"));Assert.IsFalse(Call<bool>(w,"EndAttempt",id));Assert.AreEqual(5,Read<int>(w,"Energy"));disk.Fail=false;Assert.IsTrue(Call<bool>(w,"EndAttempt",id));Assert.AreEqual(4,Read<int>(w,"Energy"));Assert.IsTrue(Call<bool>(w,"AddCoins"));}
        [Test] public void CoinOverflowSaturatesRatherThanWrapping(){Seed(int.MaxValue-100,5,0,clock.Now.UtcTicks);var w=Ledger();Call<bool>(w,"AddCoins");Assert.AreEqual(int.MaxValue,Read<int>(w,"Coins"));}
        [Test] public void TransientSaveFailureRecoversOnRefreshWithoutAnotherGrant(){var disk=new FailingStore();var w=Ledger(disk);disk.Fail=true;Call<bool>(w,"AddCoins");Assert.IsFalse(Read<bool>(w,"CanEnter"));disk.Fail=false;Assert.IsTrue(Call<bool>(w,"Refresh"));Assert.AreEqual(1600,Read<int>(w,"Coins"));Assert.IsTrue(Read<bool>(w,"CanEnter"));}
        [Test] public void FuturePrimaryWithOlderBackupMustNotBeDowngraded(){var w=Ledger();Seed(9999,5,0,clock.Now.UtcTicks,99);string before;memory.TryRead(Slot,out before);var loaded=Ledger();Assert.IsFalse(Read<bool>(loaded,"Writable"));string after;memory.TryRead(Slot,out after);Assert.AreEqual(before,after);}
        [Test] public void CountdownRefreshDoesNotWriteEverySecond(){var w=Ledger();Spend(w);string first;memory.TryRead(Slot,out first);for(int i=0;i<20;i++){clock.Advance(1);Call<bool>(w,"Refresh");}string after;memory.TryRead(Slot,out after);Assert.AreEqual(first,after);Assert.AreEqual(880,Read<int>(w,"RecoverySeconds"));}
        [Test] public void FailedSettlementSurvivesReturnHomeAndRefreshDebitsItBeforeNewEntry()
        {
            var disk=new FailingStore();var w=Ledger(disk);int id=Call<int>(w,"BeginAttempt");disk.Fail=true;
            Assert.IsFalse(Call<bool>(w,"EndAttempt",id));Assert.AreEqual(5,Read<int>(w,"Energy"));Assert.IsFalse(Read<bool>(w,"CanEnter"));
            disk.Fail=false;Assert.IsTrue(Call<bool>(w,"Refresh"));Assert.AreEqual(4,Read<int>(w,"Energy"));Assert.IsTrue(Read<bool>(w,"AttemptDebited"));
            Assert.IsTrue(Call<bool>(w,"EndAttempt",id));Assert.AreEqual(4,Read<int>(w,"Energy"));
        }
        [Test] public void FileStoreRoundTripKeepsResourcesAndRejectsAnUnreadableExistingPath()
        {
            var type=typeof(SortBoardModel).Assembly.GetType("AllSorted.SortResourceSaveStore");Assert.IsNotNull(type,"Strict project save read boundary missing.");
            string folder=Path.Combine(Path.GetTempPath(),"AllSortedWallet-"+Guid.NewGuid().ToString("N"));
            try {
                var store=(ISaveStore)Activator.CreateInstance(type,new object[]{folder});var w=Ledger(store);Call<bool>(w,"AddCoins");Spend(w);
                var loaded=Ledger(store);Assert.AreEqual(2400,Read<int>(loaded,"Coins"));Assert.AreEqual(4,Read<int>(loaded,"Energy"));
                string path=Path.Combine(folder,Slot+".json");File.Delete(path);Directory.CreateDirectory(path);
                var blocked=Ledger(store);Assert.IsFalse(Read<bool>(blocked,"Writable"));Assert.IsFalse(Call<bool>(blocked,"AddCoins"));Assert.IsTrue(Directory.Exists(path));
            } finally {if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
