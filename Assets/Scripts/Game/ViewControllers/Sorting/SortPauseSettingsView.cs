using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Renders session preferences inside the existing lazy pause popup.</summary>
    [RequireComponent(typeof(SortPopupView))]
    public sealed class SortPauseSettingsView : AllSortedController
    {
        public Button[] ToggleButtons;
        public GameObject[] DisabledMarks;
        public Button QuitButton;
        private readonly UnityAction[] actions=new UnityAction[3];
        private SortSettingsModel model;
        private UiPopupRouter router;
        private SortPopupView popup;
        private IUnRegister observation;
        private bool bound;

        /// <summary>Can be checked before the inactive prefab is opened or the board paused.</summary>
        public bool HasValidBindings {
            get {
                if(ToggleButtons==null || ToggleButtons.Length!=3 || DisabledMarks==null || DisabledMarks.Length!=3 || !QuitButton) return false;
                for(int i=0;i<3;i++) if(!ToggleButtons[i] || !DisabledMarks[i]) return false;
                return true;
            }
        }

        /// <summary>Caches dependencies and owns one listener/observer for this cached popup's lifetime.</summary>
        private void Awake()
        {
            model=this.GetModel<SortSettingsModel>();router=this.GetUtility<UiPopupRouter>();popup=GetComponent<SortPopupView>();
            if(!HasValidBindings) {Debug.LogError("Pause settings bindings are incomplete.",this);enabled=false;return;}
            for(int i=0;i<3;i++) {int index=i;actions[i]=()=>Toggle(index);ToggleButtons[i].onClick.AddListener(actions[i]);}
            bound=true;observation=model.Revision.Register(value=>Refresh());Refresh();
        }

        /// <summary>Refreshes reused instances without rebinding callbacks or resetting preferences.</summary>
        private void OnEnable() {if(bound) Refresh();}

        /// <summary>Rejects hidden/non-top callbacks, then delegates one mutation to the project Command.</summary>
        private void Toggle(int index)
        {
            if(!bound || !popup || !popup.IsOpen || !gameObject.activeInHierarchy || router.ActiveView!=popup || !ToggleButtons[index].IsInteractable()) return;
            this.SendCommand(new ToggleSortSettingCommand((SortSetting)index));
        }

        /// <summary>Shows the rounded dim plate and red OFF slash without tinting the surrounding row.</summary>
        private void Refresh()
        {
            for(int i=0;i<3;i++) {
                if(!ToggleButtons[i] || !DisabledMarks[i]) continue;
                bool active=model.GetEnabled((SortSetting)i);ToggleButtons[i].image.color=Color.white;
                DisabledMarks[i].SetActive(!active);
            }
        }

        /// <summary>Unregisters only this popup's owned listeners and observer during teardown.</summary>
        private void OnDestroy()
        {
            if(observation!=null) observation.UnRegister();if(!bound) return;
            for(int i=0;i<3;i++) if(ToggleButtons[i]) ToggleButtons[i].onClick.RemoveListener(actions[i]);bound=false;
        }
    }
}
