using GameFoundation.Core;
using GameFoundation.Save;
using QFramework;
namespace AllSorted
{
    public sealed class SortWalletSystem : AbstractSystem
    {
        private SortWalletModel model;
        private SortBoosterModel boosters;
        protected override void OnInit()
        {
            model=this.GetModel<SortWalletModel>();boosters=this.GetModel<SortBoosterModel>();var store=this.GetUtility<ISaveStore>();var clock=this.GetUtility<IFoundationClock>();
            model.Ledger=new SortWalletLedger(store,clock);boosters.BindLedger(()=>model.Ledger);Publish();
        }
        public bool Refresh(){bool accepted=model.Ledger.Refresh();Publish();return accepted;}
        public bool ClaimVictoryReward(int token){bool accepted=model.Ledger.ClaimVictoryReward(token);Publish();return accepted;}
        public bool AddCoins(){bool accepted=model.Ledger.AddCoins();Publish();return accepted;}
        public bool ClaimBundle(int index){bool accepted=model.Ledger.ClaimBundle(index);PublishInventory(accepted);return accepted;}
        public bool ClaimCoinPack(int index){bool accepted=model.Ledger.ClaimCoinPack(index);Publish();return accepted;}
        public bool BuyEntryBooster(int index){bool accepted=model.Ledger.BuyEntryBooster(index);Publish();return accepted;}
        public bool AddEntryCharge(int index){bool accepted=model.Ledger.AddEntryCharge(index);Publish();return accepted;}
        public bool SpendEntryCharges(int mask){bool accepted=model.Ledger.SpendEntryCharges(mask);Publish();return accepted;}
        public bool AddEnergy(){bool accepted=model.Ledger.AddEnergy();Publish();return accepted;}
        public bool EndAttempt(int token){bool accepted=model.Ledger.EndAttempt(token);Publish();return accepted;}
        public int BeginAttempt(){return model.Ledger.BeginAttempt();}
        public bool Flush(){bool accepted=model.Ledger.Flush();Publish();return accepted;}
        public bool BuyBooster(SortBooster booster){bool accepted=model.Ledger.BuyBooster(booster);PublishInventory(accepted);return accepted;}
        public bool TrySpendBooster(SortBooster booster){bool accepted=model.Ledger.TrySpendBooster(booster);PublishInventory(accepted);return accepted;}
        internal bool AddBoosterCharge(SortBooster booster){bool accepted=model.Ledger.TryAddBoosterCharge(booster);PublishInventory(accepted);return accepted;}
        private void PublishInventory(bool accepted){Publish();if(accepted)boosters.Revision.Value++;}
        private void Publish(){model.Revision.Value=model.Ledger.Revision;}
    }
}
