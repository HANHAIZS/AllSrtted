using System;
using QFramework;

namespace AllSorted
{
    /// <summary>Observable inventory projection; the wallet ledger owns all persisted counts.</summary>
    public sealed class SortBoosterModel : AbstractModel
    {
        private Func<SortWalletLedger> ledger;
        public readonly BindableProperty<int> Revision=new BindableProperty<int>(0);

        public int GetCharges(SortBooster booster)
        {
            int index=(int)booster;
            if(index<0 || index>=4)return 0;
            var current=ledger==null?null:ledger();
            return current==null?3:current.GetCharges(booster);
        }

        internal void BindLedger(Func<SortWalletLedger> source){ledger=source;}
        protected override void OnInit() { }
    }
}
