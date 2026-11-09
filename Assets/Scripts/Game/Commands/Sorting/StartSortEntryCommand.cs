using QFramework;
namespace AllSorted
{
    public sealed class StartSortEntryCommand : AbstractCommand
    {
        private readonly int level,mask;public bool Accepted {get;private set;}
        public StartSortEntryCommand(int level,int mask){this.level=level;this.mask=mask;}
        protected override void OnExecute(){var system=this.GetSystem<SortGameSystem>();Accepted=system.StartEntryLevel(level,mask);}
    }
    public sealed class AddSortEntryChargeCommand : AbstractCommand
    {
        private readonly int index;public bool Accepted {get;private set;}
        public AddSortEntryChargeCommand(int index){this.index=index;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.AddEntryCharge(index);}
    }
}
