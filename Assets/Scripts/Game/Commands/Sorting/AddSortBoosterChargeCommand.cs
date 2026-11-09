using QFramework;

namespace AllSorted
{
    public sealed class AddSortBoosterChargeCommand : AbstractCommand
    {
        private readonly SortBooster booster;
        public bool Accepted { get; private set; }

        /// <summary>Captures one corner-badge refill request.</summary>
        public AddSortBoosterChargeCommand(SortBooster booster) {this.booster=booster;}

        /// <summary>Resolves the policy owner once without invoking the booster effect.</summary>
        protected override void OnExecute()
        {
            var system=this.GetSystem<SortGameSystem>();Accepted=system.AddBoosterCharge(booster);
        }
    }
}
