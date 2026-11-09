using GameFoundation.UI;
using QFramework;
using UnityEngine;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        private bool suspendedWithoutPopup,terminalFallback;
        private float nextResultAttempt;
        private string lastFailedResultKey;

        /// <summary>Prepares the lazy popup before changing either pause state or input gates.</summary>
        public void RequestPause()
        {
            if(!ready || entranceActive || cardBoard!=null) return;
            if(entryStartFailed){TryRecoverEntryHome();return;}
            if(terminalFallback) {RestartLevel();return;}
            if(suspendedWithoutPopup && model.Board.Phase==SortPhase.Paused) ResumeLevel();
            if(model.Board.Phase!=SortPhase.Playing) return;
            var popup=PreparePopup(PausePopupKey,true);if(!popup) return;
            CancelDrag();popup.Present("TAKE A BREATHER","Your shapes will be right here.","KEEP SORTING","START AGAIN");
            router.Show(popup.PopupId,UiPopupOpenMode.Replace);Layers.SetModal(true);
            this.SendCommand(new PauseSortCommand(true));
        }

        /// <summary>Resumes only a paused game and releases this feature's modal gate.</summary>
        public void ResumeLevel()
        {
            if(!ready || model.Board.Phase!=SortPhase.Paused) return;
            router.Clear(true);Layers.SetModal(false);this.SendCommand(new PauseSortCommand(false));seenPhase=SortPhase.Playing;
            suspendedWithoutPopup=false;SetPauseCaption(defaultPauseCaption);StatusLabel.text="MAKE ROOM. MATCH THREE.";
        }

        /// <summary>Restarts the active level rather than discarding its progression.</summary>
        public void RestartLevel()
        {
            if(!ready || !isActiveAndEnabled || entranceActive)return;
            var popup=ActivePopup;
            bool animate=!entranceActive && model.Board.Phase==SortPhase.Paused && popup && popup.IsOpen && popup.GetComponent<SortPauseSettingsView>();
            float closeSeconds=animate?Mathf.Max(0,popup.CloseSeconds):0;
            if(ReloadLevel(false) && animate)BeginRestartEntrance(closeSeconds);
        }

        /// <summary>Routes start/retry/next through Commands while invalidating all prior visual instances.</summary>
        private bool ReloadLevel(bool advance,int explicitLevel=0)
        {
            if(!ready || !isActiveAndEnabled || entranceActive || cardBoard!=null || (advance && model.Board.Phase!=SortPhase.Won)) return false;
            bool accepted=true;
            if(advance) {var command=new AdvanceSortLevelCommand();this.SendCommand(command);accepted=command.Accepted;}
            else if(explicitLevel>0) {
                if(entryFromHome){entryFromHome=false;var command=new StartSortEntryCommand(explicitLevel,startingEntryBoosters);startingEntryBoosters=0;this.SendCommand(command);accepted=command.Accepted;
                    if(!accepted){ShowEntryStartFailure();return false;}}
                else this.SendCommand(new StartSortLevelCommand(explicitLevel));
            }
            else {var command=new RestartSortLevelCommand(wallet.AttemptId);this.SendCommand(command);accepted=command.Accepted;}
            if(!accepted) {StatusLabel.text="NO ENERGY. RETURN HOME TO REFILL.";return false;}
            CancelDrag();router.Clear(true);Layers.SetModal(false);
            foreach(var p in pieces.Values) if(p.Transform) Destroy(p.Transform.gameObject);
            pieces.Clear();seenRevision=-1;seenMatches=0;seenPhase=SortPhase.Playing;
            suspendedWithoutPopup=false;terminalFallback=false;nextResultAttempt=0;SetPauseCaption(defaultPauseCaption);
            ClearBoosterFeedback();
            StatusLabel.text="MAKE ROOM. MATCH THREE.";
            ReconcilePieces();FitCamera();RefreshBoosterUi(true);return true;
        }

        /// <summary>Retries missing terminal UI at a bounded rate while retaining an explicit replay route.</summary>
        private void RefreshPhase()
        {
            var phase=model.Phase.Value;
            if(phase!=SortPhase.Won && phase!=SortPhase.Lost) {seenPhase=phase;return;}
            bool won=phase==SortPhase.Won;string resultKey=won?VictoryPopupKey:ResultPopupKey;
            if(Time.unscaledTime<nextResultAttempt && lastFailedResultKey==resultKey) return;
            CancelDrag();var popup=PreparePopup(resultKey,false,lastFailedResultKey!=resultKey);
            if(!popup) {
                lastFailedResultKey=resultKey;nextResultAttempt=Time.unscaledTime+1;
                terminalFallback=true;SetPauseCaption("RETRY");StatusLabel.text="RESULT UNAVAILABLE. TAP RETRY.";return;
            }
            seenPhase=phase;terminalFallback=false;lastFailedResultKey=null;SetPauseCaption(defaultPauseCaption);
            bool final=won && model.LevelNumber.Value==model.Levels.Count;
            string body=won?(final?"All "+model.Levels.Count+" levels are complete!":model.TotalMatches+" neat sets. Level "+model.LevelNumber.Value+" complete."):"A little more room, a fresh start.";
            if(wallet.Energy==0)body="NO ENERGY. WAIT OR TAP HOME TO REFILL.";
            popup.Present(won?"ALL SORTED!":"TIME TO TRY AGAIN",body,won?"Continue":"PLAY AGAIN",wallet.Energy==0?"HOME":"");
            if(won){popup.Body.gameObject.SetActive(wallet.Energy==0);var reward=popup.GetComponent<SortVictoryRewardView>();if(reward)reward.Present(wallet.AttemptId,wallet.Coins);}
            router.Show(popup.PopupId,UiPopupOpenMode.Replace);Layers.SetModal(true);
        }

        /// <summary>Loads one declared Resources key on first use and reuses the owned instance.</summary>
        private SortPopupView PreparePopup(string key,bool pause,bool reportFailure=true)
        {
            SortPopupView view;if(popups.TryGetValue(key,out view)) return view;
            var prefab=Resources.Load<GameObject>(key);
            if(!prefab) {if(reportFailure) Debug.LogError("Sorting popup resource missing: "+key,this);return null;}
            var go=Instantiate(prefab,Layers.Hosts[2].transform,false);view=go.GetComponent<SortPopupView>();
            var settings=pause?go.GetComponent<SortPauseSettingsView>():null;
            if(!view || !view.Primary || !view.Secondary || (pause && (!settings || !settings.HasValidBindings))) {Destroy(go);Debug.LogError("Invalid sorting popup: "+key,this);return null;}
            go.SetActive(false);var ownedView=view;var victory=view.GetComponent<SortVictoryRewardView>();
            if(victory)victory.Configure(()=>ready && isActiveAndEnabled && router.ActiveView==ownedView && model.Board.Phase==SortPhase.Won && wallet.AttemptId==victory.AttemptId,
                ()=>{var command=new ClaimSortVictoryRewardCommand(victory.AttemptId);this.SendCommand(command);return command.Accepted;},
                ()=>wallet.Coins,()=>ReloadLevel(true));
            view.Configure(()=>{if(router.ActiveView==ownedView) {if(pause) ResumeLevel();else if(victory)victory.Collect();else ReloadLevel(model.Board.Phase==SortPhase.Won);}},
                ()=>{if(router.ActiveView==ownedView) {if(!pause) {if(ownedView.Secondary.gameObject.activeSelf)TryReturnHome();}else RestartLevel();}},()=>{router.NotifyClosed(ownedView);if(router.ActiveView==null) Layers.SetModal(false);});
            if(pause) settings.QuitButton.onClick.AddListener(()=>{if(router.ActiveView==ownedView && ownedView && ownedView.IsOpen) RequestLeaveConfirmation();});
            router.Register(view);popups.Add(key,view);return view;
        }

        /// <summary>Freezes lifecycle state independently of optional modal resource availability.</summary>
        private void SuspendLevel()
        {
            if(!ready || entryStartFailed) return;
            if(entranceActive)FinishSceneEntrance();
            if(cardBoard!=null)EndCardEffect(true);
            CancelDrag();
            if(model.Board.Phase!=SortPhase.Playing) return;
            this.SendCommand(new PauseSortCommand(true));suspendedWithoutPopup=true;
        }

        /// <summary>Returning to the app resumes only its silent lifecycle suspension.</summary>
        private void ResumeSuspendedLevel()
        {
            if(!ready || !suspendedWithoutPopup || lifecycleAppPaused || lifecycleFocusLost)return;
            suspendedWithoutPopup=false;
            if(model.Board.Phase==SortPhase.Paused)ResumeLevel();
        }

        /// <summary>Uses the existing authored HUD button as an accessible fallback action.</summary>
        private void SetPauseCaption(string caption) {if(pauseCaption) pauseCaption.text=caption;}
    }
}
