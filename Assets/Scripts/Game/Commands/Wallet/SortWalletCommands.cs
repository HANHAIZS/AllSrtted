using QFramework;
namespace AllSorted
{
    public sealed class AddSortCoinsCommand : AbstractCommand
    {
        public bool Accepted {get;private set;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.AddCoins();}
    }
    public sealed class AddSortEnergyCommand : AbstractCommand
    {
        public bool Accepted {get;private set;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.AddEnergy();}
    }
    public sealed class RefreshSortWalletCommand : AbstractCommand
    {
        public bool Flush;
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();if(Flush)wallet.Flush();else wallet.Refresh();}
    }
    public sealed class EndSortAttemptCommand : AbstractCommand
    {
        private readonly int token;public bool Accepted {get;private set;}
        public EndSortAttemptCommand(int token){this.token=token;}
        protected override void OnExecute(){var wallet=this.GetSystem<SortWalletSystem>();Accepted=wallet.EndAttempt(token);}
    }
}
