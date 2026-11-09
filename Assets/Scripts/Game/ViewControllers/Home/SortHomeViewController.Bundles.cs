using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using QFramework;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        public ScrollRect ShopScroll;
        public RectTransform BundleRoot;
        public RectTransform NoAdsOffer;
        public Button[] BundleButtons;
        public Text BundleNotice;
        private readonly UnityAction[] bundleActions=new UnityAction[3];
        private bool bundlesReady,bundleLayoutFailed;
        private void InitializeBundles()
        {
            if(bundleLayoutFailed || !ShopScroll || !BundleRoot)return;
            if(BundleButtons==null || BundleButtons.Length!=3 || !BundleNotice){BundleRoot.gameObject.SetActive(false);Debug.LogError("Shop bundle bindings are incomplete.",this);return;}
            foreach(var button in BundleButtons)if(!button){BundleRoot.gameObject.SetActive(false);Debug.LogError("Shop bundle purchase button is missing.",this);return;}
            for(int i=0;i<3;i++){int index=i;bundleActions[i]=()=>ClaimBundle(index);BundleButtons[i].onClick.AddListener(bundleActions[i]);}
            bundlesReady=true;LayoutCoinShop();
        }
        private void ClaimBundle(int index)
        {
            if(!bundlesReady || !CanUsePage(0) || !BundleRoot.gameObject.activeInHierarchy || !BundleButtons[index].isActiveAndEnabled || !BundleButtons[index].IsInteractable())return;
            var command=new ClaimSortBundleCommand(index);this.SendCommand(command);
            BundleNotice.text=command.Accepted?string.Empty:!string.IsNullOrEmpty(resources.Error)?resources.Error:"Bundle cannot be added: inventory limit reached";
            BundleNotice.gameObject.SetActive(!command.Accepted);
            RefreshResourceText();
        }
        private bool LayoutBundleScroll(float top,float bottom)
        {
            if(bundleLayoutFailed)return false;
            if(!ShopScroll && CoinShopRoot.parent==Pages[0].transform)return false; // Legacy fixed coin shop.
            var scrollHost=ShopScroll?ShopScroll:CoinShopRoot.GetComponentInParent<ScrollRect>();
            bool valid=ShopScroll && ShopScroll.content && BundleRoot && BundleNotice && BundleButtons!=null && BundleButtons.Length==3;
            if(valid)foreach(var button in BundleButtons)if(!button)valid=false;
            if(!valid){
                bundleLayoutFailed=true;bundlesReady=false;
                if(BundleRoot)BundleRoot.gameObject.SetActive(false);
                CoinShopRoot.SetParent(Pages[0].transform,false);
                CoinShopRoot.anchorMin=CoinShopRoot.anchorMax=new Vector2(.5f,.5f);
                if(scrollHost)scrollHost.gameObject.SetActive(false);
                Debug.LogError("Shop bundle scroll bindings are incomplete; coin packs restored to fixed layout.",this);return false;
            }
            var page=(RectTransform)Pages[0].transform;
            var viewport=(RectTransform)ShopScroll.transform;
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(20,bottom);viewport.offsetMax=new Vector2(-20,-top);
            float scale=Mathf.Max(.1f,(page.rect.width-40)/666f);
            var content=ShopScroll.content;
            content.localScale=Vector3.one*scale;
            // Fixed logical width makes the authored cards/coin grid scale together on every device.
            float offerHeight=NoAdsOffer?260:0;
            content.sizeDelta=new Vector2(666,1620+offerHeight);
            BundleRoot.anchoredPosition=new Vector2(0,-450-offerHeight);
            CoinShopRoot.localScale=Vector3.one;CoinShopRoot.anchoredPosition=new Vector2(0,-1275-offerHeight);
            return true;
        }
        private void DisposeBundles()
        {
            if(BundleButtons!=null)for(int i=0;i<Mathf.Min(3,BundleButtons.Length);i++)if(BundleButtons[i] && bundleActions[i]!=null)BundleButtons[i].onClick.RemoveListener(bundleActions[i]);
            bundlesReady=false;
        }
    }
}
