using System;
using GameFoundation.Core;
using GameFoundation.Save;
namespace AllSorted
{
    [Serializable]
    public sealed class SortWalletSave
    {
        public int Coins,Energy;
        public long NextRecoveryUtcTicks,LastSeenUtcTicks,UnlimitedUntilUtcTicks;
        public int[] BoosterCharges;
        public int[] EntryCharges;
    }

    /// <summary>The single resource state and its save-before-publish transactions.</summary>
    public sealed partial class SortWalletLedger
    {
        public const string SaveSlot="all-sorted-wallet";
        public const int MaxEnergy=5,RecoveryIntervalSeconds=900,BoosterPrice=1600;
        private const long IntervalTicks=RecoveryIntervalSeconds*TimeSpan.TicksPerSecond;
        private readonly VersionedSaveService<SortWalletSave> save;
        private readonly IFoundationClock clock;
        private SortWalletSave state;
        private long observedUtcTicks;
        private bool initialSavePending,attemptDebited,settlementPending;
        private int attempt;
        private bool victoryRewardClaimed;
        public int Coins {get{return state.Coins;}}
        public int Energy {get{return state.Energy;}}
        public int Revision {get;private set;}
        public string Error {get;private set;}
        public bool Writable {get;private set;}
        public bool CanEnter {get{return Writable && !settlementPending && string.IsNullOrEmpty(Error) && (Energy>0 || HasUnlimitedLives);}}
        public int AttemptId {get{return attempt;}}
        public bool AttemptDebited {get{return attemptDebited;}}
        public int RecoverySeconds
        {
            get {
                if(Energy>=MaxEnergy) return 0;
                long remaining=Math.Max(0,state.NextRecoveryUtcTicks-Now());
                return (int)Math.Min(int.MaxValue,(remaining+TimeSpan.TicksPerSecond-1)/TimeSpan.TicksPerSecond);
            }
        }

        public SortWalletLedger(ISaveStore store,IFoundationClock clock)
        {
            if(store==null) throw new ArgumentNullException("store");
            this.clock=clock??throw new ArgumentNullException("clock");
            observedUtcTicks=clock.UtcNow.UtcTicks;
            state=new SortWalletSave{Coins=1600,Energy=MaxEnergy,LastSeenUtcTicks=observedUtcTicks,BoosterCharges=new[]{3,3,3,3},EntryCharges=new[]{3,3}};
            save=new VersionedSaveService<SortWalletSave>(SaveSlot,4,store,new ValidatedSerializer(),clock,null,new ISaveMigration[]{new BoosterMigration(),new EntryMigration(),new UnlimitedMigration()});
            Writable=true;Error=string.Empty;
            try {
                // Foundation normally falls back to a backup; a valid newer primary must stay untouched.
                string raw;
                if(store.TryRead(SaveSlot,out raw)) {
                    SaveEnvelope envelope=null;
                    try {envelope=new JsonUtilitySaveSerializer().Deserialize<SaveEnvelope>(raw);} catch(Exception) { }
                    if(envelope!=null && envelope.Version>4 && SaveChecksum.IsValid(envelope.Version,envelope.Payload,envelope.Checksum)) {
                        Block("Resource save is newer than this application. Existing data retained.");return;
                    }
                }
                if(store.TryReadBackup(SaveSlot,out raw)) {
                    SaveEnvelope backup=null;
                    try {backup=new JsonUtilitySaveSerializer().Deserialize<SaveEnvelope>(raw);} catch(Exception) { }
                    if(backup!=null && backup.Version>4 && SaveChecksum.IsValid(backup.Version,backup.Payload,backup.Checksum)) {
                        Block("Resource backup is newer than this application. Existing data retained.");return;
                    }
                }
                var loaded=save.Load();
                if(loaded.IsSuccess) {
                    if(!Valid(loaded.Value)) {Block("Resource save data is invalid.");return;}
                    state=Copy(loaded.Value);observedUtcTicks=Math.Max(observedUtcTicks,state.LastSeenUtcTicks);
                } else if(loaded.Status==SaveLoadStatus.Missing) initialSavePending=true;
                else {Block("Resource save unavailable: "+loaded.Status+". Existing data retained.");return;}
            } catch(Exception error) {Block("Unable to read resources: "+error.Message);return;}
            Refresh();
        }

        /// <summary>Accrues whole UTC intervals; fractional time survives grants, reloads and offline catchup.</summary>
        public bool Refresh()
        {
            if(!Writable) return false;
            long now=Now();observedUtcTicks=now;
            var next=Copy(state);
            bool recovered=false;
            if(next.Energy<MaxEnergy && now>=next.NextRecoveryUtcTicks) {
                int gained=(int)Math.Min(MaxEnergy-next.Energy,1+(now-next.NextRecoveryUtcTicks)/IntervalTicks);
                next.Energy+=gained;
                next.NextRecoveryUtcTicks=next.Energy==MaxEnergy?0:next.NextRecoveryUtcTicks+gained*IntervalTicks;
                recovered=true;
            }
            if(settlementPending) {
                if(next.Energy>0) {
                    if(next.Energy==MaxEnergy)next.NextRecoveryUtcTicks=NextInterval(now);
                    next.Energy--;
                }
                if(!Commit(next))return false;
                settlementPending=false;attemptDebited=true;return true;
            }
            return (!recovered && !initialSavePending) || Commit(next);
        }

        public int GetEntryCharges(int index){return index>=0 && index<2?state.EntryCharges[index]:0;}
        public bool BuyEntryBooster(int index)
        {
            if(index<0 || index>=2 || GetEntryCharges(index)!=0 || !Refresh() || Coins<BoosterPrice)return false;
            var next=Copy(state);next.Coins-=BoosterPrice;next.EntryCharges[index]=3;return Commit(next);
        }
        public bool AddEntryCharge(int index)
        {
            if(index<0 || index>1 || GetEntryCharges(index)==int.MaxValue || !Refresh())return false;
            var next=Copy(state);next.EntryCharges[index]++;return Commit(next);
        }
        public bool SpendEntryCharges(int mask)
        {
            if(mask<0 || mask>3 || !Refresh())return false;
            if(mask==0)return true;
            for(int i=0;i<2;i++)if((mask&(1<<i))!=0 && GetEntryCharges(i)<=0)return false;
            var next=Copy(state);for(int i=0;i<2;i++)if((mask&(1<<i))!=0)next.EntryCharges[i]--;return Commit(next);
        }

        public int GetCharges(SortBooster booster)
        {
            int index=(int)booster;
            return index>=0 && index<4?state.BoosterCharges[index]:0;
        }

        /// <summary>Saves the debit and selected depleted allowance in one wallet transaction.</summary>
        public bool BuyBooster(SortBooster booster)
        {
            int index=(int)booster;
            if(index<0 || index>=4 || GetCharges(booster)!=0 || Coins<BoosterPrice || !Refresh())return false;
            var next=Copy(state);next.Coins-=BoosterPrice;next.BoosterCharges[index]=3;
            return Commit(next);
        }

        /// <summary>Durably reserves one use before the caller applies its validated board effect.</summary>
        public bool TrySpendBooster(SortBooster booster)
        {
            if(GetCharges(booster)<=0 || !Refresh())return false;
            var next=Copy(state);next.BoosterCharges[(int)booster]--;
            return Commit(next);
        }

        /// <summary>Compatibility for the developer refill command; player purchases use BuyBooster.</summary>
        internal bool TryAddBoosterCharge(SortBooster booster)
        {
            int index=(int)booster;
            if(index<0 || index>=4 || GetCharges(booster)!=0 || !Refresh())return false;
            var next=Copy(state);next.BoosterCharges[index]=1;return Commit(next);
        }

        /// <summary>Commits the complete simulated pack before exposing any granted coins.</summary>
        public bool ClaimCoinPack(int index)
        {
            int coins=SortCoinPackCatalog.GetCoins(index);
            if(coins<=0 || Coins>int.MaxValue-coins || !Refresh())return false;
            var next=Copy(state);next.Coins+=coins;
            return Commit(next);
        }

        /// <summary>Idempotent reward for a live attempt; publish only after the wallet save succeeds.</summary>
        public bool ClaimVictoryReward(int token)
        {
            if(token<=0 || token!=attempt)return false;
            if(victoryRewardClaimed)return true;
            if(Coins>int.MaxValue-30 || !Refresh())return false;
            var next=Copy(state);next.Coins+=30;
            if(!Commit(next))return false;
            victoryRewardClaimed=true;return true;
        }

        public bool AddCoins()
        {
            if(!Refresh()) return false;
            var next=Copy(state);next.Coins=(int)Math.Min(int.MaxValue,(long)next.Coins+800);
            return next.Coins==Coins || Commit(next);
        }
        public bool AddEnergy()
        {
            if(!Refresh()) return false;
            if(Energy>=MaxEnergy) return true;
            var next=Copy(state);next.Energy++;
            if(next.Energy==MaxEnergy) next.NextRecoveryUtcTicks=0;
            return Commit(next);
        }
        public int BeginAttempt()
        {
            if(settlementPending && !Refresh())throw new InvalidOperationException("The previous attempt is awaiting a resource save.");
            attempt=attempt==int.MaxValue?1:attempt+1;attemptDebited=false;victoryRewardClaimed=false;return attempt;
        }
        /// <summary>Repeated settlement succeeds without another debit; stale attempt tokens are rejected.</summary>
        public bool EndAttempt(int token)
        {
            if(token<=0 || token!=attempt) return false;
            if(attemptDebited) return true;
            if(HasUnlimitedLives){attemptDebited=true;return true;}
            settlementPending=true;return Refresh();
        }
        public bool Flush()
        {
            if(!Refresh()) return false;
            return state.LastSeenUtcTicks==Now() || Commit(Copy(state));
        }

        private long Now(){return Math.Max(observedUtcTicks,clock.UtcNow.UtcTicks);}
        private static long NextInterval(long now){return Math.Min(DateTimeOffset.MaxValue.UtcTicks,now+IntervalTicks);}
        private bool Commit(SortWalletSave next)
        {
            next.LastSeenUtcTicks=Now();
            try {save.Save(next);}
            catch(Exception error) {initialSavePending=true;SetError("Unable to save resources: "+error.Message);return false;}
            state=next;initialSavePending=false;Error=string.Empty;Revision++;return true;
        }
        private void Block(string error){Writable=false;SetError(error);}
        private void SetError(string error){if(Error!=error){Error=error;Revision++;}}
        private static bool Valid(SortWalletSave value)
        {
            if(!ValidOld(value) || value.EntryCharges==null || value.EntryCharges.Length!=2)return false;
            foreach(int count in value.EntryCharges)if(count<0)return false;
            if(value.UnlimitedUntilUtcTicks<0 || value.UnlimitedUntilUtcTicks>DateTimeOffset.MaxValue.UtcTicks)return false;
            return true;
        }
        private static bool ValidOld(SortWalletSave value)
        {
            if(!ValidResources(value))return false;
            if(value.BoosterCharges==null || value.BoosterCharges.Length!=4)return false;
            foreach(int count in value.BoosterCharges)if(count<0)return false;
            return true;
        }
        private static bool ValidResources(SortWalletSave value)
        {
            if(value==null || value.Coins<0 || value.Energy<0 || value.Energy>MaxEnergy || value.LastSeenUtcTicks<=0 || value.LastSeenUtcTicks>DateTimeOffset.MaxValue.UtcTicks) return false;
            if(value.Energy==MaxEnergy) return value.NextRecoveryUtcTicks==0;
            return value.NextRecoveryUtcTicks>0 && value.NextRecoveryUtcTicks<=NextInterval(value.LastSeenUtcTicks);
        }
        private sealed class ValidatedSerializer : ISaveSerializer
        {
            private readonly JsonUtilitySaveSerializer json=new JsonUtilitySaveSerializer();
            public string Serialize<T>(T value){return json.Serialize(value);}
            public T Deserialize<T>(string value)
            {
                var result=json.Deserialize<T>(value);
                if(typeof(T)==typeof(SortWalletSave) && !Valid(result as SortWalletSave))
                    throw new InvalidOperationException("Resource save data is invalid.");
                return result;
            }
        }

        private sealed class BoosterMigration : ISaveMigration
        {
            public int FromVersion {get{return 1;}}
            public int ToVersion {get{return 2;}}
            public string Migrate(string payload)
            {
                var json=new JsonUtilitySaveSerializer();var old=json.Deserialize<SortWalletSave>(payload);
                if(!ValidResources(old))throw new InvalidOperationException("Legacy resource save data is invalid.");
                old.BoosterCharges=new[]{3,3,3,3};return json.Serialize(old);
            }
        }
        private sealed class EntryMigration : ISaveMigration
        {
            public int FromVersion {get{return 2;}}public int ToVersion {get{return 3;}}
            public string Migrate(string payload){var json=new JsonUtilitySaveSerializer();var old=json.Deserialize<SortWalletSave>(payload);if(!ValidOld(old))throw new InvalidOperationException("Legacy booster save data is invalid.");old.EntryCharges=new[]{3,3};return json.Serialize(old);}
        }
        private static SortWalletSave Copy(SortWalletSave value)
        {
            return new SortWalletSave{Coins=value.Coins,Energy=value.Energy,NextRecoveryUtcTicks=value.NextRecoveryUtcTicks,LastSeenUtcTicks=value.LastSeenUtcTicks,UnlimitedUntilUtcTicks=value.UnlimitedUntilUtcTicks,BoosterCharges=(int[])value.BoosterCharges.Clone(),EntryCharges=(int[])value.EntryCharges.Clone()};
        }
    }
}
