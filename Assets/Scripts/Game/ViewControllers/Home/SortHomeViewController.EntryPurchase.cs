using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        public string EntryPurchasePopupKey="Prefabs/UI/Popups/BoosterPurchasePopup";
        private SortPopupView entryPurchase;
        private Image entryPurchaseIcon;
        private Text entryPurchaseNotice;
        private int entryPurchaseIndex=-1;
        private static readonly string[] EntryPurchaseTitles={"Vacuum Box","Time Alarm"};
        private static readonly string[] EntryPurchaseBodies={"Vacuums up three identical items and clears them in one go","Adds 30 seconds to the timer"};
        public SortPopupView ActiveEntryPurchasePopup {get{return entryPurchase && entryPurchase.IsOpen?entryPurchase:null;}}
        public bool TryOpenEntryPurchase(int index)
        {
            if(index<0 || index>=2 || !CanUseEntryPopup() || resources.GetEntryCharges(index)!=0)return false;
            if(!PrepareEntryPurchase())return false;
            entryPurchaseIndex=index;entryPurchaseIcon.sprite=entryIcons[index].sprite;
            entryPurchase.Present(EntryPurchaseTitles[index],EntryPurchaseBodies[index],"1600","Close");
            var title=entryPurchase.Title;title.fontSize=64;title.resizeTextForBestFit=false;float available=title.rectTransform.rect.width-40;
            if(title.preferredWidth>available)title.fontSize=Mathf.Max(32,Mathf.FloorToInt(64*available/title.preferredWidth));
            if(!entryRouter.Show(entryPurchase.PopupId,UiPopupOpenMode.Replace))return false;
            Layers.SetModal(true);RefreshEntryPurchase();return true;
        }
        private bool PrepareEntryPurchase()
        {
            if(entryPurchase)return true;
            var prefab=string.IsNullOrEmpty(EntryPurchasePopupKey)?null:Resources.Load<GameObject>(EntryPurchasePopupKey);
            if(!prefab){entryPopup.Body.text="Purchase unavailable. Please retry.";Debug.LogError("Entry purchase popup resource missing: "+EntryPurchasePopupKey,this);return false;}
            var go=Instantiate(prefab,Layers.Hosts[2].transform,false);go.SetActive(false);var view=go.GetComponent<SortPopupView>();
            var icon=go.transform.Find("PopupContent/Inset/BoosterIcon");var notice=go.transform.Find("PopupContent/Notice");
            if(!view || view.PopupId!="BoosterPurchasePopup" || !view.Title || !view.Body || !view.Primary || !view.Secondary || !view.PrimaryCaption || !view.SecondaryCaption || !icon || !icon.GetComponent<Image>() || !notice || !notice.GetComponent<Text>() || !view.PurchaseCoinBalance || !view.PurchaseCoinPlus){
                Destroy(go);entryPopup.Body.text="Purchase unavailable. Please retry.";Debug.LogError("Invalid entry purchase popup: "+EntryPurchasePopupKey,this);return false;
            }
            view.Id="EntryBoosterPurchasePopup";entryPurchase=view;entryPurchaseIcon=icon.GetComponent<Image>();entryPurchaseNotice=notice.GetComponent<Text>();
            view.Configure(BuyEntryPurchase,ReturnFromEntryPurchase,()=>{entryRouter.NotifyClosed(view);if(entryRouter.ActiveView==null)Layers.SetModal(false);});
            view.ConfigurePurchaseWallet(CanUseEntryPurchase,()=>{this.SendCommand(new AddSortCoinsCommand());RefreshEntryPurchase();});
            entryRouter.Register(view);return true;
        }
        private bool CanUseEntryPurchase(){return ready && isActiveAndEnabled && !loading && !transitioning && SelectedTab==SortHomeTab.Home && entryPurchase && entryPurchase.IsOpen && entryRouter.ActiveView==entryPurchase && entryPurchaseIndex>=0 && entryPurchaseIndex<2;}
        private void BuyEntryPurchase()
        {
            if(!CanUseEntryPurchase())return;
            var command=new BuySortEntryBoosterCommand(entryPurchaseIndex);this.SendCommand(command);
            if(command.Accepted)ReturnFromEntryPurchase();
            else{RefreshEntryPurchase();if(!entryPurchaseNotice.gameObject.activeSelf){entryPurchaseNotice.text="Unable to purchase. Please try again.";entryPurchaseNotice.gameObject.SetActive(true);}}
        }
        private void ReturnFromEntryPurchase()
        {
            if(!CanUseEntryPurchase() || !entryPopup)return;
            entryRouter.Show(entryPopup.PopupId,UiPopupOpenMode.Replace);entryPurchaseIndex=-1;Layers.SetModal(true);RefreshEntryPopup();
        }
        private void RefreshEntryPurchase()
        {
            if(!entryPurchase || entryRouter.ActiveView!=entryPurchase || entryPurchaseIndex<0 || entryPurchaseIndex>=2)return;
            entryPurchase.RenderPurchaseCoins(resources.Coins);
            bool affordable=resources.Coins>=SortWalletLedger.BoosterPrice && resources.GetEntryCharges(entryPurchaseIndex)==0 && string.IsNullOrEmpty(resources.Error);
            entryPurchase.Primary.interactable=affordable;entryPurchase.Secondary.interactable=true;
            string message=!string.IsNullOrEmpty(resources.Error)?resources.Error:resources.Coins<SortWalletLedger.BoosterPrice?"Not enough coins":"";
            entryPurchaseNotice.text=message;entryPurchaseNotice.gameObject.SetActive(message.Length>0);
        }
        private void DisposeEntryPurchase()
        {
            if(!object.ReferenceEquals(entryPurchase,null) && entryRouter!=null){if(entryPurchase && object.ReferenceEquals(entryRouter.ActiveView,entryPurchase))entryRouter.Clear(true);entryRouter.Unregister(entryPurchase);if(entryPurchase)Destroy(entryPurchase.gameObject);}
            entryPurchase=null;entryPurchaseIndex=-1;
        }
    }
}
