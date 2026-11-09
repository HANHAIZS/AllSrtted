using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AllSorted.Editor
{
    [InitializeOnLoad]
    public static class PrototypeLevelMenu
    {
        private const string ScenePath="Assets/Scenes/GameScene.unity";

        /// <summary>Clears cancelled launch intent when returning to Edit Mode.</summary>
        static PrototypeLevelMenu()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredEditMode) SessionState.EraseInt(SortGameViewController.EditorPreviewStartKey);
            };
        }

        /// <summary>Starts a fresh preview of the first project-owned level.</summary>
        [MenuItem("All Sorted/关卡选择/第 1 关",false,100)]
        public static void PlayFirst() {PlayLevel(1);}

        /// <summary>Starts a fresh preview of the second project-owned level.</summary>
        [MenuItem("All Sorted/关卡选择/第 2 关",false,101)]
        public static void PlaySecond() {PlayLevel(2);}

        /// <summary>Starts a fresh preview of the third project-owned level.</summary>
        [MenuItem("All Sorted/关卡选择/第 3 关",false,102)]
        public static void PlayThird() {PlayLevel(3);}

        [MenuItem("All Sorted/关卡选择/第 4 关",false,103)] public static void PlayFourth(){PlayLevel(4);}
        [MenuItem("All Sorted/关卡选择/第 5 关",false,104)] public static void PlayFifth(){PlayLevel(5);}
        /// <summary>Prevents requests while scripts, imports or a Play Mode transition are in flight.</summary>
        [MenuItem("All Sorted/关卡选择/第 1 关",true)]
        [MenuItem("All Sorted/关卡选择/第 2 关",true)]
        [MenuItem("All Sorted/关卡选择/第 3 关",true)]
        [MenuItem("All Sorted/关卡选择/第 4 关",true)]
        [MenuItem("All Sorted/关卡选择/第 5 关",true)]
        private static bool CanSelect()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                EditorApplication.isPlaying==EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>Switches a running preview or safely opens the saved scene before entering Play Mode.</summary>
        private static void PlayLevel(int levelNumber)
        {
            if(!CanSelect()) return;
            if(EditorApplication.isPlaying) {
                var view=UnityEngine.Object.FindObjectOfType<SortGameViewController>();
                if(!view || !view.isActiveAndEnabled) {
                    var home=UnityEngine.Object.FindObjectOfType<SortHomeViewController>();
                    if(home && home.isActiveAndEnabled && Application.CanStreamedLevelBeLoaded("GameScene")) {
                        SessionState.SetInt(SortGameViewController.EditorPreviewStartKey,levelNumber);
                        SceneManager.LoadScene("GameScene",LoadSceneMode.Single);EditorApplication.isPaused=false;return;
                    }
                    Debug.LogError("Open the All Sorted prototype scene before selecting a level in Play Mode.");return;
                }
                view.PreviewLevelInEditor(levelNumber);EditorApplication.isPaused=false;return;
            }
            if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) {Debug.LogError("All Sorted prototype scene is missing: "+ScenePath);return;}
            if(SceneManager.GetActiveScene().path!=ScenePath || SceneManager.sceneCount!=1) {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            SessionState.SetInt(SortGameViewController.EditorPreviewStartKey,levelNumber);
            EditorApplication.isPlaying=true;
        }
    }
}
