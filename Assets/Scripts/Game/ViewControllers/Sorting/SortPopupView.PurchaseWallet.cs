using System;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortPopupView
    {
        public Text PurchaseCoinBalance;
        public Button PurchaseCoinPlus;
        private UnityAction purchaseCoinAction;
        /// <summary>Owner supplies its cached top-modal gate and existing wallet command.</summary>
        public void ConfigurePurchaseWallet(Func<bool> canUse,Action addCoins)
        {
            if(!PurchaseCoinPlus)return;
            if(purchaseCoinAction!=null)PurchaseCoinPlus.onClick.RemoveListener(purchaseCoinAction);
            purchaseCoinAction=()=>{if(IsOpen && isActiveAndEnabled && PurchaseCoinPlus.gameObject.activeInHierarchy && PurchaseCoinPlus.IsInteractable() && canUse!=null && canUse() && addCoins!=null)addCoins();};
            PurchaseCoinPlus.onClick.AddListener(purchaseCoinAction);
        }
        public void RenderPurchaseCoins(int coins){if(PurchaseCoinBalance)PurchaseCoinBalance.text=coins.ToString();}
        private void OnDestroy(){if(PurchaseCoinPlus && purchaseCoinAction!=null)PurchaseCoinPlus.onClick.RemoveListener(purchaseCoinAction);purchaseCoinAction=null;}
    }
}
