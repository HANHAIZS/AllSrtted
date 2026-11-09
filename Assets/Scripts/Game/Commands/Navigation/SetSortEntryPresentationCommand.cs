using QFramework;
namespace AllSorted
{
    public sealed class SetSortEntryPresentationCommand : AbstractCommand
    {
        private readonly bool entering;
        public SetSortEntryPresentationCommand(bool value){entering=value;}
        protected override void OnExecute(){var game=this.GetSystem<SortGameSystem>();game.SetEntryPresentation(entering);}
    }
}
