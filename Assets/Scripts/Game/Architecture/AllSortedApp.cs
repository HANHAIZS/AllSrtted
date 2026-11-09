using GameFoundation.Core;
using GameFoundation.Save;
using GameFoundation.HotUpdate;
using GameFoundation.Sdk;
using GameFoundation.UI;
using QFramework;

namespace AllSorted
{
    public sealed class AllSortedApp : Architecture<AllSortedApp>
    {
        /// <summary>
        /// Registers shared infrastructure before project-owned models and systems.
        /// </summary>
        protected override void Init()
        {
            FoundationCoreModule.Register(this);
            FoundationSaveModule.Register(this);
            FoundationHotUpdateModule.Register(this);
            FoundationSdkModule.Register(this);
            FoundationUiModule.Register(this);
            RegisterUtility<ISaveStore>(new SortResourceSaveStore());

            RegisterUtility<ISortLevelRepository>(new SortExpressCampaign());
            RegisterModel(new SortBoardModel());
            RegisterModel(new SortBoosterModel());
            RegisterModel(new SortSettingsModel());
            RegisterModel(new SortHomeNavigationModel());
            RegisterModel(new SortWalletModel());
            RegisterSystem(new SortWalletSystem());
            RegisterSystem(new SortGameSystem());
        }
    }
}
