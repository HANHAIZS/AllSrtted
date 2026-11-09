using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        public string EntryPopupKey="Prefabs/UI/Popups/PrelevelPopup";
        private UiPopupRouter entryRouter;
        private SortPopupView entryPopup;
        private readonly Button[] entryItems=new Button[2],entryPlus=new Button[2];
        private readonly Text[] entryCounts=new Text[2];
        private readonly Image[] entryIcons=new Image[2];
        private readonly GameObject[] entrySelections=new GameObject[2],entryChecks=new GameObject[2];
        private int entrySelection;
        private Sprite entryAddBadge,entryRemainingBadge;
        public SortPopupView ActiveEntryPopup {get{return entryPopup && entryPopup.IsOpen?entryPopup:null;}}
        private void InitializeEntryPopup(){entryRouter=this.GetUtility<UiPopupRouter>();}
        public bool OpenEntryPopup()
        {
            if(!CanUsePage(1) || !StartButton.IsInteractable())return false;
            this.SendCommand(refreshWallet);if(resources==null || !resources.CanEnter){ResourceNotice("NO ENERGY. TAP + OR WAIT TO REFILL.");return false;}
            if(!PrepareEntryPopup())return false;
            entrySelection=0;entryPopup.Title.text="LEVEL "+board.LevelNumber.Value.ToString("00");entryPopup.Body.text="Select boosters";
            entryRouter.Show(entryPopup.PopupId,UiPopupOpenMode.Replace);Layers.SetModal(true);RefreshEntryPopup();return true;
        }
        private bool PrepareEntryPopup()
        {
            if(entryPopup)return true;
            var prefab=string.IsNullOrEmpty(EntryPopupKey)?null:Resources.Load<GameObject>(EntryPopupKey);
            if(!prefab){ResourceNotice("Level entry popup unavailable. Please retry.");Debug.LogError("Level entry popup resource missing: "+EntryPopupKey,this);return false;}
            var go=Instantiate(prefab,Layers.Hosts[2].transform,false);go.SetActive(false);var view=go.GetComponent<SortPopupView>();
            bool valid=view && view.PopupId=="PrelevelPopup" && view.Title && view.Body && view.Primary && view.Secondary && view.PrimaryCaption && view.SecondaryCaption;
            for(int i=0;i<2 && valid;i++){
                var item=go.transform.Find("PopupContent/Item"+i);var plus=item?item.Find("Plus"):null;var count=item?item.Find("Count"):null;var mark=item?item.Find("Selected"):null;var remaining=item?item.Find("RemainingBadge"):null;var check=item?item.Find("CheckBadge"):null;var icon=item?item.Find("Icon"):null;
                valid=icon && icon.GetComponent<Image>() && icon.GetComponent<Image>().sprite && item && item.GetComponent<Button>() && plus && plus.GetComponent<Button>() && count && count.GetComponent<Text>() && mark && mark.GetComponent<Image>() && mark.GetComponent<Image>().sprite && check && check.GetComponent<Image>() && check.GetComponent<Image>().sprite && remaining && remaining.GetComponent<Image>() && remaining.GetComponent<Image>().sprite;
                if(valid){var add=plus.GetComponent<Button>();valid=add.image && add.image.sprite;}
                if(valid){entryIcons[i]=icon.GetComponent<Image>();entryItems[i]=item.GetComponent<Button>();entryPlus[i]=plus.GetComponent<Button>();entryCounts[i]=count.GetComponent<Text>();entrySelections[i]=mark.gameObject;entryChecks[i]=check.gameObject;}
            }
            if(!valid){Destroy(go);ResourceNotice("Level entry popup unavailable. Please retry.");Debug.LogError("Invalid level entry popup: "+EntryPopupKey,this);return false;}
            entryPopup=view;
            entryAddBadge=entryPlus[0].image.sprite;entryRemainingBadge=entryItems[0].transform.Find("RemainingBadge").GetComponent<Image>().sprite;
            for(int i=0;i<2;i++)entryItems[i].transform.Find("RemainingBadge").gameObject.SetActive(false);
            view.Configure(()=>{if(CanUseEntryPopup())BeginGame(entrySelection,true);},CloseEntryPopup,()=>{entryRouter.NotifyClosed(view);if(entryRouter.ActiveView==null)Layers.SetModal(false);});
            for(int i=0;i<2;i++){
                int index=i;
                entryItems[i].onClick.AddListener(()=>{if(CanUseEntryPopup() && resources.GetEntryCharges(index)>0){entrySelection^=1<<index;RefreshEntryPopup();}});
                entryPlus[i].onClick.AddListener(()=>TryOpenEntryPurchase(index));
            }
            entryRouter.Register(view);return true;
        }
        private bool CanUseEntryPopup(){return ready && isActiveAndEnabled && !loading && !transitioning && SelectedTab==SortHomeTab.Home && entryPopup && entryPopup.IsOpen && entryRouter.ActiveView==entryPopup;}
        public void CloseEntryPopup(){if(!CanUseEntryPopup())return;entrySelection=0;entryRouter.Clear(true);Layers.SetModal(false);}
        private void RefreshEntryPopup()
        {
            if(!entryPopup || resources==null)return;
            for(int i=0;i<2;i++){
                int count=resources.GetEntryCharges(i);if(count<=0)entrySelection&=~(1<<i);
                bool selected=(entrySelection&(1<<i))!=0;
                entryCounts[i].text=count.ToString();entryCounts[i].gameObject.SetActive(count>0 && !selected);entryPlus[i].gameObject.SetActive(!selected);entryPlus[i].image.sprite=count>0?entryRemainingBadge:entryAddBadge;entrySelections[i].SetActive(selected);entryChecks[i].SetActive(selected);entryItems[i].interactable=count>0;
                entryPlus[i].interactable=count==0;
            }
            entryPopup.Primary.interactable=resources.CanEnter;RefreshEntryPurchase();
        }
        private void DisposeEntryPopup()
        {
            DisposeEntryPurchase();
            if(!object.ReferenceEquals(entryPopup,null) && entryRouter!=null){if(entryPopup && object.ReferenceEquals(entryRouter.ActiveView,entryPopup))entryRouter.Clear(true);entryRouter.Unregister(entryPopup);if(entryPopup)Destroy(entryPopup.gameObject);}
            entryPopup=null;if(Layers)Layers.SetModal(false);
        }
    }
}
