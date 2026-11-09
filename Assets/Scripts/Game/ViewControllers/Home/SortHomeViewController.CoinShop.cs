using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        public RectTransform CoinShopRoot;
        public Button[] CoinPackButtons;
        public Text CoinShopNotice;
        private readonly UnityAction[] coinPackActions=new UnityAction[6];
        private readonly Vector3[] coinShopCorners=new Vector3[4];
        private RectTransform coinShopHeader;
        private bool coinShopReady;
        private void InitializeCoinShop()
        {
            if(!CoinShopRoot)return;
            if(CoinPackButtons==null || CoinPackButtons.Length!=6 || !CoinShopNotice){CoinShopRoot.gameObject.SetActive(false);Debug.LogError("Shop coin pack bindings are incomplete.",this);return;}
            foreach(var button in CoinPackButtons)if(!button){CoinShopRoot.gameObject.SetActive(false);Debug.LogError("Shop coin pack button is missing.",this);return;}
            coinShopHeader=Pages[0].transform.Find("ShopHeader/Title") as RectTransform;
            for(int i=0;i<6;i++){int index=i;coinPackActions[i]=()=>ClaimCoinPack(index);CoinPackButtons[i].onClick.AddListener(coinPackActions[i]);}
            coinShopReady=true;LayoutCoinShop();
        }
        private void ClaimCoinPack(int index)
        {
            if(!coinShopReady || !CanUsePage(0) || !CoinShopRoot.gameObject.activeInHierarchy || !CoinPackButtons[index].isActiveAndEnabled || !CoinPackButtons[index].IsInteractable())return;
            var command=new ClaimSortCoinPackCommand(index);this.SendCommand(command);
            CoinShopNotice.text=command.Accepted?"Added "+SortCoinPackCatalog.GetDisplayAmount(index)+" coins · No charge":!string.IsNullOrEmpty(resources.Error)?resources.Error:"Coin limit reached";
        }
        private void LayoutCoinShop()
        {
            if(!coinShopReady)return;
            var page=(RectTransform)Pages[0].transform;float top=240,bottom=NavigationArea.rect.height+20;
            if(coinShopHeader){coinShopHeader.GetWorldCorners(coinShopCorners);float low=float.MaxValue;foreach(var corner in coinShopCorners)low=Mathf.Min(low,page.InverseTransformPoint(corner).y);top=Mathf.Max(top,page.rect.height*.5f-low+20);}
            NavigationArea.GetWorldCorners(coinShopCorners);float high=float.MinValue;foreach(var corner in coinShopCorners)high=Mathf.Max(high,page.InverseTransformPoint(corner).y);bottom=Mathf.Max(bottom,high+page.rect.height*.5f+20);
            if(LayoutBundleScroll(top,bottom))return;
            float scale=Mathf.Max(.1f,Mathf.Min((page.rect.width-40)/666,(page.rect.height-top-bottom)/670));
            CoinShopRoot.localScale=Vector3.one*scale;CoinShopRoot.anchoredPosition=new Vector2(0,(bottom-top)*.5f);
        }
        private void DisposeCoinShop()
        {
            if(CoinPackButtons!=null)for(int i=0;i<Mathf.Min(6,CoinPackButtons.Length);i++)if(CoinPackButtons[i] && coinPackActions[i]!=null)CoinPackButtons[i].onClick.RemoveListener(coinPackActions[i]);
            coinShopReady=false;
        }
    }
}
