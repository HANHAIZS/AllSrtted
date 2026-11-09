using System;
using System.Collections;
using System.Reflection;
using GameFoundation.Core;
using GameFoundation.Save;
using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.TestTools;
namespace AllSorted.PlayTests
{
    public sealed class LegacySortLevelRepository : ISortLevelRepository
    {
        public int Count {get{return PrototypeLevel.Count;}}
        public SortBoardState Create(int number){return PrototypeLevel.Create(number);}
    }
    public sealed class WalletTestClock : IFoundationClock
    {
        public DateTimeOffset Now=new DateTimeOffset(2026,10,27,0,0,0,TimeSpan.Zero);
        public DateTimeOffset UtcNow {get{return Now;}} public float RealtimeSinceStartup {get{return Time.realtimeSinceStartup;}}
    }
    [SetUpFixture]
    public sealed class WalletTestScope
    {
        private static readonly FieldInfo ArchitectureField=typeof(Architecture<AllSortedApp>).GetField("mArchitecture",BindingFlags.Static|BindingFlags.NonPublic);
        private object previous;private Action<AllSortedApp> patch;
        public static WalletTestClock Clock;
        [OneTimeSetUp] public void UseMemoryOnly()
        {
            previous=ArchitectureField.GetValue(null);patch=AllSortedApp.OnRegisterPatch;Clock=new WalletTestClock();
            AllSortedApp.OnRegisterPatch=app=>{if(patch!=null)patch(app);app.RegisterUtility<ISortLevelRepository>(new LegacySortLevelRepository());app.RegisterUtility<ISaveStore>(new MemorySaveStore());app.RegisterUtility<IFoundationClock>(Clock);};
            ArchitectureField.SetValue(null,null);
        }
        [OneTimeTearDown] public void RestorePlayerArchitecture()
        {
            if(Application.isPlaying) foreach(var controller in UnityEngine.Object.FindObjectsOfType<AllSortedController>())UnityEngine.Object.DestroyImmediate(controller);
            ArchitectureField.SetValue(null,previous);AllSortedApp.OnRegisterPatch=patch;
        }
        // Fixtures may seed the authoritative in-memory save without production reset hooks.
        public static int[] Charges()
        {
            var model=AllSortedApp.Interface.GetModel<SortWalletModel>();
            var ledger=typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(model,null);
            var state=typeof(SortWalletLedger).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ledger);
            return ((SortWalletSave)state).BoosterCharges;
        }
        public static void Reset()
        {
            if(Clock==null)return;Clock.Now=new DateTimeOffset(2026,10,27,0,0,0,TimeSpan.Zero);
            var model=AllSortedApp.Interface.GetModel<SortWalletModel>();
            typeof(SortWalletModel).GetProperty("Ledger",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(model,new SortWalletLedger(new MemorySaveStore(),Clock),null);
            model.Revision.Value=1;
            AllSortedApp.Interface.GetModel<SortBoosterModel>().Revision.Value++;
        }
    }
    public abstract class WalletTestBase
    {
        [UnitySetUp] public IEnumerator ResetWalletBeforeEachTest(){WalletTestScope.Reset();yield break;}
    }
}
