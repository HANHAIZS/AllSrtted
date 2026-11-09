using QFramework;

namespace AllSorted
{
    public enum SortSetting { Music, Sound, Haptic }

    public sealed class SortSettingsModel : AbstractModel
    {
        private readonly bool[] enabled={true,true,true};
        public readonly BindableProperty<int> Revision=new BindableProperty<int>(0);

        /// <summary>Returns the session preference; unknown settings are unavailable.</summary>
        public bool GetEnabled(SortSetting setting)
        {
            int index=(int)setting;
            return index>=0 && index<enabled.Length && enabled[index];
        }

        /// <summary>Flips one valid preference and publishes the completed change once.</summary>
        internal bool TryToggle(SortSetting setting)
        {
            int index=(int)setting;
            if(index<0 || index>=enabled.Length) return false;
            enabled[index]=!enabled[index];Revision.Value++;return true;
        }

        /// <summary>Defaults belong to this model's lifetime, independent of board replacement.</summary>
        protected override void OnInit() { }
    }
}
