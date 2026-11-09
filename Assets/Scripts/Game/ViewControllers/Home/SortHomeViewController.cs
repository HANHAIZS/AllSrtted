using System;
using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Owns the scene-local menu views; state writes remain project Commands.</summary>
    public sealed partial class SortHomeViewController : AllSortedController
    {
        public Camera HomeCamera;
        public SortUiLayers Layers;
        public CanvasGroup[] Pages;
        public RectTransform[] PageArt;
        public Button[] TabButtons;
        public RectTransform[] TabIcons;
        public Text[] TabLabels;
        public RectTransform NavigationArea,SelectionPlate,LeftRail,RightRail;
        public RectTransform[] Dividers;
        public Button StartButton;
        public Text LevelLabel,ErrorLabel;
        public Button[] SettingButtons;
        public GameObject[] OffMarks;
        public string GameSceneName="GameScene";
        public string LevelTransitionKey="Prefabs/UI/Transitions/LevelTransition";
        public float TransitionSeconds=.25f,PageFadeSeconds=.12f;
        private SortHomeNavigationModel navigation;
        private SortBoardModel board;
        private SortSettingsModel preferences;
        private IUnRegister selectionObserver,settingsObserver,levelObserver;
        private readonly UnityAction[] tabActions=new UnityAction[3],settingActions=new UnityAction[3];
        private UnityAction startAction;
        private bool ready,loading;
        public bool IsTransitioning {get {return transitioning;}}
        public bool IsLoading {get {return loading;}}
        public SortHomeTab SelectedTab {get {return navigation==null?SortHomeTab.Home:navigation.SelectedTab.Value;}}

        private void Awake()
        {
            navigation=this.GetModel<SortHomeNavigationModel>();board=this.GetModel<SortBoardModel>();preferences=this.GetModel<SortSettingsModel>();InitializeEntryPopup();
            if(!HomeCamera || !Layers || !NavigationArea || !SelectionPlate || !LeftRail || !RightRail || Dividers==null || Dividers.Length!=2 || !Dividers[0] || !Dividers[1] || !StartButton || !LevelLabel ||
                Pages==null || Pages.Length!=3 || PageArt==null || PageArt.Length!=3 || TabButtons==null || TabButtons.Length!=3 ||
                TabIcons==null || TabIcons.Length!=3 || TabLabels==null || TabLabels.Length!=3 || SettingButtons==null || SettingButtons.Length!=3 || OffMarks==null || OffMarks.Length!=3) {
                Fail("Home scene bindings are incomplete.");enabled=false;return;
            }
            for(int i=0;i<3;i++) if(!TabButtons[i] || !TabIcons[i] || !TabLabels[i] || !HasPage(i)) {Fail("Home page bindings are incomplete: "+i);enabled=false;return;}
            Layers.Initialize();this.SendCommand(new ClearSortHomeEntryCommand());this.SendCommand(new SelectSortHomeTabCommand(SortHomeTab.Home));
            for(int i=0;i<3;i++) {
                int index=i;tabActions[i]=()=>SelectPage((SortHomeTab)index);settingActions[i]=()=>ToggleSetting(index);
                TabButtons[i].onClick.AddListener(tabActions[i]);SettingButtons[i].onClick.AddListener(settingActions[i]);
            }
            startAction=()=>OpenEntryPopup();StartButton.onClick.AddListener(startAction);ready=true;
            selectionObserver=navigation.Revision.Register(value=>BeginTransition());
            settingsObserver=preferences.Revision.Register(value=>RefreshSettings());levelObserver=board.LevelNumber.Register(value=>RefreshLevel());
            RefreshSettings();RefreshLevel();RefreshLayout();SnapToSelection();InitializeResources();InitializeCoinShop();InitializeBundles();
        }

        private void OnEnable() {if(ready) {RefreshLayout();SnapToSelection();RefreshSettings();RefreshLevel();SnapResourceHud();LayoutCoinShop();}}
        private void Update()
        {
            if(!ready) return;
            if(!Mathf.Approximately(lastWidth,NavigationArea.rect.width) || !Mathf.Approximately(lastHeight,((RectTransform)Layers.Hosts[0].transform).rect.height)) RefreshLayout();
            if(transitioning) AdvanceTransition(Time.unscaledDeltaTime);
            UpdateResources();
        }

        /// <summary>Checks presentation availability before publishing a new page target.</summary>
        public bool SelectPage(SortHomeTab tab)
        {
            int index=(int)tab;
            if(!ready || loading || Layers.GameplayBlocked || !isActiveAndEnabled || index<0 || index>2 || !TabButtons[index].IsInteractable()) return false;
            if(!HasPage(index)) {Fail("Menu page is unavailable: "+tab);return false;}
            var command=new SelectSortHomeTabCommand(tab);this.SendCommand(command);if(command.Accepted) ClearError();return command.Accepted;
        }
        private bool HasPage(int i)
        {
            if(!Pages[i] || !PageArt[i]) return false;
            if(i==2) for(int n=0;n<3;n++) if(!SettingButtons[n] || !OffMarks[n]) return false;
            return true;
        }
        private bool CanUsePage(int i) {return ready && isActiveAndEnabled && !loading && !Layers.GameplayBlocked && !transitioning && (int)navigation.SelectedTab.Value==i && Pages[i] && Pages[i].interactable;}
        private void ToggleSetting(int index)
        {
            if(!CanUsePage(2) || !SettingButtons[index].IsInteractable()) return;
            this.SendCommand(new ToggleSortSettingCommand((SortSetting)index));
        }
        private void RefreshSettings() {for(int i=0;i<3;i++) if(OffMarks[i]) OffMarks[i].SetActive(!preferences.GetEnabled((SortSetting)i));}
        private void RefreshLevel() {LevelLabel.text="Level "+board.LevelNumber.Value;}

        /// <summary>Validates a real scene before creating the exclusive consumable level intent.</summary>
        public bool TryStartGame(){return BeginGame(0,false);}
        private bool BeginGame(int mask,bool fromPopup)
        {
            if(!(fromPopup?CanUseEntryPopup():CanUsePage(1)) || (!fromPopup && !StartButton.IsInteractable())) return false;
            if(string.IsNullOrEmpty(GameSceneName) || !Application.CanStreamedLevelBeLoaded(GameSceneName)) {this.SendCommand(new ClearSortHomeEntryCommand());Fail("Game scene is unavailable: "+GameSceneName);return false;}
            var bridge=SortLevelTransitionView.Prepare(LevelTransitionKey,HomeCamera);if(!bridge){ResourceNotice("Level transition unavailable. Please retry.");return false;}
            var request=new RequestSortHomeEntryCommand(board.LevelNumber.Value,mask);this.SendCommand(request);if(!request.Accepted) {bridge.Finish();ResourceNotice(resources!=null && !string.IsNullOrEmpty(resources.Error)?resources.Error:"NO ENERGY. TAP + OR WAIT TO REFILL.");return false;}
            loading=true;SetPageInput(-1);SetNavigationInput(false);
            if(bridge.Begin(GameSceneName))return true;
            this.SendCommand(new ClearSortHomeEntryCommand());loading=false;SetNavigationInput(true);SetPageInput(1);ResourceNotice("Unable to open game scene. Please retry.");return false;
        }
        private void SetNavigationInput(bool value) {for(int i=0;i<3;i++) TabButtons[i].interactable=value;}
        private void SetPageInput(int page) {for(int i=0;i<3;i++) if(Pages[i]) {Pages[i].interactable=i==page;Pages[i].blocksRaycasts=i==page;}}
        private void Fail(string message) {Debug.LogError(message,this);if(ErrorLabel) {ErrorLabel.text=message;ErrorLabel.gameObject.SetActive(true);}}
        private void ClearError() {if(ErrorLabel) ErrorLabel.gameObject.SetActive(false);}

        private void OnDisable() {transitioning=false;DisableResources();if(ready) SetPageInput(-1);}
        private void OnDestroy()
        {
            DisposeEntryPopup();DisposeBundles();DisposeCoinShop();DisposeResources();
            if(selectionObserver!=null) selectionObserver.UnRegister();if(settingsObserver!=null) settingsObserver.UnRegister();if(levelObserver!=null) levelObserver.UnRegister();
            if(!ready) return;
            for(int i=0;i<3;i++) {if(TabButtons[i]) TabButtons[i].onClick.RemoveListener(tabActions[i]);if(SettingButtons[i]) SettingButtons[i].onClick.RemoveListener(settingActions[i]);}
            if(StartButton) StartButton.onClick.RemoveListener(startAction);ready=false;
        }
    }
}
