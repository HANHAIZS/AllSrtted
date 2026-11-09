using GameFoundation.UI;
using UnityEngine;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public string LeavingPopupKey="Prefabs/UI/Popups/LeavingPopup";
        private SortPopupView pauseBeforeLeaving;
        private int leavingAttempt;
        /// <summary>Prepares the confirmation before replacing pause; no wallet mutation occurs here.</summary>
        public bool RequestLeaveConfirmation()
        {
            var pause=ActivePopup;
            if(!ready || returningHome || !isActiveAndEnabled || model.Board.Phase!=SortPhase.Paused || !pause || !pause.IsOpen || !pause.GetComponent<SortPauseSettingsView>())return false;
            SortPopupView view;
            if(!popups.TryGetValue(LeavingPopupKey,out view)) {
                var prefab=Resources.Load<GameObject>(LeavingPopupKey);
                if(!prefab){Debug.LogError("Leaving popup resource missing: "+LeavingPopupKey,this);return false;}
                var go=Instantiate(prefab,Layers.Hosts[2].transform,false);view=go.GetComponent<SortPopupView>();
                if(!view || !view.Title || !view.Body || !view.Primary || !view.Secondary || !view.PrimaryCaption || !view.SecondaryCaption || view.PopupId==pause.PopupId){Destroy(go);Debug.LogError("Invalid leaving popup: "+LeavingPopupKey,this);return false;}
                go.SetActive(false);var owned=view;
                view.Configure(()=>{if(CanUseLeaving(owned))TryReturnHome();},()=>{
                    if(!CanUseLeaving(owned) || !pauseBeforeLeaving)return;
                    router.Show(pauseBeforeLeaving.PopupId,UiPopupOpenMode.Replace);Layers.SetModal(true);
                },()=>{router.NotifyClosed(owned);if(router.ActiveView==null)Layers.SetModal(false);});
                router.Register(view);popups.Add(LeavingPopupKey,view);
            }
            pauseBeforeLeaving=pause;leavingAttempt=wallet.AttemptId;
            view.Present("Leaving","You will lose 1 life","Leave","Close");
            CancelDrag();router.Show(view.PopupId,UiPopupOpenMode.Replace);Layers.SetModal(true);return true;
        }
        private bool CanUseLeaving(SortPopupView view)
        {
            return ready && !returningHome && isActiveAndEnabled && view && view.IsOpen && router.ActiveView==view && model.Board.Phase==SortPhase.Paused && wallet.AttemptId==leavingAttempt;
        }
    }
}
