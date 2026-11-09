using QFramework;

namespace AllSorted
{
    public sealed class ClaimSortCoinPackCommand : AbstractCommand
    {
        private readonly int index;
        public bool Accepted { get; private set; }

        public ClaimSortCoinPackCommand(int index) { this.index = index; }

        protected override void OnExecute()
        {
            var wallet = this.GetSystem<SortWalletSystem>();
            Accepted = wallet.ClaimCoinPack(index);
        }
    }
}
