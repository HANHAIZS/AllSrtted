using QFramework;
namespace AllSorted
{
    public sealed class ClaimSortBundleCommand : AbstractCommand
    {
        private readonly int index;
        public bool Accepted {get;private set;}
        public ClaimSortBundleCommand(int index){this.index=index;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.ClaimBundle(index);}
    }
}
