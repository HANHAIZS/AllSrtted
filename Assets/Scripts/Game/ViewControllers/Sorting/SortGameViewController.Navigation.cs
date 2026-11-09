using System;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public string HomeSceneName="HomeScene";
        private bool returningHome,entryFromHome;
        private int startingEntryBoosters;
        private bool entryStartFailed;
        private void ShowEntryStartFailure()
        {
            entryStartFailed=true;string error=string.IsNullOrEmpty(wallet.Error)?"Unable to start selected boosters. Please retry.":wallet.Error;
            StatusLabel.text=error;SetPauseCaption("HOME");this.SendCommand(new ReportSortEntryErrorCommand(error));TryRecoverEntryHome();
        }
        private bool TryRecoverEntryHome()
        {
            if(returningHome)return false;
            if(string.IsNullOrEmpty(HomeSceneName) || !Application.CanStreamedLevelBeLoaded(HomeSceneName)){Debug.LogError("Entry recovery Home scene unavailable: "+HomeSceneName,this);return false;}
            returningHome=true;
            try{SceneManager.LoadScene(HomeSceneName,LoadSceneMode.Single);return true;}
            catch(Exception error){returningHome=false;Debug.LogError("Unable to return from failed entry: "+error.Message,this);return false;}
        }

        /// <summary>Consumes the Home request once; direct and editor-only scene entries keep their defaults.</summary>
        private int ConsumeStartLevel()
        {
            var request=new ConsumeSortHomeEntryCommand();this.SendCommand(request);entryFromHome=request.LevelNumber>0;startingEntryBoosters=request.EntryBoosters;
#if UNITY_EDITOR
            int preview=ConsumeEditorPreviewLevel();if(preview>0) {entryFromHome=false;startingEntryBoosters=0;return preview;}
#endif
            return request.LevelNumber>0?request.LevelNumber:1;
        }

        /// <summary>Leaves only a top, open pause modal, retaining that modal if the target is unavailable.</summary>
        public bool TryReturnHome()
        {
            if(!ready || returningHome || !isActiveAndEnabled || (model.Board.Phase!=SortPhase.Paused && model.Board.Phase!=SortPhase.Lost && model.Board.Phase!=SortPhase.Won) || !ActivePopup || !ActivePopup.IsOpen) return false;
            if(string.IsNullOrEmpty(HomeSceneName) || !Application.CanStreamedLevelBeLoaded(HomeSceneName)) {Debug.LogError("Home scene is unavailable: "+HomeSceneName,this);return false;}
            int token=wallet.AttemptId;bool charge=model.Board.Phase!=SortPhase.Won;
            returningHome=true;CancelDrag();this.SendCommand(new ClearSortHomeEntryCommand());
            try {
                SceneManager.LoadScene(HomeSceneName,LoadSceneMode.Single);
                if(charge) {
                    var settle=new EndSortAttemptCommand(token);this.SendCommand(settle);
                    if(!settle.Accepted && StatusLabel)StatusLabel.text="RESOURCE SAVE PENDING. RETRYING FROM HOME.";
                }
                return true;
            }
            catch(Exception error) {returningHome=false;Debug.LogError("Unable to open Home scene: "+error.Message,this);return false;}
        }
    }
}
