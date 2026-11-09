using QFramework;

namespace AllSorted
{
    public sealed class ToggleSortSettingCommand : AbstractCommand
    {
        private readonly SortSetting setting;
        public bool Accepted {get;private set;}

        /// <summary>Captures one independent session preference toggle.</summary>
        public ToggleSortSettingCommand(SortSetting setting) {this.setting=setting;}

        /// <summary>Resolves the settings owner once at execution setup.</summary>
        protected override void OnExecute()
        {
            var model=this.GetModel<SortSettingsModel>();Accepted=model.TryToggle(setting);
        }
    }
}
