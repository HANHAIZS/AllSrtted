using QFramework;
namespace AllSorted
{
    public sealed class BuySortEntryBoosterCommand : AbstractCommand
    {
        private readonly int index;public bool Accepted {get;private set;}
        public BuySortEntryBoosterCommand(int index){this.index=index;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.BuyEntryBooster(index);}
    }
}
