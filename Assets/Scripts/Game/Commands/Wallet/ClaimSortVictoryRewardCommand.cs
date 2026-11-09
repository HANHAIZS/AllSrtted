using QFramework;
namespace AllSorted
{
    public sealed class ClaimSortVictoryRewardCommand : AbstractCommand
    {
        private readonly int token;
        public bool Accepted {get;private set;}
        public ClaimSortVictoryRewardCommand(int token){this.token=token;}
        protected override void OnExecute()
        {
            var board=this.GetModel<SortBoardModel>();var wallet=this.GetSystem<SortWalletSystem>();
            Accepted=board.Board!=null && board.Board.Phase==SortPhase.Won && wallet.ClaimVictoryReward(token);
        }
    }
}
