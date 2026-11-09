using QFramework;

namespace AllSorted
{
    public sealed class CompleteSortCardTransformationCommand : AbstractCommand
    {
        private readonly SortBoardState board;

        public CompleteSortCardTransformationCommand(SortBoardState board)
        {
            this.board = board;
        }

        protected override void OnExecute()
        {
            this.GetSystem<SortGameSystem>().CompleteCardTransformation(board);
        }
    }
}
