using QFramework;

namespace AllSorted
{
    public sealed class UseSortBoosterCommand : AbstractCommand
    {
        private readonly SortBooster booster;
        private readonly bool animateCard;
        public bool Accepted { get; private set; }
        public int[] AffectedPieceIds { get; private set; }=new int[0];

        /// <summary>Captures one toolbar action without owning gameplay state.</summary>
        public UseSortBoosterCommand(SortBooster booster,bool animateCard=false) {this.booster=booster;this.animateCard=animateCard;}

        /// <summary>Resolves dependencies once and copies the accepted selection for transient presentation.</summary>
        protected override void OnExecute()
        {
            var system=this.GetSystem<SortGameSystem>();var model=this.GetModel<SortBoardModel>();
            Accepted=system.UseBooster(booster,animateCard);AffectedPieceIds=new int[Accepted?model.Board.BoosterPieceIds.Count:0];
            if(Accepted) for(int index=0;index<AffectedPieceIds.Length;index++) AffectedPieceIds[index]=model.Board.BoosterPieceIds[index];
        }
    }
}
