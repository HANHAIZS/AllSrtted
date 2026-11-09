using QFramework;
namespace AllSorted
{
    public sealed class SortWalletModel : AbstractModel
    {
        internal SortWalletLedger Ledger {get;set;}
        public readonly BindableProperty<int> Revision=new BindableProperty<int>(0);
        public int GetEntryCharges(int index){return Ledger==null?3:Ledger.GetEntryCharges(index);}
        public int Coins {get{return Ledger==null?1600:Ledger.Coins;}}
        public int Energy {get{return Ledger==null?5:Ledger.Energy;}}
        public int RecoverySeconds {get{return Ledger==null?0:Ledger.RecoverySeconds;}}
        public bool HasUnlimitedLives {get{return Ledger!=null && Ledger.HasUnlimitedLives;}}
        public long UnlimitedSeconds {get{return Ledger==null?0:Ledger.UnlimitedSeconds;}}
        public bool CanEnter {get{return Ledger!=null && Ledger.CanEnter;}}
        public string Error {get{return Ledger==null?string.Empty:Ledger.Error;}}
        public int AttemptId {get{return Ledger==null?0:Ledger.AttemptId;}}
        protected override void OnInit() { }
    }
}
