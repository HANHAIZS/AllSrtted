using QFramework;
namespace AllSorted
{
    public sealed class StartSortLevelCommand : AbstractCommand
    {
        private readonly int levelNumber;
        /// <summary>Contains an explicit level selection or zero for retrying the current level.</summary>
        public StartSortLevelCommand(int levelNumber=0) {this.levelNumber=levelNumber;}
        /// <summary>Routes start/retry through the project system.</summary>
        protected override void OnExecute() { var system=this.GetSystem<SortGameSystem>();system.StartLevel(levelNumber); }
    }

    public sealed class RestartSortLevelCommand : AbstractCommand
    {
        private readonly int token;
        public bool Accepted {get;private set;}
        public RestartSortLevelCommand():this(0) { }
        public RestartSortLevelCommand(int token){this.token=token;}
        protected override void OnExecute(){var system=this.GetSystem<SortGameSystem>();Accepted=system.RestartLevel(token);}
    }

    public sealed class AdvanceSortLevelCommand : AbstractCommand
    {
        public bool Accepted {get;private set;}
        /// <summary>Routes progression through the System's winning-state guard.</summary>
        protected override void OnExecute() {var system=this.GetSystem<SortGameSystem>();Accepted=system.AdvanceLevel();}
    }

    public sealed class MoveSortPieceCommand : AbstractCommand
    {
        public int FromShelf,FromSlot,ToShelf,ToSlot;
        public bool Accepted { get; private set; }

        /// <summary>Contains one input request, not a second owner of board state.</summary>
        public MoveSortPieceCommand(int a,int b,int c,int d) { FromShelf=a;FromSlot=b;ToShelf=c;ToSlot=d; }

        /// <summary>Publishes the synchronous result for drag snap-back presentation.</summary>
        protected override void OnExecute()
        {
            var system=this.GetSystem<SortGameSystem>();Accepted=system.Move(FromShelf,FromSlot,ToShelf,ToSlot);
        }
    }

    public sealed class PauseSortCommand : AbstractCommand
    {
        private readonly bool paused;
        /// <summary>Captures an explicit pause/resume intent.</summary>
        public PauseSortCommand(bool value) { paused=value; }
        /// <summary>Keeps gameplay pause policy outside the Foundation UI router.</summary>
        protected override void OnExecute() { var system=this.GetSystem<SortGameSystem>();system.Pause(paused); }
    }

    public sealed class TickSortCommand : AbstractCommand
    {
        public float DeltaTime;
        /// <summary>Accepts a reused request to avoid allocating a command every frame.</summary>
        protected override void OnExecute() { var system=this.GetSystem<SortGameSystem>();system.Tick(DeltaTime); }
    }
}
