#if UNITY_EDITOR
using UnityEditor;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public const string EditorPreviewStartKey="AllSorted.EditorPreview.StartLevel";

        /// <summary>Consumes only an explicit Editor launch request; zero lets Home or direct-entry policy decide.</summary>
        private int ConsumeEditorPreviewLevel()
        {
            int level=SessionState.GetInt(EditorPreviewStartKey,0);SessionState.EraseInt(EditorPreviewStartKey);
            return level>=1 && level<=model.Levels.Count?level:0;
        }

        /// <summary>Lets Editor menus reuse the complete Command-based reset and modal cleanup path.</summary>
        public void PreviewLevelInEditor(int levelNumber)
        {
            if(levelNumber<1 || levelNumber>model.Levels.Count) throw new System.ArgumentOutOfRangeException(nameof(levelNumber));
            ReloadLevel(false,levelNumber);
        }
    }
}
#endif
