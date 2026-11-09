using System;
using NUnit.Framework;
using QFramework;
namespace AllSorted.Tests
{
    public sealed class SortWalletFlowTests : WalletTestBase
    {
        private IArchitecture app;private SortWalletModel wallet;private SortBoardModel board;
        [SetUp] public void Start(){app=AllSortedApp.Interface;wallet=app.GetModel<SortWalletModel>();board=app.GetModel<SortBoardModel>();app.SendCommand(new ClearSortHomeEntryCommand());app.SendCommand(new StartSortLevelCommand(1));}
        private void Restart()
        {
            var type=typeof(SortBoardModel).Assembly.GetType("AllSorted.RestartSortLevelCommand");Assert.IsNotNull(type,"Player restart command is missing.");app.SendCommand((ICommand)Activator.CreateInstance(type));
        }
        private void DrainTo(int count){for(int i=0;i<5 && wallet.Energy>count;i++){app.SendCommand(new EndSortAttemptCommand(wallet.AttemptId));app.SendCommand(new StartSortLevelCommand(1));}Assert.AreEqual(count,wallet.Energy,"Attempt settlement is not connected.");}
        [Test] public void TimeoutChargesOnlyOnceAcrossRepeatedTicks(){app.SendCommand(new TickSortCommand{DeltaTime=181});Assert.AreEqual(4,wallet.Energy);app.SendCommand(new TickSortCommand{DeltaTime=999});Assert.AreEqual(4,wallet.Energy);}
        [Test] public void FailureThenReplayDoesNotDebitThatAttemptAgain(){app.SendCommand(new TickSortCommand{DeltaTime=181});Restart();Assert.AreEqual(4,wallet.Energy);Assert.AreEqual(SortPhase.Playing,board.Phase.Value);Assert.AreEqual(180,board.Seconds.Value);}
        [Test] public void QuitSettlementRejectsStaleAttemptTokens(){int token=wallet.AttemptId;app.SendCommand(new EndSortAttemptCommand(token));Assert.AreEqual(4,wallet.Energy);app.SendCommand(new StartSortLevelCommand(2));var stale=new EndSortAttemptCommand(token);app.SendCommand(stale);Assert.IsFalse(stale.Accepted);Assert.AreEqual(4,wallet.Energy);}
        [Test] public void LastPointCanRestartToZeroButNextRestartKeepsBoard(){DrainTo(1);Restart();Assert.AreEqual(0,wallet.Energy);Assert.AreEqual(SortPhase.Playing,board.Phase.Value);var same=board.Board;Restart();Assert.AreSame(same,board.Board);Assert.AreEqual(0,wallet.Energy);}
        [Test] public void ZeroRejectsEntryWithoutLeavingPendingIntent(){DrainTo(0);var enter=new RequestSortHomeEntryCommand(1);app.SendCommand(enter);Assert.IsFalse(enter.Accepted);Assert.AreEqual(0,app.GetModel<SortHomeNavigationModel>().PendingLevel);}
        [Test] public void EntryDoesNotChargeAndPlusRestoresEligibility(){DrainTo(0);app.SendCommand(new AddSortEnergyCommand());var enter=new RequestSortHomeEntryCommand(2);app.SendCommand(enter);Assert.IsTrue(enter.Accepted);Assert.AreEqual(1,wallet.Energy);}
        [Test] public void UtcRecoveryContinuesWhileGameplayPaused(){DrainTo(0);app.SendCommand(new PauseSortCommand(true));WalletTestScope.Clock.Now=WalletTestScope.Clock.Now.AddSeconds(900);app.SendCommand(new TickSortCommand{DeltaTime=.1f});Assert.AreEqual(1,wallet.Energy);Assert.AreEqual(SortPhase.Paused,board.Phase.Value);}
        [Test] public void PauseAndResumeNeverCharge(){app.SendCommand(new PauseSortCommand(true));app.SendCommand(new PauseSortCommand(false));Assert.AreEqual(5,wallet.Energy);}
        private sealed class FailingStore : GameFoundation.Save.ISaveStore
        {
            private readonly GameFoundation.Save.MemorySaveStore store=new GameFoundation.Save.MemorySaveStore();public bool Fail;public int Writes;
            public bool TryRead(string slot,out string value){return store.TryRead(slot,out value);}
            public bool TryReadBackup(string slot,out string value){return store.TryReadBackup(slot,out value);}
            public void Write(string slot,string value){Writes++;if(Fail)throw new System.IO.IOException("test write failure");store.Write(slot,value);}
            public void Restore(string slot,string value){store.Restore(slot,value);}public void Delete(string slot){store.Delete(slot);}
        }
        [Test] public void FailedLossSettlementRetriesAtBoundedRateAndCannotFundAnotherBoard()
        {
            var disk=new FailingStore();typeof(SortWalletModel).GetProperty("Ledger",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(wallet,new SortWalletLedger(disk,WalletTestScope.Clock),null);
            app.SendCommand(new StartSortLevelCommand(1));disk.Fail=true;int before=disk.Writes;
            app.SendCommand(new TickSortCommand{DeltaTime=181});Assert.AreEqual(5,wallet.Energy);Assert.IsFalse(wallet.CanEnter);Assert.AreEqual(before+1,disk.Writes);
            for(int i=0;i<5;i++)app.SendCommand(new TickSortCommand{DeltaTime=.1f});Assert.AreEqual(before+1,disk.Writes);
            disk.Fail=false;app.SendCommand(new TickSortCommand{DeltaTime=1});Assert.AreEqual(4,wallet.Energy);
            Restart();Assert.AreEqual(4,wallet.Energy);Assert.AreEqual(SortPhase.Playing,board.Phase.Value);
        }
    }
}
