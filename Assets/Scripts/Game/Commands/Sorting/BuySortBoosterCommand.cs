using QFramework;

namespace AllSorted
{
    public sealed class BuySortBoosterCommand : AbstractCommand
    {
        private readonly SortBooster booster;
        private readonly int attempt;
        public bool Accepted {get;private set;}
        public BuySortBoosterCommand(SortBooster booster,int attempt){this.booster=booster;this.attempt=attempt;}
        protected override void OnExecute()
        {
            var system=this.GetSystem<SortGameSystem>();
            Accepted=system.BuyBooster(booster,attempt);
        }
    }
}
