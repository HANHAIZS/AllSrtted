using QFramework;
namespace AllSorted
{
    public sealed class SortBoardModel : AbstractModel
    {
        public ISortLevelRepository Levels {get;private set;}
        public bool IsEntering {get;internal set;}
        public SortBoardState Board { get; internal set; }
        public readonly BindableProperty<int> LevelNumber=new BindableProperty<int>(1);
        public int TotalMatches {get {return Board.InitialCount/3;}}
        public readonly BindableProperty<int> BoardRevision=new BindableProperty<int>(0);
        public readonly BindableProperty<int> MatchCount=new BindableProperty<int>(0);
        public readonly BindableProperty<int> Seconds=new BindableProperty<int>(180);
        public readonly BindableProperty<SortPhase> Phase=new BindableProperty<SortPhase>(SortPhase.Playing);

        /// <summary>Initializes only project-local runtime state; no save or original data is loaded.</summary>
        protected override void OnInit() { Levels=this.GetUtility<ISortLevelRepository>();Board=Levels.Create(1); }
    }
}
