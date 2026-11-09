using System;
using GameFoundation.Save;
namespace AllSorted
{
    public sealed partial class SortWalletLedger
    {
        public bool HasUnlimitedLives {get{return state.UnlimitedUntilUtcTicks>Now();}}
        public long UnlimitedSeconds {get{return Math.Max(0,(state.UnlimitedUntilUtcTicks-Now()+TimeSpan.TicksPerSecond-1)/TimeSpan.TicksPerSecond);}}
        /// <summary>One durable write grants the entire product, or exposes none of it.</summary>
        public bool ClaimBundle(int index)
        {
            int coins=SortBundleCatalog.GetCoins(index),charges=SortBundleCatalog.GetCharges(index);
            if(coins<=0 || !Refresh() || Coins>int.MaxValue-coins)return false;
            foreach(int count in state.BoosterCharges)if(count>int.MaxValue-charges)return false;
            foreach(int count in state.EntryCharges)if(count>int.MaxValue-charges)return false;
            long duration=SortBundleCatalog.GetHours(index)*TimeSpan.TicksPerHour;
            long start=Math.Max(Now(),state.UnlimitedUntilUtcTicks);
            if(start>DateTimeOffset.MaxValue.UtcTicks-duration)return false;
            var next=Copy(state);next.Coins+=coins;
            for(int i=0;i<4;i++)next.BoosterCharges[i]+=charges;
            for(int i=0;i<2;i++)next.EntryCharges[i]+=charges;
            next.UnlimitedUntilUtcTicks=start+duration;
            return Commit(next);
        }
        private sealed class UnlimitedMigration : ISaveMigration
        {
            public int FromVersion {get{return 3;}}public int ToVersion {get{return 4;}}
            public string Migrate(string payload)
            {
                var json=new JsonUtilitySaveSerializer();var old=json.Deserialize<SortWalletSave>(payload);
                if(!Valid(old))throw new InvalidOperationException("Legacy wallet save data is invalid.");
                old.UnlimitedUntilUtcTicks=0;return json.Serialize(old);
            }
        }
    }
}
