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
    public sealed class ShopCoinPackTests : WalletTestBase
    {
        private const string Slot = "all-sorted-wallet";
        private MemorySaveStore memory;
        private PackClock clock;

        private sealed class PackClock : IFoundationClock
        {
            public DateTimeOffset Now = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
            public DateTimeOffset UtcNow { get { return Now; } }
            public float RealtimeSinceStartup { get { return 0; } }
        }

        private sealed class FailingStore : ISaveStore
        {
            public readonly MemorySaveStore Memory = new MemorySaveStore();
            public bool Fail;
            public Action BeforeWrite;
            public bool TryRead(string slot, out string value) { return Memory.TryRead(slot, out value); }
            public bool TryReadBackup(string slot, out string value) { return Memory.TryReadBackup(slot, out value); }
            public void Write(string slot, string value)
            {
                if (BeforeWrite != null) BeforeWrite();
                if (Fail) throw new IOException("Shop test save failure");
                Memory.Write(slot, value);
            }
            public void Restore(string slot, string value) { Memory.Restore(slot, value); }
            public void Delete(string slot) { Memory.Delete(slot); }
        }

        [SetUp]
        public void FreshStore() { memory = new MemorySaveStore(); clock = new PackClock(); }

        private static Type RequiredType(string name)
        {
            var type = typeof(SortBoardModel).Assembly.GetType("AllSorted." + name);
            Assert.IsNotNull(type, name + " capability is missing.");
            return type;
        }

        private static T Call<T>(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method, name + " capability is missing.");
            return (T)method.Invoke(target, args);
        }

        private static T Read<T>(object target, string name)
        {
            return (T)target.GetType().GetProperty(name).GetValue(target, null);
        }

        private static T Catalog<T>(string name, int index)
        {
            var method = RequiredType("SortCoinPackCatalog").GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, name + " catalog mapping is missing.");
            return (T)method.Invoke(null, new object[] { index });
        }

        private object Ledger(ISaveStore store = null)
        {
            return Activator.CreateInstance(RequiredType("SortWalletLedger"), new object[] { store ?? memory, clock });
        }

        private void Seed(int coins, int energy = 4, int version = 2)
        {
            var state = new SortWalletSave {
                Coins = coins, Energy = energy, LastSeenUtcTicks = clock.Now.UtcTicks,
                NextRecoveryUtcTicks = energy == 5 ? 0 : clock.Now.AddSeconds(900).UtcTicks,
                BoosterCharges = new[] { 0, 1, 2, 3 }
            };
            string payload = JsonUtility.ToJson(state);
            memory.Write(Slot, JsonUtility.ToJson(new SaveEnvelope {
                Version = version, SavedAtUtc = clock.Now.ToString("O"), Payload = payload,
                Checksum = SaveChecksum.Compute(version, payload)
            }));
        }

        private static string Raw(ISaveStore store)
        {
            string raw;
            Assert.IsTrue(store.TryRead(Slot, out raw));
            return raw;
        }

        [TestCase(0, 1000, "$1.99", "1.000")]
        [TestCase(1, 5000, "$8.99", "5.000")]
        [TestCase(2, 10000, "$16.99", "10.000")]
        [TestCase(3, 25000, "$34.99", "25.000")]
        [TestCase(4, 50000, "$64.99", "50.000")]
        [TestCase(5, 100000, "$99.99", "100.000")]
        public void EachDisplayedPackGrantsItsFullAmountAndReloads(int index, int coins, string price, string display)
        {
            var count = RequiredType("SortCoinPackCatalog").GetField("Count", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(count, "Catalog pack count is missing.");
            Assert.AreEqual(6, count.GetValue(null));
            Assert.AreEqual(coins, Catalog<int>("GetCoins", index));
            Assert.AreEqual(price, Catalog<string>("GetPrice", index));
            Assert.AreEqual(display, Catalog<string>("GetDisplayAmount", index));
            var ledger = Ledger();
            Assert.IsTrue(Call<bool>(ledger, "ClaimCoinPack", index));
            Assert.AreEqual(1600 + coins, Read<int>(ledger, "Coins"));
            Assert.AreEqual(1600 + coins, Read<int>(Ledger(), "Coins"));
            Assert.AreEqual(4, JsonUtility.FromJson<SaveEnvelope>(Raw(memory)).Version);
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void InvalidPackDoesNotGrantOrRewriteSave(int index)
        {
            Assert.AreEqual(0, Catalog<int>("GetCoins", index));
            Assert.AreEqual(string.Empty, Catalog<string>("GetPrice", index));
            var ledger = Ledger();
            string before = Raw(memory);
            int revision = Read<int>(ledger, "Revision");
            Assert.IsFalse(Call<bool>(ledger, "ClaimCoinPack", index));
            Assert.AreEqual(1600, Read<int>(ledger, "Coins"));
            Assert.AreEqual(revision, Read<int>(ledger, "Revision"));
            Assert.AreEqual(before, Raw(memory));
        }

        [TestCase(0, 1000)]
        [TestCase(5, 100000)]
        public void OverflowRejectsWholePackAndExactBoundaryStillSucceeds(int index, int amount)
        {
            Seed(int.MaxValue - amount + 1);
            var ledger = Ledger();
            string before = Raw(memory);
            Assert.IsFalse(Call<bool>(ledger, "ClaimCoinPack", index));
            Assert.AreEqual(int.MaxValue - amount + 1, Read<int>(ledger, "Coins"));
            Assert.AreEqual(before, Raw(memory));
            Assert.AreEqual(int.MaxValue - amount + 1, Read<int>(Ledger(), "Coins"));
            Seed(int.MaxValue - amount);
            ledger = Ledger();
            Assert.IsTrue(Call<bool>(ledger, "ClaimCoinPack", index));
            Assert.AreEqual(int.MaxValue, Read<int>(Ledger(), "Coins"));
        }

        [Test]
        public void SaveFailurePublishesNoCoinsAndRetryGrantsOnce()
        {
            var disk = new FailingStore();
            var ledger = Ledger(disk);
            string before = Raw(disk);
            disk.Fail = true;
            disk.BeforeWrite = () => Assert.AreEqual(1600, Read<int>(ledger, "Coins"), "Coins published before durable save.");
            Assert.IsFalse(Call<bool>(ledger, "ClaimCoinPack", 3));
            Assert.AreEqual(1600, Read<int>(ledger, "Coins"));
            Assert.AreEqual(before, Raw(disk));
            Assert.IsNotEmpty(Read<string>(ledger, "Error"));
            disk.Fail = false;
            Assert.IsTrue(Call<bool>(ledger, "ClaimCoinPack", 3));
            disk.BeforeWrite = null;
            Assert.AreEqual(26600, Read<int>(Ledger(disk), "Coins"));
            Assert.IsEmpty(Read<string>(ledger, "Error"));
            Assert.IsTrue(Call<bool>(ledger, "ClaimCoinPack", 3));
            Assert.AreEqual(51600, Read<int>(Ledger(disk), "Coins"));
        }

        [Test]
        public void PackPreservesInventoryEnergyAndPartialRecoveryInterval()
        {
            Seed(2200);
            var ledger = Ledger();
            clock.Now = clock.Now.AddSeconds(600);
            Assert.IsTrue(Call<bool>(ledger, "ClaimCoinPack", 2));
            var loaded = Ledger();
            Assert.AreEqual(12200, Read<int>(loaded, "Coins"));
            Assert.AreEqual(4, Read<int>(loaded, "Energy"));
            Assert.AreEqual(300, Read<int>(loaded, "RecoverySeconds"));
            for (int i = 0; i < 4; i++) Assert.AreEqual(i, Call<int>(loaded, "GetCharges", (SortBooster)i));
            clock.Now = clock.Now.AddSeconds(300);
            Call<bool>(loaded, "Refresh");
            Assert.AreEqual(5, Read<int>(loaded, "Energy"));
        }

        [Test]
        public void ProtectedFutureAndCorruptSavesCannotReceivePacks()
        {
            Seed(1234, 5, 99);
            string before = Raw(memory);
            Assert.IsFalse(Call<bool>(Ledger(), "ClaimCoinPack", 0));
            Assert.AreEqual(before, Raw(memory));
            memory = new MemorySaveStore();
            memory.Write(Slot, "bad"); memory.Write(Slot, "still bad");
            Assert.IsFalse(Call<bool>(Ledger(), "ClaimCoinPack", 0));
            Assert.AreEqual("still bad", Raw(memory));
        }

        [TestCase(0, 2600)]
        [TestCase(1, 6600)]
        [TestCase(2, 11600)]
        [TestCase(3, 26600)]
        [TestCase(4, 51600)]
        [TestCase(5, 101600)]
        public void CommandPublishesTheSelectedPackToSharedWallet(int index, int expected)
        {
            var app = AllSortedApp.Interface;
            var wallet = app.GetModel<SortWalletModel>();
            var ledger = Ledger();
            typeof(SortWalletModel).GetProperty("Ledger", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(wallet, ledger, null);
            var command = Activator.CreateInstance(RequiredType("ClaimSortCoinPackCommand"), new object[] { index });
            app.SendCommand((ICommand)command);
            Assert.IsTrue(Read<bool>(command, "Accepted"));
            Assert.AreEqual(expected, wallet.Coins);
            Assert.AreEqual(Read<int>(ledger, "Revision"), wallet.Revision.Value);
            Assert.AreEqual(expected, Read<int>(Ledger(), "Coins"));
        }
    }
}
