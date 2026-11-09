using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QFramework;

namespace AllSorted.Tests
{
    public sealed class SortSettingsTests : WalletTestBase
    {
        private int[] originalCharges;
        private int originalQuotaRevision;

        [SetUp] public void IsolateSessionSettings()
        {
            var quota=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            var charges=GetChargesForTest(quota);
            originalCharges=(int[])charges.Clone();originalQuotaRevision=quota.Revision.Value;
            for(int index=0;index<charges.Length;index++) charges[index]=3;
            quota.Revision.Value++;
            ResetSettings();
            AllSortedApp.Interface.SendCommand(new StartSortLevelCommand(1));
        }

        [TearDown] public void RestoreSettingsAfterTest()
        {
            ResetSettings();
            if(originalCharges==null) return;
            var quota=AllSortedApp.Interface.GetModel<SortBoosterModel>();
            Array.Copy(originalCharges,GetChargesForTest(quota),originalCharges.Length);
            quota.Revision.Value=originalQuotaRevision;
        }

        /// <summary>Fresh models default on; invalid settings cannot be treated as enabled.</summary>
        [Test] public void FreshSessionDefaultsAllSettingsOn()
        {
            var model=new SortSettingsModel();
            foreach(SortSetting setting in Enum.GetValues(typeof(SortSetting))) Assert.IsTrue(model.GetEnabled(setting));
            Assert.AreEqual(0,model.Revision.Value);
            Assert.IsFalse(model.GetEnabled((SortSetting)(-1)));
            Assert.IsFalse(model.GetEnabled((SortSetting)3));
            Assert.IsFalse(model.GetEnabled((SortSetting)int.MaxValue));
        }

        /// <summary>Each accepted command flips only its target and publishes its completed state once.</summary>
        [Test] public void EachToggleChangesOnlyItsSettingAndNotifiesExactlyOnce()
        {
            var model=AllSortedApp.Interface.GetModel<SortSettingsModel>();
            int notifications=0,revision=model.Revision.Value;
            foreach(SortSetting setting in Enum.GetValues(typeof(SortSetting))) {
                bool before=model.GetEnabled(setting);
                var registration=model.Revision.Register(value=> {
                    notifications++;
                    Assert.AreEqual(!before,model.GetEnabled(setting),"Observers must see the completed toggle.");
                });
                try {
                    Assert.IsTrue(Toggle(setting));
                    foreach(SortSetting other in Enum.GetValues(typeof(SortSetting)))
                        Assert.AreEqual(other!=setting,model.GetEnabled(other),other.ToString());
                    Assert.AreEqual(revision+1,model.Revision.Value);Assert.AreEqual(1,notifications);
                }
                finally {registration.UnRegister();}
                Assert.IsTrue(Toggle(setting));Assert.IsTrue(model.GetEnabled(setting));
                revision=model.Revision.Value;notifications=0;
            }
        }

        /// <summary>Mutating one application model cannot leak preferences into a new session.</summary>
        [Test] public void FreshModelDoesNotShareExistingSessionPreferences()
        {
            var current=AllSortedApp.Interface.GetModel<SortSettingsModel>();
            Assert.IsTrue(Toggle(SortSetting.Sound));Assert.IsFalse(current.GetEnabled(SortSetting.Sound));
            var fresh=new SortSettingsModel();
            Assert.IsTrue(fresh.GetEnabled(SortSetting.Sound));Assert.AreEqual(0,fresh.Revision.Value);
            Assert.AreNotSame(current,fresh);
        }

        /// <summary>Both ends of the enum boundary reject without a notification or gameplay mutation.</summary>
        [Test] public void InvalidSettingsRejectWithoutAnyStateChangeOrPublication()
        {
            var app=AllSortedApp.Interface;var settings=app.GetModel<SortSettingsModel>();
            var board=app.GetModel<SortBoardModel>();var quota=app.GetModel<SortBoosterModel>();
            string before=BoardSnapshot(board),charges=QuotaSnapshot(quota);int revision=settings.Revision.Value,notifications=0;
            var registration=settings.Revision.Register(value=>notifications++);
            try {
                foreach(int invalid in new[]{-1,3,999,int.MinValue,int.MaxValue}) {
                    Assert.IsFalse(Toggle((SortSetting)invalid));
                    foreach(SortSetting setting in Enum.GetValues(typeof(SortSetting))) Assert.IsTrue(settings.GetEnabled(setting));
                    Assert.AreEqual(revision,settings.Revision.Value);Assert.AreEqual(0,notifications);
                    Assert.AreEqual(before,BoardSnapshot(board));Assert.AreEqual(charges,QuotaSnapshot(quota));
                }
            }
            finally {registration.UnRegister();}
        }

        /// <summary>Settings remain usable while paused without changing pieces, either clock, or spent quota.</summary>
        [Test] public void TogglingPausedSettingsLeavesBoardClockAndBoosterQuotaUntouched()
        {
            var app=AllSortedApp.Interface;var board=app.GetModel<SortBoardModel>();
            var quota=app.GetModel<SortBoosterModel>();var previousBoard=board.Board;
            var freeze=new UseSortBoosterCommand(SortBooster.Freeze);app.SendCommand(freeze);Assert.IsTrue(freeze.Accepted);
            app.SendCommand(new TickSortCommand{DeltaTime=0.5f});app.SendCommand(new PauseSortCommand(true));
            string before=BoardSnapshot(board),charges=QuotaSnapshot(quota);
            foreach(SortSetting setting in Enum.GetValues(typeof(SortSetting))) Assert.IsTrue(Toggle(setting));
            Assert.AreSame(previousBoard,board.Board);Assert.AreEqual(before,BoardSnapshot(board));
            Assert.AreEqual(charges,QuotaSnapshot(quota));Assert.AreEqual(SortPhase.Paused,board.Phase.Value);
        }

        /// <summary>Retry and every explicit level replace only gameplay state, preserving the same settings model.</summary>
        [Test] public void RetryAndLevelSelectionPreservePreferencesAndSpentQuota()
        {
            var app=AllSortedApp.Interface;var settings=app.GetModel<SortSettingsModel>();
            var board=app.GetModel<SortBoardModel>();var quota=app.GetModel<SortBoosterModel>();
            Assert.IsTrue(Toggle(SortSetting.Music));Assert.IsTrue(Toggle(SortSetting.Haptic));
            var freeze=new UseSortBoosterCommand(SortBooster.Freeze);app.SendCommand(freeze);Assert.IsTrue(freeze.Accepted);
            int revision=settings.Revision.Value;string charges=QuotaSnapshot(quota);int notifications=0;
            var registration=settings.Revision.Register(value=>notifications++);
            try {
                foreach(int level in new[]{0,2,3,1}) {
                    var previousBoard=board.Board;app.SendCommand(new StartSortLevelCommand(level));
                    Assert.AreNotSame(previousBoard,board.Board);Assert.AreSame(settings,app.GetModel<SortSettingsModel>());
                    Assert.IsFalse(settings.GetEnabled(SortSetting.Music));Assert.IsTrue(settings.GetEnabled(SortSetting.Sound));
                    Assert.IsFalse(settings.GetEnabled(SortSetting.Haptic));Assert.AreEqual(revision,settings.Revision.Value);
                    Assert.AreEqual(0,notifications);Assert.AreEqual(charges,QuotaSnapshot(quota));
                    Assert.AreEqual(180f,board.Board.RemainingSeconds);Assert.AreEqual(0f,board.Board.FreezeSeconds);
                }
            }
            finally {registration.UnRegister();}
        }

        private static bool Toggle(SortSetting setting)
        {
            var command=new ToggleSortSettingCommand(setting);
            AllSortedApp.Interface.SendCommand(command);return command.Accepted;
        }

        // Test-only isolation: normal level/scene replacement deliberately preserves these values.
        private static void ResetSettings()
        {
            var model=AllSortedApp.Interface.GetModel<SortSettingsModel>();
            Assert.IsNotNull(model,"The existing application architecture must register session settings.");
            var enabled=(bool[])typeof(SortSettingsModel).GetField("enabled",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(model);
            for(int index=0;index<enabled.Length;index++) enabled[index]=true;
            model.Revision.Value++;
        }

        private static int[] GetChargesForTest(SortBoosterModel model)
        {
            return WalletTestScope.Charges();
        }

        private static string QuotaSnapshot(SortBoosterModel model)
        {
            return string.Join(",",Enum.GetValues(typeof(SortBooster)).Cast<SortBooster>().Select(model.GetCharges))+"|"+model.Revision.Value;
        }

        private static string BoardSnapshot(SortBoardModel model)
        {
            var board=model.Board;
            var slots=(List<SortPiece>[][])typeof(SortBoardState).GetField("slots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
            return string.Join("/",slots.SelectMany(row=>row).Select(stack=>string.Join(",",stack.Select(piece=>piece.InstanceId+":"+piece.Kind))))+
                "|"+board.Revision+"|"+board.Phase+"|"+board.RemainingCount+"|"+board.RemainingSeconds+"|"+board.FreezeSeconds+
                "|"+board.Matches+"|"+string.Join(",",board.BoosterPieceIds)+"|"+string.Join(",",board.MatchedShelves)+
                "|"+model.BoardRevision.Value+"|"+model.Seconds.Value+"|"+model.Phase.Value+"|"+model.MatchCount.Value+"|"+model.LevelNumber.Value;
        }
    }
}
