using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortGameViewController
    {
        public string BoosterPurchasePopupKey="Prefabs/UI/Popups/BoosterPurchasePopup";
        private SortPopupView purchasePopup;
        private Image purchaseIcon;
        private Text purchaseNotice;
        private SortBooster purchaseBooster;
        private int purchaseAttempt,purchaseWalletRevision=-1;
        private static readonly string[] PurchaseTitles={"Vacuum","Shuffle Dice","Wildcard","Freeze Watch"};
        private static readonly string[] PurchaseDescriptions={"Vacuums up a set of three identical items","Shuffles all remaining items into a new layout","Turns three groups of three items into identical wildcards","Stops the timer for 15 seconds while you keep sorting"};

        /// <summary>Prepares the shared offer before pausing, without granting stock or using its effect.</summary>
        public bool TryOpenBoosterPurchase(SortBooster booster)
        {
            int index=(int)booster;
            if(!ready || entryStartFailed || !isActiveAndEnabled || !Layers || Layers.GameplayBlocked || index<0 || index>=4 || model.Board.Phase!=SortPhase.Playing || model.Board.RemainingSeconds<=0 || boosterStock.GetCharges(booster)!=0)return false;
            if(!PreparePurchasePopup())return false;
            purchaseBooster=booster;purchaseAttempt=wallet.AttemptId;purchaseWalletRevision=-1;purchaseIcon.sprite=boosterIcons[index].sprite;
            purchasePopup.Present(PurchaseTitles[index],PurchaseDescriptions[index],"1600","Close");
            var title=purchasePopup.Title;title.resizeTextForBestFit=false;title.fontSize=64;
            float available=((RectTransform)title.transform).rect.width-16;
            if(title.preferredWidth>available)title.fontSize=Mathf.Max(32,Mathf.FloorToInt(64*available/title.preferredWidth));
            CancelDrag();this.SendCommand(new PauseSortCommand(true));
            if(!router.Show(purchasePopup.PopupId,UiPopupOpenMode.Replace)){this.SendCommand(new PauseSortCommand(false));return false;}
            Layers.SetModal(true);RefreshPurchaseOffer();return true;
        }
        private bool PreparePurchasePopup()
        {
            if(purchasePopup)return true;
            if(string.IsNullOrEmpty(BoosterPurchasePopupKey)){Debug.LogError("Booster purchase popup resource missing: empty key",this);return false;}
            var prefab=Resources.Load<GameObject>(BoosterPurchasePopupKey);
            if(!prefab){Debug.LogError("Booster purchase popup resource missing: "+BoosterPurchasePopupKey,this);return false;}
            var go=Instantiate(prefab,Layers.Hosts[2].transform,false);go.SetActive(false);var view=go.GetComponent<SortPopupView>();
            var icon=go.transform.Find("PopupContent/Inset/BoosterIcon");var notice=go.transform.Find("PopupContent/Notice");
            if(!view || view.PopupId!="BoosterPurchasePopup" || !view.Title || !view.Body || !view.Primary || !view.Secondary || !view.PrimaryCaption || !view.SecondaryCaption || !icon || !icon.GetComponent<Image>() || !notice || !notice.GetComponent<Text>() || !view.PurchaseCoinBalance || !view.PurchaseCoinPlus){
                Destroy(go);Debug.LogError("Invalid booster purchase popup: "+BoosterPurchasePopupKey,this);return false;
            }
            purchasePopup=view;purchaseIcon=icon.GetComponent<Image>();purchaseNotice=notice.GetComponent<Text>();
            view.Configure(PurchaseSelectedBooster,ClosePurchaseOffer,()=>{router.NotifyClosed(view);if(router.ActiveView==null)Layers.SetModal(false);});
            view.ConfigurePurchaseWallet(CanUsePurchase,()=>{this.SendCommand(new AddSortCoinsCommand());purchaseWalletRevision=-1;RefreshPurchaseOffer();});
            router.Register(view);popups.Add(BoosterPurchasePopupKey,view);return true;
        }
        private bool CanUsePurchase()
        {
            return ready && isActiveAndEnabled && !returningHome && purchasePopup && purchasePopup.IsOpen && router.ActiveView==purchasePopup && model.Board.Phase==SortPhase.Paused && wallet.AttemptId==purchaseAttempt;
        }
        private void PurchaseSelectedBooster()
        {
            if(!CanUsePurchase())return;
            var command=new BuySortBoosterCommand(purchaseBooster,purchaseAttempt);this.SendCommand(command);
            if(command.Accepted)ClosePurchaseOffer();
            else {purchaseWalletRevision=-1;RefreshPurchaseOffer();if(!purchaseNotice.gameObject.activeSelf){purchaseNotice.text="Unable to purchase. Please try again.";purchaseNotice.gameObject.SetActive(true);}}
        }
        private void ClosePurchaseOffer()
        {
            if(!CanUsePurchase())return;
            router.Clear(true);Layers.SetModal(false);purchaseAttempt=0;this.SendCommand(new PauseSortCommand(false));seenPhase=SortPhase.Playing;RefreshBoosterUi(true);
        }
        private void RefreshPurchaseOffer()
        {
            if(!purchasePopup || router.ActiveView!=purchasePopup)return;
            if(wallet.AttemptId!=purchaseAttempt || model.Board.Phase!=SortPhase.Paused){router.Clear(true);Layers.SetModal(false);purchaseAttempt=0;return;}
            if(purchaseWalletRevision==wallet.Revision.Value)return;purchaseWalletRevision=wallet.Revision.Value;
            purchasePopup.RenderPurchaseCoins(wallet.Coins);
            bool affordable=wallet.Coins>=1600 && string.IsNullOrEmpty(wallet.Error);
            purchasePopup.Primary.interactable=affordable;purchasePopup.Secondary.interactable=true;
            string message=!string.IsNullOrEmpty(wallet.Error)?wallet.Error:wallet.Coins<1600?"Not enough coins":"";
            purchaseNotice.text=message;purchaseNotice.gameObject.SetActive(message.Length>0);
        }
    }
}
