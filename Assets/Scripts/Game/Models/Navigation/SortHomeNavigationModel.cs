using QFramework;
namespace AllSorted
{
    public enum SortHomeTab {Shop,Home,Settings}

    /// <summary>Owns menu selection and a single consumable scene-entry intent, never gameplay state.</summary>
    public sealed class SortHomeNavigationModel : AbstractModel
    {
        public readonly BindableProperty<SortHomeTab> SelectedTab=new BindableProperty<SortHomeTab>(SortHomeTab.Home);
        public readonly BindableProperty<int> Revision=new BindableProperty<int>(0);
        public int PendingLevel {get;private set;}
        public string EntryError {get;private set;}
        internal void SetEntryError(string error){EntryError=error;}
        internal string ConsumeEntryError(){string error=EntryError;EntryError=null;return error;}
        public int PendingEntryBoosters {get;private set;}
        private ISortLevelRepository levels;
        protected override void OnInit() {levels=this.GetUtility<ISortLevelRepository>();}
        internal bool Select(SortHomeTab tab)
        {
            if(tab<SortHomeTab.Shop || tab>SortHomeTab.Settings || SelectedTab.Value==tab) return false;
            SelectedTab.Value=tab;Revision.Value++;return true;
        }
        internal bool RequestEntry(int level,int mask=0)
        {
            if(mask<0 || mask>3 || level<1 || level>levels.Count || PendingLevel!=0) return false;
            PendingLevel=level;PendingEntryBoosters=mask;return true;
        }
        internal int ConsumeEntry() {int level=PendingLevel;PendingLevel=0;PendingEntryBoosters=0;return level;}
        internal void ClearEntry() {PendingLevel=0;PendingEntryBoosters=0;}
    }
}
