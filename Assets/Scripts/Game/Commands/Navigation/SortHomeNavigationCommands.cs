using QFramework;
namespace AllSorted
{
    public sealed class SelectSortHomeTabCommand : AbstractCommand
    {
        private readonly SortHomeTab tab;
        public bool Accepted {get;private set;}
        public SelectSortHomeTabCommand(SortHomeTab value) {tab=value;}
        protected override void OnExecute() {var model=this.GetModel<SortHomeNavigationModel>();Accepted=model.Select(tab);}
    }
    public sealed class RequestSortHomeEntryCommand : AbstractCommand
    {
        private readonly int level,mask;
        public bool Accepted {get;private set;}
        public RequestSortHomeEntryCommand(int value):this(value,0) { }
        public RequestSortHomeEntryCommand(int value,int boosters) {level=value;mask=boosters;}
        protected override void OnExecute() {
            var model=this.GetModel<SortHomeNavigationModel>();var wallet=this.GetSystem<SortWalletSystem>();var balances=this.GetModel<SortWalletModel>();
            if(mask<0 || mask>3)return;
            for(int i=0;i<2;i++)if((mask&(1<<i))!=0 && balances.GetEntryCharges(i)<=0)return;
            Accepted=wallet.Refresh() && balances.CanEnter && model.RequestEntry(level,mask);
        }
    }
    public sealed class ConsumeSortHomeEntryCommand : AbstractCommand
    {
        public int LevelNumber {get;private set;}
        public int EntryBoosters {get;private set;}
        protected override void OnExecute() {var model=this.GetModel<SortHomeNavigationModel>();EntryBoosters=model.PendingEntryBoosters;LevelNumber=model.ConsumeEntry();}
    }
    public sealed class ClearSortHomeEntryCommand : AbstractCommand
    {
        protected override void OnExecute() {var model=this.GetModel<SortHomeNavigationModel>();model.ClearEntry();}
    }
    public sealed class ReportSortEntryErrorCommand : AbstractCommand
    {
        private readonly string error;public ReportSortEntryErrorCommand(string error){this.error=error;}
        protected override void OnExecute(){var model=this.GetModel<SortHomeNavigationModel>();model.SetEntryError(error);}
    }
    public sealed class ConsumeSortEntryErrorCommand : AbstractCommand
    {
        public string Error {get;private set;}
        protected override void OnExecute(){var model=this.GetModel<SortHomeNavigationModel>();Error=model.ConsumeEntryError();}
    }
}
