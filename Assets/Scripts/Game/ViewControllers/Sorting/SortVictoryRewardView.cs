using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace AllSorted
{
    /// <summary>Owns only reward presentation; the scene owner supplies gated commands and progression.</summary>
    public sealed class SortVictoryRewardView : AllSortedController
    {
        public SortPopupView Popup;
        public RectTransform WalletBar,CoinTarget,CoinPile;
        public Text Balance,Reward;
        public Image[] FlyingCoins;
        private Func<bool> claim,canContinue;
        private Func<int> balance;
        private Action advance;
        private Coroutine routine;
        private bool busy,collected;
        private int token;
        public int AttemptId {get{return token;}}
        public bool IsCollecting {get{return busy;}}
        public void Configure(Func<bool> allowed,Func<bool> grant,Func<int> readBalance,Action next)
        {canContinue=allowed;claim=grant;balance=readBalance;advance=next;}
        public void Present(int attempt,int coins)
        {
            if(token!=attempt){token=attempt;collected=false;}
            Balance.text=coins.ToString();Reward.text="30";Popup.Primary.interactable=true;Popup.Secondary.interactable=true;
        }
        public void Collect()
        {
            if(busy || !Popup.IsOpen || canContinue==null || !canContinue())return;
            if(collected){advance();return;}
            busy=true;Popup.Primary.interactable=false;Popup.Secondary.interactable=false;routine=StartCoroutine(Fly());
        }
        private IEnumerator Fly()
        {
            float elapsed=0;const float flight=.58f,stagger=.032f;float total=flight+Mathf.Max(0,FlyingCoins.Length-1)*stagger;
            while(elapsed<total){
                if(!Popup.IsOpen || !canContinue()){ResetVisuals();yield break;}
                elapsed+=Time.unscaledDeltaTime;
                for(int i=0;i<FlyingCoins.Length;i++){
                    var image=FlyingCoins[i];float t=(elapsed-i*stagger)/flight;
                    image.gameObject.SetActive(t>=0 && t<1);if(t<0 || t>=1)continue;
                    var parent=(RectTransform)image.transform.parent;
                    Vector3 from=parent.InverseTransformPoint(CoinPile.position)+new Vector3((i%3-1)*35,(i%4-1)*25,0);
                    Vector3 to=parent.InverseTransformPoint(CoinTarget.position);
                    Vector3 bend=(from+to)*.5f+new Vector3(90+i*6,100,0);float u=t*t*(3-2*t);
                    image.rectTransform.localPosition=(1-u)*(1-u)*from+2*(1-u)*u*bend+u*u*to;
                    image.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,.48f,u);
                }
                yield return null;
            }
            foreach(var image in FlyingCoins)image.gameObject.SetActive(false);
            if(!Popup.IsOpen || !canContinue()){ResetVisuals();yield break;}
            if(!claim()){
                Popup.Body.text="COULD NOT SAVE. PLEASE TRY AGAIN.";Popup.Body.gameObject.SetActive(true);ResetVisuals();yield break;
            }
            collected=true;Balance.text=balance().ToString();
            for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){
                if(!Popup.IsOpen || !canContinue()){ResetVisuals();yield break;}
                WalletBar.localScale=Vector3.one*(1+.09f*Mathf.Sin(t/.2f*Mathf.PI));yield return null;
            }
            ResetVisuals();if(Popup.IsOpen && canContinue())advance();
        }
        private void OnDisable(){if(routine!=null)StopCoroutine(routine);ResetVisuals();}
        private void ResetVisuals()
        {
            routine=null;busy=false;if(WalletBar)WalletBar.localScale=Vector3.one;
            if(FlyingCoins!=null)foreach(var image in FlyingCoins)if(image)image.gameObject.SetActive(false);
            if(Popup){Popup.Primary.interactable=true;Popup.Secondary.interactable=true;}
        }
    }
}
