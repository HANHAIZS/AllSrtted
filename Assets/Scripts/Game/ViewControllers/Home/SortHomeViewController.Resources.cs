using GameFoundation.UI;
using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace AllSorted
{
    public sealed partial class SortHomeViewController
    {
        public CanvasGroup ResourceHud;
        public RectTransform ResourceSafeArea,EnergyBar,CoinBar;
        public Button EnergyPlus,CoinPlus;
        public Text EnergyValue,EnergyRecoveryLabel,CoinValue;
        private SortWalletModel resources;
        private IUnRegister resourceObserver;
        private UnityAction addCoinsAction,addEnergyAction;
        private readonly RefreshSortWalletCommand refreshWallet=new RefreshSortWalletCommand();
        private readonly RefreshSortWalletCommand flushWallet=new RefreshSortWalletCommand{Flush=true};
        private float nextWalletRefresh,lastResourceWidth=-1;
        private bool resourcesReady;
        private int authoredRecoveryFontSize;
        private bool authoredRecoveryBestFit;

        private void InitializeResources()
        {
            resources=this.GetModel<SortWalletModel>();
            if(!ResourceHud) return; // Older editor-authored scenes remain navigable.
            if(!ResourceSafeArea || !EnergyBar || !CoinBar || !EnergyPlus || !CoinPlus || !EnergyValue || !EnergyRecoveryLabel || !CoinValue) {
                ResourceHud.gameObject.SetActive(false);Debug.LogError("Home resource HUD bindings are incomplete.",this);return;
            }
            resourcesReady=true;authoredRecoveryFontSize=EnergyRecoveryLabel.fontSize;authoredRecoveryBestFit=EnergyRecoveryLabel.resizeTextForBestFit;
            addCoinsAction=()=>{if(CanUseResources()){this.SendCommand(new AddSortCoinsCommand());ShowResourceError();}};
            addEnergyAction=()=>{if(CanUseResources() && !resources.HasUnlimitedLives && resources.Energy<5 && EnergyPlus.gameObject.activeInHierarchy){this.SendCommand(new AddSortEnergyCommand());ShowResourceError();}};
            CoinPlus.onClick.AddListener(addCoinsAction);EnergyPlus.onClick.AddListener(addEnergyAction);
            resourceObserver=resources.Revision.Register(value=>RefreshResourceText());
            this.SendCommand(refreshWallet);RefreshResourceText();ShowResourceError();SnapResourceHud();
            var entryError=new ConsumeSortEntryErrorCommand();this.SendCommand(entryError);if(!string.IsNullOrEmpty(entryError.Error))ResourceNotice(entryError.Error);
        }
        private bool CanUseResources()
        {
            return resourcesReady && !Layers.GameplayBlocked && ready && isActiveAndEnabled && !loading && !transitioning && SelectedTab!=SortHomeTab.Settings && ResourceHud.interactable;
        }
        private void UpdateResources()
        {
            if(!resourcesReady) return;
            nextWalletRefresh-=Time.unscaledDeltaTime;
            if(nextWalletRefresh<=0){nextWalletRefresh=1;this.SendCommand(refreshWallet);RefreshResourceText();ShowResourceError();}
            bool visible=SelectedTab!=SortHomeTab.Settings && !loading;
            ResourceHud.alpha=Mathf.MoveTowards(ResourceHud.alpha,visible?1:0,Time.unscaledDeltaTime/Mathf.Max(.01f,PageFadeSeconds));
            ResourceHud.interactable=ResourceHud.blocksRaycasts=visible && !transitioning && isActiveAndEnabled;
            LayoutResources();
        }
        private void SnapResourceHud()
        {
            if(!resourcesReady)return;
            bool visible=SelectedTab!=SortHomeTab.Settings && !loading;
            ResourceHud.alpha=visible?1:0;ResourceHud.interactable=ResourceHud.blocksRaycasts=visible && !transitioning && isActiveAndEnabled;
            LayoutResources();
        }
        private void LayoutResources()
        {
            float width=Mathf.Max(100,Mathf.Min(180,ResourceSafeArea.rect.width*.25f-24));
            if(Mathf.Approximately(lastResourceWidth,width))return;lastResourceWidth=width;
            EnergyBar.anchoredPosition=new Vector2(-width*.625f-14,-18);CoinBar.anchoredPosition=new Vector2(width*.625f+14,-18);
            EnergyBar.localScale=CoinBar.localScale=Vector3.one*1.25f;
            FitResourceBar(CoinBar,width);EnergyBar.sizeDelta=CoinBar.sizeDelta;
        }
        private static void FitResourceBar(RectTransform bar,float width)
        {
            var sprite=bar.GetComponent<Image>().sprite;if(!sprite)return;
            var size=new Vector2(width,width*sprite.rect.height/sprite.rect.width);
            if(bar.sizeDelta!=size)bar.sizeDelta=size;
        }
        private void RefreshResourceText()
        {
            if(!resourcesReady)return;
            CoinValue.text=resources.Coins.ToString();EnergyValue.text=resources.HasUnlimitedLives?"∞":resources.Energy.ToString();
            bool canAddEnergy=!resources.HasUnlimitedLives && resources.Energy<5;if(EnergyPlus.gameObject.activeSelf!=canAddEnergy)EnergyPlus.gameObject.SetActive(canAddEnergy);
            int remaining=resources.RecoverySeconds;
            RefreshEntryPopup();
            long unlimited=resources.UnlimitedSeconds;
            EnergyRecoveryLabel.text=resources.HasUnlimitedLives?string.Format("{0:00}:{1:00}:{2:00}",unlimited/3600,unlimited/60%60,unlimited%60):resources.Energy>=5?"Full":string.Format("{0:00}:{1:00}",remaining/60,remaining%60);
            EnergyRecoveryLabel.resizeTextForBestFit=resources.HasUnlimitedLives?false:authoredRecoveryBestFit;
            EnergyRecoveryLabel.fontSize=authoredRecoveryFontSize;
            if(resources.HasUnlimitedLives){
                float available=Mathf.Max(1,EnergyRecoveryLabel.rectTransform.rect.width-3);
                float preferred=EnergyRecoveryLabel.preferredWidth;
                if(preferred>available)EnergyRecoveryLabel.fontSize=Mathf.Max(1,Mathf.FloorToInt(authoredRecoveryFontSize*available/preferred));
            }
        }
        private void ShowResourceError()
        {
            if(!resourcesReady || string.IsNullOrEmpty(resources.Error)) return;
            ResourceNotice(resources.Error);
        }
        private void ResourceNotice(string message)
        {
            if(ErrorLabel){ErrorLabel.text=message;ErrorLabel.gameObject.SetActive(true);}
        }
        private void DisableResources(){if(ResourceHud)ResourceHud.interactable=ResourceHud.blocksRaycasts=false;}
        private void DisposeResources()
        {
            if(resourceObserver!=null)resourceObserver.UnRegister();
            if(CoinPlus && addCoinsAction!=null)CoinPlus.onClick.RemoveListener(addCoinsAction);
            if(EnergyPlus && addEnergyAction!=null)EnergyPlus.onClick.RemoveListener(addEnergyAction);
            resourcesReady=false;
        }
        private void OnApplicationPause(bool paused)
        {
            if(!ready || resources==null)return;this.SendCommand(paused?flushWallet:refreshWallet);RefreshResourceText();
        }
        private void OnApplicationFocus(bool focused)
        {
            if(!ready || resources==null)return;this.SendCommand(focused?refreshWallet:flushWallet);RefreshResourceText();
        }
        private void OnApplicationQuit(){if(ready && resources!=null)this.SendCommand(flushWallet);}
    }
}
